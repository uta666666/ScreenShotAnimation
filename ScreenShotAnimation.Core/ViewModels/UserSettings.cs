using ScreenShotAnimation.Interfaces;

namespace ScreenShotAnimation.ViewModels
{
    public class UserSettings : IUserSettings
    {
        public int Fps { get => Properties.Settings.Default.Fps; set => Properties.Settings.Default.Fps = value; }

        public void Save()
        {
            Properties.Settings.Default.Save();
        }
    }
}
