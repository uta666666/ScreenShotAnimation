using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Reactive.Bindings;
using System.Drawing;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Media.Imaging;
using System.IO;
using ImageMagick;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;
using System.Reactive.Linq;
using ScreenShotAnimation.Models;
using System.Diagnostics;
using System.Threading;
using Reactive.Bindings.Extensions;
using Livet.Messaging;
using Livet.Messaging.IO;
using MaterialDesignThemes.Wpf;

namespace ScreenShotAnimation.ViewModels
{
    public class MainViewModel : Livet.ViewModel
    {
        #region フィールド変数

        private readonly DispatcherTimer _timer = new DispatcherTimer();
        //ms
        private int _animationInterval;

        private readonly List<Task> _tasks = new List<Task>();
        //private readonly SingleTask _tasks = new SingleTask();

        private GifBitmapEncoder _encoder = new GifBitmapEncoder();

        //private object _lockobj = new object();

        private System.Windows.Point _capturePoint;
        private Animation _animation;

        private CancellationTokenSource _tokenSoruce;

        #endregion フィールド変数


        /// <summary>
        /// コンストラクタ
        /// </summary>
        public MainViewModel()
        {
            WindowState = new ReactiveProperty<WindowState>(System.Windows.WindowState.Normal);
            ResizeMode = new ReactiveProperty<ResizeMode>(System.Windows.ResizeMode.CanResizeWithGrip);

            SavePath = new ReactiveProperty<string>();
            CaptureWidth = new ReactiveProperty<double>();
            CaptureHeight = new ReactiveProperty<double>();
            Fps = new ReactiveProperty<int>(15);

            IsRecording = new ReactiveProperty<bool>(false);
            IsSaving = new ReactiveProperty<bool>(false);
            IsCanMove = IsRecording.Select(x => !x).ToReactiveProperty();
            IsTopMost = IsSaving.Select(x => !x).ToReactiveProperty();

            ProgDialogVM = new ReactiveProperty<ProgressViewModel>();
            SnackBarMessageQueue = new SnackbarMessageQueue();



            CloseCommand = new ReactiveCommand();
            CloseCommand.Subscribe(() =>
            {
                Application.Current.Shutdown();
            });


            MinimizeCommand = IsRecording.Select(x => !x).ToReactiveCommand();
            MinimizeCommand.Subscribe(() =>
            {
                WindowState.Value = System.Windows.WindowState.Minimized;
            });


            OpenSaveFileDialogCommand = new[] { IsRecording, IsSaving }.CombineLatestValuesAreAllFalse().ToReactiveCommand();
            OpenSaveFileDialogCommand.Subscribe(() =>
            {
                CallSaveFileDialog();
            });

            
            StartRecordingCommand = new[] { IsRecording, IsSaving }.CombineLatestValuesAreAllFalse().ToReactiveCommand<UIElement>();
            StartRecordingCommand.Subscribe(async (UIElement x) =>
            {
                if (string.IsNullOrWhiteSpace(SavePath.Value))
                {
                    if (!CallSaveFileDialog())
                    {
                        return;
                    }
                }

                //ms
                _animationInterval = (int)Math.Round(1000d / Fps.Value);

                _capturePoint = x.PointToScreen(new System.Windows.Point(0, 0));
                _animation = new Animation(_animationInterval);

                ResizeMode.Value = System.Windows.ResizeMode.NoResize;
                IsRecording.Value = true;

                _tokenSoruce = new CancellationTokenSource();
                var token = _tokenSoruce.Token;
                await StartRecordingAsync(token);
            });


            StopRecordingCommand = IsRecording.ToReactiveCommand();
            StopRecordingCommand.Subscribe(async () =>
            {
                ResizeMode.Value = System.Windows.ResizeMode.CanResizeWithGrip;
                IsRecording.Value = false;

                ShowProgress("保存中...");
                try
                {
                    _tokenSoruce.Cancel();

                    // 動作中のタスクがあれば待つ
                    await Task.WhenAll(_tasks.ToArray());
                    await _animation.WriteAnimationGIFAsync(SavePath.Value);

                    ShowSaveCompletedMessage();
                }
                finally
                {
                    IsSaving.Value = false;
                }
            });
        }

