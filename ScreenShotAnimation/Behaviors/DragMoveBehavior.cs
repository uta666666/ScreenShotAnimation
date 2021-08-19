using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace ScreenShotAnimation.Behaviors
{
    public class DragMoveBehavior : Behavior<Window>
    {
        public bool IsCanMove
        {
            get
            {
                return (bool)GetValue(IsCanMoveProperty);
            }
            set
            {
                SetValue(IsCanMoveProperty, value);
            }
        }

        public static readonly DependencyProperty IsCanMoveProperty = DependencyProperty.Register(nameof(IsCanMove), typeof(bool), typeof(DragMoveBehavior), new PropertyMetadata(null));




        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.MouseLeftButtonDown += AssociatedObject_MouseLeftButtonDown;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.MouseLeftButtonDown -= AssociatedObject_MouseLeftButtonDown;
        }

        private void AssociatedObject_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (IsCanMove == false) return;

            AssociatedObject.DragMove();
        }
    }
}
