using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace ScreenShotAnimation.Models
{
    public class FrameRate : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private int _fps = 15;

        public int Fps
        {
            get
            {
                return _fps;
            }
            set
            {
                if (_fps == value) return;

                _fps = value;
                RaisePropertyChanged();
            }
        }

        public int Interval
        {
            get
            {
                return (int)Math.Round(1000d / _fps);
            }
        }

    }
}
