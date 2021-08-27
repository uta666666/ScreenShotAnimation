using Livet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenShotAnimation.ViewModels
{
    public class ProgressViewModel : ViewModel
    {
        public string Message { get; private set; }

        public ProgressViewModel(string message)
        {
            Message = message;
        }
    }
}
