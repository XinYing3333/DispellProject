// PauseView.cs
using System;
using UnityEngine;

namespace UI.Pause
{
    // 最小 View：只做顯示/隱藏與面板切換。
    // 之後你要加動畫：把 PlayOpen/PlayClose/PlayTabSwitch 做成播動畫，播完呼叫 NotifyXXX。
    public sealed class PauseView : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject root;

        [Header("Panels")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject optionsPanel;
        [SerializeField] private GameObject confirmPanel;

        public event Action OpenFinished;
        public event Action CloseFinished;
        public event Action TabSwitchFinished;

        void Reset()
        {
            root = gameObject;
        }

        public void SetVisible(bool visible)
        {
            if (root) root.SetActive(visible);
        }

        public void PlayOpen()
        {
            SetVisible(true);
            NotifyOpenFinished();
        }

        public void PlayClose()
        {
            NotifyCloseFinished();
        }

        public void ShowMain()
        {
            if (mainPanel) mainPanel.SetActive(true);
            if (optionsPanel) optionsPanel.SetActive(false);
            if (confirmPanel) confirmPanel.SetActive(false);
        }

        public void ShowOptions()
        {
            if (mainPanel) mainPanel.SetActive(false);
            if (optionsPanel) optionsPanel.SetActive(true);
            if (confirmPanel) confirmPanel.SetActive(false);
        }

        public void ShowConfirm()
        {
            if (mainPanel) mainPanel.SetActive(false);
            if (optionsPanel) optionsPanel.SetActive(false);
            if (confirmPanel) confirmPanel.SetActive(true);
        }

        public void PlayTabSwitch(OptionsTab from, OptionsTab to)
        {
            // 先不做動畫，直接完成
            NotifyTabSwitchFinished();
        }

        public void SetFocus(string focusId)
        {
            // 最小版本不做（你後面用來觸發高亮/特效）
        }

        public void SetEditing(bool isEditing, string fieldId)
        {
            // 最小版本不做（你後面用來觸發編輯模式特效）
        }

        // 給動畫事件/Signal 用
        public void NotifyOpenFinished() => OpenFinished?.Invoke();

        public void NotifyCloseFinished()
        {
            SetVisible(false);
            CloseFinished?.Invoke();
        }

        public void NotifyTabSwitchFinished() => TabSwitchFinished?.Invoke();
    }
}
