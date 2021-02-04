using ByteFlow.Asyncs;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace ByteFlow
{
    public class ScheduleTaskManager
    {
        private readonly ConcurrentDictionary<string, ScheduleTaskInfo> _scheduledTasks = new();

        /// <summary>
        /// 用于安排一个定时任务，比如定时发送心跳包等等。连接关闭时，子类无需停止任务，该工作由此父类完成
        /// </summary>
        /// <param name="taskUniqueName">此任务的唯一名称</param>
        /// <param name="task">任务具体的内容</param>
        /// <param name="interval">任务执行的时间间隔</param>
        /// <returns>添加成功，返回True；否则，返回False</returns>
        public bool ScheduleTask(string taskUniqueName, Action<TimeSpan> task, TimeSpan interval)
        {
            if (string.IsNullOrWhiteSpace(taskUniqueName))
            {
                throw new ArgumentException($"{nameof(taskUniqueName)} cannot be null or empty.");
            }

            if (this._scheduledTasks.ContainsKey(taskUniqueName))
            {
                return false;
            }


            var taskItem = new ScheduleTaskInfo(taskUniqueName, task, interval);
            taskItem.Start();
            return this._scheduledTasks.TryAdd(taskUniqueName, taskItem);
        }

        /// <summary>
        /// 停止指定的任务
        /// </summary>
        /// <param name="taskUniqueName">待停止的任务的名称</param>
        /// <returns>成功，返回True；否则，返回False</returns>
        public bool StopTask(string taskUniqueName)
        {
            if (string.IsNullOrWhiteSpace(taskUniqueName))
            {
                throw new ArgumentException($"{nameof(taskUniqueName)} cannot be null or empty.");
            }

            if (!this._scheduledTasks.ContainsKey(taskUniqueName))
            {
                return false;
            }

            var task = this._scheduledTasks[taskUniqueName];
            this._scheduledTasks.TryRemove(taskUniqueName, out var _);
            task.Stop();
            return true;
        }

        /// <summary>
        /// 停止所有任务
        /// </summary>
        public void StopAllTasks()
        {
            var tasks = this._scheduledTasks.Values.ToList();
            this._scheduledTasks.Clear();
            foreach (var task in tasks)
            {
                task.Stop();
            }
        }
    }

    internal class ScheduleTaskInfo
    {
        public readonly string Name;

        public readonly Action<TimeSpan> Task;

        public readonly TimeSpan Interval;

        private readonly TaskTimer _timer;

        public ScheduleTaskInfo(string name, Action<TimeSpan> task, TimeSpan interval)
        {
            this.Name = name;
            this.Task = task;
            this.Interval = interval;
            this._timer = new TaskTimer();
            this._timer.Tick += this.OnTimerTick;
        }

        private void OnTimerTick(TimeSpan duration) => this.Task.Invoke(duration);

        public void Start() => this._timer.Start(this.Interval);

        public void Stop()
        {
            this._timer.Tick -= this.OnTimerTick;
            this._timer.Stop();
        }
    }
}
