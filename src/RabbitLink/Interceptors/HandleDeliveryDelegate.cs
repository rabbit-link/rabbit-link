using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitLink.Consumer;
using RabbitLink.Messaging;

namespace RabbitLink.Interceptors;

/// <summary>
/// Delegate for handling message delivery.
/// </summary>
public delegate Task<LinkConsumerAckStrategy> HandleDeliveryDelegate(ILinkConsumedMessage<ReadOnlyMemory<byte>> msg, CancellationToken ct);
