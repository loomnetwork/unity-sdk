using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Loom.Client.Internal;

namespace Loom.Client
{
    public class DefaultDAppChainClientCallExecutor : IDAppChainClientCallExecutor
    {
        private readonly AsyncSemaphore callAsyncSemaphore = new AsyncSemaphore(1);

        public DAppChainClientConfiguration Configuration { get; }

        public DefaultDAppChainClientCallExecutor(DAppChainClientConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        public async Task<T> Call<T>(Func<Task<T>> taskProducer)
        {
            Task<T> task = (Task<T>) await ExecuteTaskWithRetryOnInvalidTxNonceException(
                () => ExecuteTaskWaitForOtherTasks(
                    () => ExecuteTaskWithTimeout(taskProducer, this.Configuration.CallTimeout)
                ));

            return await task;
        }

        public async Task Call(Func<Task> taskProducer)
        {
            Task task = await ExecuteTaskWithRetryOnInvalidTxNonceException(
                () => ExecuteTaskWaitForOtherTasks(
                    () => ExecuteTaskWithTimeout(taskProducer, this.Configuration.CallTimeout)
                ));

            await task;
        }

        public async Task<T> StaticCall<T>(Func<Task<T>> taskProducer)
        {
            Task<T> task = (Task<T>) await ExecuteTaskWaitForOtherTasks(
                () => ExecuteTaskWithTimeout(taskProducer, this.Configuration.StaticCallTimeout)
            );

            return await task;
        }

        public async Task StaticCall(Func<Task> taskProducer)
        {
            Task task = await ExecuteTaskWaitForOtherTasks(
                () => ExecuteTaskWithTimeout(taskProducer, this.Configuration.StaticCallTimeout)
            );

            await task;
        }

        public async Task<T> NonBlockingStaticCall<T>(Func<Task<T>> taskProducer)
        {
            Task<T> task = (Task<T>) await ExecuteTaskWithTimeout(taskProducer, this.Configuration.StaticCallTimeout);
            return await task;
        }

        public async Task NonBlockingStaticCall(Func<Task> taskProducer)
        {
            Task task = await ExecuteTaskWithTimeout(taskProducer, this.Configuration.StaticCallTimeout);
            await task;
        }

        private static async Task<Task> ExecuteTaskWithTimeout(Func<Task> taskProducer, int timeoutMs)
        {
            Task task = taskProducer();
#if UNITY_WEBGL && !UNITY_EDITOR
            // TODO: support timeout for WebGL
            await task;
            return task;
#else
            if (timeoutMs == Timeout.Infinite)
            {
                await task;
                return task;
            }

            try
            {
                Task timeoutTask = Task.Delay(timeoutMs);
                Task result = await Task.WhenAny(task, timeoutTask);
                if (result == task)
                {
                    await task;
                    return task;
                }
            } catch (AggregateException e)
            {
                ExceptionDispatchInfo.Capture(e.InnerException).Throw();
            }

            throw new TimeoutException();
#endif
        }

        private async Task<Task> ExecuteTaskWaitForOtherTasks(Func<Task<Task>> taskProducer)
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

        private async Task<Task> ExecuteTaskWithRetryOnInvalidTxNonceException(Func<Task<Task>> taskTaskProducer)
        {
            int badNonceCount = 0;
            do
            {
                try
                {
                    Task<Task> task = taskTaskProducer();
                    await task;
                    return await task;
                } catch (InvalidTxNonceException)
                {
                    badNonceCount++;
                }

                // WaitForSecondsRealtime can throw a "get_realtimeSinceStartup can only be called from the main thread." error.
                // WebGL doesn't have threads, so use WaitForSecondsRealtime for WebGL anyway
                const float delay = 0.5f;
#if UNITY_WEBGL && !UNITY_EDITOR
                await new WaitForSecondsRealtime(delay);
#else
                await Task.Delay(TimeSpan.FromSeconds(delay));
#endif
            } while (this.Configuration.InvalidNonceTxRetries != 0 && badNonceCount <= this.Configuration.InvalidNonceTxRetries);

            throw new InvalidTxNonceException(1, "sequence number does not match");
        }
    }
}
