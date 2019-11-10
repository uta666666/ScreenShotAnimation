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
using SSMovie.Model;

namespace SSMovie.ViewModel {
    public class MainViewModel : Livet.ViewModel {

        private readonly DispatcherTimer _timer = new DispatcherTimer();
        // フレームレート(1/100 s)
        private readonly int _frameRate = 3;//30ms

        //private readonly List<Task> _tasks = new List<Task>();
        private readonly SingleTask _tasks = new SingleTask();

        private GifBitmapEncoder _encoder = new GifBitmapEncoder();

        private List<string> _images;


        #region Win32API関連

        private const int SRCCOPY = 13369376;
        private const int CAPTUREBLT = 1073741824;

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("gdi32.dll")]
        private static extern int BitBlt(IntPtr hDestDC,
            int x,
            int y,
            int nWidth,
            int nHeight,
            IntPtr hSrcDC,
            int xSrc,
            int ySrc,
            int dwRop);

        [DllImport("user32.dll")]
        private static extern IntPtr ReleaseDC(IntPtr hwnd, IntPtr hdc);

        #endregion Win32API関連


        public MainViewModel() {
            SavePath = new ReactiveProperty<string>();
            IsProcessing = new ReactiveProperty<bool>();
            CanStartProcess = IsProcessing.Select(n => !n).ToReactiveProperty();

            _images = new List<string>();

            InitializeDispatcherTimer();

            StartRecordingCommand = new ReactiveCommand();
            StartRecordingCommand.Subscribe(() => {
                if (string.IsNullOrWhiteSpace(SavePath.Value)) {
                    return;
                }
                IsProcessing.Value = true;
                _timer.Start();
            });

            StopRecordingCommand = new ReactiveCommand();
            StopRecordingCommand.Subscribe(() => {
                // タイマーを止める
                _timer.Stop();
                // 動作中のタスクがあれば待つ
                //Task.WaitAll(_tasks.ToArray());
                _tasks.Play(() => {
                    GenerateAnimationGIF(_images, SavePath.Value);

                    IsProcessing.Value = false;
                });
            });
        }

        private void InitializeDispatcherTimer() {
            // フレームレートに合わせて起動時間を設定する
            int interval_msec = _frameRate * 10;
            _timer.Interval = new TimeSpan(0, 0, 0, 0, interval_msec);
            _timer.Tick += dispatcherTimer_Tick;
        }

        private bool GenerateAnimationGIF(List<string> files, string dstPath) {
            //String[] files = Directory.GetFiles(tmpDir);
            if (files.Count == 0) return false;
            using (MagickImageCollection collection = new MagickImageCollection()) {
                MagickImage canvas = new MagickImage(files[files.Count - 1]);
                canvas.AnimationDelay = 250;
                canvas.Scale((int)(canvas.Width * 0.5), (int)(canvas.Height * 0.5));
                collection.Add(canvas);

                //int perFrame = (int)Math.Ceiling(600.0 / files.Count);
                foreach (string file in files) {
                    canvas = new MagickImage(file);
                    canvas.AnimationDelay = _frameRate;
                    canvas.Scale((int)(canvas.Width * 0.5), (int)(canvas.Height * 0.5));
                    collection.Add(canvas);
                }

                collection.Optimize();
                collection.Write(dstPath);
            };
            return true;
        }


        private void dispatcherTimer_Tick(object sender, EventArgs e) {
            // 時間がかかる処理なのでバックグラウンドで実行する
            _tasks.Add(
                x => {
                    // スクリーンショットを撮る
                    using (var bmp = CaptureScreen()) {
                        //using (var bmp = GetScreenShot(_videoWidth, _videoHeight)) {

                        var tempDir = Path.Combine(Path.GetDirectoryName(SavePath.Value), "ssmovietemp");
                        if (!Directory.Exists(tempDir)) {
                            Directory.CreateDirectory(tempDir);
                        }
                        var fileName = Path.Combine(tempDir, Path.GetFileNameWithoutExtension(SavePath.Value) + _images.Count.ToString());
                        bmp.Save(fileName, ImageFormat.Png);

                        lock (_images) {
                            _images.Add(fileName);
                        }
                    }
                });
        }

        private Bitmap GetScreenShot(int width, int height) {
            var cursor = new System.Windows.Forms.Cursor(System.Windows.Forms.Cursor.Current.Handle);
            System.Drawing.Point cPoint = System.Windows.Forms.Cursor.Position;
            System.Drawing.Point hotSpot = cursor.HotSpot;
            System.Drawing.Point position = new System.Drawing.Point((cPoint.X - hotSpot.X), (cPoint.Y - hotSpot.Y));

            var resizedBmp = new Bitmap(width, height);
            using (var bmp = new Bitmap((int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight))
            using (Graphics g = Graphics.FromImage(bmp))
            using (Graphics resizedG = Graphics.FromImage(resizedBmp)) {
                // スクリーンショットを撮る
                g.CopyFromScreen(new System.Drawing.Point(0, 0), new System.Drawing.Point(0, 0), bmp.Size);

                cursor.Draw(g, new Rectangle(position, cursor.Size));

                // 動画サイズを減らすためリサイズする
                resizedG.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;
                resizedG.DrawImage(bmp, 0, 0, width, height);
            }

            return resizedBmp;
        }

        private Bitmap CaptureScreen() {
            var cursor = new System.Windows.Forms.Cursor(System.Windows.Forms.Cursor.Current.Handle);
            cursor = System.Windows.Forms.Cursors.Arrow;
            System.Drawing.Point cPoint = System.Windows.Forms.Cursor.Position;
            System.Drawing.Point hotSpot = cursor.HotSpot;
            System.Drawing.Point position = new System.Drawing.Point((cPoint.X - hotSpot.X), (cPoint.Y - hotSpot.Y));

            //プライマリモニタのデバイスコンテキストを取得
            IntPtr disDC = GetDC(IntPtr.Zero);
            //Bitmapの作成
            Bitmap bmp = new Bitmap(
                System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width,
                System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height);
            //Graphicsの作成
            Graphics g = Graphics.FromImage(bmp);
            //Graphicsのデバイスコンテキストを取得
            IntPtr hDC = g.GetHdc();
            //Bitmapに画像をコピーする
            BitBlt(hDC, 0, 0, bmp.Width, bmp.Height,
                disDC, 0, 0, SRCCOPY);
            //解放
            g.ReleaseHdc(hDC);
            
            //カーソルを追加
            cursor.Draw(g, new Rectangle(position, cursor.Size));

            g.Dispose();
            ReleaseDC(IntPtr.Zero, disDC);

            return bmp;
        }


        public ReactiveProperty<string> SavePath { get; set; }

        public ReactiveProperty<bool> CanStartProcess { get; set; }

        public ReactiveProperty<bool> IsProcessing { get; set; }

        public ReactiveCommand StartRecordingCommand { get; private set; }

        public ReactiveCommand StopRecordingCommand { get; private set; }
    }
}
