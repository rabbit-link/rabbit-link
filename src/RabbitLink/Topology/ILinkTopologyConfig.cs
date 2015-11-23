#region Usings

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

#endregion

namespace RabbitLink.Topology
{
    public interface ILinkTopologyConfig
    {
        Task<ILinkExchage> ExchangeDeclare(
            string name,
            LinkExchangeType type,
            bool durable = true,
            bool autoDelete = false,
            string alternateExchange = null,
            bool delayed = false
            );

        Task<ILinkExchage> ExchangeDeclarePassive(string name);
        Task<ILinkExchage> ExchangeDeclareDefault();
        Task ExchangeDelete(ILinkExchage exchange, bool ifUnused = false);

        Task<ILinkQueue> QueueDeclareExclusiveByServer();

        Task<ILinkQueue> QueueDeclareExclusive(
            bool autoDelete = true,
            TimeSpan? messageTtl = null,
            TimeSpan? expires = null,
            byte? maxPriority = null,
            int? maxLength = null,
            int? maxLengthBytes = null,
            string deadLetterExchange = null,
            string deadLetterRoutingKey = null
            );

        Task<ILinkQueue> QueueDeclareExclusive(
            string prefix,
            bool autoDelete = true,
            TimeSpan? messageTtl = null,
            TimeSpan? expires = null,
            byte? maxPriority = null,
            int? maxLength = null,
            int? maxLengthBytes = null,
            string deadLetterExchange = null,
            string deadLetterRoutingKey = null
            );

        Task<ILinkQueue> QueueDeclarePassive(string name);

        Task<ILinkQueue> QueueDeclare(
            string name,
            bool durable = true,
            bool exclusive = false,
            bool autoDelete = false,
            TimeSpan? messageTtl = null,
            TimeSpan? expires = null,
            byte? maxPriority = null,
            int? maxLength = null,
            int? maxLengthBytes = null,
            string deadLetterExchange = null,
            string deadLetterRoutingKey = null
            );

        Task QueueDelete(ILinkQueue queue, bool ifUnused = false, bool ifEmpty = false);
        Task QueuePurge(ILinkQueue queue);

        Task Bind(ILinkExchage destination, ILinkExchage source, string routingKey = null,
            IDictionary<string, object> arguments = null);

        Task Bind(ILinkQueue queue, ILinkExchage exchange, string routingKey = null,
            IDictionary<string, object> arguments = null);
    }
}