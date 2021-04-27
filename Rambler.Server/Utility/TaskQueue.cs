namespace Rambler.Server.Utility
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Queues up incoming tasks to simplify processing a bit.
    /// FIFO for tasks
    /// Note:  Queued tasks must handle their own errors or this will kaboom.
    /// </summary>
    public class TaskQueue
    {
        private readonly BlockingCollection<Func<Task>> queue = new BlockingCollection<Func<Task>>();

        /// <summary>
        /// Default constructor
        /// </summary>
        public TaskQueue() { }

        /// <summary>
        /// Starts processing the queue
        /// </summary>
        /// <param name="options">Optional specific task creation options</param>
        /// <param name="cancelToken">Optional cancellation token for the queue</param>
        /// <returns>The task running the </returns>
        public async Task Start(TaskCreationOptions options = TaskCreationOptions.LongRunning, CancellationToken cancelToken = default(CancellationToken))
        {
            //upwrap all the things
            await await Task.Factory.StartNew(async () =>
            {
                foreach (var task in queue.GetConsumingEnumerable(cancelToken))
                {
                    await task();
                }
            }, options);
        }

        /// <summary>
        /// Enqueues a piece of work
        /// </summary>
        /// <param name="task">The piece of work to perform.  Should not be 'started'</param>
        public void Enqueue(Func<Task> task)
        {
            queue.Add(task);
        }

        /// <summary>
        /// Enqueues a piece of work and wraps it to return a result task when it's completed
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="task"></param>
        /// <returns></returns>
        public Task<T> Enqueue<T>(Func<Task<T>> task)
        {
            var tcs = new TaskCompletionSource<T>();
            queue.Add(() =>
            {
                return task().SetCompletion(tcs);
            });
            return tcs.Task;
        }

        /// <summary>
        /// Stops the queue, finishes processing any existing data.
        /// Await the task returned on Start() to wait for completion.
        /// Cancellation token is necessary to cancel work
        /// </summary>
        public void Stop()
        {
            queue.CompleteAdding();
        }
    }
}
