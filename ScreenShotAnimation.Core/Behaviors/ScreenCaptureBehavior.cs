using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ScreenShotAnimation.Behaviors
{
    public class ScreenCaptureBehavior : Behavior<Window>
    {
        public Canvas MainCanvas
        {
            get { return (Canvas)GetValue(MainCanvasProperty); }
            set { SetValue(MainCanvasProperty, value); }
        }

        public double CaptureWidth
        {
            get { return (double)GetValue(CaptureWidthProperty); }
            set { SetValue(CaptureWidthProperty, value); }
        }

        public double CaptureHeight
        {
            get { return (double)GetValue(CaptureHeightProperty); }
            set { SetValue(CaptureHeightProperty, value); }
        }

        public double CapturePointX
        {
            get { return (double)GetValue(CapturePointXProperty); }
            set { SetValue(CapturePointXProperty, value); }
        }

        public double CapturePointY
        {
            get { return (double)GetValue(CapturePointYProperty); }
            set { SetValue(CapturePointYProperty, value); }
        }

        public Color StrokeColor
        {
            get { return (Color)GetValue(StrokeColorProperty); }
            set { SetValue(StrokeColorProperty, value); }
        }

        public static readonly DependencyProperty MainCanvasProperty = DependencyProperty.Register(nameof(MainCanvas), typeof(Canvas), typeof(ScreenCaptureBehavior), new PropertyMetadata(null));
        public static readonly DependencyProperty CaptureWidthProperty = DependencyProperty.Register(nameof(CaptureWidth), typeof(double), typeof(ScreenCaptureBehavior), new PropertyMetadata(null));
        public static readonly DependencyProperty CaptureHeightProperty = DependencyProperty.Register(nameof(CaptureHeight), typeof(double), typeof(ScreenCaptureBehavior), new PropertyMetadata(null));
        public static readonly DependencyProperty CapturePointXProperty = DependencyProperty.Register(nameof(CapturePointX), typeof(double), typeof(ScreenCaptureBehavior), new PropertyMetadata(null));
        public static readonly DependencyProperty CapturePointYProperty = DependencyProperty.Register(nameof(CapturePointY), typeof(double), typeof(ScreenCaptureBehavior), new PropertyMetadata(null));
        public static readonly DependencyProperty StrokeColorProperty = DependencyProperty.Register(nameof(StrokeColor), typeof(Color), typeof(ScreenCaptureBehavior), new PropertyMetadata(Colors.GreenYellow, PropertyChangedCallback));


        private static void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = d as ScreenCaptureBehavior;
            behavior?.SetStrokeColor((Color)e.NewValue);
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.WindowState = System.Windows.WindowState.Maximized;
            AssociatedObject.Topmost = true;
            AssociatedObject.Cursor = Cursors.Cross;

            AssociatedObject.ContentRendered += AssociatedObject_ContentRendered;
            AssociatedObject.MouseLeftButtonDown += AssociatedObject_MouseLeftButtonDown;
            AssociatedObject.MouseMove += MainWindow_MouseMove;
            AssociatedObject.MouseUp += MainWindow_MouseUp;
            AssociatedObject.KeyDown += AssociatedObject_KeyDown;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.ContentRendered -= AssociatedObject_ContentRendered;
            AssociatedObject.MouseLeftButtonDown -= AssociatedObject_MouseLeftButtonDown;
            AssociatedObject.MouseMove -= MainWindow_MouseMove;
            AssociatedObject.MouseUp -= MainWindow_MouseUp;
            AssociatedObject.KeyDown -= AssociatedObject_KeyDown;

            AssociatedObject.Cursor = Cursors.Arrow;
        }


        private Point _startPoint;
        private Rectangle _selectionRectangle;
        private bool _isDragging;
        private bool _isCompleted;


        /// <summary>
        /// コンテンツ描画完了時の処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AssociatedObject_ContentRendered(object sender, EventArgs e)
        {
            MainCanvas.Background = Brushes.White;
            _isCompleted = false;
        }

        /// <summary>
        /// マウス左ボタン押下時の処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AssociatedObject_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _isDragging = true;
                _startPoint = e.GetPosition(AssociatedObject);

                if (_isCompleted)
                {
                    AssociatedObject.CaptureMouse();
                    return;
                }

                MainCanvas.Children.Clear();
                _selectionRectangle = new Rectangle
                {
                    Stroke = Brushes.GreenYellow,
                    StrokeThickness = 6,
                    StrokeDashArray = [4, 2],
                    Fill = Brushes.Transparent,
                };
                Canvas.SetLeft(_selectionRectangle, _startPoint.X);
                Canvas.SetTop(_selectionRectangle, _startPoint.Y);
                MainCanvas.Children.Add(_selectionRectangle);

                AssociatedObject.CaptureMouse();
            }
        }

        private void SetStrokeColor(Color color)
        {
            _selectionRectangle.Stroke = new SolidColorBrush(color);
        }

        /// <summary>
        /// マウス移動時の処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                Point endPoint = e.GetPosition(AssociatedObject);

                if (_isCompleted)
                {
                    double x = Canvas.GetLeft(_selectionRectangle);
                    double y = Canvas.GetTop(_selectionRectangle);
                    var diffX = endPoint.X - _startPoint.X;
                    var diffY = endPoint.Y - _startPoint.Y;

                    Canvas.SetLeft(_selectionRectangle, x + diffX);
                    Canvas.SetTop(_selectionRectangle, y + diffY);
                    _startPoint = endPoint;
                }
                else
                {
                    double x = Math.Min(_startPoint.X, endPoint.X) - 6;
                    double y = Math.Min(_startPoint.Y, endPoint.Y) - 6;
                    double width = Math.Abs(_startPoint.X - endPoint.X);
                    double height = Math.Abs(_startPoint.Y - endPoint.Y);

                    Canvas.SetLeft(_selectionRectangle, x);
                    Canvas.SetTop(_selectionRectangle, y);
                    _selectionRectangle.Width = width;
                    _selectionRectangle.Height = height;
                }

                (var clipWidth, var clipHeight) = UpdateClipSize();
                SetCaptureDimensions(clipWidth, clipHeight);
                SetClipGeometry(clipWidth, clipHeight);
            }
        }

        private (double clipWidth, double clipHeight) UpdateClipSize()
        {
            var clipWidth = Math.Max(0, _selectionRectangle.Width - 12);
            var clipHeight = Math.Max(0, _selectionRectangle.Height - 12);
            return (clipWidth, clipHeight);
        }

        private void SetCaptureDimensions(double clipWidth, double clipHeight)
        {
            CaptureHeight = clipHeight;
            CaptureWidth = clipWidth;
            CapturePointX = Canvas.GetLeft(_selectionRectangle) - 1; //ここが何の-4なのかがわからない。実際に動かして調整した。
            CapturePointY = Canvas.GetTop(_selectionRectangle) - 1;  //ここが何の-4なのかがわからない。実際に動かして調整した。
        }

        private void SetClipGeometry(double clipWidth, double clipHeight)
        {
            var clipGeometry = new CombinedGeometry(
                GeometryCombineMode.Exclude,
                new RectangleGeometry(new Rect(0, 0, AssociatedObject.Width, AssociatedObject.Height)),
                new RectangleGeometry(new Rect(Canvas.GetLeft(_selectionRectangle) + 6, Canvas.GetTop(_selectionRectangle) + 6, clipWidth, clipHeight)));

            MainCanvas.Clip = clipGeometry;
        }

        /// <summary>
        /// マウス左ボタン解放時の処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released)
            {
                _isDragging = false;

                MainCanvas.Background = Brushes.Transparent;

                AssociatedObject.ReleaseMouseCapture();
                AssociatedObject.Cursor = Cursors.Arrow;

                _isCompleted = true;
            }
        }

        /// <summary>
        /// キーボードイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AssociatedObject_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                AssociatedObject.Close();
                e.Handled = true;
                return;
            }
        }

    }
}
