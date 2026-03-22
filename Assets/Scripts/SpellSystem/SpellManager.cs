using System.Collections.Generic;
using Player;
using SpellSystem;
using UnityEngine;

public class SpellManager : MonoBehaviour
{
    public static SpellManager Instance { get; private set; }

    [SerializeField] private int maxActiveNodes = 2;
    private Queue<ISpellAffectable> _activeEffects = new Queue<ISpellAffectable>();

    private void Awake() => Instance = this;

    private void Update()
    {
        // 偵測 Y 鍵收回
        if (PlayerInputHandler.Instance.SkillPressed)
        {
            RecallAll();
        }
    }

    public void RegisterEffect(ISpellAffectable target, SpellType type, Vector3 hitPoint)
    {
        // 自動恢復邏輯：超過數量則彈出最舊的
        if (_activeEffects.Count >= maxActiveNodes)
        {
            var oldest = _activeEffects.Dequeue();
            oldest.OnSpellRecall();
        }

        target.OnSpellHit(type, hitPoint);
        _activeEffects.Enqueue(target);
    }

    public void RecallAll()
    {
        while (_activeEffects.Count > 0)
        {
            _activeEffects.Dequeue().OnSpellRecall();
        }
    }
}