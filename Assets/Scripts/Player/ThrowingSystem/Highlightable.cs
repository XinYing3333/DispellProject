using Player.InteractionSystem;
using UnityEngine;

[DisallowMultipleComponent]
public class Highlightable : MonoBehaviour, IFocusable
{
    [Header("描邊 / 高亮節點")]
    [SerializeField] private Outline outlineScript; // 可放 Outline、或任何繼承Behaviour的效果腳本
    [SerializeField] private GameObject outlineObject; // 若你只是用一個外框子物件

    void Awake()
    {
        SetEnabled(false);
    }

    public void OnFocusGained() => SetEnabled(true);
    public void OnFocusLost()  => SetEnabled(false);

    private void SetEnabled(bool on)
    {
        if (outlineScript) outlineScript.enabled = on;
        if (outlineObject) outlineObject.SetActive(on);
    }

    void OnDisable() => SetEnabled(false);
}