// OptionsController.cs
using UnityEngine;

namespace UI.Pause
{
    // 只管：Tab、Browsing/Editing、欄位索引、調整 Draft。
    public sealed class OptionsController
    {
        public OptionsTab Tab { get; private set; } = OptionsTab.Audio;
        public OptionsSubState SubState { get; private set; } = OptionsSubState.Browsing;

        // 每個 Tab 的焦點欄位（最小：各 3 個）
        int audioIndex = 0;   // 0 Master, 1 Music, 2 SFX
        int videoIndex = 0;   // 0 Resolution, 1 Aspect, 2 Fullscreen
        int ctrlIndex  = 0;   // 0 InvertCamera, 1 Sensitivity, 2 (保留)

        public void EnterDefault()
        {
            Tab = OptionsTab.Audio;
            SubState = OptionsSubState.Browsing;
        }

        public int CurrentFieldIndex => Tab switch
        {
            OptionsTab.Audio => audioIndex,
            OptionsTab.Video => videoIndex,
            _ => ctrlIndex
        };

        public bool IsEditing => SubState == OptionsSubState.Editing;

        public void TabLeft()
        {
            if (IsEditing) return;
            Tab = (OptionsTab)(((int)Tab + 2) % 3);
        }

        public void TabRight()
        {
            if (IsEditing) return;
            Tab = (OptionsTab)(((int)Tab + 1) % 3);
        }

        public void Navigate(int dx, int dy)
        {
            if (!IsEditing)
            {
                // Browsing：只換焦點（上下）
                if (dy != 0) MoveField(-dy); // 上=+1 => -1
                return;
            }

            // Editing：調整值（左右/上下都算）
            int delta = dx != 0 ? dx : (dy != 0 ? dy : 0);
            if (delta != 0) AdjustValue(delta);
        }

        public void Submit()
        {
            if (!IsEditing)
            {
                SubState = OptionsSubState.Editing;
                return;
            }

            SubState = OptionsSubState.Browsing;
        }

        public void Cancel()
        {
            if (IsEditing)
                SubState = OptionsSubState.Browsing;
            // Browsing 的 Cancel 由 PauseController 決定是回 MainMenu
        }

        void MoveField(int step)
        {
            switch (Tab)
            {
                case OptionsTab.Audio:
                    audioIndex = Mathf.Clamp(audioIndex + step, 0, 2);
                    break;
                case OptionsTab.Video:
                    videoIndex = Mathf.Clamp(videoIndex + step, 0, 2);
                    break;
                case OptionsTab.Controls:
                    ctrlIndex = Mathf.Clamp(ctrlIndex + step, 0, 1); // 最小：2 個欄位
                    break;
            }
        }

        void AdjustValue(int delta)
        {
            var d = SettingsStore.Draft;

            switch (Tab)
            {
                case OptionsTab.Audio:
                    AdjustAudio(d, audioIndex, delta);
                    break;

                case OptionsTab.Video:
                    AdjustVideo(d, videoIndex, delta);
                    break;

                case OptionsTab.Controls:
                    AdjustControls(d, ctrlIndex, delta);
                    break;
            }
        }

        static void AdjustAudio(GameSettings s, int idx, int delta)
        {
            const int step = 5;
            if (idx == 0) s.audio.master = Mathf.Clamp(s.audio.master + delta * step, 0, 100);
            if (idx == 1) s.audio.music  = Mathf.Clamp(s.audio.music  + delta * step, 0, 100);
            if (idx == 2) s.audio.sfx    = Mathf.Clamp(s.audio.sfx    + delta * step, 0, 100);
        }

        static void AdjustVideo(GameSettings s, int idx, int delta)
        {
            if (idx == 0)
            {
                int max = Mathf.Max(0, OptionsData.Resolutions.Count - 1);
                s.video.resolutionIndex = Mathf.Clamp(s.video.resolutionIndex + delta, 0, max);
            }
            else if (idx == 1)
            {
                int max = Mathf.Max(0, OptionsData.AspectRatios.Count - 1);
                s.video.aspectIndex = Mathf.Clamp(s.video.aspectIndex + delta, 0, max);
            }
            else if (idx == 2)
            {
                // fullscreen：任何 delta 都翻轉
                s.video.fullscreen = !s.video.fullscreen;
            }
        }

        static void AdjustControls(GameSettings s, int idx, int delta)
        {
            if (idx == 0)
            {
                s.controls.invertCamera = !s.controls.invertCamera;
            }
            else if (idx == 1)
            {
                const int step = 5;
                s.controls.sensitivity = Mathf.Clamp(s.controls.sensitivity + delta * step, 1, 200);
            }
        }
    }
}
