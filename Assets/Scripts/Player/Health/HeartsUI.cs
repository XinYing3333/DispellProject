// HeartsUI.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HeartsUI : MonoBehaviour
{
    public Health target;
    public Image heartPrefab;           // 一個「滿心」圖
    public Sprite fullHeart;
    public Sprite emptyHeart;
    private readonly List<Image> _pool = new();

    private void OnEnable()
    {
        if (target)
        {
            target.OnHealthChanged += Refresh;
            Refresh(target.GetCurrent(), target.GetMax());
        }
    }
    private void OnDisable()
    {
        if (target) target.OnHealthChanged -= Refresh;
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