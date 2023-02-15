namespace RabbitLink.Interceptors;

/// <summary>
/// Combination of consumer/published interceptors.
/// </summary>
public interface IMessageInterceptor : IDeliveryInterceptor, IPublishInterceptor
{
}
