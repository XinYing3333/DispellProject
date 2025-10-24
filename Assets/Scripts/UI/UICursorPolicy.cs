using System.Collections.Generic;
using UnityEngine;

public class UICursorPolicy : MonoBehaviour
{
    public static UICursorPolicy Instance { get; private set; }

    // 哪些面板要求游標可見（只在鍵鼠模式下會生效）
    private readonly HashSet<Object> _openPanels = new();

    private void Awake()
    {
        if (Instance) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        if (ControlSchemeHint.Instance)
            ControlSchemeHint.Instance.OnModeChanged += _ => Apply();
    }

    private void OnDisable()
    {
        if (ControlSchemeHint.Instance)
            ControlSchemeHint.Instance.OnModeChanged -= _ => Apply();
    }

    public void PanelOpened(Object owner)
    {
        if (owner == null) return;
        _openPanels.Add(owner);
        Apply();
    }

    public void PanelClosed(Object owner)
    {
        if (owner == null) return;
        _openPanels.Remove(owner);
        Apply();
    }

    public void Apply()
    {
        bool anyUIOpen = _openPanels.Count > 0;
        bool isGamepad = ControlSchemeHint.Instance && ControlSchemeHint.Instance.IsGamepad;

        if (isGamepad)
        {
            // 搖桿一律隱藏
            Cursor.visible   = false;
            Cursor.lockState = CursorLockMode.Locked;
            return;
        }

        // 鍵鼠：有面板 → 顯示；否則隱藏
        if (anyUIOpen)
        {
            Cursor.visible   = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Cursor.visible   = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}