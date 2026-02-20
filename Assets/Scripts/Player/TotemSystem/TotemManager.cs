using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem; // 假設你使用新版 Input System

public class TotemManager : MonoBehaviour
{
    [Header("Totem Settings")]
    public List<TotemData> ownedTotems = new List<TotemData>();
    private int _currentIndex = 0;

    [Header("References")]
    public TotemData currentTotem;
    // 這裡可以引用你的 UI 腳本來更新右下角的圓圈
    // public TotemUI totemUI; 

    private float _lastAttackTime;

    // 當玩家吸收特定念頭後，呼叫此方法
    public void AddTotem(TotemData newData)
    {
        if (!ownedTotems.Contains(newData))
        {
            ownedTotems.Add(newData);
            // 第一次獲得圖騰時自動選取
            if (ownedTotems.Count == 1) SelectTotem(0);
        }
    }

    // 切換圖騰 (由 Input 觸發)
    public void SwitchTotem(int direction)
    {
        if (ownedTotems.Count <= 1) return;

        _currentIndex = (_currentIndex + direction + ownedTotems.Count) % ownedTotems.Count;
        SelectTotem(_currentIndex);
    }

    private void SelectTotem(int index)
    {
        currentTotem = ownedTotems[index];
        Debug.Log($"當前圖騰切換為: {currentTotem.totemName}");
        // totemUI.UpdateUI(currentTotem); // 更新 UI 圖示
    }

    public void ExecuteAttack()
    {
        if (currentTotem == null || Time.time < _lastAttackTime + currentTotem.cooldown) return;

        switch (currentTotem.type)
        {
            case TotemType.Attack:
                PerformAttackTotem();
                break;
            case TotemType.Stun:
                PerformStunTotem();
                break;
            case TotemType.GiantBird:
                // 巨鳥可能是被動滑翔或主動觸發，這裡先留空
                break;
        }

        _lastAttackTime = Time.time;
    }

    // --- 具體邏輯實現 ---

    private void PerformAttackTotem()
    {
        // 1. 尋找距離最遠的敵人 (透過 Tag 或 Layer)
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        GameObject farthestEnemy = null;
        float maxDist = 0;

        foreach (var enemy in enemies)
        {
            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (dist > maxDist)
            {
                maxDist = dist;
                farthestEnemy = enemy;
            }
        }

        if (farthestEnemy != null)
        {
            Debug.Log($"向 {farthestEnemy.name} 施放追擊並擊暈");
            // 這裡實作你的彈道或擊暈邏輯
        }
    }

    private void PerformStunTotem()
    {
        Debug.Log("釋放小範圍定身波");
        float stunRadius = 5f;
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, stunRadius);
        foreach (var hit in hitColliders)
        {
            if (hit.CompareTag("Enemy"))
            {
                // hit.GetComponent<EnemyAI>().ApplyStun(3.0f);
            }
        }
    }
}