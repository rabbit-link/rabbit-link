#region Usings

using RabbitMQ.Client;

#endregion

namespace RabbitLink.Connection
{
    internal interface ILinkConnectionFactory
    {
        IConnection GetConnection();

        #region Properties

        string UserName { get; }
        string Name { get; }

        #endregion
    }
}
