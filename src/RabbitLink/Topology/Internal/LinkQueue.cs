#region Usings

using System;

#endregion

namespace RabbitLink.Topology.Internal
{
    internal class LinkQueue : ILinkQueue
    {
        public LinkQueue(string name, bool isExclusive)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));

            Name = name;
            IsExclusive = isExclusive;
        }

        public string Name { get; }

        public bool IsExclusive { get; }
    }
}