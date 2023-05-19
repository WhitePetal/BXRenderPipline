using System;
using System.Collections;
using System.Collections.Generic;
using System.Timers;

namespace CityBuilder
{
    public class BTimer
    {
        public enum LogLevel
        {
            Info,
            Log,
            Warning,
            Error
        }
        public delegate void TaskLog(string str, LogLevel logLevel = LogLevel.Log);

        private TaskLog taskLog;

        private int id;
        private Dictionary<int, TaskFlag> idDic = new Dictionary<int, TaskFlag>();

        private DateTime startDateTime = new DateTime(1970, 1, 1, 0, 0, 0);
        private double nowTime;

        private const int recTaskListMemCount = 1000;

        private int timeTaskListCount;
        private List<TimeTask> timeTaskList = new List<TimeTask>();

        private int frameTaskListCount;
        private List<FrameTask> frameTaskList = new List<FrameTask>();


        public void ResetTimer()
        {
            idDic.Clear();
            timeTaskList.Clear();
            frameTaskList.Clear();
            id = 0;
        }

        public void SetLog(TaskLog taskLog)
        {
            this.taskLog = taskLog;
        }
        private void LogInfo(string str, LogLevel logLevel)
        {
            if(taskLog != null) taskLog.Invoke(str, logLevel);
        }

        public IDPack AddTimeTask(Action<int> callBack, double delay, int count = 1, TimeUnit unit = TimeUnit.Secound)
        {
            switch(unit)
            {
                case TimeUnit.Millisecound:
                    break;
                case TimeUnit.Secound:
                    delay *= 1000;
                    break;
                case TimeUnit.Minute:
                    delay *= 1000 * 60;
                    break;
                case TimeUnit.Hour:
                    delay *= 1000 * 60 * 60;
                    break;
                case TimeUnit.Day:
                    delay *= 1000 * 60 * 60 * 24;
                    break;
            }

            nowTime = GetUTCMilliseconds();
            double destTime = nowTime + delay;

            int id = GetId();
            if (id == -1) return new IDPack(id, TaskType.TimeTask);

            idDic[id] = new TaskFlag(idDic[id], TaskType.TimeTask);
            TimeTask task = new TimeTask
            {
                id = id,
                delay = delay,
                destTime = destTime,
                callBack = callBack,
                count = count
            };

            timeTaskList.Add(task);
            // 加入后，可能前面都是些已经被销毁的任务
            // 因此需要将其交换到 timeTaskListCount 指针后，然后更新指针
            if (timeTaskListCount != timeTaskList.Count)
            {
                int index = timeTaskListCount;
                int last = timeTaskList.Count - 1;
                TimeTask temp = timeTaskList[last];
                timeTaskList[last] = timeTaskList[index];
                timeTaskList[index] = temp;
            }
            ++timeTaskListCount;
            return new IDPack(id, TaskType.TimeTask);
        }

        public bool ReplaceTimeTask(int id, Action<int> callBack, double delay, int count = 1, TimeUnit unit = TimeUnit.Secound)
        {
            switch (unit)
            {
                case TimeUnit.Millisecound:
                    break;
                case TimeUnit.Secound:
                    delay *= 1000;
                    break;
                case TimeUnit.Minute:
                    delay *= 1000 * 60;
                    break;
                case TimeUnit.Hour:
                    delay *= 1000 * 60 * 60;
                    break;
                case TimeUnit.Day:
                    delay *= 1000 * 60 * 60 * 24; // 最大支持 24天
                    break;
            }
            nowTime = GetUTCMilliseconds();
            double destTime = nowTime + delay;
            TimeTask task = new TimeTask
            {
                id = id,
                delay = delay,
                destTime = destTime,
                callBack = callBack,
                count = count,
            };

            if (idDic.ContainsKey(id) && idDic[id].active)
            {
                timeTaskList[idDic[id].index] = task;
                return true;
            }

            return false;
        }

