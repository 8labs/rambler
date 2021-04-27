namespace Rambler.Server.Utility
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Mutates a state object.
    /// Mutate actions applied are FIFO.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Mutator<TState>
    {
        private readonly SerialQueue queue = new SerialQueue();

        private readonly TState state;

        public Mutator(TState state)
        {
            this.state = state;
        }

        public Task<T> Enqueue<T>(Func<TState, T> action)
        {
            return queue.Enqueue(() => action(state));
        }

        public Task<T> Enqueue<T>(Func<TState, Task<T>> action)
        {
            return queue.Enqueue(() => action(state));
        }

        public Task Enqueue(Func<TState, Task> action)
        {
            return queue.Enqueue(() => action(state));
        }

        public Task Stop()
        {
            return queue.StopAdding();
        }
    }
}
