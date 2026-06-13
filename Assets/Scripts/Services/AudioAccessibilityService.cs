using UnityEngine;

namespace TetrisCourse
{
    public sealed class AudioAccessibilityService
    {
        private readonly AudioSource musicSource;

        public AudioAccessibilityService(AudioSource musicSource)
        {
            this.musicSource = musicSource;
        }

        public void Apply(UserSettings settings, UIManager uiManager)
        {
            // Фоновая музыка управляется отдельным флагом musicOn.
            // Источник зациклен и играет всегда; при выключении мы его просто заглушаем,
            // чтобы музыка корректно возобновилась после разблокировки звука браузером (WebGL).
            if (musicSource != null)
            {
                musicSource.mute = !settings.musicOn;
            }

            // Звуковые эффекты (например, падение фигур) будут добавлены позже
            // и тогда будут управляться флагом soundOn независимо от музыки.

            uiManager.ApplySettings(settings);
        }
    }
}
