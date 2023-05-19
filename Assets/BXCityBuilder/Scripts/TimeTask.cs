using System;
using System.Collections;
using System.Collections.Generic;

namespace CityBuilder
{
    public enum TaskType
    {
        TimeTask,
        FrameTask
    }

    public enum TimeUnit
    {
        Millisecound,
        Secound,
        Minute,
        Hour,
        Day
    }

    public struct TimeTask
    {
        public int id;
        public double delay;
        public double destTime;
        public Action<int> callBack;
        public int count;
    }

    public struct FrameTask
    {
        public int id;
        public int curFrame;
        public int delay;
        public Action<int> callBack;
        public int count;

        public void Destory()
        {
            callBack = null;
        }
    }

    public struct TaskFlag
    {
        public int id;
        public int index;
        public TaskType taskType;
        public bool active;

        public TaskFlag(int id, int index, TaskType taskType, bool active)
        {
            this.id = id;
            this.index = index;
            this.active = active;
            this.taskType = taskType;
        }

        public TaskFlag(TaskFlag taskFlag, TaskType taskType)
        {
            this.id = taskFlag.id;
            this.index = taskFlag.index;
            this.taskType = taskType;
            this.active = taskFlag.active;
        }

        public void SetActive(bool active)
        {
            this.active = active;
        }
    }

    public struct TaskPack
    {
        public int id;
        public Action<int> callBack;

        public TaskPack(int id, Action<int> callBack)
        {
            this.id = id;
            this.callBack = callBack;
        }
    }

    public struct IDPack
    {
        public int id;
        public TaskType taskType;

        public IDPack(int id, TaskType taskType)
        {
            this.id = id;
            this.taskType = taskType;
        }
    }
}
