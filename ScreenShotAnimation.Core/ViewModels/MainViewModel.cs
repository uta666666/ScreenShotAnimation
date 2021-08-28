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

        //ms
        private int _animationInterval;
        private readonly List<Task> _tasks = new List<Task>();
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
            IsProcessing = IsRecording.CombineLatest(IsSaving, (x, y) => x || y).ToReadOnlyReactiveProperty();
            IsCanMove = IsRecording.Select(x => !x).ToReactiveProperty();
            IsTopMost = IsSaving.Select(x => !x).ToReactiveProperty();

            IsShowDialog = new ReactiveProperty<bool>();
            ProgDialogVM = new ReactiveProperty<Livet.ViewModel>();
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
                if (!CallSaveFileDialog())
                {
                    return;
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
                IsSaving.Value = true;

                ShowProgress("保存中...");
                try
                {
                    _tokenSoruce.Cancel();

                    // 動作中のタスクがあれば待つ
                    await Task.WhenAll(_tasks.ToArray());
                    await _animation.WriteAnimationGIFAsync(SavePath.Value);
                }
                finally
                {
                    IsSaving.Value = false;
                    CloseProgress();
                }

                ShowSaveCompletedMessage();
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
            IsShowDialog.Value = true;
        }

        /// <summary>
        /// 処理中を閉じる
        /// </summary>
        private void CloseProgress()
        {
            IsShowDialog.Value = false;
        }

        /// <summary>
        /// 保存完了のメッセージ
        /// </summary>
        private void ShowSaveCompletedMessage()
        {
            var actionHandler = new Action<object>(_ => { Process.Start(new ProcessStartInfo(SavePath.Value) { UseShellExecute = true }); });
            ShowSnackBar("ファイルの保存が完了しました。", "開く", actionHandler);

            //var messageVM = new MessageViewModel("Completed", "ファイルを保存しました。", "OK", "開く");
            //ProgDialogVM.Value = messageVM;
            //IsShowDialog.Value = true;

            //messageVM.ReturnType.Where(x => x != MessageViewReturnType.None).Subscribe(t =>
            //{
            //    switch (t)
            //    {
            //        case MessageViewReturnType.Button2:
            //            IsShowDialog.Value = false;
            //            Process.Start(new ProcessStartInfo(SavePath.Value) { UseShellExecute = true });
            //            break;
            //        default:
            //            IsShowDialog.Value = false;
            //            break;
            //    }
            //});
        }

        /// <summary>
        /// メッセージを表示
        /// </summary>
        /// <param name="text"></param>
        /// <param name="actionContent"></param>
        /// <param name="actionHandler"></param>
        private void ShowSnackBar(string text, string actionContent = null, Action<object> actionHandler = null)
        {
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
                while (!token.IsCancellationRequested)
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


        #region Command

        public ReactiveProperty<string> SavePath { get; set; }

        public ReactiveProperty<bool> IsRecording { get; set; }

        public ReactiveProperty<bool> IsSaving { get; set; }

        public ReadOnlyReactiveProperty<bool> IsProcessing { get; set; }

        public ReactiveProperty<double> CaptureWidth { get; set; }

        public ReactiveProperty<double> CaptureHeight { get; set; }

        public ReactiveProperty<ResizeMode> ResizeMode { get; set; }

        public ReactiveProperty<bool> IsCanMove { get; set; }

        public ReactiveProperty<int> Fps { get; set; }

        public ReactiveProperty<bool> IsTopMost { get; set; }

        public ReactiveProperty<WindowState> WindowState { get; set; }

        public ReactiveProperty<bool> IsShowDialog { get; set; }

        public ReactiveProperty<Livet.ViewModel> ProgDialogVM { get; set; }

        public SnackbarMessageQueue SnackBarMessageQueue { get; private set; }

        #endregion Command

        #region Property

        public ReactiveCommand<UIElement> StartRecordingCommand { get; private set; }

        public ReactiveCommand StopRecordingCommand { get; private set; }

        public ReactiveCommand CloseCommand { get; private set; }

        public ReactiveCommand MinimizeCommand { get; private set; }

        public ReactiveCommand OpenSaveFileDialogCommand { get; private set; }

        #endregion Command
    }
}
