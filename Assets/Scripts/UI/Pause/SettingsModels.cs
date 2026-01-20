// SettingsModels.cs
using System;
using UnityEngine;

namespace UI.Pause
{
    [Serializable]
    public sealed class AudioSettings
    {
        [Range(0, 100)] public int master = 100;
        [Range(0, 100)] public int music = 80;
        [Range(0, 100)] public int sfx = 80;

        public AudioSettings Clone() => new AudioSettings { master = master, music = music, sfx = sfx };
    }

    [Serializable]
    public sealed class VideoSettings
    {
        public int resolutionIndex = 0;
        public int aspectIndex = 0;
        public bool fullscreen = true;

        public VideoSettings Clone() => new VideoSettings
        {
            resolutionIndex = resolutionIndex,
            aspectIndex = aspectIndex,
            fullscreen = fullscreen
        };
    }

    [Serializable]
    public sealed class ControlSettings
    {
        public bool invertCamera = false;
        [Range(1, 200)] public int sensitivity = 60;

        public ControlSettings Clone() => new ControlSettings
        {
            invertCamera = invertCamera,
            sensitivity = sensitivity
        };
    }

    [Serializable]
    public sealed class GameSettings
    {
        public AudioSettings audio = new AudioSettings();
        public VideoSettings video = new VideoSettings();
        public ControlSettings controls = new ControlSettings();

        public GameSettings Clone() => new GameSettings
        {
            audio = audio.Clone(),
            video = video.Clone(),
            controls = controls.Clone()
        };
    }

    public static class SettingsStore
    {
        // 現階段：記憶體版本（功能先跑通）
        public static GameSettings Runtime { get; private set; } = new GameSettings();
        public static GameSettings Draft { get; private set; } = new GameSettings();

        public static void BeginDraft() => Draft = Runtime.Clone();

        public static void ApplyDraft()
        {
            Runtime = Draft.Clone();
            // TODO: 套用到 AudioMixer / Screen / QualitySettings / 你的控制系統
        }

        public static void DiscardDraft() => Draft = Runtime.Clone();
    }
}
