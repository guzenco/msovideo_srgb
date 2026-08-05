using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace msovideo_srgb
{
    public static class ActionScheduler
    {
        private static Dictionary<string, Queue<ScheduledAction>> _actions = new Dictionary<string, Queue<ScheduledAction>>();
        private static Dictionary<string, int> _priorities = new Dictionary<string, int>();
        private static HashSet<string> _protected = new HashSet<string>();

        private static Task _executor;

        private static readonly object _lock = new object();

        private static void Execute()
        {
            ScheduledAction scheduledAction;

            while (true)
            {
                lock (_lock)
                {
                    if(_actions.Count == 0)
                    {
                        _executor = null;
                        return;
                    }

                    string taskId = _actions.Keys.Aggregate((max, next) => _priorities[next] > _priorities[max] ? next : max);

                    if (_actions[taskId].Count == 0)
                    {
                        _actions.Remove(taskId);
                        continue;
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
                if (!_priorities.ContainsKey(taskId))
                {
                    _priorities.Add(taskId, int.MinValue);
                }

                if (!_actions.ContainsKey(taskId))
                {
                    _actions.Add(taskId, new Queue<ScheduledAction>());
                }

                _actions[taskId].Enqueue(new ScheduledAction(action, exceptionHandler));

                if (_executor == null)
                {
                    _executor = Task.Run(Execute);
                }
            }
        }

        public static void SetPriority(string taskId, int priority)
        {
            lock (_lock)
            {
                if (!_priorities.ContainsKey(taskId))
                {
                    _priorities.Add(taskId, priority);             
                }
                else
                {
                    _priorities[taskId] = priority;
                }
            }
        }

        public static void Protect(string taskId)
        {
            lock (_lock)
            {
                _protected.Add(taskId);
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

        public static void ClearAll()
        {
            lock (_lock)
            {
                foreach (var taskIds in _actions.Keys)
                {
                    if (_protected.Contains(taskIds)) continue;
                    _actions[taskIds].Clear();
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
