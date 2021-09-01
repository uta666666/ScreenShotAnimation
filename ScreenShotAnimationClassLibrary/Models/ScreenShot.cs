using ScreenShotAnimation.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace ScreenShotAnimation.Models
{
    public class ScreenShot
    {
        private static int CursorStep { get; set; }

        /// <summary>
        /// スクリーンショット取得
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static Bitmap CaptureWithCursor(int x, int y, int width, int height)
        {
            //プライマリモニタのデバイスコンテキストを取得
            IntPtr disDC = NativeMethods.GetDC(IntPtr.Zero);
            //Bitmapの作成
            Bitmap bmp = new Bitmap(width, height);
            //Graphicsの作成
            Graphics g = Graphics.FromImage(bmp);
            //Graphicsのデバイスコンテキストを取得
            IntPtr hDC = g.GetHdc();
            //Bitmapに画像をコピーする
            NativeMethods.BitBlt(hDC, 0, 0, bmp.Width, bmp.Height, disDC, x, y, NativeMethods.SRCCOPY);
            //解放
            //g.ReleaseHdc(hDC);

            #region Cursor

            try
            {
                var cursorInfo = new NativeMethods.CURSORINFO();
                cursorInfo.cbSize = Marshal.SizeOf(cursorInfo);

                if (NativeMethods.GetCursorInfo(out cursorInfo))
                {
                    if (cursorInfo.flags == NativeMethods.CURSOR_SHOWING)
                    {
                        var hicon = NativeMethods.CopyIcon(cursorInfo.hCursor);

                        if (hicon != IntPtr.Zero)
                        {
                            if (NativeMethods.GetIconInfo(hicon, out var iconInfo))
                            {
                                var CursorX = cursorInfo.ptScreenPos.x - x;
                                var CursorY = cursorInfo.ptScreenPos.y - y;

                                //If the cursor rate needs to be precisely captured.
                                //https://source.winehq.org/source/dlls/user32/cursoricon.c#2325
                                //int rate = 0, num = 0;
                                //var ok1 = Native.GetCursorFrameInfo(cursorInfo.hCursor, IntPtr.Zero, 17, ref rate, ref num);

                                //CursorStep
                                var ok = NativeMethods.DrawIconEx(hDC, CursorX - iconInfo.xHotspot, CursorY - iconInfo.yHotspot, cursorInfo.hCursor, 0, 0, CursorStep, IntPtr.Zero, 0x0003);

                                if (!ok)
                                {
                                    CursorStep = 0;
                                    NativeMethods.DrawIconEx(hDC, CursorX - iconInfo.xHotspot, CursorY - iconInfo.yHotspot, cursorInfo.hCursor, 0, 0, CursorStep, IntPtr.Zero, 0x0003);
                                }
                                else
                                {
                                    CursorStep++;
                                }

                                //Set to fix all alpha bits back to 255.
                                //frame.RemoveAnyTransparency = iconInfo.hbmMask != IntPtr.Zero;
                            }

                            NativeMethods.DeleteObject(iconInfo.hbmColor);
                            NativeMethods.DeleteObject(iconInfo.hbmMask);
                        }

                        NativeMethods.DestroyIcon(hicon);
                    }

                    NativeMethods.DeleteObject(cursorInfo.hCursor);
                }
            }
            catch (Exception ex)
            {
                _ = LogWriter.ErrorAsync(ex);
            }

            #endregion

            //解放
            g.ReleaseHdc(hDC);
            g.Dispose();
            NativeMethods.ReleaseDC(IntPtr.Zero, disDC);

            return bmp;
        }
    }
}
