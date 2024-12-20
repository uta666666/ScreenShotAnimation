using Livet.Messaging;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace ScreenShotAnimation.ViewModels
{
    public class CaptureRectViewModel : Livet.ViewModel
    {
        public CaptureRectViewModel()
        {
            CaptureWidth = new ReactiveProperty<double>();
            CaptureHeight = new ReactiveProperty<double>();
            CapturePointX = new ReactiveProperty<double>();
            CapturePointY = new ReactiveProperty<double>();
            StrokeColor = new ReactiveProperty<Color>(Colors.Blue);

            CloseCommand = new ReactiveCommand();
            CloseCommand.Subscribe(() =>
            {
                Messenger.Raise(new InteractionMessage("CloseKey"));
                CaptureWidth.Value = 0;
                CaptureHeight.Value = 0;
            });
        }


        public ReactiveProperty<double> CaptureWidth { get; set; }

        public ReactiveProperty<double> CaptureHeight { get; set; }

        public ReactiveProperty<double> CapturePointX { get; set; }

        public ReactiveProperty<double> CapturePointY { get; set; }

        public ReactiveProperty<Color> StrokeColor { get; set; }


        public ReactiveCommand CloseCommand { get; private set; }
    }
}
