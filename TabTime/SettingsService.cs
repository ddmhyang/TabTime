namespace TabTime
{
    public class SettingsService : ISettingsService
    {
        public AppSettings LoadSettings() => DataManager.LoadSettings();
        public void SaveSettings(AppSettings settings) => DataManager.SaveSettings(settings);
    }
}