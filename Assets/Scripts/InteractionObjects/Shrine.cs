using System;
using UnityEngine;
using System.Collections;
using Player;

public class Shrine : MonoBehaviour
{
    [Header("Settings")]
    public float cooldownTime = 5f;

    [Header("References")]
    public GameObject interactUI;
    public ParticleSystem healEffect;

    private bool isPlayerInRange = false;
    private bool isReady = true;
    private GameObject player;
    private Health _playerHP;

    private void Awake()
    {
        _playerHP = GameObject.FindGameObjectWithTag("Player").GetComponent<Health>();
        interactUI.SetActive(false);
    }

    void Update()
    {
        if (isPlayerInRange && isReady && PlayerInputHandler.Instance.InteractPressed)
        {
            SavePerformHeal();
        }
    }

    void SavePerformHeal()
    {
        isReady = false;
        interactUI.SetActive(false);

        // 播放特效
        if (healEffect != null) healEffect.Play();
        AudioManager.Instance.PlaySFX(SFXType.PickUp);

        _playerHP.FullHeal();
        CollectionSystem.SaveCollection();


        StartCoroutine(CooldownRoutine());
    }

    IEnumerator CooldownRoutine()
    {
        yield return new WaitForSeconds(cooldownTime);
        isReady = true;

        // 若冷卻結束時玩家仍在範圍內，重啟 UI
        if (isPlayerInRange)
        {
            interactUI.SetActive(true);
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            player = other.gameObject;
            if (isReady) interactUI.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            interactUI.SetActive(false);
        }
    }
}