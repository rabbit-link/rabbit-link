#region Usings

using System;

#endregion

namespace RabbitLink.Topology.Internal
{
    internal class LinkExchange : ILinkExchange
    {
        public LinkExchange(string name)
        {
            if (string.IsNullOrWhiteSpace(name) && name != "")
                throw new ArgumentNullException(nameof(name));

            Name = name;
        }

        public string Name { get; }
    }
}
