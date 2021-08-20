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

namespace ScreenShotAnimation.ViewModels
{
    public class MainViewModel : Livet.ViewModel
    {

        private readonly DispatcherTimer _timer = new DispatcherTimer();
        //ms
        private int _animationInterval;

        private readonly List<Task> _tasks = new List<Task>();
        //private readonly SingleTask _tasks = new SingleTask();

        private GifBitmapEncoder _encoder = new GifBitmapEncoder();

        //private List<string> _images;
        //private List<Bitmap> _images;

        //private object _lockobj = new object();

        private System.Windows.Point _capturePoint;
        private Animation _animation;

        private CancellationTokenSource _tokenSoruce;



        /// <summary>
        /// コンストラクタ
        /// </summary>
        public MainViewModel()
        {
            SavePath = new ReactiveProperty<string>();

            CaptureWidth = new ReactiveProperty<double>();
            CaptureHeight = new ReactiveProperty<double>();

            ResizeMode = new ReactiveProperty<ResizeMode>(System.Windows.ResizeMode.CanResizeWithGrip);
            IsCapturing = new ReactiveProperty<bool>(false);
            IsSaving = new ReactiveProperty<bool>(false);
            IsCanMove = IsCapturing.Select(x => !x).ToReactiveProperty();
            IsTopMost = IsSaving.Select(x => !x).ToReactiveProperty();

            Fps = new ReactiveProperty<int>(15);


            //_images = new List<string>();
            //_images = new List<Bitmap>();

            InitializeDispatcherTimer();

            CloseCommand = new ReactiveCommand();
            CloseCommand.Subscribe(() =>
            {
                Application.Current.Shutdown();
            });

            OpenSaveFileDialogCommand = new[] { IsCapturing, IsSaving }.CombineLatestValuesAreAllFalse().ToReactiveCommand();
            OpenSaveFileDialogCommand.Subscribe(() =>
            {
                CallSaveFileDialog();
            });

            //StartRecordingCommand = new ReactiveCommand<UIElement>();
            StartRecordingCommand = new[] { IsCapturing, IsSaving }.CombineLatestValuesAreAllFalse().ToReactiveCommand<UIElement>();
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
                //IsCanMove.Value = false;
                IsCapturing.Value = true;

                //_timer.Start();
                _tokenSoruce = new CancellationTokenSource();
                var token = _tokenSoruce.Token;
                await StartRecordingAsync(token);
            });

