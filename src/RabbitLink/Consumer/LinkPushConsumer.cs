#region Usings

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx.Synchronous;
using RabbitLink.Configuration;
using RabbitLink.Exceptions;
using RabbitLink.Logging;
using RabbitLink.Messaging;

#endregion

namespace RabbitLink.Consumer
{
    internal class LinkPushConsumer : ILinkPushConsumerInternal
    {
        private readonly LinkConsumerConfiguration _configuration;
        private readonly ILinkPullConsumerInternal _consumer;
        private readonly CancellationTokenSource _disposedCancellation = new CancellationTokenSource();
        private readonly LinkConsumerHandlerFinder _handlerFinder;
        private readonly ILinkLogger _logger;
        private readonly Task _loopTask;
        private readonly Action<Exception, ILinkRecievedMessage<byte[]>> _serializationErrorHandler;
        private readonly object _sync = new object();

        public LinkPushConsumer(ILinkPullConsumerInternal consumer, LinkConsumerConfiguration configuration,
            LinkConfiguration linkConfiguration,
            LinkConsumerHandlerFinder handlerFinder,
            Action<Exception, ILinkRecievedMessage<byte[]>> serializationErrorHandler
            )
        {
            if (consumer == null)
                throw new ArgumentNullException(nameof(consumer));

            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            if (linkConfiguration == null)
                throw new ArgumentNullException(nameof(linkConfiguration));

            if (handlerFinder == null)
                throw new ArgumentNullException(nameof(handlerFinder));

            if (serializationErrorHandler == null)
                throw new ArgumentNullException(nameof(serializationErrorHandler));


            _configuration = configuration;
            _handlerFinder = handlerFinder;
            _serializationErrorHandler = serializationErrorHandler;

            _consumer = consumer;
            _consumer.Disposed += ConsumerOnDisposed;

            _logger = linkConfiguration.LoggerFactory.CreateLogger($"{GetType().Name}({Id:D})");

            if (_logger == null)
                throw new ArgumentException("Cannot create logger", nameof(linkConfiguration.LoggerFactory));

            _loopTask = Task.Run(async () => await Loop().ConfigureAwait(false));

            _logger.Debug("Created");
        }

        #region IDisposable implementation

        public void Dispose()
        {
            if (_disposedCancellation.IsCancellationRequested)
                return;

            lock (_sync)
            {
                if (_disposedCancellation.IsCancellationRequested)
                    return;

                _logger.Debug("Disposing");

                _disposedCancellation.Cancel();
                _loopTask.WaitWithoutException();

                _consumer.Disposed -= ConsumerOnDisposed;
                _consumer.Dispose();

                _logger.Debug("Disposed");
                _logger.Dispose();
            }

            Disposed?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Events

        public event EventHandler Disposed;

        #endregion

        private Task ExecuteHandler(ILinkAckableRecievedMessage<byte[]> msg, LinkConsumerHandler handler)
        {
            return Task.Run(async () =>
            {
                var strategy = LinkConsumerAckStrategy.Ack;
                try
                {
                    await handler.Handle(msg, _configuration.MessageSerializer)
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.Warning("Handler throws exception: {0}", ex);

                    var deserializationException = ex as LinkDeserializationException;
                    if (deserializationException != null)
                    {
                        try
                        {
                            _serializationErrorHandler(
                                deserializationException.InnerException,
                                deserializationException.RawMessage
                                );
                        }
                        catch (Exception serializationHandlerException)
                        {
                            _logger.Error("SerializationError handler error: {0}", serializationHandlerException);
                        }
                    }

                    try
                    {
                        strategy = (ex is TaskCanceledException)
                            ? _configuration.ErrorStrategy.OnHandlerCancelled(msg)
                            : _configuration.ErrorStrategy.OnHandlerError(msg, ex);
                    }
                    catch (Exception errorStrategyException)
                    {
                        _logger.Error("Nacking message due ErrorStrategy exception: {0}", errorStrategyException);
                        strategy = LinkConsumerAckStrategy.Nack;
                    }

                    _logger.Debug("Selected error strategy: {0}", strategy);
                }

                switch (strategy)
                {
                    case LinkConsumerAckStrategy.Ack:
                        msg.Ack();
                        break;
                    case LinkConsumerAckStrategy.Nack:
                        msg.Nack();
                        break;
                    case LinkConsumerAckStrategy.NackWithRequeue:
                        msg.Nack(true);
                        break;
                    default:
                        msg.Nack();
                        break;
                }
            });
        }

        private async Task Loop()
        {
            var tasks = new ConcurrentDictionary<Task, object>();

            while (!_disposedCancellation.IsCancellationRequested)
            {
                ILinkAckableRecievedMessage<byte[]> msg;
                LinkConsumerHandler handler = null;

                try
                {
                    msg = await _consumer.GetMessageAsync<byte[]>(_disposedCancellation.Token)
                        .ConfigureAwait(false);
                }
                catch (ObjectDisposedException)
                {
                    _logger.Debug("Underlying consumer disposed, disposing...");
#pragma warning disable 4014                    
                    Task.Run(() => Dispose());
#pragma warning restore 4014
                    break;
                }
                catch (TaskCanceledException)
                {
                    continue;
                }
                catch (Exception ex)
                {
                    _logger.Error("Cannot consume message: {0}", ex);
                    continue;
                }

                try
                {
                    handler = _handlerFinder.Find(msg, _configuration.TypeNameMapping);
                }
                catch (Exception ex)
                {
                    _logger.Error("Search handler error: {0}", ex);
                }

                if (handler == null)
                {
                    _logger.Error("Cannot get handler for message, nacking");
                    msg.Nack();
                    continue;
                }

                if (handler.Parallel)
                {
                    var task = ExecuteHandler(msg, handler);
                    tasks.TryAdd(task, null);

#pragma warning disable 4014
                    task.ContinueWith(t =>
#pragma warning restore 4014
                    {
                        object value;
                        tasks.TryRemove(task, out value);
                    });
                }
                else
                {
                    _logger.Debug("Waiting for previous tasks");
                    await Task.WhenAll(tasks.Select(x => x.Key))
                        .ConfigureAwait(false);

                    _logger.Debug("All previous tasks completed");


                    await ExecuteHandler(msg, handler)
                        .ConfigureAwait(false);
                }
            }

            tasks.Clear();
        }

        #region Consumer handlers

        private void ConsumerOnDisposed(object sender, EventArgs eventArgs)
        {
            Dispose();
        }

        #endregion

        #region Propertis

        public Guid Id => _consumer.Id;
        public ushort PrefetchCount => _consumer.PrefetchCount;
        public bool AutoAck => _consumer.AutoAck;
        public int Priority => _consumer.Priority;
        public bool CancelOnHaFailover => _consumer.CancelOnHaFailover;
        public bool Exclusive => _consumer.Exclusive;

        #endregion
    }
}