using UnityEngine;
using System;

public interface IPlayerInputSource
{
    Vector2 MoveInput { get; }
    float MoveSpeedMultiplier { get; }
    bool IsCollecting { get; }
    bool IsAiming { get; }
    bool InteractPressed { get; }

    bool JumpPressed { get; }  
    bool SkillPressed { get; }

    bool DashPressed { get; }

    void ResetJump();
    void ResetDash();
    
    void SetSpellType(SpellType newSpellType);


    event Action OnJump;
    event Action OnSkill;
    event Action OnDash;
    event Action OnShoot;
    event Action OnSwitchThrow;
}