            //StopRecordingCommand = new ReactiveCommand();
            StopRecordingCommand = IsCapturing.ToReactiveCommand();
            StopRecordingCommand.Subscribe(async () =>
            {
                ResizeMode.Value = System.Windows.ResizeMode.CanResizeWithGrip;
                //IsCanMove.Value = true;
                IsCapturing.Value = false;

                IsSaving.Value = true;
                try
                {
                    // タイマーを止める
                    //_timer.Stop();
                    _tokenSoruce.Cancel();

                    // 動作中のタスクがあれば待つ
                    await Task.WhenAll(_tasks.ToArray());
                    await _animation.WriteAnimationGIFAsync(SavePath.Value);
                }
                finally
                {
                    IsSaving.Value = false;
                    //foreach (var img in _images)
                    //{
                    //    File.Delete(img);
                    //}
                }
            });
        }

        private bool CallSaveFileDialog()
        {
            var msg = new SavingFileSelectionMessage("SaveFileDialog");
            Messenger.Raise(msg);
            if (msg.Response == null || msg.Response.Length == 0)
            {
                return false;
            }
            SavePath.Value = msg.Response[0];
            return true;
        }


        /// <summary>
        /// タイマーの初期化
        /// </summary>
        private void InitializeDispatcherTimer()
        {
            // フレームレートに合わせて起動時間を設定する
            int interval_msec = _animationInterval;
            _timer.Interval = new TimeSpan(0, 0, 0, 0, interval_msec);
            _timer.Tick += dispatcherTimer_TickAsync;
        }

        /// <summary>
        /// タイマー
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dispatcherTimer_TickAsync(object sender, EventArgs e)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            // 時間がかかる処理なのでバックグラウンドで実行する
            _tasks.Add(
                Task.Run(() =>
                {
                    _animation.AddGifAnimation((int)_capturePoint.X, (int)_capturePoint.Y, (int)CaptureWidth.Value, (int)CaptureHeight.Value);

                }));

            sw.Stop();
            Debug.WriteLine(sw.ElapsedMilliseconds);
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


        ///// <summary>
        ///// GIF作成
        ///// </summary>
        ///// <param name="files"></param>
        ///// <param name="dstPath"></param>
        ///// <returns></returns>
        //private bool GenerateAnimationGIF(List<string> files, string dstPath)
        //{
        //    if (files.Count == 0) return false;

        //    using (MagickImageCollection collection = new MagickImageCollection())
        //    {
        //        MagickImage canvas = new MagickImage(files[files.Count - 1]);
        //        canvas.AnimationDelay = 250;
        //        canvas.Scale((int)(canvas.Width * 0.5), (int)(canvas.Height * 0.5));
        //        collection.Add(canvas);

        //        //int perFrame = (int)Math.Ceiling(600.0 / files.Count);
        //        foreach (string file in files)
        //        {
        //            canvas = new MagickImage(file);
        //            canvas.AnimationDelay = _frameRate;
        //            canvas.Scale((int)(canvas.Width * 0.5), (int)(canvas.Height * 0.5));
        //            collection.Add(canvas);
        //        }

        //        //collection.Optimize();
        //        collection.Write(dstPath);
        //    };
        //    return true;
        //}

        ///// <summary>
        ///// GIF作成
        ///// </summary>
        ///// <param name="images"></param>
        ///// <param name="dstPath"></param>
        ///// <returns></returns>
        //private bool GenerateAnimationGIF(List<Bitmap> images, string dstPath)
        //{
        //    if (images.Count == 0) return false;

        //    using (MagickImageCollection collection = new MagickImageCollection())
        //    {
        //        var lastimg = images[images.Count - 1];
        //        MagickImage canvas = new MagickImage(lastimg);
        //        canvas.AnimationDelay = 250;
        //        canvas.Scale((int)(canvas.Width * 0.5), (int)(canvas.Height * 0.5));
        //        collection.Add(canvas);

        //        //int perFrame = (int)Math.Ceiling(600.0 / files.Count);
        //        foreach (Bitmap img in images)
        //        {
        //            canvas = new MagickImage(img);
        //            canvas.AnimationDelay = _frameRate;
        //            canvas.Scale((int)(canvas.Width * 0.5), (int)(canvas.Height * 0.5));
        //            collection.Add(canvas);
        //        }

        //        collection.Optimize();
        //        collection.Write(dstPath);
        //    };
        //    return true;
        //}


        ///// <summary>
        ///// スクリーンショット取得（.NetFramework）
        ///// </summary>
        //private Bitmap GetScreenShot(int width, int height)
        //{
        //    var cursor = new System.Windows.Forms.Cursor(System.Windows.Forms.Cursor.Current.Handle);
        //    System.Drawing.Point cPoint = System.Windows.Forms.Cursor.Position;
        //    System.Drawing.Point hotSpot = cursor.HotSpot;
        //    System.Drawing.Point position = new System.Drawing.Point((cPoint.X - hotSpot.X), (cPoint.Y - hotSpot.Y));

        //    var resizedBmp = new Bitmap(width, height);
        //    using (var bmp = new Bitmap((int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight))
        //    using (Graphics g = Graphics.FromImage(bmp))
        //    using (Graphics resizedG = Graphics.FromImage(resizedBmp))
        //    {
        //        // スクリーンショットを撮る
        //        g.CopyFromScreen(new System.Drawing.Point(0, 0), new System.Drawing.Point(0, 0), bmp.Size);

        //        cursor.Draw(g, new Rectangle(position, cursor.Size));

        //        // 動画サイズを減らすためリサイズする
        //        resizedG.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;
        //        resizedG.DrawImage(bmp, 0, 0, width, height);
        //    }

        //    return resizedBmp;
        //}




        public ReactiveProperty<string> SavePath { get; set; }

        public ReactiveProperty<bool> IsCapturing { get; set; }

        public ReactiveProperty<bool> IsSaving { get; set; }

        public ReactiveProperty<double> CaptureWidth { get; set; }

        public ReactiveProperty<double> CaptureHeight { get; set; }

        public ReactiveProperty<ResizeMode> ResizeMode { get; set; }

        public ReactiveProperty<bool> IsCanMove { get; set; }

        public ReactiveProperty<int> Fps { get; set; }

        public ReactiveProperty<bool> IsTopMost { get; set; }



        public ReactiveCommand<UIElement> StartRecordingCommand { get; private set; }

        public ReactiveCommand StopRecordingCommand { get; private set; }

        public ReactiveCommand CloseCommand { get; private set; }

        public ReactiveCommand OpenSaveFileDialogCommand { get; private set; }
    }
}
