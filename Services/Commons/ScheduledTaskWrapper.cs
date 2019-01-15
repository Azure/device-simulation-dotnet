using System;
using System.Collections.Generic;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Commons
{
    /// <summary>
    /// Scheduled Task Wrapper
    /// </summary>
    public class ScheduledTaskWrapper : IComparer<ScheduledTaskWrapper>, IComparable<ScheduledTaskWrapper>
    {
        /// <summary>
        /// Task execution interval
        /// </summary>
        private readonly int interval;

        /// <summary>
        /// Action to be executed
        /// </summary>
        private readonly Action action;

        /// <summary>
        /// Scheduled time to run
        /// </summary>
        private DateTime scheduledTimeToRun = DateTime.Now;

        /// <summary>
        /// Task id
        /// </summary>
        private Guid id;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduledTaskWrapper"/> class
        /// </summary>
        public ScheduledTaskWrapper()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduledTaskWrapper"/> class
        /// </summary>
        /// <param name="action">Action to be executed</param>
        /// <param name="scheduledTimeToRun">Scheduled running time</param>
        /// <param name="interval">Action execution interval</param>
        public ScheduledTaskWrapper(Action action, DateTime scheduledTimeToRun, int interval)
        {
            this.action = action;
            this.scheduledTimeToRun = scheduledTimeToRun;
            this.interval = interval;
            this.Id = Guid.NewGuid();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduledTaskWrapper"/> class
        /// </summary>
        /// <param name="action">Action to be executed</param>
        /// <param name="interval">Action execution interval</param>
        public ScheduledTaskWrapper(Action action, int interval)
            : this(action, DateTime.Now.AddMilliseconds(interval), interval)
        {
        }

        /// <summary>
        /// Gets or sets scheduled time
        /// </summary>
        public DateTime ScheduledTimeToRun
        {
            get { return this.scheduledTimeToRun; }
            set { this.scheduledTimeToRun = value; }
        }

        /// <summary>
        /// Gets interval
        /// </summary>
        public int Interval
        {
            get { return this.interval; }
        }

        /// <summary>
        /// Gets action
        /// </summary>
        public Action Action
        {
            get { return this.action; }
        }

        /// <summary>
        /// Gets Task id
        /// </summary>
        public Guid Id
        {
            get { return this.id; }
            private set { this.id = value; }
        }

        /// <summary>
        /// Compare two scheduled task wrapper.
        /// </summary>
        /// <param name="x">Wrapper a to be compared</param>
        /// <param name="y">Wrapper b to be compared</param>
        /// <returns>0 if a equals  b, 1 if a greater than b, -1 if a less than b</returns>
        public int Compare(ScheduledTaskWrapper x, ScheduledTaskWrapper y)
        {
            return x.ScheduledTimeToRun.CompareTo(y.ScheduledTimeToRun);
        }

        /// <summary>
        /// Compare this wrapper to other
        /// </summary>
        /// <param name="other">the other wrapper to be compared</param>
        /// <returns>0 if a equals b, 1 if a greater than b, -1 if a less than b</returns>
        public int CompareTo(ScheduledTaskWrapper other)
        {
            return this.Compare(this, other);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (ReferenceEquals(obj, null))
            {
                return false;
            }

            if (!(obj is ScheduledTaskWrapper))
            {
                return false;
            }

            return this.ScheduledTimeToRun == ((ScheduledTaskWrapper)obj).ScheduledTimeToRun;
        }

        public override int GetHashCode()
        {
            return this.ScheduledTimeToRun.GetHashCode();
        }

        public static bool operator ==(ScheduledTaskWrapper left, ScheduledTaskWrapper right)
        {
            if (ReferenceEquals(left, null))
            {
                return ReferenceEquals(right, null);
            }

            return left.Equals(right);
        }

        public static bool operator !=(ScheduledTaskWrapper left, ScheduledTaskWrapper right)
        {
            return !(left == right);
        }

        public static bool operator <(ScheduledTaskWrapper left, ScheduledTaskWrapper right)
        {
            return ReferenceEquals(left, null) ? !ReferenceEquals(right, null) : left.CompareTo(right) < 0;
        }

        public static bool operator <=(ScheduledTaskWrapper left, ScheduledTaskWrapper right)
        {
            return ReferenceEquals(left, null) || left.CompareTo(right) <= 0;
        }

        public static bool operator >(ScheduledTaskWrapper left, ScheduledTaskWrapper right)
        {
            return !ReferenceEquals(left, null) && left.CompareTo(right) > 0;
        }

        public static bool operator >=(ScheduledTaskWrapper left, ScheduledTaskWrapper right)
        {
            return ReferenceEquals(left, null) ? ReferenceEquals(right, null) : left.CompareTo(right) >= 0;
        }
    }
}