        public IDPack AddFrameTask(Action<int> callBack, int delay, int count = 1)
        {
            int id = GetId();
            if (id == -1) return new IDPack(id, TaskType.FrameTask);

            idDic[id] = new TaskFlag(idDic[id], TaskType.FrameTask);
            FrameTask task = new FrameTask
            {
                id = id,
                curFrame = 0,
                delay = delay,
                callBack = callBack,
                count = count
            };

            frameTaskList.Add(task);
            // 加入后，可能前面都是些已经被销毁的任务
            // 因此需要将其交换到 frameTaskListCount 指针后，然后更新指针
            if ((frameTaskListCount + 1) != frameTaskList.Count)
            {
                int index = frameTaskListCount;
                int last = frameTaskList.Count - 1;
                FrameTask temp = frameTaskList[last];
                frameTaskList[last] = frameTaskList[index];
                frameTaskList[index] = temp;
            }
            ++frameTaskListCount;
            return new IDPack(id, TaskType.FrameTask);
        }

        public bool ReplaceFrameTask(int id, Action<int> callBack, int delay, int count = 1)
        {
            FrameTask task = new FrameTask
            {
                id = id,
                curFrame = 0,
                delay = delay,
                callBack = callBack,
                count = count
            };

            if (idDic.ContainsKey(id) && idDic[id].active)
            {
                frameTaskList[idDic[id].index] = task;
                return true;
            }

            return false;
        }

        public double GetMillisecondsTime()
        {
            return nowTime;
        }
        public DateTime GetLocalDateTime()
        {
            DateTime dt = TimeZoneInfo.ConvertTimeFromUtc(startDateTime.AddMilliseconds(nowTime), TimeZoneInfo.Local);
            return dt;
        }
        public int GetYear()
        {
            return GetLocalDateTime().Year;
        }
        public int GetMonth()
        {
            return GetLocalDateTime().Month;
        }
        public int GetDay()
        {
            return GetLocalDateTime().Day;
        }
        public int GetWeek()
        {
            return (int)GetLocalDateTime().DayOfWeek;
        }
        public string GetLocalTimeStr()
        {
            DateTime dt = GetLocalDateTime();
            string str = GetTimeStr(dt.Hour) + ":" + GetTimeStr(dt.Minute) + ":" + GetTimeStr(dt.Second);
            return str;
        }

        public void Tick()
        {
            TimeTaskTick();
            FrameTaskTick();

            RecDelTimeTask();
            RecDelFrameTask();
        }

        private string GetTimeStr(int time)
        {
            if (time < 10) return "0" + time.ToString();
            else return time.ToString();
        }

        private int GetId()
        {
            id += 1;

            int len = 0;
            while (true)
            {
                if (id == int.MaxValue) id = 0;

                if (idDic.ContainsKey(id))
                    ++id;
                else
                    break;

                ++len;
                if (len == int.MaxValue)
                {
                    LogInfo("计时认为已满，无法添加任务", LogLevel.Error);
                    return -1;
                }
            }

            TaskFlag flag = new TaskFlag
            {
                id = id,
                active = true
            };
            idDic.Add(id, flag);
            return id;
        }

        private double GetUTCMilliseconds()
        {
            TimeSpan ts = DateTime.UtcNow - startDateTime;
            return ts.TotalMilliseconds;
        }

        private void TimeTaskTick()
        {
            nowTime = GetUTCMilliseconds();
            for(int i = 0; i < timeTaskListCount; ++i)
            {
                TimeTask task = timeTaskList[i];
                if (nowTime.CompareTo(task.destTime) < 0)
                    continue;
                else
                {
                    Action<int> cb = task.callBack;
                    try
                    {
                        cb.Invoke(task.id);
                    }
                    catch (Exception e)
                    {
                        LogInfo(e.Message + " === " + cb.ToString(), LogLevel.Error);
                    }
                }

                if (task.count == 1)
                    DeleteTimeTask(task.id);
                else
                {
                    if (task.count > 0)
                        --task.count;

                    task.destTime += task.delay;
                    timeTaskList[i] = task;
                }
            }
        }

