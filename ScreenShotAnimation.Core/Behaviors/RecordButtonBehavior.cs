using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ScreenShotAnimation.Behaviors
{
    public class RecordButtonBehavior : Behavior<Button>
    {
        public ICommand StartCommand
        {
            get { return (ICommand)GetValue(StartCommandProperty); }
            set { SetValue(StartCommandProperty, value); }
        }

        public ICommand StopCommand
        {
            get { return (ICommand)GetValue(StopCommandProperty); }
            set { SetValue(StopCommandProperty, value); }
        }

        public bool IsRecording
        {
            get { return (bool)GetValue(IsRecordingProperty); }
            set { SetValue(IsRecordingProperty, value); }
        }

        public static readonly DependencyProperty StartCommandProperty = DependencyProperty.Register(nameof(StartCommand), typeof(ICommand), typeof(RecordButtonBehavior), new PropertyMetadata(null));
        public static readonly DependencyProperty StopCommandProperty = DependencyProperty.Register(nameof(StopCommand), typeof(ICommand), typeof(RecordButtonBehavior), new PropertyMetadata(null));
        public static readonly DependencyProperty IsRecordingProperty = DependencyProperty.Register(nameof(IsRecording), typeof(bool), typeof(RecordButtonBehavior), new PropertyMetadata(false, OnIsRecordingChanged));


        //プロパティ変更時の処理
        private static void OnIsRecordingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = d as RecordButtonBehavior;
            behavior?.UpdateButtonState();
        }

        private void UpdateButtonState()
        {
            if (IsRecording)
            {
                AssociatedObject.Command = StopCommand;
            }
            else
            {
                AssociatedObject.Command = StartCommand;
            }

            AssociatedObject.IsEnabled = AssociatedObject.Command != null && AssociatedObject.Command.CanExecute(null);
        }


        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.Click += AssociatedObject_Click;

            AssociatedObject.IsEnabled = AssociatedObject.Command != null && AssociatedObject.Command.CanExecute(null);
        }

        private void AssociatedObject_Click(object sender, RoutedEventArgs e)
        {
            AssociatedObject.Command?.Execute(null);
        }
    }
}
