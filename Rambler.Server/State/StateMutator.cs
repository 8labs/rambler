namespace Rambler.Server.State
{
    using Microsoft.Extensions.Logging;
    using System;
    using System.Threading.Tasks;
    using Utility;

    /// <summary>
    /// Mutates a state object.
    /// Mutate actions applied are FIFO.
    /// TODO: At this point, this is a state DB and handles transactions.
    /// There's no reason for it to be FIFO anymore and could probably use simple locks.
    /// </summary>
    public class StateMutator
    {
        private readonly SerialQueue queue = new SerialQueue();

        private readonly StateCache state;

        private readonly ILogger<StateMutator> log;

        public StateMutator(StateCache state, ILogger<StateMutator> log)
        {
            this.state = state;
            this.log = log;
        }

        public Task<T> Enqueue<T>(Func<StateCache, T> action)
        {
            return queue.Enqueue(() => action(state));
        }

        public Task<T> Enqueue<T>(Func<StateCache, Task<T>> action)
        {
            return queue.Enqueue(() => action(state));
        }

        public Task Enqueue(Func<StateCache, Task> action)
        {
            return queue.Enqueue(() => action(state));
        }

        public Task Stop()
        {
            return queue.StopAdding();
        }
    }
}
