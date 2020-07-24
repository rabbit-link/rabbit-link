#region Usings

using System;

#endregion

namespace RabbitLink.Consumer
{
    /// <summary>
    ///     Error strategy for <see cref="ILinkConsumer" /> message handling
    /// </summary>
    public interface ILinkConsumerErrorStrategy
    {
        /// <summary>
        ///     Handle message handler error
        /// </summary>
        /// <param name="ex">handle exception</param>
        /// <returns>Strategy to deal with message</returns>
        LinkConsumerAckStrategy HandleError(Exception ex);

        /// <summary>
        /// Handle message handler task cancellation
        /// </summary>
        /// <returns>Strategy to deal with message</returns>
        LinkConsumerAckStrategy HandleCancellation();
    }
}
