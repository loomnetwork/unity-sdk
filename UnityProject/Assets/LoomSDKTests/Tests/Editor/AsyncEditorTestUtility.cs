using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using UnityEditor;

namespace Loom.Client.Tests
{
    public static class AsyncEditorTestUtility
    {
        public static IEnumerator AsyncTest(Func<Task> testAction, Func<Task> beforeTestActionCallback = null, int timeout = 10000) {
            return
                TaskAsIEnumerator(Task.Run(() =>
                {
                    try
                    {
                        beforeTestActionCallback?.Invoke().Wait();
                        testAction().Wait();
                    } catch (AggregateException e)
                    {
                        UnityEngine.Debug.LogException(e);
                        ExceptionDispatchInfo.Capture(e.InnerException).Throw();
                    }
                }), timeout);
        }

        public static IEnumerator TaskAsIEnumerator(Task task, int timeout = 10000)
        {
            Stopwatch timeoutStopwatch = Stopwatch.StartNew();
            while (!task.IsCompleted)
            {
                if (timeoutStopwatch.ElapsedMilliseconds > timeout)
                    throw new Exception($"Test task {task} timed out after {timeout} ms");

                yield return null;
            }

            if (task.IsFaulted)
                task.Wait();
        }

        public static async Task<bool> WaitWithTimeout(float timeout, Func<bool> isCompletedFunc)
        {
            if (isCompletedFunc == null)
                throw new ArgumentNullException(nameof(isCompletedFunc));

            if (timeout <= 0)
                throw new ArgumentOutOfRangeException(nameof(timeout));

            if (isCompletedFunc())
                return false;

            double startTimestamp = EditorApplication.timeSinceStartup;
            bool timedOut = false;

            while (true)
            {
                if (isCompletedFunc())
                    break;

                if (EditorApplication.timeSinceStartup - startTimestamp > timeout)
                {
                    timedOut = true;
                    break;
                }

                await Task.Delay(200);
            }

            return timedOut;
        }
    }
}
