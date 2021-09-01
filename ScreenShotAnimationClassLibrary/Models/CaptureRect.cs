using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace ScreenShotAnimation.Models
{
    public class CaptureRect : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private double _width;
        public double Width
        {
            get
            {
                return _width;
            }
            set
            {
                if (_width == value) return;

                _width = value;
                RaisePropertyChanged();
            }
        }

        private double _height;
        public double Height
        {
            get
            {
                return _height;
            }
            set
            {
                if (_height == value) return;

                _height = value;
                RaisePropertyChanged();
            }
        }

        private double _x;
        public double X
        {
            get
            {
                return _x;
            }
            set
            {
                if (_x == value) return;

                _x = value;
                RaisePropertyChanged();
            }
        }

        private double _y;
        public double Y
        {
            get
            {
                return _y;
            }
            set
            {
                if (_y == value) return;

                _y = value;
                RaisePropertyChanged();
            }
        }
    }
}
