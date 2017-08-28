using System;

namespace RabbitLink
{
    /// <summary>
    /// Handler delegate for state changes
    /// </summary>
    /// <typeparam name="TState">State type</typeparam>
    /// <param name="oldState">Old state</param>
    /// <param name="newsState">New state</param>
    public delegate void LinkStateHandler<in TState>(TState oldState, TState newsState)
        where TState : IComparable;
}