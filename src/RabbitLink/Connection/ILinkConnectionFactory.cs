using RabbitMQ.Client;

namespace RabbitLink.Connection
{
    internal interface ILinkConnectionFactory
    {
        #region Properties

        string UserName { get; }
        string Name { get; }

        #endregion

        IConnection GetConnection();
    }
}
