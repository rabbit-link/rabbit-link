namespace RabbitLink.Messaging
{
    /// <summary>
    /// Delegate for <see cref="ILinkPulledMessage{TBody}.Ack"/>
    /// </summary>
    public delegate void LinkPulledMessageAckDelegate();
}