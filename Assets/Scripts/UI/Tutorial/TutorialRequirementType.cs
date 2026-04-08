namespace DefaultNamespace.Tutorial
{
    public enum TutorialRequirementType
    {
        // Actions (瞬時觸發)
        Jump,
        Dash,
        Shoot,
        Skill,
        Collect,
        Interact,
        Target,
    
        // States (持續狀態)
        IsAiming,
        IsCollecting,
        IsPaused,
        IsTargeting,
        IsMoving,
        InAir,
    
        // Events (外部事件標記)
        FirstAdsorb,
        TotemCollectSuccess,
        ThrowAStoppablePlatform,
        FirstStunEnemy,
        FirstCollectEnemy,
        FirstThrowTraffic,
        EnemyDefeated,
        BossKilled
    }
}