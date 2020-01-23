using KejUtils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EspressoMUD
{
    public static class TickManager
    {
        private const int NumberOfSets = 64;
        private const long MillisecondsPerSet = 100;
        public const int NowLimit = 5; // Events happening less than 5 ms from now are just fired immediately.

        private static long StartOffset;
        private static MovingConcurrentSet[] TickSets = new MovingConcurrentSet[NumberOfSets];
        private static ConcurrentBag<FuturedTask> ImmediateTickSet = new ConcurrentBag<FuturedTask>();
        //private static ConcurrentBag<FuturedTask> FutureTickSet = new ConcurrentBag<FuturedTask>();
        private static DateTime NextWakeup; //This isn't volatile, but that should be okay. If it's out of date, threads might be a little more inefficient, but nothing should break.
        private static ManualResetEvent MaySleep = new ManualResetEvent(false);
        private static CircularList<FuturedTask> TasksToRunSoon = new CircularList<FuturedTask>();
        //private static FuturedTask SearchTask = new FuturedTask(null, default(DateTime));

        static TickManager()
        {
            for (int i = 0; i < NumberOfSets; i++)
            {
                TickSets[i] = new MovingConcurrentSet();
                TickSets[i].scheduleOffset = i;
            }
            StartOffset = DateTime.UtcNow.Ticks;
        }
        public static void StartTickManager()
        {
            new Thread(RunTickManager).Start();
        }
        
        private static void RunOrQueue(FuturedTask task)
        {
            if (task.IsCanceled()) return;
            if ((task.scheduledTime - DateTime.UtcNow).TotalMilliseconds < NowLimit && !ThreadManager.MUDIsPaused)
            {
                ThreadPool.QueueUserWorkItem(RunTask, task);
            }
            else
            {
                int index = TasksToRunSoon.BinarySearch(task);
                if (index < 0) index = ~index;
                TasksToRunSoon.Insert(index, task);
            }
        }
        private static void RunTask(object task)
        {
            //TODO: Try/catch here for exception logging or similar things
            FuturedTask taskToRun = (FuturedTask)task;
            taskToRun.Start();
        }

        public static BaseTask Add(Action callback, DateTime time)
        {
            FuturedTask task = new FuturedTask(callback, time);
            Add(task);
            return task;
        }

        private static void RunTickManager()
        {
            NextWakeup = DateTime.UtcNow;
            int nextSet = 0;
            while (!Program.ShutdownTrigger.WaitOne(0))
            {
                DateTime currentTime = DateTime.UtcNow;
                FuturedTask nextTask;
                while (ImmediateTickSet.TryTake(out nextTask))
                {
                    RunOrQueue(nextTask);
                }
                nextTask = null;
                while (!ThreadManager.MUDIsPaused && TasksToRunSoon.Count > 0)
                {
                    nextTask = TasksToRunSoon[0];
                    if (nextTask.scheduledTime > currentTime + new TimeSpan(0, 0, 0, 0, NowLimit)) break;
                    TasksToRunSoon.RemoveAt(0);
                    if (nextTask.IsCanceled()) continue;
                    ThreadPool.QueueUserWorkItem(RunTask, nextTask);
                }
                //Floor of division, but +1. Gets all the sets that might have things to run right now.
                long rawSetIndex = 1 + ((currentTime.Ticks - StartOffset) / TimeSpan.TicksPerMillisecond / MillisecondsPerSet);
                int setIndex = (int)rawSetIndex;
                //int setToAddTo = setIndex % NumberOfSets;
                while (setIndex > nextSet)
                {
                    MovingConcurrentSet setToSwitch = TickSets[nextSet % NumberOfSets];
                    setToSwitch.switching = true;
                    while (setToSwitch.set.TryTake(out nextTask))
                    {
                        RunOrQueue(nextTask);
                    }

                    while (setToSwitch.busy > 0)
                    {
                        Thread.Sleep(new TimeSpan(TimeSpan.TicksPerMillisecond / 10));
                    }
                    
                    setToSwitch.scheduleOffset += NumberOfSets;
                    setToSwitch.switching = false;
                    nextSet++;
                }
                int delay;
                if (TasksToRunSoon.Count > 0)
                {
                    delay = (int)(TasksToRunSoon[0].scheduledTime - DateTime.UtcNow).TotalMilliseconds;
                }
                else
                {
                    delay = (int)((nextSet * MillisecondsPerSet) -
                        (DateTime.UtcNow.Ticks - StartOffset) / TimeSpan.TicksPerMillisecond);
                }
                
                if (delay > 0)
                {
                    //Wait until next timer.
                    MaySleep.WaitOne(delay); //Don't really care why we woke up, either our own alarm or another thread.
                }
            }
        }

        private static void AddToImmediate(FuturedTask task)
        {
            ImmediateTickSet.Add(task);
            if (task.scheduledTime < NextWakeup)
            {
                MaySleep.Set();
            }
        }
        private static void Add(FuturedTask task)
        {
            long tillTask = (task.scheduledTime - DateTime.UtcNow).Ticks;
            if (tillTask < NowLimit)
            {
                AddToImmediate(task);
            }
            else  //Could have another else if here for putting stuff into the future set right away, but that should be uncommon enough that it doesn't need to be checked here.
            {
                long rawSetIndex = (task.scheduledTime.Ticks - StartOffset) / TimeSpan.TicksPerMillisecond / MillisecondsPerSet;
                int setIndex = (int)rawSetIndex;
                int setToAddTo = setIndex % NumberOfSets;
                //int setOffset = (int)(setIndex) / NumberOfSets; ? Don't *really* need this.

                int success = TickSets[setToAddTo].Add(task, setIndex);
                if (success < 0)
                {
                    AddToImmediate(task);
                }
                else if (success > 0)
                {
                    //TODO: add to 'future set' instead? Add to now works okay but isn't ideal if there are many long-term waits.
                    AddToImmediate(task);
                }
            }
        }


        private class MovingConcurrentSet
        {
            public ConcurrentBag<FuturedTask> set = new ConcurrentBag<FuturedTask>();
            public int scheduleOffset; //From some quick math, this should last some years before being a problem and needing longs.
            public volatile int busy;
            public bool switching;


            /// <summary>
            /// Try to add the task to this queue.
            /// </summary>
            /// <param name="task"></param>
            /// <param name="expectedOffset"></param>
            /// <returns>negative if the task is too soon, positive if the task is too far in the future. 0 if the task has been added to this queue.</returns>
            public int Add(FuturedTask task, int expectedOffset)
            {
                if (switching)
                {
                    int diff = scheduleOffset - expectedOffset;
                    return diff == 0 ? -1 : diff;
                }
                else
                {
                    busy++;
                    int diff = scheduleOffset - expectedOffset;
                    if (diff == 0)
                    {
                        set.Add(task);
                    }
                    busy--;
                    return diff;
                }
            }
        }

        private class FuturedTask : BaseTask
        {
            private Action callback;
            //private bool finished //don't need this.
            
            protected override void PerformTask() { callback(); }
            
            public FuturedTask(Action callback, DateTime scheduledTime) : base(scheduledTime)
            {
                this.callback = callback;
            }
        }
    }
}
