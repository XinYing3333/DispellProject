using System.Collections.Generic;
using Player;
using SpellSystem;
using UnityEngine;

public class SpellManager : MonoBehaviour
{
    public static SpellManager Instance { get; private set; }
    
    [SerializeField] private int maxActiveNodes = 2;
    private Queue<ISpellAffectable> _activeEffects = new Queue<ISpellAffectable>();

    // 提供外部查詢目前是否有啟用的法術
    public bool HasActiveSpells => _activeEffects.Count > 0;

    private void Awake() => Instance = this;

    private void Update()
    {
        if (PlayerInputHandler.Instance.SkillPressed && HasActiveSpells)
        {
            RecallAll();
        }
    }

    public void RegisterEffect(ISpellAffectable target, SpellType type, Vector3 hitPoint)
    {
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
            AudioManager.Instance.PlaySFX(SFXType.Recall);
            _activeEffects.Dequeue().OnSpellRecall();
        }
    }
}