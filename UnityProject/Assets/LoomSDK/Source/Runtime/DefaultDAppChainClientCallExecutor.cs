using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Loom.Client.Internal.AsyncEx;
using UnityEngine;
#if UNITY_WEBGL
using Loom.Client.Unity.Internal.UnityAsyncAwaitUtil;
#endif

namespace Loom.Client
{
    /// <summary>
    /// Default call executor provides handling for general blockchain situations.
    /// 1. Calls throw a <see cref="TimeoutException"/> if the calls receives no response for too long.
    /// 2. Calls are queued, there can be only one active call at any given moment.
    /// 3. If the blockchain reports an invalid nonce, the call will be retried a number of times.
    /// </summary>
    public class DefaultDAppChainClientCallExecutor : IDAppChainClientCallExecutor, ILogProducer
    {
        private readonly AsyncSemaphore callAsyncSemaphore = new AsyncSemaphore(1);
        private readonly DAppChainClientConfiguration configuration;

        public ILogger Logger { get; set; } = NullLogger.Instance;

        public DefaultDAppChainClientCallExecutor(DAppChainClientConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            this.configuration = configuration;
        }

        public virtual async Task<T> Call<T>(Func<Task<T>> taskProducer, CallDescription callDescription)
        {
            Task<T> task = (Task<T>) await ExecuteTaskWithRetryOnInvalidTxNonceException(
                () => ExecuteTaskWaitForOtherTasks(
                    () => ExecuteTaskWithTimeout(taskProducer, this.configuration.CallTimeout)
                ));

            return await task;
        }

        public virtual async Task Call(Func<Task> taskProducer, CallDescription callDescription)
        {
            Task task = await ExecuteTaskWithRetryOnInvalidTxNonceException(
                () => ExecuteTaskWaitForOtherTasks(
                    () => ExecuteTaskWithTimeout(taskProducer, this.configuration.CallTimeout)
                ));

            await task;
        }

        public virtual async Task<T> StaticCall<T>(Func<Task<T>> taskProducer, CallDescription callDescription)
        {
            Task<T> task = (Task<T>) await ExecuteTaskWaitForOtherTasks(
                () => ExecuteTaskWithTimeout(taskProducer, this.configuration.StaticCallTimeout)
            );

            return await task;
        }

        public virtual async Task StaticCall(Func<Task> taskProducer, CallDescription callDescription)
        {
            Task task = await ExecuteTaskWaitForOtherTasks(
                () => ExecuteTaskWithTimeout(taskProducer, this.configuration.StaticCallTimeout)
            );

            await task;
        }

        public virtual async Task<T> NonBlockingStaticCall<T>(Func<Task<T>> taskProducer, CallDescription callDescription)
        {
            Task<T> task = (Task<T>) await ExecuteTaskWithTimeout(taskProducer, this.configuration.StaticCallTimeout);
            return await task;
        }

        public virtual async Task NonBlockingStaticCall(Func<Task> taskProducer, CallDescription callDescription)
        {
            Task task = await ExecuteTaskWithTimeout(taskProducer, this.configuration.StaticCallTimeout);
            await task;
        }

        protected virtual async Task<Task> ExecuteTaskWithTimeout(Func<Task> taskProducer, int timeoutMs)
        {
            Task task = taskProducer();
            try
            {
                bool timedOut = await RunTaskWithTimeout(task, timeoutMs);
                if (!timedOut)
                    return task;
            } catch (AggregateException e)
            {
                ExceptionDispatchInfo.Capture(e.InnerException).Throw();
            }

            throw new TimeoutException("The game session was dropped. Please check your internet connection and try again later.");
        }

        protected virtual async Task<Task> ExecuteTaskWaitForOtherTasks(Func<Task<Task>> taskProducer)
        {
            try
            {
                await this.callAsyncSemaphore.WaitAsync();
                Task<Task> task = taskProducer();
                await task;
                return await task;
            } finally
            {
                this.callAsyncSemaphore.Release();
            }
        }

        protected virtual async Task<Task> ExecuteTaskWithRetryOnInvalidTxNonceException(Func<Task<Task>> taskTaskProducer)
        {
            int badNonceCount = 0;
            float delay = 0.5f;
            TxCommitException lastNonceException;
            do
            {
                try
                {
                    Task<Task> task = taskTaskProducer();
                    await task;
                    return await task;
                } catch (TxCommitException e) when (e is InvalidTxNonceException || e is TxAlreadyExistsInCacheException)
                {
                    badNonceCount++;
                    lastNonceException = e;
                }

                this.Logger.Log($"[NonceLog] badNonceCount == {badNonceCount}, delay: {delay:F2}");

                // WaitForSecondsRealtime can throw a "get_realtimeSinceStartup can only be called from the main thread." error.
                // WebGL doesn't have threads, so use WaitForSecondsRealtime for WebGL anyway
#if UNITY_WEBGL
                await new WaitForSecondsRealtime(delay);
#else
                await Task.Delay(TimeSpan.FromSeconds(delay));
#endif
                delay *= 1.75f;
            } while (
                this.configuration.InvalidNonceTxRetries != 0 &&
                badNonceCount <= this.configuration.InvalidNonceTxRetries);

            throw lastNonceException;
        }

        /// <summary>
        /// Waits for the task to complete for up to <paramref name="timeoutMilliseconds"/>
        /// and returns whether it completed in time.
        /// </summary>
        /// <param name="task"></param>
        /// <param name="timeoutMilliseconds"></param>
        /// <returns>True if task timed out, false otherwise.</returns>
        private static async Task<bool> RunTaskWithTimeout(Task task, int timeoutMilliseconds)
        {
            if (timeoutMilliseconds < 0)
                throw new ArgumentOutOfRangeException(nameof(timeoutMilliseconds));

            if (timeoutMilliseconds == 0)
                return !task.IsCompleted;

#if UNITY_WEBGL
            // TODO: support timeout for WebGL
            await task;
            return false;
#else
            if (timeoutMilliseconds == Timeout.Infinite)
            {
                await task;
                return false;
            }

            CancellationTokenSource cts = new CancellationTokenSource();
            Task delayTask = Task.Delay(timeoutMilliseconds, cts.Token);
            Task firstTask = await Task.WhenAny(task, delayTask);
            if (firstTask == task) {
                cts.Cancel();
                await task;
                return false;
            }

            return true;
#endif
        }
    }
}
