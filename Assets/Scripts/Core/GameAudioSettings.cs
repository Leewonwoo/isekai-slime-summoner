using DarkTonic.MasterAudio;
using UnityEngine;

namespace CrossDefense.Core
{
    public static class GameAudioSettings
    {
        const string MusicVolumeKey = "CrossDefense.Settings.MusicVolume";
        const string EffectsVolumeKey = "CrossDefense.Settings.EffectsVolume";
        const float DefaultVolume = 0.8f;

        public static float MusicVolume =>
            Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumeKey, DefaultVolume));

        public static float EffectsVolume =>
            Mathf.Clamp01(PlayerPrefs.GetFloat(EffectsVolumeKey, DefaultVolume));

        public static void SetMusicVolume(float volume)
        {
            float normalized = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(MusicVolumeKey, normalized);
            PersistentAudioSettings.MusicVolume = normalized;
        }

        public static void SetEffectsVolume(float volume)
        {
            float normalized = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(EffectsVolumeKey, normalized);
            PersistentAudioSettings.MixerVolume = normalized;
        }

        public static void ApplyStoredSettings()
        {
            PersistentAudioSettings.MusicVolume = MusicVolume;
            PersistentAudioSettings.MixerVolume = EffectsVolume;
        }

        public static void Save() => PlayerPrefs.Save();
    }
}
