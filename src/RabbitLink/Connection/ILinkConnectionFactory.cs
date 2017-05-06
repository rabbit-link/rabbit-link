using RabbitMQ.Client;

namespace RabbitLink.Connection
{
    interface ILinkConnectionFactory
    {
        #region Properties

        string UserName { get; }
        string Name { get; }

        #endregion

        IConnection GetConnection();
    }
}