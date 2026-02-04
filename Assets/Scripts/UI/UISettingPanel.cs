using Player;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DefaultNamespace
{
    public class UISettingPanel : MonoBehaviour
    {
        public enum SettingPanel
        {
            Audio = 0,
            Graphics = 1,
            Controls = 2
        }

        [SerializeField] private SettingPanel _currentPanel = SettingPanel.Audio;

        [SerializeField] private GameObject audioPanel;
        [SerializeField] private GameObject graphicsPanel;
        [SerializeField] private GameObject controlsPanel;
        [SerializeField] private GameObject audioFirst, graphicsFirst, controlsFirst;
        [SerializeField] private TMP_Text audioTitle, graphicsTitle, controlsTitle;

        private void OnEnable()
        {
            SetSettingPanel();
        }

        private void Update()
        {
            // ===== Debug Input =====
            if (PlayerInputHandler.Instance.SettingLeftPressed || Input.GetKeyDown(KeyCode.Q))
            {
                SwitchPanel(-1);
            }

            if (PlayerInputHandler.Instance.SettingRightPressed || Input.GetKeyDown(KeyCode.E))
            {
                SwitchPanel(+1);
            }
        }

        // dir = -1 左, +1 右
        public void SwitchPanel(int dir)
        {
            int next = (int)_currentPanel + dir;
            next = Mathf.Clamp(next, 0, 2);

            if ((int)_currentPanel == next)
                return;

            _currentPanel = (SettingPanel)next;
            SetSettingPanel();
        }

        private void SetSettingPanel()
        {
            audioPanel.SetActive(_currentPanel == SettingPanel.Audio);
            graphicsPanel.SetActive(_currentPanel == SettingPanel.Graphics);
            controlsPanel.SetActive(_currentPanel == SettingPanel.Controls);
            EnsureSelection();
        }
        
        private void EnsureSelection()
        {
            if (EventSystem.current == null) return;

            GameObject target = null;

            switch (_currentPanel)
            {
                case SettingPanel.Audio:
                    target = audioFirst;
                    audioTitle.color = Color.yellow;
                    graphicsTitle.color = Color.white;
                    controlsTitle.color = Color.white;
                    break;
                case SettingPanel.Graphics:
                    target = graphicsFirst;   
                    graphicsTitle.color = Color.yellow;
                    controlsTitle.color = Color.white;
                    audioTitle.color = Color.white;
                    break;
                case SettingPanel.Controls:
                    target = controlsFirst; 
                    controlsTitle.color = Color.yellow;
                    graphicsTitle.color = Color.white;
                    audioTitle.color = Color.white;
                    break;
            }

            if (target != null)
            {
                EventSystem.current.SetSelectedGameObject(target);
            }
        }
    }
}