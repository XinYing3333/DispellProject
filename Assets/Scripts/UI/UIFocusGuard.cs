using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIFocusGuard : MonoBehaviour
{
    [SerializeField] private bool enableGuard = true;
    [SerializeField] private float reselectCooldown = 0.05f;

    private float _cooldown;
    private readonly Stack<GameObject> _firstSelectedStack = new();
    private GameObject _lastSelected;

    private void OnEnable()
    {
        if (ControlSchemeHint.Instance)
            ControlSchemeHint.Instance.OnModeChanged += _ => _cooldown = 0f; // 切換時重置一下
    }

    private void OnDisable()
    {
        if (ControlSchemeHint.Instance)
            ControlSchemeHint.Instance.OnModeChanged -= _ => _cooldown = 0f;
    }

    void Update()
    {
        if (!enableGuard) return;
        _cooldown -= Time.unscaledDeltaTime;

        var es = EventSystem.current;
        if (!es) return;

        if (es.currentSelectedGameObject)
            _lastSelected = es.currentSelectedGameObject;

        // 只有在搖桿模式才保護
        if (ControlSchemeHint.Instance == null || !ControlSchemeHint.Instance.IsGamepad) return;
        if (_firstSelectedStack.Count == 0) return; // 沒有任何面板註冊，視為沒 UI

        if (es.currentSelectedGameObject == null && _cooldown <= 0f)
        {
            GameObject target =
                (_firstSelectedStack.Count > 0) ? _firstSelectedStack.Peek() :
                _lastSelected ? _lastSelected :
                FindAnySelectableInScene();

            if (target)
            {
                es.SetSelectedGameObject(null);
                es.SetSelectedGameObject(target);
                _cooldown = reselectCooldown;
            }
        }
    }

    public void PushFirstSelected(GameObject go)
    {
        if (go) _firstSelectedStack.Push(go);
    }

    public void PopFirstSelected(GameObject go)
    {
        var tmp = new Stack<GameObject>();
        while (_firstSelectedStack.Count > 0)
        {
            var top = _firstSelectedStack.Pop();
            if (top == go) break;
            tmp.Push(top);
        }
        while (tmp.Count > 0) _firstSelectedStack.Push(tmp.Pop());
    }

    private GameObject FindAnySelectableInScene()
    {
        var s = GameObject.FindFirstObjectByType<Selectable>();
        return s ? s.gameObject : null;
    }
}
