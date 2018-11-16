using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Loom.Client.Internal.AsyncEx
{
    /// <summary>
    ///     A collection of cancelable <see cref="TaskCompletionSource{TResult}" /> instances. Implementations must assume the
    ///     caller is holding a lock.
    /// </summary>
    /// <typeparam name="T">The type of the results. If this isn't needed, use <see cref="object" />.</typeparam>
    internal interface IAsyncWaitQueue<T>
    {
        /// <summary>
        ///     Gets whether the queue is empty.
        /// </summary>
        bool IsEmpty { get; }

        /// <summary>
        ///     Creates a new entry and queues it to this wait queue. The returned task must support both synchronous and
        ///     asynchronous waits.
        /// </summary>
        /// <returns>The queued task.</returns>
        Task<T> Enqueue();

        /// <summary>
        ///     Removes a single entry in the wait queue and completes it. This method may only be called if <see cref="IsEmpty" />
        ///     is <c>false</c>. The task continuations for the completed task must be executed asynchronously.
        /// </summary>
        /// <param name="result">The result used to complete the wait queue entry. If this isn't needed, use <c>default(T)</c>.</param>
        void Dequeue(T result = default(T));

        /// <summary>
        ///     Removes all entries in the wait queue and completes them. The task continuations for the completed tasks must be
        ///     executed asynchronously.
        /// </summary>
        /// <param name="result">The result used to complete the wait queue entries. If this isn't needed, use <c>default(T)</c>.</param>
        void DequeueAll(T result = default(T));

        /// <summary>
        ///     Attempts to remove an entry from the wait queue and cancels it. The task continuations for the completed task must
        ///     be executed asynchronously.
        /// </summary>
        /// <param name="task">The task to cancel.</param>
        /// <param name="cancellationToken">The cancellation token to use to cancel the task.</param>
        bool TryCancel(Task task, CancellationToken cancellationToken);

        /// <summary>
        ///     Removes all entries from the wait queue and cancels them. The task continuations for the completed tasks must be
        ///     executed asynchronously.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token to use to cancel the tasks.</param>
        void CancelAll(CancellationToken cancellationToken);
    }

    /// <summary>
    ///     Provides extension methods for wait queues.
    /// </summary>
    internal static class AsyncWaitQueueExtensions
    {
        /// <summary>
        ///     Creates a new entry and queues it to this wait queue. If the cancellation token is already canceled, this method
        ///     immediately returns a canceled task without modifying the wait queue.
        /// </summary>
        /// <param name="this">The wait queue.</param>
        /// <param name="mutex">A synchronization object taken while cancelling the entry.</param>
        /// <param name="token">The token used to cancel the wait.</param>
        /// <returns>The queued task.</returns>
        public static Task<T> Enqueue<T>(this IAsyncWaitQueue<T> @this, object mutex, CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                return Task.FromCanceled<T>(token);
            }

            Task<T> ret = @this.Enqueue();
            if (!token.CanBeCanceled)
            {
                return ret;
            }

            CancellationTokenRegistration registration = token.Register(() =>
                {
                    lock (mutex)
                    {
                        @this.TryCancel(ret, token);
                    }
                },
                false);
            ret.ContinueWith(_ => registration.Dispose(),
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
            return ret;
        }
    }

    /// <summary>
    ///     The default wait queue implementation, which uses a double-ended queue.
    /// </summary>
    /// <typeparam name="T">The type of the results. If this isn't needed, use <see cref="object" />.</typeparam>
    [DebuggerDisplay("Count = {" + nameof(Count) + "}")]
    internal sealed class DefaultAsyncWaitQueue<T> : IAsyncWaitQueue<T>
    {
        private readonly LinkedList<TaskCompletionSource<T>> queue = new LinkedList<TaskCompletionSource<T>>();

        private int Count => this.queue.Count;

        bool IAsyncWaitQueue<T>.IsEmpty => this.Count == 0;

        Task<T> IAsyncWaitQueue<T>.Enqueue()
        {
            TaskCompletionSource<T> tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            this.queue.AddLast(tcs);
            return tcs.Task;
        }

        void IAsyncWaitQueue<T>.Dequeue(T result)
        {
            TaskCompletionSource<T> taskCompletionSource = this.queue.First.Value;
            this.queue.RemoveFirst();
            taskCompletionSource.SetResult(result);
        }

        void IAsyncWaitQueue<T>.DequeueAll(T result)
        {
            foreach (TaskCompletionSource<T> source in this.queue)
            {
                source.TrySetResult(result);
            }

            this.queue.Clear();
        }

        bool IAsyncWaitQueue<T>.TryCancel(Task task, CancellationToken cancellationToken)
        {
            LinkedListNode<TaskCompletionSource<T>> node = this.queue.First;
            if (node == null)
                return false;

            while (node != null)
            {
                if (node.Value.Task == task)
                {
                    node.Value.TrySetCanceled(cancellationToken);
                    this.queue.Remove(node);
                    return true;
                }

                node = node.Next;
            }

            return false;
        }

        void IAsyncWaitQueue<T>.CancelAll(CancellationToken cancellationToken)
        {
            foreach (TaskCompletionSource<T> source in this.queue)
            {
                source.TrySetCanceled(cancellationToken);
            }

            this.queue.Clear();
        }
    }
}
