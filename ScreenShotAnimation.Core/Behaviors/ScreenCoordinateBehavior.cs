using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ScreenShotAnimation.Behaviors
{
    public class ScreenCoordinateBehavior : Behavior<Window>
    {
        public double CaptureWidth
        {
            get
            {
                return (double)GetValue(CaptureWidthProperty);
            }
            set
            {
                SetValue(CaptureWidthProperty, value);
            }
        }

        public double CaptureHeight
        {
            get
            {
                return (double)GetValue(CaptreuHeightProperty);
            }
            set
            {
                SetValue(CaptreuHeightProperty, value);
            }
        }

        public double CapturePointX
        {
            get
            {
                return (double)GetValue(CapturePointXProperty);
            }
            set
            {
                SetValue(CapturePointXProperty, value);
            }
        }

        public double CapturePointY
        {
            get
            {
                return (double)GetValue(CapturePointYProperty);
            }
            set
            {
                SetValue(CapturePointYProperty, value);
            }
        }

        public Border CaptureControl
        {
            get
            {
                return (Border)GetValue(CaptureControlProperty);
            }
            set
            {
                SetValue(CaptureControlProperty, value);
            }
        }


        public static readonly DependencyProperty CaptureWidthProperty = DependencyProperty.Register(nameof(CaptureWidth), typeof(double), typeof(ScreenCoordinateBehavior), new PropertyMetadata(null));
        public static readonly DependencyProperty CaptreuHeightProperty = DependencyProperty.Register(nameof(CaptureHeight), typeof(double), typeof(ScreenCoordinateBehavior), new PropertyMetadata(null));
        public static readonly DependencyProperty CapturePointXProperty = DependencyProperty.Register(nameof(CapturePointX), typeof(double), typeof(ScreenCoordinateBehavior), new PropertyMetadata(null));
        public static readonly DependencyProperty CapturePointYProperty = DependencyProperty.Register(nameof(CapturePointY), typeof(double), typeof(ScreenCoordinateBehavior), new PropertyMetadata(null));

        public static readonly DependencyProperty CaptureControlProperty = DependencyProperty.Register(nameof(CaptureControl), typeof(UIElement), typeof(ScreenCoordinateBehavior), new PropertyMetadata(null));



        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.Loaded += AssociatedObject_Loaded;
            AssociatedObject.LocationChanged += AssociatedObject_LocationChanged;
            AssociatedObject.SizeChanged += AssociatedObject_SizeChanged;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.Loaded -= AssociatedObject_Loaded;
            AssociatedObject.LocationChanged -= AssociatedObject_LocationChanged;
            AssociatedObject.SizeChanged -= AssociatedObject_SizeChanged;

            base.OnDetaching();
        }


        private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
        {
            if (CaptureControl == null) return;

            CaptureWidth = CaptureControl.ActualWidth;
            CaptureHeight = CaptureControl.ActualHeight;

            var p = CaptureControl.PointToScreen(new Point(0, 0));
            CapturePointX = p.X;
            CapturePointY = p.Y;
        }

        private void AssociatedObject_LocationChanged(object sender, EventArgs e)
        {
            if (CaptureControl == null) return;

            CaptureWidth = CaptureControl.ActualWidth;
            CaptureHeight = CaptureControl.ActualHeight;

            var p = CaptureControl.PointToScreen(new Point(0, 0));
            CapturePointX = p.X;
            CapturePointY = p.Y;
        }

        private void AssociatedObject_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (CaptureControl == null) return;

            CaptureWidth = CaptureControl.ActualWidth;
            CaptureHeight = CaptureControl.ActualHeight;
        }
    }
}
