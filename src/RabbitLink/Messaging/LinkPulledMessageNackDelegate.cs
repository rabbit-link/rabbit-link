namespace RabbitLink.Messaging
{
    /// <summary>
    /// Delegate for <see cref="ILinkPulledMessage.Nack"/>
    /// </summary>
    /// <param name="requeue">If true returns message to queue</param>
    public delegate void LinkPulledMessageNackDelegate(bool requeue = false);
}