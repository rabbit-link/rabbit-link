#region Usings

using System;

#endregion

namespace RabbitLink.Internals.Async
{
    internal class OnceDisposable : IDisposable
    {
        private readonly Action _onDisposeAction;
        private readonly object _sync = new object();
        private bool _disposed;

        public OnceDisposable(Action onDispose)
        {
            _onDisposeAction = onDispose;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            lock (_sync)
            {
                if (_disposed)
                    return;

                _onDisposeAction?.Invoke();
                _disposed = true;
            }
        }
    }
}