        private void RecDelTimeTask()
        {
            if(timeTaskList.Count - timeTaskListCount >= recTaskListMemCount)
            {
                timeTaskList.RemoveRange(timeTaskListCount, timeTaskList.Count - timeTaskListCount);
                // 只是 Remove 元素，List 只会前移指针而不会实际释放内存
                // 需要调用 TrimExcess，强制释放内存，重制 List Capacity
                // 但实际上没必要主动调用 TrimExcess，列表末尾这块内存由 C# 自己管理即可
                //timeTaskList.TrimExcess();
            }
        }

        public void DeleteTimeTask(int id)
        {
            if(idDic.ContainsKey(id) && idDic[id].active)
            {
                RemoveTimeListItem(idDic[id].index);
            }
        }

        private void RemoveTimeListItem(int index)
        {
            if (timeTaskListCount == 0) return;

            TimeTask task = timeTaskList[index];
            idDic[task.id].SetActive(false);

            RemoveListItem_TimeTask(index);

            // 更新因为删除操作，交换到 index 位置的元素 下标
            if (index < timeTaskList.Count)
            {
                TimeTask indexTask = timeTaskList[index];
                TaskFlag flag = new TaskFlag
                {
                    id = indexTask.id,
                    index = index,
                    active = true
                };
                idDic[indexTask.id] = flag;
            }
        }

        private void RemoveListItem_TimeTask(int index)
        {
            int last = timeTaskListCount - 1;
            TimeTask temp = timeTaskList[index];
            timeTaskList[index] = timeTaskList[last];
            timeTaskList[last] = temp;
            --timeTaskListCount;
        }

        private void FrameTaskTick()
        {
            for (int i = 0; i < frameTaskListCount; ++i)
            {
                FrameTask task = frameTaskList[i];
                if (task.curFrame < task.delay)
                {
                    task.curFrame += 1;
                    frameTaskList[i] = task;
                    continue;
                }

                Action<int> cb = task.callBack;
                try
                {
                    if (cb != null) cb.Invoke(task.id);
                }
                catch (Exception e)
                {
                    LogInfo(e.ToString() + task.id, LogLevel.Error);
                }

                if (task.count == 1)
                {
                    DeleteFrameTask(task.id);
                }
                else
                {
                    task.curFrame = 0;
                    if (task.count > 0)
                    {
                        --task.count;
                    }
                    frameTaskList[i] = task;
                }
            }
        }

        private void RecDelFrameTask()
        {
            if (frameTaskList.Count - frameTaskListCount >= recTaskListMemCount)
            {
                frameTaskList.RemoveRange(frameTaskListCount, frameTaskList.Count - frameTaskListCount);
                // 只是 Remove 元素，List 只会前移指针而不会实际释放内存
                // 需要调用 TrimExcess，强制释放内存，重制 List Capacity
                // 但实际上没必要主动调用 TrimExcess，列表末尾这块内存由 C# 自己管理即可
                //frameTaskList.TrimExcess();
            }
        }

        public void DeleteFrameTask(int id)
        {
            if (idDic.ContainsKey(id) && idDic[id].active)
            {
                RemoveFrameListItem(idDic[id].index);
            }
        }

        private void RemoveFrameListItem(int index)
        {
            if (frameTaskListCount == 0) return;

            FrameTask task = frameTaskList[index];
            idDic[task.id].SetActive(false);

            RemoveListItem_FrameTask(index);
            if (index < frameTaskList.Count)
            {
                FrameTask indexTask = frameTaskList[index];
                TaskFlag flag = new TaskFlag
                {
                    id = indexTask.id,
                    index = index,
                    active = true
                };
                idDic[indexTask.id] = flag;
            }
        }

        private void RemoveListItem_FrameTask(int index)
        {
            int last = frameTaskListCount - 1;
            FrameTask temp = frameTaskList[index];
            frameTaskList[index] = frameTaskList[last];
            frameTaskList[last] = temp;
            --frameTaskListCount;
        }
    }
}
