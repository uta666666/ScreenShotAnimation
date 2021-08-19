using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenShotAnimation.Models
{
    public class SingleTask
    {
        //private readonly List<Action<Action>> _actionList = new List<Action<Action>>();
        private Queue<Action> _actionList = new Queue<Action>();


        private Task _task;
        public Task Task {
            get { return _task; }
        }

        private bool _isPlaying;

        public bool IsPlaying {
            get { return _isPlaying; }
        }

        /// <summary>
        /// タスクを追加します
        /// </summary>
        public async Task AddAsync(Action task)
        {
            lock (_actionList)
            {
                _actionList.Enqueue(task);
            }

            if (_isPlaying)
            {
                return;
            }
            _isPlaying = true;

            _task = PlayAsync();
            await _task;

            _isPlaying = false;
        }

        /// <summary>
        /// タスクを実行します
        /// </summary>
        public async Task PlayAsync(Action onCompleted = null)
        {

            _task = Task.Run(async () =>
            {
                while (_actionList.Count > 0)
                {

                    Action task;
                    lock (_actionList)
                    {
                        task = _actionList.Dequeue();
                    }

                    await Task.Run(task);
                }
            });
            await _task;
        }
    }
}
