using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RabbitLink.Internals.Async;
using RabbitLink.Messaging;
using RabbitLink.Messaging.Internals;

namespace RabbitLink.Consumer
{
    internal class LinkPullConsumerQueue :IDisposable
    {
        private readonly AsyncLock _sync;
        private readonly SemaphoreSlim _readSem = new SemaphoreSlim(0);

        public LinkPullConsumerQueue()
        {
            
        }

        public async Task PutAsync(ILinkConsumedMessage message)
        {
            var msg = new LinkPulledMessage(message);

            using (await _sync.LockAsync(msg.Cancellation).ConfigureAwait(false))
            {
                
            }
        }

        public async Task TakeAsync(CancellationToken cancellation)
        {
            await _readSem.WaitAsync(cancellation)
                .ConfigureAwait(false);
            
            using (await _sync.LockAsync(cancellation).ConfigureAwait(false))
            {
                
            }
        }

        public void Dispose()
        {

        }
    }
}
