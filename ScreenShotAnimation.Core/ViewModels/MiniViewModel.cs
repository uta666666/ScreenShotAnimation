using Livet.Messaging;
using Livet.Messaging.IO;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.Options;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using ScreenShotAnimation.Models;
using ScreenShotAnimation.Util;
using ScreenShotAnimation.Views;
using System;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace ScreenShotAnimation.ViewModels
{
    public class MiniViewModel : Livet.ViewModel
    {
        #region フィールド変数

        private Recorder _recorder;

        private CaptureRectViewModel _captureViewModel;
        private AppSettings _settings;

        #endregion フィールド変数

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public MiniViewModel(IOptions<AppSettings> settings)
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


            OpenSaveFileDialogCommand = new[] { IsRecording, IsSaving }.CombineLatestValuesAreAllFalse().ToReactiveCommand();
            OpenSaveFileDialogCommand.Subscribe(() =>
            {
                CallSaveFileDialog();
            });

            var isNotSetWidth = CaptureWidth.Select(x => x == 0);
            var isNotSetHeight = CaptureHeight.Select(x => x == 0);
            StartRecordingCommand = new[] { IsRecording, IsSaving, isNotSetWidth, isNotSetHeight }.CombineLatestValuesAreAllFalse().ToReactiveCommand<Window>();
            StartRecordingCommand.Subscribe(async (Window x) =>
            {
                if (!CallSaveFileDialog())
                {
                    return;
                }

                await Task.Delay(500);

                if (_captureViewModel != null)
                {
                    _captureViewModel.StrokeColor.Value = Colors.HotPink;
                }

                IsRecording.Value = true;

                await _recorder.StartAsync();
            });


            StopRecordingCommand = IsRecording.ToReactiveCommand();
            StopRecordingCommand.Subscribe(async () =>
            {
                CloseCaptureWindow();

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


            CaptureCommand = new ReactiveCommand<Window>();
            CaptureCommand.Subscribe(w =>
            {
                CloseCaptureWindow();

                _captureViewModel = new CaptureRectViewModel();
                _captureViewModel.CaptureWidth = CaptureWidth;
                _captureViewModel.CaptureHeight = CaptureHeight;
                _captureViewModel.CapturePointX = CapturePointX;
                _captureViewModel.CapturePointY = CapturePointY;

                Messenger.Raise(new TransitionMessage(_captureViewModel, "CaptureKey"));
            });

            CloseCaptureWindowCommand = new ReactiveCommand();
            CloseCaptureWindowCommand.Subscribe(() =>
            {
                CloseCaptureWindow();
            });
        }

        /// <summary>
        /// キャプチャーwindowを消す
        /// </summary>
        private void CloseCaptureWindow()
        {
            if (_captureViewModel != null)
            {
                _captureViewModel.CloseCommand?.Execute();
                _captureViewModel = null;
                CaptureWidth.Value = 0;
                CaptureHeight.Value = 0;
            }
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
            var actionHandler = new Action<object>(_ =>
            {
                Process.Start(new ProcessStartInfo(SavePath.Value) { UseShellExecute = true });
            });
            ShowSnackBar("ファイルの保存が完了しました。", "開く", actionHandler);
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


        #region Property

        public ReactiveProperty<string> SavePath { get; set; }

        public ReactiveProperty<bool> IsRecording { get; set; }

        public ReactiveProperty<bool> IsSaving { get; set; }

        public ReadOnlyReactiveProperty<bool> IsProcessing { get; set; }

        public ReactiveProperty<double> CaptureWidth { get; set; }

        public ReactiveProperty<double> CaptureHeight { get; set; }

        public ReactiveProperty<double> CapturePointX { get; set; }

        public ReactiveProperty<double> CapturePointY { get; set; }

        public ReactiveProperty<bool> IsCanMove { get; set; }

        public ReactiveProperty<int> Fps { get; set; }

        public ReactiveProperty<bool> IsTopMost { get; set; }

        public ReactiveProperty<bool> IsShowDialog { get; set; }

        public ReactiveProperty<Livet.ViewModel> ProgDialogVM { get; set; }

        public SnackbarMessageQueue SnackBarMessageQueue { get; private set; }

        #endregion Property

        #region Command

        public ReactiveCommand<Window> StartRecordingCommand { get; private set; }

        public ReactiveCommand StopRecordingCommand { get; private set; }

        public ReactiveCommand CloseCommand { get; private set; }

        public ReactiveCommand OpenSaveFileDialogCommand { get; private set; }

        public ReactiveCommand<Window> CaptureCommand { get; private set; }

        public ReactiveCommand CloseCaptureWindowCommand { get; private set; }

        #endregion Command
    }
}
