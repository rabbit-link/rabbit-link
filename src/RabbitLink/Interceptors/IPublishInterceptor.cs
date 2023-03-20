using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitLink.Messaging;

namespace RabbitLink.Interceptors;

/// <summary>
/// Interceptor for messages publishing.
/// </summary>
public interface IPublishInterceptor
{
    /// <summary>
    /// Executes interceptor logic for publishing of message to rabbit mq.
    /// </summary>
    /// <param name="msg">Message to be published.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <param name="executeCore">
    /// Delegate for next call in publish pipeline. MUST be called in normal flow of interception,
    /// can be missed due to invalid state of message or exceptions in interceptors.
    /// </param>
    /// <returns>Promise of publish completion.</returns>
    Task Intercept(ILinkPublishMessage<ReadOnlyMemory<byte>> msg, CancellationToken ct, HandlePublishDelegate executeCore);
}
