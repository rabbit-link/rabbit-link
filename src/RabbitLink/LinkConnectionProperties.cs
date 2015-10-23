#region Usings

using System;

#endregion

namespace RabbitLink
{
    public sealed class LinkConnectionProperties : ICloneable
    {
        public string ConnectionString { get; set; }
        public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(3);
        public TimeSpan RecoveryInterval { get; set; } = TimeSpan.FromSeconds(3);

        public bool AutoStart { get; set; } = true;

        object ICloneable.Clone()
        {
            return new LinkConnectionProperties
            {
                RecoveryInterval = RecoveryInterval,
                ConnectionTimeout = ConnectionTimeout,
                ConnectionString = ConnectionString
            };
        }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(ConnectionString))
                throw new ArgumentNullException(nameof(ConnectionString));

            if (
                ConnectionTimeout.TotalMilliseconds <= 0 ||
                ConnectionTimeout.TotalMilliseconds > int.MaxValue
                )
            {
                throw new ArgumentOutOfRangeException(nameof(ConnectionTimeout),
                    "TotalMilliseconds must be greater than 0 and less than Int32.MaxValue");
            }

            if (RecoveryInterval.TotalMilliseconds <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(RecoveryInterval),
                    "TotalMilliseconds must be greater than 0");
            }
        }

        public LinkConnectionProperties Clone()
        {
            return (LinkConnectionProperties) ((ICloneable) this).Clone();
        }
    }
}