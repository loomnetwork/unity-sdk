using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Loom.Client.Internal.AsyncEx;

namespace Loom.Client
{
    /// <summary>
    /// Default call executor provides handling for general blockchain situations.
    /// 1. Calls throw a <see cref="TimeoutException"/> if the calls receives no response for too long.
    /// 2. Calls are queued, there can be only one active call at any given moment.
    /// 3. If the blockchain reports an invalid nonce, the call will be retried a number of times.
    /// </summary>
    public class DefaultDAppChainClientCallExecutor : IDAppChainClientCallExecutor
    {
        private readonly AsyncSemaphore callAsyncSemaphore = new AsyncSemaphore(1);
        private readonly IDAppChainClientConfigurationProvider configurationProvider;

        public DefaultDAppChainClientCallExecutor(IDAppChainClientConfigurationProvider configurationProvider)
        {
            if (configurationProvider == null)
                throw new ArgumentNullException(nameof(configurationProvider));

            this.configurationProvider = configurationProvider;
        }

        public async Task<T> Call<T>(Func<Task<T>> taskProducer)
        {
            Task<T> task = (Task<T>) await ExecuteTaskWithRetryOnInvalidTxNonceException(
                () => ExecuteTaskWaitForOtherTasks(
                    () => ExecuteTaskWithTimeout(taskProducer, this.configurationProvider.Configuration.CallTimeout)
                ));

            return await task;
        }

        public async Task Call(Func<Task> taskProducer)
        {
            Task task = await ExecuteTaskWithRetryOnInvalidTxNonceException(
                () => ExecuteTaskWaitForOtherTasks(
                    () => ExecuteTaskWithTimeout(taskProducer, this.configurationProvider.Configuration.CallTimeout)
                ));

            await task;
        }

        public async Task<T> StaticCall<T>(Func<Task<T>> taskProducer)
        {
            Task<T> task = (Task<T>) await ExecuteTaskWaitForOtherTasks(
                () => ExecuteTaskWithTimeout(taskProducer, this.configurationProvider.Configuration.StaticCallTimeout)
            );

            return await task;
        }

        public async Task StaticCall(Func<Task> taskProducer)
        {
            Task task = await ExecuteTaskWaitForOtherTasks(
                () => ExecuteTaskWithTimeout(taskProducer, this.configurationProvider.Configuration.StaticCallTimeout)
            );

            await task;
        }

        public async Task<T> NonBlockingStaticCall<T>(Func<Task<T>> taskProducer)
        {
            Task<T> task = (Task<T>) await ExecuteTaskWithTimeout(taskProducer, this.configurationProvider.Configuration.StaticCallTimeout);
            return await task;
        }

        public async Task NonBlockingStaticCall(Func<Task> taskProducer)
        {
            Task task = await ExecuteTaskWithTimeout(taskProducer, this.configurationProvider.Configuration.StaticCallTimeout);
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
            } while (
                this.configurationProvider.Configuration.InvalidNonceTxRetries != 0 &&
                badNonceCount <= this.configurationProvider.Configuration.InvalidNonceTxRetries);

            throw new InvalidTxNonceException(1, "sequence number does not match");
        }
    }
}
