namespace TetrisCourse
{
    public sealed class SettingsService
    {
        private readonly StorageService storageService;

        public UserSettings Settings { get; private set; }
        public AppFlags Flags { get; private set; }

        public SettingsService(StorageService storageService)
        {
            this.storageService = storageService;
            Settings = storageService.LoadSettings();
            Flags = storageService.LoadFlags();
        }

        public void ToggleSound()
        {
            Settings.soundOn = !Settings.soundOn;
            SaveSettings();
        }

        public void ToggleMusic()
        {
            Settings.musicOn = !Settings.musicOn;
            SaveSettings();
        }

        public void ToggleContrast()
        {
            Settings.contrastMode = !Settings.contrastMode;
            SaveSettings();
        }

        public void MarkTutorialShown()
        {
            Settings.tutorialShown = true;
            Flags.firstLaunchResolved = true;
            SaveSettings();
            storageService.SaveFlags(Flags);
        }

        private void SaveSettings()
        {
            storageService.SaveSettings(Settings);
        }
    }
}
