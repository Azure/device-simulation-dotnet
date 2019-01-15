using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Commons
{
    /// <summary>
    /// Async scheduled Task Executor class
    /// </summary>
    public static class AsyncScheduledTasksExecutor
    {
        /// <summary>
        /// sync object
        /// </summary>
        private static readonly object SyncObject = new object();

        /// <summary>
        /// Timer for task
        /// </summary>
        private static readonly Timer TaskTimer = new Timer(Execute, null, Timeout.Infinite, Timeout.Infinite);

        /// <summary>
        /// List of scheduled tasks
        /// </summary>
        private static readonly SortedSet<AsyncScheduledTaskWrapper> ScheduledTasks = new SortedSet<AsyncScheduledTaskWrapper>(new AsyncScheduledTaskWrapper());

        /// <summary>
        /// Schedule a task running on specified time with defined interval.
        /// </summary>
        /// <param name="function">Async function to be executed</param>
        /// <param name="scheduledTimeToRun">Specified time</param>
        /// <param name="interval">Interval for task running</param>
        public static void ScheduleTask(Func<Task> function, DateTime scheduledTimeToRun, int interval)
        {
            var task = new AsyncScheduledTaskWrapper(function, scheduledTimeToRun, interval);
            ScheduleTask(task);
        }

        /// <summary>
        /// Schedule a task running with defined interval.
        /// </summary>
        /// <param name="function">Function to be executed</param>
        /// <param name="interval">Interval for task running</param>
        public static void ScheduleTask(Func<Task> function, int interval)
        {
            ScheduleTask(function, DateTime.Now.AddMilliseconds(interval), interval);
        }

        /// <summary>
        /// Schedule a task
        /// </summary>
        /// <param name="scheduledTask">Scheduled task wrapper</param>
        public static void ScheduleTask(AsyncScheduledTaskWrapper scheduledTask)
        {
            lock (SyncObject)
            {
                while (ScheduledTasks.Contains(scheduledTask))
                {
                    scheduledTask.ScheduledTimeToRun = scheduledTask.ScheduledTimeToRun.AddTicks(3);
                }

                ScheduledTasks.Add(scheduledTask);

                if (ScheduledTasks.Count > 1)
                {
                    var min = ScheduledTasks.Min;

                    if (min.CompareTo(scheduledTask) == 0)
                    {
                        var dueTime = (long)scheduledTask.ScheduledTimeToRun.Subtract(DateTime.Now).TotalMilliseconds;
                        TaskTimer.Change(dueTime > 0 ? dueTime : 0, Timeout.Infinite);
                    }
                }
                else
                {
                    var dueTime = (long)scheduledTask.ScheduledTimeToRun.Subtract(DateTime.Now).TotalMilliseconds;
                    TaskTimer.Change(dueTime > 0 ? dueTime : 0, Timeout.Infinite);
                }
            }
        }

        /// <summary>
        /// Retrieve a scheduled task wrapper.
        /// </summary>
        /// <returns>Scheduled Task Wrapper</returns>
        private static AsyncScheduledTaskWrapper Retrieve()
        {
            var item = ScheduledTasks.Min;

            if (item != null)
            {
                ScheduledTasks.Remove(item);
            }

            return item;
        }

        /// <summary>
        /// Peek a scheduled task wrapper.
        /// </summary>
        /// <returns>Scheduled Task Wrapper</returns>
        private static AsyncScheduledTaskWrapper Peek()
        {
            return ScheduledTasks.Min;
        }

        /// <summary>
        /// Execute scheduled task
        /// </summary>
        /// <param name="state">task state</param>
        private static void Execute(object state)
        {
            lock (SyncObject)
            {
                var nextFunctionToRun = Peek();

                while (nextFunctionToRun != null && nextFunctionToRun.ScheduledTimeToRun.CompareTo(DateTime.Now) < 0)
                {
                    var item = Retrieve();
#pragma warning disable CA2008 // Do not create tasks without passing a TaskScheduler
                    Task.Factory.StartNew(
                        async () =>
                        {
                            try
                            {
                                await item.Function();
                                if (item.Interval > 0)
                                {
                                    ScheduleTask(item.Function, item.Interval);
                                }
                            }
                            catch
                            {
                            }
                        });
#pragma warning restore CA2008 // Do not create tasks without passing a TaskScheduler

                    nextFunctionToRun = Peek();
                }

                if (nextFunctionToRun == null)
                {
                    return;
                }

                var dueTime = (long)nextFunctionToRun.ScheduledTimeToRun.Subtract(DateTime.Now).TotalMilliseconds;
                TaskTimer.Change(dueTime > 0 ? dueTime : 0, Timeout.Infinite);
            }
        }
    }
}