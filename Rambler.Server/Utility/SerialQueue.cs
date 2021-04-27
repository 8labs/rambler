namespace Rambler.Server.Utility
{
    using System;
    using System.Threading.Tasks;
    using System.Threading;

    /// <summary>
    /// Original: https://github.com/Gentlee/SerialQueue
    /// FIFO queue of tasks.
    /// Simply continues each task with the next.
    /// </summary>
    public class SerialQueue
    {
        readonly object locker = new object();

        WeakReference<Task> lastTask;

        readonly CancellationToken cancelToken;

        bool isStopped = false;

        public SerialQueue() : this(default(CancellationToken)) { }

        public SerialQueue(CancellationToken cancelToken)
        {
            this.cancelToken = cancelToken;
        }

        /// <summary>
        /// Enqueues a simple func rather than a task. A
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="task"></param>
        /// <returns></returns>
        public Task<T> Enqueue<T>(Func<T> task)
        {
            lock (locker)
            {
                CheckCanAdd();
                Task<T> result;
                if (lastTask != null && lastTask.TryGetTarget(out var prev))
                {
                    result = prev.ContinueWith((t) => task(), cancelToken);
                }
                else
                {
                    result = Task.Run(task); // TODO - proper scheduler?
                }

                lastTask = new WeakReference<Task>(result);
                return result;
            }
        }

        /// <summary>
        /// Enqueue a task that returns a result B
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="task"></param>
        /// <returns></returns>
        public Task<T> Enqueue<T>(Func<Task<T>> task)
        {
            lock (locker)
            {
                CheckCanAdd();
                Task<T> result;
                if (lastTask != null && lastTask.TryGetTarget(out var prev))
                {
                    result = prev.ContinueWith((t) => task(), cancelToken).Unwrap();
                }
                else
                {
                    result = task();
                }

                lastTask = new WeakReference<Task>(result);
                return result;
            }
        }

        /// <summary>
        /// Enqueue a tasks C
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        public Task Enqueue(Func<Task> task)
        {
            lock (locker)
            {
                CheckCanAdd();
                Task result;
                if (lastTask != null && lastTask.TryGetTarget(out var prev))
                {
                    result = prev.ContinueWith((t) => task(), cancelToken).Unwrap();
                }
                else
                {
                    result = task();
                }

                lastTask = new WeakReference<Task>(result);
                return result;
            }
        }

        public Task StopAdding()
        {
            lock (locker)
            {
                isStopped = true;
                if (lastTask != null && lastTask.TryGetTarget(out var prev))
                {
                    return prev;
                }
                else
                {
                    return Task.CompletedTask;
                }
            }
        }

        private void CheckCanAdd()
        {
            cancelToken.ThrowIfCancellationRequested();

            if (isStopped)
            {
                throw new InvalidOperationException("Serial queue has been stopped.");
            }
        }
    }
}
