using RabbitLink.Consumer;
using RabbitLink.Rpc;

namespace RabbitLink.Builders
{
    /// <summary>
    /// Rpc server builder
    /// </summary>
    public interface ILinkRpcServerBuilder
    {
        /// <summary>
        /// Build rpc consumer instance of <see cref="ILinkConsumer"/>
        /// </summary>
        /// <returns><see cref="ILinkConsumer"/></returns>
        ILinkConsumer Build();

        /// <summary>
        /// When true - skip request without ReplayTo setted
        /// When false - execute handler and ignore response
        /// Default - false
        /// </summary>
        ILinkRpcServerBuilder StrictTwoWay(bool strictTwoWay);


        /// <summary>
        /// Specify raw input rpc handler for builder
        /// For skip standart serialization, do not set ILinkProducer serializer and use TOut = byte[]
        /// </summary>
        /// <typeparam name="TOut">response type</typeparam>
        ILinkRpcServerBuilder Handler<TOut>(LinkServeHandlerDelegate<byte[], TOut> handler)
            where TOut : class;

        /// <summary>
        /// Specify rpc handler for builder
        /// </summary>
        /// <typeparam name="TIn">request type</typeparam>
        /// <typeparam name="TOut">response type</typeparam>
        ILinkRpcServerBuilder Handler<TIn, TOut>(LinkServeHandlerDelegate<TIn, TOut> handler)
            where TOut : class
            where TIn : class;
    }
}
