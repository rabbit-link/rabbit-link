using System.Threading;
using System.Threading.Tasks;
using RabbitLink.Messaging;

namespace RabbitLink.Interceptors;

/// <summary>
/// Delegate for handling message publish.
/// </summary>
public delegate Task HandlePublishDelegate(ILinkPublishMessage<byte[]> msg, CancellationToken cancellation);
