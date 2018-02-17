using System.Threading.Tasks;
using RabbitLink.Messaging;

namespace RabbitLink.Rpc
{
    /// <summary>
    /// Rpc server request handler
    /// </summary>
    /// <param name="message">request message</param>
    /// <typeparam name="TRequest">request type</typeparam>
    /// <typeparam name="TResponse">response type</typeparam>
    public delegate Task<ILinkPublishMessage<TResponse>> LinkServeHandlerDelegate<in TRequest, TResponse>(
        ILinkConsumedMessage<TRequest> message)
        where TResponse : class
        where TRequest : class;
}
