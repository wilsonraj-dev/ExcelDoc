using System.Threading.Channels;
using ExcelDoc.Server.Background.Interfaces;
using ExcelDoc.Server.Options;
using Microsoft.Extensions.Options;

namespace ExcelDoc.Server.Background
{
    public class BackgroundTaskQueue : IBackgroundTaskQueue
    {
        private readonly Channel<ProcessamentoQueueItem> _queue;

        public BackgroundTaskQueue(IOptions<ProcessingOptions> options)
        {
            var queueOptions = new BoundedChannelOptions(options.Value.QueueCapacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            };

            _queue = Channel.CreateBounded<ProcessamentoQueueItem>(queueOptions);
        }

        public ValueTask EnqueueAsync(ProcessamentoQueueItem item, CancellationToken cancellationToken = default)
        {
            return _queue.Writer.WriteAsync(item, cancellationToken);
        }

        public ValueTask<ProcessamentoQueueItem> DequeueAsync(CancellationToken cancellationToken)
        {
            return _queue.Reader.ReadAsync(cancellationToken);
        }
    }
}
