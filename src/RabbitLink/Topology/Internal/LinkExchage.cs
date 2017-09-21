#region Usings

using System;

#endregion

namespace RabbitLink.Topology.Internal
{
    internal class LinkExchage : ILinkExchage
    {
        public LinkExchage(string name)
        {
            if (string.IsNullOrWhiteSpace(name) && name != "")
                throw new ArgumentNullException(nameof(name));

            Name = name;
        }

        public string Name { get; }
    }
}
