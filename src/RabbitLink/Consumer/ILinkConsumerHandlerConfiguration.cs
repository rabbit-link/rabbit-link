#region Usings

using System;
using System.Threading.Tasks;
using RabbitLink.Messaging;

#endregion

namespace RabbitLink.Consumer
{
    public interface ILinkConsumerHandlerConfiguration
    {
        /// <summary>
        ///     Handles TypeName mapped message parallel, not waiting when handler will be processed.
        /// </summary>
        /// <param name="onMessage">message handler, must return started task</param>
        /// <remarks>If T == Object then all TypeName mapped messages than not handle by other handler will be passed</remarks>
        ILinkConsumerHandlerConfiguration MappedParallelAsync<T>(Func<ILinkRecievedMessage<T>, Task> onMessage)
            where T : class;

        /// <summary>
        ///     Waits for all previous messages processed.
        ///     Then handle TypeName mapped message and waits when handler will be processed.
        /// </summary>
        /// <param name="onMessage">message handler, must return started task</param>
        /// <remarks>If T == Object then all TypeName mapped messages than not handle by other handler will be passed</remarks>
        ILinkConsumerHandlerConfiguration MappedAsync<T>(Func<ILinkRecievedMessage<T>, Task> onMessage) where T : class;

        /// <summary>
        ///     Handles all other message parallel, deserializes it to type <see cref="T" /> not waiting when handler will be
        ///     processed.
        /// </summary>
        /// <param name="onMessage">message handler, must return started task</param>
        void AllParallelAsync<T>(Func<ILinkRecievedMessage<T>, Task> onMessage) where T : class;

        /// <summary>
        ///     Handles all other message parallel, not waiting when handler will be processed.
        /// </summary>
        /// <param name="onMessage">message handler, must return started task</param>
        void AllParallelAsync(Func<ILinkRecievedMessage<byte[]>, Task> onMessage);

        /// <summary>
        ///     Waits for all previous messages processed.
        ///     Then handle message, deserialize it to type <see cref="T" /> and waits when handler will be processed.
        /// </summary>
        /// <param name="onMessage">message handler, must return started task</param>
        void AllAsync<T>(Func<ILinkRecievedMessage<T>, Task> onMessage) where T : class;

        /// <summary>
        ///     Waits for all previous messages processed.
        ///     Then handle message and waits when handler will be processed.
        /// </summary>
        /// <param name="onMessage">message handler, must return started task</param>
        void AllAsync(Func<ILinkRecievedMessage<byte[]>, Task> onMessage);
    }
}