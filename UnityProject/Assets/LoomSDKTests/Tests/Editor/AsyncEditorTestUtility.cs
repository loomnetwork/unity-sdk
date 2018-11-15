using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

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
    }
}
