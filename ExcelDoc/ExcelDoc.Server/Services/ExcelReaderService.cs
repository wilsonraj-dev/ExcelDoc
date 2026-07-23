using System.Buffers;
using System.IO.Compression;
using ClosedXML.Excel;
using ExcelDoc.Server.Background;
using ExcelDoc.Server.Localization;
using ExcelDoc.Server.Services.Interfaces;

namespace ExcelDoc.Server.Services
{
    public class ExcelReaderService : IExcelReaderService
    {
        private const int MaxPreviewInputBytes = 10_000_000;
        private const int MaxPreviewArchiveEntries = 2_048;
        private const long MaxPreviewUncompressedBytes = 64_000_000;
        private const int PreviewBufferSize = 64 * 1024;
        private const int MaxConcurrentPreviews = 2;

        private static readonly SemaphoreSlim PreviewGate = new(
            MaxConcurrentPreviews,
            MaxConcurrentPreviews);

        private readonly IMessageService _messageService;

        public ExcelReaderService(IMessageService messageService)
        {
            _messageService = messageService;
        }

        public async Task<IReadOnlyList<string>> ReadFirstRowAsync(
            Stream stream,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(stream);

            await PreviewGate.WaitAsync(cancellationToken);
            try
            {
                try
                {
                    await using var safeStream = await CopyToBoundedSeekableStreamAsync(
                        stream,
                        cancellationToken);
                    await ValidateArchiveAsync(safeStream, cancellationToken);

                    safeStream.Position = 0;
                    return await Task.Run<IReadOnlyList<string>>(
                        () => ReadFirstRow(safeStream, cancellationToken),
                        cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    throw new FormatException(
                        _messageService.Get(MessageKeys.InvalidExcelFile),
                        exception);
                }
            }
            finally
            {
                PreviewGate.Release();
            }
        }

        private static async Task<MemoryStream> CopyToBoundedSeekableStreamAsync(
            Stream source,
            CancellationToken cancellationToken)
        {
            if (!source.CanRead)
            {
                throw new InvalidDataException("The Excel preview stream is not readable.");
            }

            if (source.CanSeek && source.Length - source.Position > MaxPreviewInputBytes)
            {
                throw new InvalidDataException("The Excel preview exceeds the compressed size limit.");
            }

            var destination = new MemoryStream();
            var buffer = ArrayPool<byte>.Shared.Rent(PreviewBufferSize);

            try
            {
                var totalBytes = 0;
                while (true)
                {
                    var bytesRead = await source.ReadAsync(
                        buffer.AsMemory(0, PreviewBufferSize),
                        cancellationToken);
                    if (bytesRead == 0)
                    {
                        break;
                    }

                    totalBytes = checked(totalBytes + bytesRead);
                    if (totalBytes > MaxPreviewInputBytes)
                    {
                        throw new InvalidDataException("The Excel preview exceeds the compressed size limit.");
                    }

                    await destination.WriteAsync(
                        buffer.AsMemory(0, bytesRead),
                        cancellationToken);
                }

                destination.Position = 0;
                return destination;
            }
            catch
            {
                await destination.DisposeAsync();
                throw;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private static async Task ValidateArchiveAsync(
            MemoryStream stream,
            CancellationToken cancellationToken)
        {
            stream.Position = 0;
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);

            if (archive.Entries.Count == 0 || archive.Entries.Count > MaxPreviewArchiveEntries)
            {
                throw new InvalidDataException("The Excel archive contains an invalid number of entries.");
            }

            long declaredUncompressedBytes = 0;
            foreach (var entry in archive.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                declaredUncompressedBytes = checked(declaredUncompressedBytes + entry.Length);
                if (declaredUncompressedBytes > MaxPreviewUncompressedBytes)
                {
                    throw new InvalidDataException("The Excel archive exceeds the uncompressed size limit.");
                }
            }

            long actualUncompressedBytes = 0;
            var buffer = ArrayPool<byte>.Shared.Rent(PreviewBufferSize);
            try
            {
                foreach (var entry in archive.Entries)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await using var entryStream = entry.Open();

                    while (true)
                    {
                        var bytesRead = await entryStream.ReadAsync(
                            buffer.AsMemory(0, PreviewBufferSize),
                            cancellationToken);
                        if (bytesRead == 0)
                        {
                            break;
                        }

                        actualUncompressedBytes = checked(actualUncompressedBytes + bytesRead);
                        if (actualUncompressedBytes > MaxPreviewUncompressedBytes)
                        {
                            throw new InvalidDataException("The Excel archive exceeds the uncompressed size limit.");
                        }
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }

            stream.Position = 0;
        }

        private static IReadOnlyList<string> ReadFirstRow(
            Stream stream,
            CancellationToken cancellationToken)
        {
            using var workbook = new XLWorkbook(stream);
            cancellationToken.ThrowIfCancellationRequested();

            var worksheet = workbook.Worksheets.FirstOrDefault();
            var firstRow = worksheet?.FirstRowUsed();
            var lastCell = firstRow?.LastCellUsed();

            if (firstRow is null || lastCell is null)
            {
                return Array.Empty<string>();
            }

            var columns = new List<string>(lastCell.Address.ColumnNumber);
            for (var columnNumber = 1; columnNumber <= lastCell.Address.ColumnNumber; columnNumber++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                columns.Add(firstRow.Cell(columnNumber).GetFormattedString());
            }

            return columns;
        }

        public Task<IReadOnlyCollection<ExcelRowData>> ReadRowsAsync(string filePath, CancellationToken cancellationToken = default)
        {
            return Task.Run<IReadOnlyCollection<ExcelRowData>>(() =>
            {
                using var workbook = new XLWorkbook(filePath);
                var worksheet = workbook.Worksheets.First();
                var rows = new List<ExcelRowData>();

                foreach (var row in worksheet.RowsUsed())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (row.IsEmpty())
                    {
                        continue;
                    }

                    var values = row.CellsUsed()
                        .ToDictionary(cell => cell.Address.ColumnNumber, cell => cell.GetFormattedString());

                    rows.Add(new ExcelRowData
                    {
                        RowNumber = row.RowNumber(),
                        Values = values
                    });
                }

                return rows;
            }, cancellationToken);
        }
    }
}
