#region Usings

using System;

#endregion

namespace RabbitLink.Internals
{
    internal abstract class AsyncStateMachine<TState>
        where TState : IComparable
    {
        protected AsyncStateMachine(TState initialState)
        {
            State = initialState;
        }
        
        #region Properties

        public TState State { get; private set; }

        #endregion

        protected virtual void OnStateChange(TState newState)
        {
        }

        protected bool ChangeState(TState newState)
        {
            if (State.CompareTo(newState) != 0)
            {
                OnStateChange(newState);
                State = newState;

                return true;
            }

            return false;
        }
    }
}