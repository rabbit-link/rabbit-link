using System;

namespace RabbitLink.Consumer
{
    /// <summary>
    /// Delegate that provides consumer tag text based on consumer unique Id.
    /// </summary>
    /// <param name="consumerId">Consumer unique Id.</param>
    /// <returns>String representing consumer tag for RabbitMQ.</returns>
    public delegate string ConsumerTagProviderDelegate(Guid consumerId);
}
