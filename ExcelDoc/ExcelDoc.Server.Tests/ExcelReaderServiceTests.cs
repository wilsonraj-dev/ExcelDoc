using System.IO.Compression;
using System.Text;
using ClosedXML.Excel;
using ExcelDoc.Server.Localization;
using ExcelDoc.Server.Services;

namespace ExcelDoc.Server.Tests;

public sealed class ExcelReaderServiceTests
{
    [Fact]
    public async Task ReadFirstRowAsync_ReturnsFirstRowAndPreservesIntermediateEmptyColumns()
    {
        using var stream = new MemoryStream();
        using (var workbook = new XLWorkbook())
        {
            var worksheet = workbook.AddWorksheet("Importação");
            worksheet.Cell(1, 1).Value = "DocDate";
            worksheet.Cell(1, 3).Value = "CardCode";
            worksheet.Cell(2, 1).Value = "esta linha não deve ser lida";
            workbook.SaveAs(stream);
        }

        stream.Position = 0;
        var service = new ExcelReaderService(new StubMessageService());

        var result = await service.ReadFirstRowAsync(stream);

        Assert.Equal(["DocDate", string.Empty, "CardCode"], result);
    }

    [Fact]
    public async Task ReadFirstRowAsync_ThrowsFormatExceptionForInvalidWorkbook()
    {
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("arquivo inválido"));
        var service = new ExcelReaderService(new StubMessageService());

        await Assert.ThrowsAsync<FormatException>(() => service.ReadFirstRowAsync(stream));
    }

    [Fact]
    public async Task ReadFirstRowAsync_AcceptsNonSeekableInputByUsingBoundedCopy()
    {
        await using var workbookStream = CreateWorkbookStream();
        await using var stream = new NonSeekableReadStream(workbookStream);
        var service = new ExcelReaderService(new StubMessageService());

        var result = await service.ReadFirstRowAsync(stream);

        Assert.Equal(["DocDate", "CardCode"], result);
    }

    [Fact]
    public async Task ReadFirstRowAsync_RejectsArchiveWithTooManyEntries()
    {
        await using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            for (var index = 0; index < 2_049; index++)
            {
                archive.CreateEntry($"entry-{index}.xml");
            }
        }

        stream.Position = 0;
        var service = new ExcelReaderService(new StubMessageService());

        var exception = await Assert.ThrowsAsync<FormatException>(
            () => service.ReadFirstRowAsync(stream));

        Assert.Equal(MessageKeys.InvalidExcelFile, exception.Message);
    }

    [Fact]
    public async Task ReadFirstRowAsync_RejectsArchiveWhoseExpandedContentExceedsLimit()
    {
        await using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry("xl/oversized-content.bin", CompressionLevel.SmallestSize);
            await using var entryStream = entry.Open();
            var block = new byte[64 * 1024];
            var remaining = 64_000_001L;

            while (remaining > 0)
            {
                var bytesToWrite = (int)Math.Min(block.Length, remaining);
                await entryStream.WriteAsync(block.AsMemory(0, bytesToWrite));
                remaining -= bytesToWrite;
            }
        }

        stream.Position = 0;
        var service = new ExcelReaderService(new StubMessageService());

        var exception = await Assert.ThrowsAsync<FormatException>(
            () => service.ReadFirstRowAsync(stream));

        Assert.Equal(MessageKeys.InvalidExcelFile, exception.Message);
    }

    [Fact]
    public async Task ReadFirstRowAsync_LimitsConcurrentPreviews()
    {
        await using var workbookStream = CreateWorkbookStream();
        var workbookBytes = workbookStream.ToArray();
        var tracker = new ConcurrentReadTracker();
        var service = new ExcelReaderService(new StubMessageService());

        var tasks = Enumerable.Range(0, 3)
            .Select(async _ =>
            {
                await using var stream = new TrackingReadStream(workbookBytes, tracker);
                return await service.ReadFirstRowAsync(stream);
            });

        await Task.WhenAll(tasks);

        Assert.Equal(2, tracker.MaxActiveReads);
    }

    [Fact]
    public async Task ReadFirstRowAsync_PreservesCancellation()
    {
        await using var stream = CreateWorkbookStream();
        using var cancellationSource = new CancellationTokenSource();
        await cancellationSource.CancelAsync();
        var service = new ExcelReaderService(new StubMessageService());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.ReadFirstRowAsync(stream, cancellationSource.Token));
    }

    private static MemoryStream CreateWorkbookStream()
    {
        var stream = new MemoryStream();
        using (var workbook = new XLWorkbook())
        {
            var worksheet = workbook.AddWorksheet("Importacao");
            worksheet.Cell(1, 1).Value = "DocDate";
            worksheet.Cell(1, 2).Value = "CardCode";
            workbook.SaveAs(stream);
        }

        stream.Position = 0;
        return stream;
    }

    private sealed class NonSeekableReadStream(Stream inner) : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count) =>
            inner.Read(buffer, offset, count);

        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default) =>
            inner.ReadAsync(buffer, cancellationToken);

        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                inner.Dispose();
            }

            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            await inner.DisposeAsync();
            GC.SuppressFinalize(this);
        }
    }

    private sealed class TrackingReadStream(
        byte[] content,
        ConcurrentReadTracker tracker) : Stream
    {
        private readonly MemoryStream _inner = new(content, writable: false);

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count) =>
            _inner.Read(buffer, offset, count);

        public override async ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            tracker.Enter();
            try
            {
                await Task.Delay(50, cancellationToken);
                return await _inner.ReadAsync(buffer, cancellationToken);
            }
            finally
            {
                tracker.Exit();
            }
        }

        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
            }

            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            await _inner.DisposeAsync();
            GC.SuppressFinalize(this);
        }
    }

    private sealed class ConcurrentReadTracker
    {
        private int _activeReads;
        private int _maxActiveReads;

        public int MaxActiveReads => Volatile.Read(ref _maxActiveReads);

        public void Enter()
        {
            var activeReads = Interlocked.Increment(ref _activeReads);
            while (true)
            {
                var currentMaximum = Volatile.Read(ref _maxActiveReads);
                if (activeReads <= currentMaximum ||
                    Interlocked.CompareExchange(
                        ref _maxActiveReads,
                        activeReads,
                        currentMaximum) == currentMaximum)
                {
                    return;
                }
            }
        }

        public void Exit() => Interlocked.Decrement(ref _activeReads);
    }
}
