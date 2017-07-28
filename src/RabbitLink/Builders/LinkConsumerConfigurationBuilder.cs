#region Usings

using System;

#endregion

namespace RabbitLink.Builders
{
    internal class LinkConsumerConfigurationBuilder :
        ILinkConsumerConfigurationBuilder        
    {
        public LinkConsumerConfigurationBuilder(LinkConfiguration linkConfiguration)
        {
                 
        }

        internal LinkConsumerConfiguration Configuration { get; } = new LinkConsumerConfiguration();

        public ILinkConsumerConfigurationBuilder GetMessageTimeout(TimeSpan? value)
        {
            Configuration.GetMessageTimeout = value;
            return this;
        }

        public ILinkConsumerConfigurationBuilder PrefetchCount(ushort value)
        {
            Configuration.PrefetchCount = value;
            return this;
        }

        public ILinkConsumerConfigurationBuilder AutoAck(bool value)
        {
            Configuration.AutoAck = value;
            return this;
        }

        public ILinkConsumerConfigurationBuilder Priority(int value)
        {
            Configuration.Priority = value;
            return this;
        }

        public ILinkConsumerConfigurationBuilder CancelOnHaFailover(bool value)
        {
            Configuration.CancelOnHaFailover = value;
            return this;
        }

        public ILinkConsumerConfigurationBuilder Exclusive(bool value)
        {
            Configuration.Exclusive = value;
            return this;
        }   
    }
}