using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BossFight;
using DG.Tweening;
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

    private LandingTelegraph _activeTelegraph;

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

        bool telegraphDone = false;

        _activeTelegraph = LandingTelegraph.Spawn(landPoint - Vector3.up * C.Owner.groundOffset, telegraphPrefab,
            cfg.telegraphTime, cfg.telegraphStartRadius, cfg.telegraphEndRadius);

        if (_activeTelegraph != null)
        {
            _activeTelegraph.OnTelegraphFinished += () => telegraphDone = true;
        }
        else
        {
            telegraphDone = true;
        }

        float preAlignTime = cfg.telegraphTime * 0.8f;
        yield return new WaitForSeconds(preAlignTime);
        yield return C.Services.MoveHorizontalTo(C.ModelRoot, landPoint + Vector3.up * cfg.hoverHeight, 35f);

        float timeout = 2f; 
        while (!telegraphDone && timeout > 0)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        // 刪除此處的 _activeTelegraph = null; 確保下墜期間遭中斷時仍保有參照

        C.Anim.Play("bird-glide-ani");
        yield return C.Services.MoveTo(C.ModelRoot, landPoint, cfg.descendSpeed * 1.5f);

        if (cfg.landVFXPrefab) cfg.landVFXPrefab.Play();
        if (CameraShakeManager.Instance != null)
        {
            CameraShakeManager.Instance.Shake(cfg.shakeForce, 0);
            RumbleManager.Instance.Rumble(0.4f, 0.8f, 1f);
        }

        C.Services.DoLandingAoE(landPoint, cfg.landAoERadius, cfg.landAoEDamage, playerHP);

        CleanActiveRocksList();
        SpawnRocks(landPoint);

        // 攻擊落地判定完成後，手動銷毀預警並釋放參照
        if (_activeTelegraph != null)
        {
            Object.Destroy(_activeTelegraph.gameObject);
            _activeTelegraph = null;
        }

        yield return new WaitForSeconds(cfg.stunDuration);

        C.Anim.Play("bird-fly-ani");
        yield return C.Services.MoveVerticalTo(C.ModelRoot, landPoint.y + cfg.hoverHeight, cfg.riseSpeed);
    }

    // 供外部強制中斷並清理殘留物件的方法
    public void Interrupt()
    {
        if (_activeTelegraph != null)
        {
            Object.Destroy(_activeTelegraph.gameObject);
            _activeTelegraph = null;
        }
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
            Vector3 dir = Quaternion.Euler(0, angle, 0) * Vector3.forward;
            Vector3 offset = dir * cfg.rockSpawnRadius;
            Vector3 targetPos = center + offset;

            if (Physics.Raycast(targetPos + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 10f, LayerMask.GetMask("Ground")))
            {
                targetPos = hit.point;
            }

            // 【修改點】起點不再是 center，而是沿著噴發方向(dir)往外推 2.5 單位（請根據 Boss 體型調整），避開 hitbox
            float safeOffsetRadius = 2.5f; 
            Vector3 startPos = center + (dir * safeOffsetRadius) + Vector3.up * 1.5f;
    
            GameObject rock = GetRockFromPool(startPos);
            activeRocks.Add(rock);

            float jumpDuration = 0.5f + UnityEngine.Random.Range(0f, 0.2f);
            float jumpPower = 3f + UnityEngine.Random.Range(0f, 1.5f);

            rock.transform.DOComplete();
            rock.transform.DOJump(targetPos, jumpPower, 1, jumpDuration).SetEase(Ease.Linear);
            rock.transform.DORotate(new Vector3(Random.Range(180, 360), Random.Range(180, 360), 0), jumpDuration,
                    RotateMode.FastBeyond360)
                .SetRelative()
                .SetEase(Ease.OutQuad);
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