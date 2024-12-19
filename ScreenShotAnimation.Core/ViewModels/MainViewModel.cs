using Livet.Messaging.IO;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.Options;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using ScreenShotAnimation.Models;
using ScreenShotAnimation.Util;
using System;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;

namespace ScreenShotAnimation.ViewModels
{
    public class MainViewModel : Livet.ViewModel
    {
        #region フィールド変数

        private Recorder _recorder;

        private CaptureRectViewModel _captureViewModel;
        private AppSettings _settings;

        #endregion フィールド変数


        /// <summary>
        /// コンストラクタ
        /// </summary>
        public MainViewModel(IOptions<AppSettings> settings)
        {
            try
            {
                _settings = settings.Value;
                _recorder = new Recorder(_settings.RecorderSettings);

                //保存ファイル名
                SavePath = _recorder.ToReactivePropertyAsSynchronized(x => x.FilePath);

                //GIF関連の設定
                CaptureWidth = _recorder.Rect.ToReactivePropertyAsSynchronized(x => x.Width);
                CaptureHeight = _recorder.Rect.ToReactivePropertyAsSynchronized(x => x.Height);
                CapturePointX = _recorder.Rect.ToReactivePropertyAsSynchronized(x => x.X);
                CapturePointY = _recorder.Rect.ToReactivePropertyAsSynchronized(x => x.Y);
                Fps = _recorder.FrameRate.ToReactivePropertyAsSynchronized(x => x.Fps);

                //画面の制御
                WindowState = new ReactiveProperty<WindowState>(System.Windows.WindowState.Normal);
                ResizeMode = new ReactiveProperty<ResizeMode>(System.Windows.ResizeMode.CanResizeWithGrip);
                IsRecording = new ReactiveProperty<bool>(false);
                IsSaving = new ReactiveProperty<bool>(false);
                IsProcessing = IsRecording.CombineLatest(IsSaving, (x, y) => x || y).ToReadOnlyReactiveProperty();
                IsCanMove = IsRecording.Select(x => !x).ToReactiveProperty();
                IsTopMost = IsSaving.Select(x => !x).ToReactiveProperty();

                //ダイアログ関連
                IsShowDialog = new ReactiveProperty<bool>();
                ProgDialogVM = new ReactiveProperty<Livet.ViewModel>();
                SnackBarMessageQueue = new SnackbarMessageQueue();

                //コマンド作成
                CreateCommand();
            }
            catch (Exception ex)
            {
                LogWriter.ErrorAsync(ex).Wait();
            }
        }


        /// <summary>
        /// コマンド作成
        /// </summary>
        private void CreateCommand()
        {
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

            var isNotSetWidth = CaptureWidth.Select(x => x == 0);
            var isNotSetHeight = CaptureHeight.Select(x => x == 0);
            StartRecordingCommand = new[] { IsRecording, IsSaving, isNotSetWidth, isNotSetHeight }.CombineLatestValuesAreAllFalse().ToReactiveCommand<UIElement>();
            StartRecordingCommand.Subscribe(async (UIElement x) =>
            {
                if (!CallSaveFileDialog())
                {
                    return;
                }

                ResizeMode.Value = System.Windows.ResizeMode.NoResize;
                IsRecording.Value = true;

                await _recorder.StartAsync();
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
                    await _recorder.StopAsync();
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
        /// 保存ダイアログ
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
        }

        //private void ShowSaveCompletedMessage()
        //{
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
        //}

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


        #region Property

        public ReactiveProperty<string> SavePath { get; set; }

        public ReactiveProperty<bool> IsRecording { get; set; }

        public ReactiveProperty<bool> IsSaving { get; set; }

        public ReadOnlyReactiveProperty<bool> IsProcessing { get; set; }

        public ReactiveProperty<double> CaptureWidth { get; set; }

        public ReactiveProperty<double> CaptureHeight { get; set; }

        public ReactiveProperty<double> CapturePointX { get; set; }

        public ReactiveProperty<double> CapturePointY { get; set; }

        public ReactiveProperty<ResizeMode> ResizeMode { get; set; }

        public ReactiveProperty<bool> IsCanMove { get; set; }

        public ReactiveProperty<int> Fps { get; set; }

        public ReactiveProperty<bool> IsTopMost { get; set; }

        public ReactiveProperty<WindowState> WindowState { get; set; }

        public ReactiveProperty<bool> IsShowDialog { get; set; }

        public ReactiveProperty<Livet.ViewModel> ProgDialogVM { get; set; }

        public SnackbarMessageQueue SnackBarMessageQueue { get; private set; }

        #endregion Property

        #region Command

        public ReactiveCommand<UIElement> StartRecordingCommand { get; private set; }

        public ReactiveCommand StopRecordingCommand { get; private set; }

        public ReactiveCommand CloseCommand { get; private set; }

        public ReactiveCommand MinimizeCommand { get; private set; }

        public ReactiveCommand OpenSaveFileDialogCommand { get; private set; }

        #endregion Command
    }
}
