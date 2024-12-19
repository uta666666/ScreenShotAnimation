using ImageMagick;
using ScreenShotAnimation.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ScreenShotAnimation.Models
{
    public class Animation
    {
        public Animation(int animationInterval)
        {
            _animationInterval = animationInterval;
        }

        #region フィールド変数

        private int _animationInterval;
        private MagickImageCollection _collection;
        private object _lockobj = new object();

        #endregion フィールド変数


        /// <summary>
        /// スクリーンショット取得してコレクションに追加
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void AddGifAnimation(int x, int y, int width, int height)
        {
            // スクリーンショットを撮る
            using (var bmp = ScreenShot.CaptureWithCursor(x, y, width, height))
            {
                lock (_lockobj)
                {
                    AddImageCollection(bmp);
                }
            }
        }

        /// <summary>
        /// コレクションに追加
        /// </summary>
        /// <param name="bmp"></param>
        /// <returns></returns>
        public Task AddImageCollectionAsync(Bitmap bmp)
        {
            return Task.Run(() =>
            {
                lock (_lockobj)
                {
                    AddImageCollection(bmp);
                    bmp.Dispose();
                }
            });
        }

        /// <summary>
        /// コレクションに追加
        /// </summary>
        /// <param name="image"></param>
        private void AddImageCollection(Bitmap image)
        {
            if (_collection == null)
            {
                _collection = new MagickImageCollection();
            }

            using (MemoryStream ms = new MemoryStream())
            {
                image.Save(ms, ImageFormat.Bmp);
                ms.Position = 0;
                MagickImage canvas = new MagickImage(ms);
                canvas.AnimationDelay = (uint)Math.Ceiling(_animationInterval / 10d);
                _collection.Add(canvas);
            }
        }

        /// <summary>
        /// ファイル作成
        /// </summary>
        /// <param name="dstPath"></param>
        public Task WriteAnimationGIFAsync(string dstPath)
        {
            return Task.Run(() => WriteAnimationGIF(dstPath));
        }

        /// <summary>
        /// ファイル作成
        /// </summary>
        /// <param name="dstPath"></param>
        public void WriteAnimationGIF(string dstPath)
        {
            if (_collection == null)
            {
                return;
            }

            QuantizeSettings settings = new QuantizeSettings();
            settings.Colors = 256;
            _collection.Quantize(settings);
            _collection.Optimize();
            _collection.Write(dstPath);
        }
    }
}