        /// <summary>
        /// ファイル名
        /// </summary>
        /// <returns></returns>
        private bool CallSaveFileDialog()
        {
            var msg = new SavingFileSelectionMessage("SaveFileDialog");
            msg.Filter = "GIF|*.gif";
            Messenger.Raise(msg);
            if (msg.Response == null || msg.Response.Length == 0)
            {
                return false;
            }
            SavePath.Value = msg.Response[0];
            return true;
        }

        /// <summary>
        /// 処理中を表示
        /// </summary>
        /// <param name="message"></param>
        private void ShowProgress(string message)
        {
            ProgDialogVM.Value = new ProgressViewModel(message);
            IsSaving.Value = true;
        }

        /// <summary>
        /// 保存完了のメッセージ
        /// </summary>
        private void ShowSaveCompletedMessage()
        {
            var actionHandler = new Action<object>(_ => { Process.Start(SavePath.Value); });
            ShowInfoMessage("ファイルの保存が完了しました。", "開く", actionHandler);
        }

        /// <summary>
        /// メッセージを表示
        /// </summary>
        /// <param name="text"></param>
        /// <param name="actionContent"></param>
        /// <param name="actionHandler"></param>
        private void ShowInfoMessage(string text, string actionContent = null, Action<object> actionHandler = null)
        {
            //var msg = new InformationMessage(text, caption, MessageBoxImage.Information, "MessageDialog");
            //Messenger.Raise(msg);
            
            SnackBarMessageQueue.Enqueue(text, actionContent, actionHandler, null, false, true, TimeSpan.FromSeconds(5));
        }


        /// <summary>
        /// スクリーンショット取得開始
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private Task StartRecordingAsync(CancellationToken token)
        {
            return Task.Run(() =>
            {
                Stopwatch sw = new Stopwatch();
                //キャンセルされるまで繰り返す
                while (token != null && !token.IsCancellationRequested)
                {
                    sw.Start();

                    var bmp = _animation.CaptureScreenWithCursor((int)_capturePoint.X, (int)_capturePoint.Y, (int)CaptureWidth.Value, (int)CaptureHeight.Value);
                    _tasks.Add(_animation.AddImageCollectionAsync(bmp));


                    while (sw.ElapsedMilliseconds < _animationInterval)
                    {
                        Thread.Sleep(1);
                    }

                    sw.Stop();
                    sw.Reset();
                }
            });
        }



        public ReactiveProperty<string> SavePath { get; set; }

        public ReactiveProperty<bool> IsRecording { get; set; }

        public ReactiveProperty<bool> IsSaving { get; set; }

        public ReactiveProperty<double> CaptureWidth { get; set; }

        public ReactiveProperty<double> CaptureHeight { get; set; }

        public ReactiveProperty<ResizeMode> ResizeMode { get; set; }

        public ReactiveProperty<bool> IsCanMove { get; set; }

        public ReactiveProperty<int> Fps { get; set; }

        public ReactiveProperty<bool> IsTopMost { get; set; }

        public ReactiveProperty<WindowState> WindowState { get; set; }

        public ReactiveProperty<ProgressViewModel> ProgDialogVM { get; set; }

        public SnackbarMessageQueue SnackBarMessageQueue { get; private set; }



        public ReactiveCommand<UIElement> StartRecordingCommand { get; private set; }

        public ReactiveCommand StopRecordingCommand { get; private set; }

        public ReactiveCommand CloseCommand { get; private set; }

        public ReactiveCommand MinimizeCommand { get; private set; }

        public ReactiveCommand OpenSaveFileDialogCommand { get; private set; }
    }
}
