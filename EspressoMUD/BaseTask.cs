using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD
{
    public abstract class BaseTask : IComparable<BaseTask>
    {
        private bool started = false;
        private bool canceled = false;
        internal DateTime scheduledTime;


        protected BaseTask(DateTime scheduledTime)
        {
            this.scheduledTime = scheduledTime;
        }

        public bool HasStarted()
        {
            return started;
        }
        public bool IsCanceled()
        {
            return canceled;
        }
        /// <summary>
        /// Attempt to cancel this task before it is started. Tasks that have already been started will not be canceled.
        /// </summary>
        /// <param name="alreadyCanceled">If the task was canceled before this was called.</param>
        /// <returns>If the task is canceled.</returns>
        public virtual bool Cancel(out bool alreadyCanceled)
        {
            lock (this)
            {
                alreadyCanceled = canceled;
                if (started) return false;
                canceled = true;
            }
            return true;
        }

        internal bool Start()
        {
            lock (this)
            {
                if (canceled || started) return false;
                started = true;
            }
            try
            {
                PerformTask();
            }
            catch (Exception e)
            {
                //TODO: Log exception
            }
            return true;
        }
        protected abstract void PerformTask();


        public int CompareTo(BaseTask other)
        {
            return scheduledTime.CompareTo(other.scheduledTime);
        }
    }
}
