using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitLink.Consumer;
using RabbitLink.Messaging;

namespace RabbitLink.Interceptors;

/// <summary>
/// Interceptor for messages consuming.
/// </summary>
public interface IDeliveryInterceptor
{
    /// <summary>
    /// Executes interceptor logic for consuming of message to rabbit mq.
    /// </summary>
    /// <param name="msg">Message to be consumed.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <param name="executeCore">
    /// Delegate for next call in consume pipeline. MUST be called in normal flow of interception,
    /// can be missed due to invalid state of message or exceptions in interceptors.
    /// </param>
    /// <returns>Promise of consume completion with answer for rabbit mq.</returns>
    Task<LinkConsumerAckStrategy> Intercept(ILinkConsumedMessage<ReadOnlyMemory<byte>> msg, CancellationToken ct, HandleDeliveryDelegate executeCore);
}
