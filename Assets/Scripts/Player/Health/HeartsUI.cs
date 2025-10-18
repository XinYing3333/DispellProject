// HeartsUI.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using EventBus.Events.Health;

public class HeartsUI : MonoBehaviour
{
    [Header("Target")]
    public Health target;

    [Header("Prefabs & Sprites")]
    public Image heartPrefab;
    public Sprite fullHeart;
    public Sprite emptyHeart;

    private readonly List<Image> _pool = new();

    // 事件綁定（總線）
    private EventBinding<OnHealthChanged> _binding;
    void OnEnable()
    {
        _binding = new EventBinding<OnHealthChanged>(OnHealthChanged);
        EventBus<OnHealthChanged>.Register(_binding);

        if (target)
        {
            Refresh(target.GetCurrent(), target.GetMax());
        }
    }

    void OnDisable()
    {
        if (_binding == null) return;
        EventBus<OnHealthChanged>.Deregister(_binding);
        _binding = null;
    }

    private void OnHealthChanged(OnHealthChanged e)
    {
        if (!target || e.target != target.gameObject) return;
        Refresh(e.current, e.max);
    }

    void Refresh(int current, int max)
    {
        int hearts = Mathf.CeilToInt(max / (float)target.heartSize);
        int curHearts = Mathf.CeilToInt(current / (float)target.heartSize);

        // 補齊物件池
        while (_pool.Count < hearts)
        {
            var img = Instantiate(heartPrefab, transform, false);
            _pool.Add(img);
        }

        // 顯示/隱藏與滿空
        for (int i = 0; i < _pool.Count; i++)
        {
            bool active = i < hearts;
            _pool[i].gameObject.SetActive(active);
            if (active)
                _pool[i].sprite = i < curHearts ? fullHeart : emptyHeart;
        }
    }
}