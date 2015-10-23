#region Usings

using System;
using System.Threading.Tasks;
using RabbitLink.Messaging;

#endregion

namespace RabbitLink.Consumer
{
    public static class LinkConsumerHandlerConfigurationExtensions
    {
        /// <summary>
        ///     Handles TypeName mapped message parallel, not waiting when handler will be processed.
        /// </summary>
        /// <param name="onMessage">message handler</param>
        public static ILinkConsumerHandlerConfiguration MappedParallel<T>(this ILinkConsumerHandlerConfiguration @this,
            Action<ILinkRecievedMessage<T>> onMessage)
            where T : class
        {
            if (@this == null)
                throw new ArgumentNullException(nameof(@this));

            if (onMessage == null)
                throw new ArgumentNullException(nameof(onMessage));

            return @this.MappedParallelAsync<T>(msg =>
            {
                onMessage(msg);
                return Task.FromResult((object) null);
            });
        }

        /// <summary>
        ///     Waits for all previous messages processed.
        ///     Then handle TypeName mapped message and waits when handler will be processed.
        /// </summary>
        /// <param name="onMessage">message handler</param>
        public static ILinkConsumerHandlerConfiguration Mapped<T>(this ILinkConsumerHandlerConfiguration @this,
            Action<ILinkRecievedMessage<T>> onMessage)
            where T : class
        {
            if (@this == null)
                throw new ArgumentNullException(nameof(@this));

            if (onMessage == null)
                throw new ArgumentNullException(nameof(onMessage));

            return @this.MappedAsync<T>(msg =>
            {
                onMessage(msg);
                return Task.FromResult((object) null);
            });
        }

        /// <summary>
        ///     Handles all other message parallel, deserializes it to type <see cref="T" /> not waiting when handler will be
        ///     processed.
        /// </summary>
        /// <param name="onMessage">message handler</param>
        public static void AllParallel<T>(this ILinkConsumerHandlerConfiguration @this,
            Action<ILinkRecievedMessage<T>> onMessage) where T : class
        {
            if (@this == null)
                throw new ArgumentNullException(nameof(@this));

            if (onMessage == null)
                throw new ArgumentNullException(nameof(onMessage));

            @this.AllParallelAsync<T>(msg =>
            {
                onMessage(msg);
                return Task.FromResult((object) null);
            });
        }

        /// <summary>
        ///     Handles all other message parallel, not waiting when handler will be processed.
        /// </summary>
        /// <param name="onMessage">message handler</param>
        public static void AllParallel(this ILinkConsumerHandlerConfiguration @this,
            Action<ILinkRecievedMessage<byte[]>> onMessage)
        {
            if (@this == null)
                throw new ArgumentNullException(nameof(@this));

            if (onMessage == null)
                throw new ArgumentNullException(nameof(onMessage));

            @this.AllParallelAsync(msg =>
            {
                onMessage(msg);
                return Task.FromResult((object) null);
            });
        }

        /// <summary>
        ///     Waits for all previous messages processed.
        ///     Then handle message, deserialize it to type <see cref="T" /> and waits when handler will be processed.
        /// </summary>
        /// <param name="onMessage">message handler</param>
        public static void All<T>(this ILinkConsumerHandlerConfiguration @this,
            Action<ILinkRecievedMessage<T>> onMessage) where T : class
        {
            if (@this == null)
                throw new ArgumentNullException(nameof(@this));

            if (onMessage == null)
                throw new ArgumentNullException(nameof(onMessage));

            @this.AllAsync<T>(msg =>
            {
                onMessage(msg);
                return Task.FromResult((object) null);
            });
        }

        /// <summary>
        ///     Waits for all previous messages processed.
        ///     Then handle message and waits when handler will be processed.
        /// </summary>
        /// <param name="onMessage">message handler, must return started task</param>
        public static void All(this ILinkConsumerHandlerConfiguration @this,
            Func<ILinkRecievedMessage<byte[]>, Task> onMessage)
        {
            if (@this == null)
                throw new ArgumentNullException(nameof(@this));

            if (onMessage == null)
                throw new ArgumentNullException(nameof(onMessage));

            @this.AllAsync(msg =>
            {
                onMessage(msg);
                return Task.FromResult((object) null);
            });
        }
    }
}