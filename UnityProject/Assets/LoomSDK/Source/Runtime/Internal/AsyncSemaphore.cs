using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Loom.Client.Internal.AsyncEx
{
    /// <summary>
    ///     An async-compatible semaphore. Alternatively, you could use <c>SemaphoreSlim</c>.
    /// </summary>
    [DebuggerDisplay("CurrentCount = {" + nameof(count) + "}")]
    internal sealed class AsyncSemaphore
    {
        /// <summary>
        ///     The object used for mutual exclusion.
        /// </summary>
        private readonly object mutex;

        /// <summary>
        ///     The queue of TCSs that other tasks are awaiting to acquire the semaphore.
        /// </summary>
        private readonly IAsyncWaitQueue<object> queue;

        /// <summary>
        ///     The number of waits that will be immediately granted.
        /// </summary>
        private long count;

        /// <summary>
        ///     Creates a new async-compatible semaphore with the specified initial count.
        /// </summary>
        /// <param name="initialCount">The initial count for this semaphore. This must be greater than or equal to zero.</param>
        /// <param name="queue">The wait queue used to manage waiters. This may be <c>null</c> to use a default (FIFO) queue.</param>
        public AsyncSemaphore(long initialCount, IAsyncWaitQueue<object> queue = null)
        {
            this.queue = queue ?? new DefaultAsyncWaitQueue<object>();
            this.count = initialCount;
            this.mutex = new object();
        }

        /// <summary>
        ///     Gets the number of slots currently available on this semaphore. This member is seldom used; code using this member
        ///     has a high possibility of race conditions.
        /// </summary>
        public long CurrentCount
        {
            get
            {
                lock (this.mutex)
                {
                    return this.count;
                }
            }
        }

        /// <summary>
        ///     Asynchronously waits for a slot in the semaphore to be available.
        /// </summary>
        /// <param name="cancellationToken">
        ///     The cancellation token used to cancel the wait. If this is already set, then this
        ///     method will attempt to take the slot immediately (succeeding if a slot is currently available).
        /// </param>
        public Task WaitAsync(CancellationToken cancellationToken)
        {
            Task ret;
            lock (this.mutex)
            {
                // If the semaphore is available, take it immediately and return.
                if (this.count != 0)
                {
                    --this.count;
                    ret = Task.CompletedTask;
                } else
                {
                    // Wait for the semaphore to become available or cancellation.
                    ret = this.queue.Enqueue(this.mutex, cancellationToken);
                }
            }

            return ret;
        }

        /// <summary>
        ///     Asynchronously waits for a slot in the semaphore to be available.
        /// </summary>
        public Task WaitAsync()
        {
            return WaitAsync(CancellationToken.None);
        }

        /// <summary>
        ///     Synchronously waits for a slot in the semaphore to be available. This method may block the calling thread.
        /// </summary>
        /// <param name="cancellationToken">
        ///     The cancellation token used to cancel the wait. If this is already set, then this
        ///     method will attempt to take the slot immediately (succeeding if a slot is currently available).
        /// </param>
        public void Wait(CancellationToken cancellationToken)
        {
            WaitAndUnwrapException(WaitAsync(cancellationToken));
        }

        /// <summary>
        ///     Synchronously waits for a slot in the semaphore to be available. This method may block the calling thread.
        /// </summary>
        public void Wait()
        {
            Wait(CancellationToken.None);
        }

        /// <summary>
        ///     Releases the semaphore.
        /// </summary>
        public void Release(long releaseCount = 1)
        {
            if (releaseCount == 0)
            {
                return;
            }

            lock (this.mutex)
            {
                checked
                {
#pragma warning disable 0219
                    // Use dummy unused variable to force check the result for overflow
                    long test = this.count + releaseCount;
#pragma warning restore 0219
                }

                while (releaseCount != 0 && !this.queue.IsEmpty)
                {
                    this.queue.Dequeue();
                    --releaseCount;
                }

                this.count += releaseCount;
            }
        }

        /// <summary>
        ///     Waits for the task to complete, unwrapping any exceptions.
        /// </summary>
        /// <param name="task">The task. May not be <c>null</c>.</param>
        private static void WaitAndUnwrapException(Task task)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            task.GetAwaiter().GetResult();
        }
    }
}
