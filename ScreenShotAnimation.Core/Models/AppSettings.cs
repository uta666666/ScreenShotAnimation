using ScreenShotAnimation.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenShotAnimation.Models
{
    public class AppSettings
    {
        public RecorderSettings RecorderSettings { get; set; }
        public int FormStyle { get; set; }
    }

    public class RecorderSettings : IUserSettings
    {
        public int Fps { get; set; }

        public void Save()
        {
            //TODO
        }
    }
}
