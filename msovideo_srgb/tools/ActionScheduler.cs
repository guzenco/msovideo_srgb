using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace msovideo_srgb
{
    public static class ActionScheduler
    {
        private static Dictionary<string, Queue<ScheduledAction>> _actions = new Dictionary<string, Queue<ScheduledAction>>();
        private static Dictionary<string, Task> _tasks = new Dictionary<string, Task>();

        private static readonly object _lock = new object();

        private static void Execute(string taskId)
        {
            ScheduledAction scheduledAction;

            while (true)
            {
                lock (_lock)
                {
                    if (_actions[taskId].Count == 0)
                    {
                        _actions.Remove(taskId);
                        _tasks.Remove(taskId);
                        return;
                    }
                    scheduledAction = _actions[taskId].Dequeue();
                }
                try
                {
                    scheduledAction.Action();
                }
                catch (Exception e)
                {
                    if (scheduledAction.ExceptionHandler != null)
                    {
                        scheduledAction.ExceptionHandler(e);
                    }
                }
            }
        }

        public static void Add(string taskId, Action action, Action<Exception> exceptionHandler = null)
        {
            lock (_lock)
            {
                if (!_actions.ContainsKey(taskId))
                {
                    _actions.Add(taskId, new Queue<ScheduledAction>());
                }
                _actions[taskId].Enqueue(new ScheduledAction(action, exceptionHandler));
                if (!_tasks.ContainsKey(taskId))
                {
                    _tasks.Add(taskId, Task.Run(() => Execute(taskId)));
                }
            }
        }

        public static void Clear(string taskId)
        {
            lock (_lock)
            {
                if (_actions.ContainsKey(taskId))
                {
                    _actions[taskId].Clear();
                }
            }
        }

        private class ScheduledAction
        {
            public Action Action { get; }
            public Action<Exception> ExceptionHandler { get; }

            public ScheduledAction(Action action, Action<Exception> exceptionHandler)
            {
                Action = action;
                ExceptionHandler = exceptionHandler;
            }
        }
    }
}
