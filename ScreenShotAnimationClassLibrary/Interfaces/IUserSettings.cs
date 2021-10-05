namespace ScreenShotAnimation.Interfaces
{
    public interface IUserSettings
    {
        void Save();

        int Fps { get; set; }
    }
}
