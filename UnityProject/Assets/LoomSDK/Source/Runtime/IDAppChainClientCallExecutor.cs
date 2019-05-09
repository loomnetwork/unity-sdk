using System;
using System.Threading.Tasks;

namespace Loom.Client
{
    /// <summary>
    /// Call executors control the flow of blockchain calls.
    /// </summary>
    public interface IDAppChainClientCallExecutor
    {
        /// <summary>
        /// Executes a call that mutates state and returns a value.
        /// </summary>
        /// <param name="taskProducer"></param>
        /// <typeparam name="T">Return value type.</typeparam>
        Task<T> Call<T>(Func<Task<T>> taskProducer, CallDescription callDescription);

        /// <summary>
        /// Executes a call that mutates state.
        /// </summary>
        /// <param name="taskProducer"></param>
        Task Call(Func<Task> taskProducer, CallDescription callDescription);

        /// <summary>
        /// Executes a call that doesn't mutates state, and returns a value.
        /// </summary>
        /// <param name="taskProducer"></param>
        /// <typeparam name="T">Return value type.</typeparam>
        Task<T> StaticCall<T>(Func<Task<T>> taskProducer, CallDescription callDescription);

        /// <summary>
        /// Executes a call that doesn't mutates state.
        /// </summary>
        /// <param name="taskProducer"></param>
        Task StaticCall(Func<Task> taskProducer, CallDescription callDescription);

        /// <summary>
        /// Executes a call that doesn't mutates state, and returns a value.
        /// If applicable, this method will not block other calls.
        /// </summary>
        /// <param name="taskProducer"></param>
        /// <typeparam name="T">Return value type.</typeparam>
        Task<T> NonBlockingStaticCall<T>(Func<Task<T>> taskProducer, CallDescription callDescription);

        /// <summary>
        /// Executes a call that doesn't mutates state.
        /// If applicable, this method will not block other calls.
        /// </summary>
        /// <param name="taskProducer"></param>
        Task NonBlockingStaticCall(Func<Task> taskProducer, CallDescription callDescription);
    }
}
