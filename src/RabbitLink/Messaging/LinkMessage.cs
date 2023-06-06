namespace RabbitLink.Messaging
{
    /// <inheritdoc />
    public abstract class LinkMessage<TBody> : ILinkMessage<TBody>
    {
        #region Ctor

        /// <summary>
        ///     Makes instance
        /// </summary>
        /// <param name="body">Body value</param>
        /// <param name="properties">Message properties</param>
        protected LinkMessage(
            TBody body,
            LinkMessageProperties properties = null
        )
        {
            Body = body;
            Properties = properties ?? new LinkMessageProperties();
        }

        #endregion

        #region Properties

        /// <inheritdoc />
        public LinkMessageProperties Properties { get; }

        /// <inheritdoc />
        public TBody Body { get; }

        #endregion
    }
}
