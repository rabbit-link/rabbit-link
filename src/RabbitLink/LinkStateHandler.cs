using System;

namespace RabbitLink
{
    public delegate void LinkStateHandler<in TState>(TState oldState, TState newsState)
        where TState : IComparable;
}