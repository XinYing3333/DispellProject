using System;
using System.Collections;
using UnityEngine;
using BossFight;
using Object = UnityEngine.Object;

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
        // 1. 升空
        yield return C.Services.MoveVerticalTo(C.ModelRoot, cfg.hoverHeight, cfg.riseSpeed);

        // 2. 鎖定落點
        var landPoint = C.Services.GetGroundBelow(C.Player.position);
        var teleLandPoint = C.Services.GetGroundBelow(C.Player.position);
        landPoint += Vector3.up * C.Owner.groundOffset;

        // 3. 預警 (Telegraph)
        bool done = false;
        var tele = LandingTelegraph.Spawn(teleLandPoint, telegraphPrefab, cfg.telegraphTime, cfg.telegraphStartRadius,
            cfg.telegraphEndRadius);
        tele.OnTelegraphFinished += () => done = true;
        while (!done) yield return null;

        // 4. 對齊與降落
        yield return C.Services.MoveHorizontalTo(C.ModelRoot, landPoint + Vector3.up * cfg.hoverHeight, 20f);
        C.Anim.Play("bird-glide-ani"); 
        yield return C.Services.MoveTo(C.ModelRoot, landPoint, cfg.descendSpeed);
        
        // 5. 傷害判定
        C.Services.DoLandingAoE(landPoint, cfg.landAoERadius, cfg.landAoEDamage, playerHP);

        // 6. 生成石頭
        SpawnRocks(landPoint);
        C.Anim.Play("bird-fly-ani"); 
        // 7. 硬直
        yield return new WaitForSeconds(cfg.stunDuration);
    }

    private void SpawnRocks(Vector3 center)
    {
        if (cfg.rockPrefab == null || cfg.rockCount <= 0) return;

        float angleStep = 360f / cfg.rockCount;
        for (int i = 0; i < cfg.rockCount; i++)
        {
            float angle = i * angleStep;
            Vector3 offset = Quaternion.Euler(0, angle, 0) * Vector3.forward * cfg.rockSpawnRadius;
            Vector3 spawnPos = center + offset;
            
            // 確保石頭貼齊地面
            RaycastHit hit;
            if (Physics.Raycast(spawnPos + Vector3.up * 5f, Vector3.down, out hit, 10f, LayerMask.GetMask("Ground")))
            {
                spawnPos = hit.point;
            }

            Object.Instantiate(cfg.rockPrefab, spawnPos, Quaternion.Euler(0, UnityEngine.Random.Range(0, 360f), 0));
        }
    }
}