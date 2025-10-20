using System.Collections;
using UnityEngine;
using BossFight;


public class LandingAttack : IBossAttack
{
    public string Id => "Landing";
    private readonly LandingConfig cfg;
    private readonly LandingTelegraph telegraphPrefab;
    private readonly Health playerHP;

    public LandingAttack(LandingConfig cfg, LandingTelegraph telegraphPrefab, Health playerHP)
    {
        this.cfg = cfg;
        this.telegraphPrefab = telegraphPrefab;
        this.playerHP = playerHP;
    }

    public IEnumerator Execute(BossContext C)
    {
        // 升空
        yield return C.Services.MoveVerticalTo(C.ModelRoot, cfg.hoverHeight, cfg.riseSpeed);

        // 落點
        var landPoint = C.Services.GetGroundBelow(C.Player.position);
        var teleLandPoint = C.Services.GetGroundBelow(C.Player.position);
        landPoint += Vector3.up * C.Owner.groundOffset;

        // Telegraph
        bool done = false;
        var tele = LandingTelegraph.Spawn(teleLandPoint, telegraphPrefab, cfg.telegraphTime, cfg.telegraphStartRadius,
            cfg.telegraphEndRadius);
        tele.OnTelegraphFinished += () => done = true;
        while (!done) yield return null;

        // 對齊 & 降落
        yield return C.Services.MoveHorizontalTo(C.ModelRoot, landPoint + Vector3.up * cfg.hoverHeight, 20f);
        yield return C.Services.MoveTo(C.ModelRoot, landPoint, cfg.descendSpeed);

        // AoE & 硬直
        C.Services.DoLandingAoE(landPoint, cfg.landAoERadius, cfg.landAoEDamage, playerHP);
        yield return new WaitForSeconds(cfg.stunDuration);
    }
}