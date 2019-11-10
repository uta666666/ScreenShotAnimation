using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSMovie.Model {
    public class SingleTask {
        private readonly List<Action<Action>> _actionList = new List<Action<Action>>();

        private bool _isPlaying;

        /// <summary>
        /// タスクを追加します
        /// </summary>
        public void Add(Action<Action> task) {
            if (task == null || _isPlaying) {
                return;
            }

            _actionList.Add(task);
        }

        /// <summary>
        /// タスクを実行します
        /// </summary>
        public void Play(Action onCompleted = null) {
            if (_actionList.Count <= 0) {
                onCompleted?.Invoke();
                return;
            }

            int count = 0;

            Action task = null;
            task = () =>
            {
                if (_actionList.Count <= count) {
                    onCompleted?.Invoke();
                    _isPlaying = false;
                    return;
                }

                Action nextTask = task;

                _actionList[count++](() =>
                {
                    if (nextTask == null) {
                        return;
                    }
                    nextTask();
                    nextTask = null;
                });
            };

            _isPlaying = true;
            task();
        }
    }
}
