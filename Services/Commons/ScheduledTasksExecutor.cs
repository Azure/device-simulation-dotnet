using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Commons
{
    /// <summary>
    /// Scheduled Task Executor class
    /// </summary>
    public static class ScheduledTasksExecutor
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
        private static readonly SortedSet<ScheduledTaskWrapper> ScheduledTasks = new SortedSet<ScheduledTaskWrapper>(new ScheduledTaskWrapper());

        /// <summary>
        /// Schedule a task running on specified time with defined interval.
        /// </summary>
        /// <param name="action">Action to be executed</param>
        /// <param name="scheduledTimeToRun">Specified time</param>
        /// <param name="interval">Interval for task running</param>
        public static void ScheduleTask(Action action, DateTime scheduledTimeToRun, int interval)
        {
            var task = new ScheduledTaskWrapper(action, scheduledTimeToRun, interval);
            ScheduleTask(task);
        }

        /// <summary>
        /// Schedule a task running with defined interval.
        /// </summary>
        /// <param name="action">Action to be executed</param>
        /// <param name="interval">Interval for task running</param>
        public static void ScheduleTask(Action action, int interval)
        {
            ScheduleTask(action, DateTime.Now.AddMilliseconds(interval), interval);
        }

        /// <summary>
        /// Schedule a task
        /// </summary>
        /// <param name="scheduledTask">Scheduled task wrapper</param>
        public static void ScheduleTask(ScheduledTaskWrapper scheduledTask)
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
        private static ScheduledTaskWrapper Retrieve()
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
        private static ScheduledTaskWrapper Peek()
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
                var nextActionToRun = Peek();

                while (nextActionToRun != null && nextActionToRun.ScheduledTimeToRun.CompareTo(DateTime.Now) < 0)
                {
                    var item = Retrieve();
                    var t = Task.Factory.StartNew(
                        () =>
                        {
                            try
                            {
                                item.Action();
                            }
                            catch
                            {
                            }
                        });

                    // Reschedule itself
                    if (item.Interval > 0)
                    {
                        t.ContinueWith(task => ScheduleTask(item.Action, item.Interval));
                    }

                    nextActionToRun = Peek();
                }

                if (nextActionToRun != null)
                {
                    var dueTime = (long)nextActionToRun.ScheduledTimeToRun.Subtract(DateTime.Now).TotalMilliseconds;
                    TaskTimer.Change(dueTime > 0 ? dueTime : 0, Timeout.Infinite);
                }
            }
        }
    }
}
