#region Usings

using System;

#endregion

namespace RabbitLink.Logging
{
    /// <summary>
    ///     Implementation of <see cref="ILinkLoggerFactory" /> which uses <see cref="Action" /> to get
    ///     <see cref="ILinkLogger" /> instance
    /// </summary>
    public class ActionLinkLoggerFactory : ILinkLoggerFactory
    {
        private readonly Func<string, ILinkLogger> _getLoggerFunc;

        public ActionLinkLoggerFactory(Func<string, ILinkLogger> getLoggerFunc)
        {
            if (getLoggerFunc == null)
                throw new ArgumentNullException(nameof(getLoggerFunc));

            _getLoggerFunc = getLoggerFunc;
        }

        public ILinkLogger CreateLogger(string name)
        {
            return _getLoggerFunc(name);
        }
    }
}