using UnityEngine;
using UnityEngine.UI;

public class OptionTabController : MonoBehaviour
{
    public GameObject soundPanel;
    public GameObject graphicPanel;
    public GameObject tutorPanel;

    public Button soundButton;
    public Button graphicButton;
    public Button tutorButton;

    void Start()
    {
        soundButton.onClick.AddListener(ShowSound);
        graphicButton.onClick.AddListener(ShowGraphic);
        tutorButton.onClick.AddListener(ShowTutor);

        ShowSound(); // 預設顯示 SoundPanel
    }

    public void ShowSound()
    {
        soundPanel.SetActive(true);
        graphicPanel.SetActive(false);
        tutorPanel.SetActive(false);
    }

    public void ShowGraphic()
    {
        soundPanel.SetActive(false);
        graphicPanel.SetActive(true);
        tutorPanel.SetActive(false);
    }

    public void ShowTutor()
    {
        soundPanel.SetActive(false);
        graphicPanel.SetActive(false);
        tutorPanel.SetActive(true);
    }
}
