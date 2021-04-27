namespace Rambler.Server.Utility
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public static class TaskExtensions
    {
        /// <summary>
        /// Sets the task completion source values
        /// </summary>
        /// <typeparam name="T">task type result</typeparam>
        /// <param name="task">the task to add the continuation to</param>
        /// <param name="taskCompletionSource">completion source to fill</param>
        /// <returns>the task completionsource.task</returns>
        public static Task<T> SetCompletion<T>(this Task<T> task, TaskCompletionSource<T> taskCompletionSource)
        {
            task.ContinueWith(t =>
            {
                if (t.IsCanceled)
                {
                    taskCompletionSource.TrySetCanceled();
                }
                else if (t.IsFaulted)
                {
                    taskCompletionSource.TrySetException(t.Exception.InnerException);
                }
                else
                {
                    taskCompletionSource.TrySetResult(t.Result);
                }
            });

            return taskCompletionSource.Task;
        }

        public static Task<T> ContinueWith<T>(this Task task, Func<Task, Task<T>> continuationFunction, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<T>();
            task.ContinueWith(t =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    tcs.SetCanceled();
                }
                continuationFunction(t).SetCompletion(tcs);
            });
            return tcs.Task;
        }
    }
}
