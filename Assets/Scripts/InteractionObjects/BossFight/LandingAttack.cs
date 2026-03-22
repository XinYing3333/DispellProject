using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BossFight;
using Object = UnityEngine.Object;

public class LandingAttack : IBossAttack
{
    public string Id => "Landing";
    private readonly LandingConfig cfg;
    private readonly LandingTelegraph telegraphPrefab;
    private readonly Health playerHP;

    // 物件池與追蹤列表
    private static Queue<GameObject> rockPool = new Queue<GameObject>();
    private static List<GameObject> activeRocks = new List<GameObject>();
    private const int MAX_ROCKS_ON_FIELD = 6;

    public LandingAttack(LandingConfig cfg, LandingTelegraph telegraphPrefab, Health playerHP)
    {
        this.cfg = cfg;
        this.telegraphPrefab = telegraphPrefab;
        this.playerHP = playerHP;
    }

    public IEnumerator Execute(BossContext C)
    {
        Vector3 playerPos = C.Player.position;
        Vector3 landPoint = C.Services.GetGroundBelow(playerPos) + Vector3.up * C.Owner.groundOffset;

        // 預警處理
        bool telegraphDone = false;
        var tele = LandingTelegraph.Spawn(landPoint - Vector3.up * C.Owner.groundOffset, telegraphPrefab,
            cfg.telegraphTime, cfg.telegraphStartRadius, cfg.telegraphEndRadius);

        if (tele != null)
        {
            tele.OnTelegraphFinished += () => telegraphDone = true;
        }
        else
        {
            telegraphDone = true; // 防錯：若 Spawn 失敗直接繼續
        }

        // 預熱移動
        float preAlignTime = cfg.telegraphTime * 0.8f;
        yield return new WaitForSeconds(preAlignTime);
        yield return C.Services.MoveHorizontalTo(C.ModelRoot, landPoint + Vector3.up * cfg.hoverHeight, 35f);

        // 等待預警結束
        float timeout = 2f; // 安全計時器避免卡死
        while (!telegraphDone && timeout > 0)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        // 瞬間砸下
        C.Anim.Play("bird-glide-ani");
        yield return C.Services.MoveTo(C.ModelRoot, landPoint, cfg.descendSpeed * 1.5f);

        // --- 加入特效位置 ---
        if (cfg.landVFXPrefab) cfg.landVFXPrefab.Play();
        // ------------------
        
        // 傷害與生成石頭
        C.Services.DoLandingAoE(landPoint, cfg.landAoERadius, cfg.landAoEDamage, playerHP);
        
        // 執行生成邏輯 (只有在數量未達上限時才生成)
        CleanActiveRocksList(); // 先清理已被外部銷毀的無效引用
        SpawnRocks(landPoint);

        // 硬直
        yield return new WaitForSeconds(cfg.stunDuration);
        
        // 飛回高度
        C.Anim.Play("bird-fly-ani");
        yield return C.Services.MoveVerticalTo(C.ModelRoot, landPoint.y + cfg.hoverHeight, cfg.riseSpeed);
    }

    private void SpawnRocks(Vector3 center)
    {
        if (cfg.rockPrefab == null || activeRocks.Count >= MAX_ROCKS_ON_FIELD) return;

        // 計算剩餘額度，隨機生成數量不得超過上限
        int remainingQuota = MAX_ROCKS_ON_FIELD - activeRocks.Count;
        int randCount = UnityEngine.Random.Range(0, 4); 
        int countToSpawn = Mathf.Min(randCount, remainingQuota);

        if (countToSpawn <= 0) return;

        float angleStep = 360f / countToSpawn;

        for (int i = 0; i < countToSpawn; i++)
        {
            float angle = i * angleStep + UnityEngine.Random.Range(0f, 30f);
            Vector3 offset = Quaternion.Euler(0, angle, 0) * Vector3.forward * cfg.rockSpawnRadius;
            Vector3 spawnPos = center + offset;
            
            if (Physics.Raycast(spawnPos + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 10f, LayerMask.GetMask("Ground")))
            {
                spawnPos = hit.point;
            }

            GameObject rock = GetRockFromPool(spawnPos);
            activeRocks.Add(rock);
        }
    }

    private GameObject GetRockFromPool(Vector3 position)
    {
        GameObject rock;
        if (rockPool.Count > 0)
        {
            rock = rockPool.Dequeue();
            if (rock != null)
            {
                rock.transform.position = position;
                rock.transform.rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0, 360f), 0);
                rock.SetActive(true);
                return rock;
            }
        }
        
        rock = Object.Instantiate(cfg.rockPrefab, position, Quaternion.Euler(0, UnityEngine.Random.Range(0, 360f), 0));
        return rock;
    }

    // 當石頭被玩家破壞或自然消失時，應由該石頭腳本呼叫此處回收（建議透過 Event 或直接調用）
    public static void ReturnRockToPool(GameObject rock)
    {
        if (activeRocks.Contains(rock))
        {
            activeRocks.Remove(rock);
            rock.SetActive(false);
            rockPool.Enqueue(rock);
        }
    }

    private void CleanActiveRocksList()
    {
        // 移除所有 null 引用（防止物件在場上被 Destroy 而非 SetActive(false)）
        activeRocks.RemoveAll(r => r == null || !r.activeInHierarchy);
    }
}