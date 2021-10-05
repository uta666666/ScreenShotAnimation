using ScreenShotAnimation.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ScreenShotAnimation.Models
{
    public class Recorder : INotifyPropertyChanged
    {
        public Recorder(IUserSettings settings)
        {
            _settings = settings;
            Rect = new CaptureRect();
            FrameRate = new FrameRate(_settings.Fps);
        }

        private readonly List<Task> _tasks = new List<Task>();
        private CancellationTokenSource _tokenSource;
        private Animation _animation;
        private IUserSettings _settings;


        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        /// <summary>
        /// 録画領域
        /// </summary>
        public CaptureRect Rect { get; set; }

        /// <summary>
        /// フレームレート（FPS）
        /// </summary>
        public FrameRate FrameRate { get; set; }

        private string _filePath;
        /// <summary>
        /// 保存ファイル名
        /// </summary>
        public string FilePath
        {
            get
            {
                return _filePath;
            }
            set
            {
                if (_filePath == value) return;

                _filePath = value;
                RaisePropertyChanged();
            }
        }


        /// <summary>
        /// スクリーンショット取得開始
        /// </summary>
        public Task StartAsync()
        {
            //設定を保存しておく
            _settings.Fps = FrameRate.Fps;
            _settings.Save();

            var animationInterval = FrameRate.Interval;

            _animation = new Animation(animationInterval);
            _tokenSource = new CancellationTokenSource();

            var token = _tokenSource.Token;

            return Task.Run(() =>
            {
                Stopwatch sw = new Stopwatch();
                //キャンセルされるまで繰り返す
                while (!token.IsCancellationRequested)
                {
                    sw.Start();

                    var bmp = ScreenShot.CaptureWithCursor((int)Rect.X, (int)Rect.Y, (int)Rect.Width, (int)Rect.Height);
                    _tasks.Add(_animation.AddImageCollectionAsync(bmp));

                    while (sw.ElapsedMilliseconds < animationInterval)
                    {
                        Thread.Sleep(1);
                    }

                    sw.Stop();
                    sw.Reset();
                }
            });
        }

        public async Task StopAsync()
        {
            _tokenSource.Cancel();

            // 動作中のタスクがあれば待つ
            await Task.WhenAll(_tasks.ToArray());
            await _animation.WriteAnimationGIFAsync(FilePath);
        }
    }
}
