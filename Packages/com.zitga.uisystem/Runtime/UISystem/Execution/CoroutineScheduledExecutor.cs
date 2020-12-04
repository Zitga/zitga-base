/*
 * MIT License
 *
 * Copyright (c) 2018 Clark Yang
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of 
 * this software and associated documentation files (the "Software"), to deal in 
 * the Software without restriction, including without limitation the rights to 
 * use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies 
 * of the Software, and to permit persons to whom the Software is furnished to do so, 
 * subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all 
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE 
 * SOFTWARE.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Loxodon.Framework.Asynchronous;
using Loxodon.Log;
using UnityEngine;
using IAsyncResult = Loxodon.Framework.Asynchronous.IAsyncResult;

namespace Loxodon.Framework.Execution
{
    public class CoroutineScheduledExecutor : AbstractExecutor, IScheduledExecutor
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(CoroutineScheduledExecutor));

        private readonly ComparerImpl<IDelayTask> comparer = new ComparerImpl<IDelayTask>();
        private readonly List<IDelayTask> queue = new List<IDelayTask>();
        private bool running;

        public void Start()
        {
            if (running)
                return;

            running = true;

            InterceptableEnumerator ie = new InterceptableEnumerator(DoStart());
            ie.RegisterCatchBlock(e => { running = false; });
            Executors.RunOnCoroutineNoReturn(ie);
        }

        public void Stop()
        {
            if (!running)
                return;

            running = false;
            var list = new List<IDelayTask>(queue);
            foreach (IDelayTask task in list)
                if (task != null && !task.IsDone)
                    task.Cancel();
            queue.Clear();
        }

        public virtual IAsyncResult Schedule(Action command, long delay)
        {
            return Schedule(command, new TimeSpan(delay * TimeSpan.TicksPerMillisecond));
        }

        public virtual IAsyncResult Schedule(Action command, TimeSpan delay)
        {
            Check();
            return new OneTimeDelayTask(this, command, delay);
        }

        public virtual IAsyncResult<TResult> Schedule<TResult>(Func<TResult> command, long delay)
        {
            return Schedule(command, new TimeSpan(delay * TimeSpan.TicksPerMillisecond));
        }

        public virtual IAsyncResult<TResult> Schedule<TResult>(Func<TResult> command, TimeSpan delay)
        {
            Check();
            return new OneTimeDelayTask<TResult>(this, command, delay);
        }

        public virtual IAsyncResult ScheduleAtFixedRate(Action command, long initialDelay, long period)
        {
            return ScheduleAtFixedRate(command, new TimeSpan(initialDelay * TimeSpan.TicksPerMillisecond),
                new TimeSpan(period * TimeSpan.TicksPerMillisecond));
        }

        public virtual IAsyncResult ScheduleAtFixedRate(Action command, TimeSpan initialDelay, TimeSpan period)
        {
            Check();
            return new FixedRateDelayTask(this, command, initialDelay,
                period);
        }

        public virtual IAsyncResult ScheduleWithFixedDelay(Action command, long initialDelay, long delay)
        {
            return ScheduleWithFixedDelay(command, new TimeSpan(initialDelay * TimeSpan.TicksPerMillisecond),
                new TimeSpan(delay * TimeSpan.TicksPerMillisecond));
        }

        public virtual IAsyncResult ScheduleWithFixedDelay(Action command, TimeSpan initialDelay, TimeSpan delay)
        {
            Check();
            return new FixedDelayDelayTask(this, command, initialDelay,
                delay);
        }

        public virtual void Dispose()
        {
            Stop();
        }

        private void Add(IDelayTask task)
        {
            queue.Add(task);
            queue.Sort(comparer);
        }

        private bool Remove(IDelayTask task)
        {
            if (queue.Remove(task))
            {
                queue.Sort(comparer);
                return true;
            }

            return false;
        }

        protected virtual IEnumerator DoStart()
        {
            while (running)
            {
                while (running && (queue.Count <= 0 || queue[0].Delay.Ticks > 0)) yield return null;

                if (!running)
                    yield break;

                IDelayTask task = queue[0];
                queue.RemoveAt(0);
                task.Run();
            }
        }

        protected virtual void Check()
        {
            if (!running)
                throw new RejectedExecutionException("The ScheduledExecutor isn't started.");
        }

        private interface IDelayTask : IAsyncResult
        {
            TimeSpan Delay { get; }

            void Run();
        }

        private class OneTimeDelayTask : AsyncResult, IDelayTask
        {
            private readonly Action command;
            private TimeSpan delay;
            private readonly CoroutineScheduledExecutor executor;
            private readonly long startTime;

            public OneTimeDelayTask(CoroutineScheduledExecutor executor, Action command, TimeSpan delay)
            {
                startTime = (long) (Time.fixedTime * TimeSpan.TicksPerSecond);
                this.delay = delay;
                this.executor = executor;
                this.command = command;
                this.executor.Add(this);
            }

            public virtual TimeSpan Delay =>
                new TimeSpan(startTime + delay.Ticks - (long) (Time.fixedTime * TimeSpan.TicksPerSecond));

            public override bool Cancel()
            {
                if (IsDone)
                    return false;

                if (!executor.Remove(this))
                    return false;

                cancellationRequested = true;
                SetCancelled();
                return true;
            }

            public virtual void Run()
            {
                try
                {
                    if (IsDone)
                        return;

                    if (IsCancellationRequested)
                    {
                        SetCancelled();
                    }
                    else
                    {
                        command();
                        SetResult();
                    }
                }
                catch (Exception e)
                {
                    SetException(e);
                    if (log.IsWarnEnabled)
                        log.WarnFormat("{0}", e);
                }
            }
        }

        private class OneTimeDelayTask<TResult> : AsyncResult<TResult>, IDelayTask
        {
            private readonly Func<TResult> command;
            private TimeSpan delay;
            private readonly CoroutineScheduledExecutor executor;
            private readonly long startTime;

            public OneTimeDelayTask(CoroutineScheduledExecutor executor, Func<TResult> command, TimeSpan delay)
            {
                startTime = (long) (Time.fixedTime * TimeSpan.TicksPerSecond);
                this.delay = delay;
                this.executor = executor;
                this.command = command;
                this.executor.Add(this);
            }

            public virtual TimeSpan Delay =>
                new TimeSpan(startTime + delay.Ticks - (long) (Time.fixedTime * TimeSpan.TicksPerSecond));

            public override bool Cancel()
            {
                if (IsDone)
                    return false;

                if (!executor.Remove(this))
                    return false;

                cancellationRequested = true;
                SetCancelled();
                return true;
            }

            public virtual void Run()
            {
                try
                {
                    if (IsDone)
                        return;

                    if (IsCancellationRequested)
                        SetCancelled();
                    else
                        SetResult(command());
                }
                catch (Exception e)
                {
                    SetException(e);
                    if (log.IsWarnEnabled)
                        log.WarnFormat("{0}", e);
                }
            }
        }

        private class FixedRateDelayTask : AsyncResult, IDelayTask
        {
            private readonly Action command;
            private int count;
            private readonly CoroutineScheduledExecutor executor;
            private TimeSpan initialDelay;
            private TimeSpan period;
            private readonly long startTime;

            public FixedRateDelayTask(CoroutineScheduledExecutor executor, Action command, TimeSpan initialDelay,
                TimeSpan period)
            {
                startTime = (long) (Time.fixedTime * TimeSpan.TicksPerSecond);
                this.initialDelay = initialDelay;
                this.period = period;
                this.executor = executor;
                this.command = command;
                this.executor.Add(this);
            }

            public virtual TimeSpan Delay => new TimeSpan(startTime + initialDelay.Ticks + period.Ticks * count -
                                                          (long) (Time.fixedTime * TimeSpan.TicksPerSecond));

            public override bool Cancel()
            {
                if (IsDone)
                    return false;

                executor.Remove(this);
                cancellationRequested = true;
                SetCancelled();
                return true;
            }

            public virtual void Run()
            {
                try
                {
                    if (IsDone)
                        return;

                    if (IsCancellationRequested)
                    {
                        SetCancelled();
                    }
                    else
                    {
                        Interlocked.Increment(ref count);
                        executor.Add(this);
                        command();
                    }
                }
                catch (Exception e)
                {
                    if (log.IsWarnEnabled)
                        log.WarnFormat("{0}", e);
                }
            }
        }

        private class FixedDelayDelayTask : AsyncResult, IDelayTask
        {
            private readonly Action command;
            private TimeSpan delay;
            private readonly CoroutineScheduledExecutor executor;
            private long nextTime;

            public FixedDelayDelayTask(CoroutineScheduledExecutor executor, Action command, TimeSpan initialDelay,
                TimeSpan delay)
            {
                this.delay = delay;
                this.executor = executor;
                this.command = command;
                nextTime = (long) (Time.fixedTime * TimeSpan.TicksPerSecond + initialDelay.Ticks);
                this.executor.Add(this);
            }

            public virtual TimeSpan Delay => new TimeSpan(nextTime - (long) (Time.fixedTime * TimeSpan.TicksPerSecond));

            public override bool Cancel()
            {
                if (IsDone)
                    return false;

                executor.Remove(this);
                cancellationRequested = true;
                SetCancelled();
                return true;
            }

            public virtual void Run()
            {
                try
                {
                    if (IsDone)
                        return;

                    if (IsCancellationRequested)
                        SetCancelled();
                    else
                        command();
                }
                catch (Exception e)
                {
                    if (log.IsWarnEnabled)
                        log.WarnFormat("{0}", e);
                }
                finally
                {
                    if (IsCancellationRequested)
                    {
                        SetCancelled();
                    }
                    else
                    {
                        nextTime = (long) (Time.fixedTime * TimeSpan.TicksPerSecond + delay.Ticks);
                        executor.Add(this);
                    }
                }
            }
        }

        private class ComparerImpl<T> : IComparer<T> where T : IDelayTask
        {
            public int Compare(T x, T y)
            {
                if (x.Delay.Ticks == y.Delay.Ticks)
                    return 0;

                return x.Delay.Ticks > y.Delay.Ticks ? 1 : -1;
            }
        }
    }
}