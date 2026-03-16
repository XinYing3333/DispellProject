using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using Cinemachine;

public class ObstacleGroup : MonoBehaviour
{
    [Header("Obstacle Fragments Settings")]
    public Transform obstacleParent;

    public float minFloatHeight = 30f;
    public float maxFloatHeight = 50f;
    public float minDuration = 3f;
    public float maxDuration = 5f;
    public float maxDelay = 1.5f;
    public float maxRotation = 5f;

    [Header("Shake Settings")] 
    public float shakeDuration = 0.55f;
    public float shakeStrength = 0.2f;

    [Header("VFX & Audio")] 
    public ParticleSystem absorbParticlePrefab;

    [Header("Cinemachine Shake")]
    [SerializeField] private CinemachineImpulseSource impulseSource;
    [SerializeField] private float globalShakeDelay = 0.2f; // 震動延遲，與碎塊顫抖同步

    private List<Transform> obstacleFragments = new List<Transform>();
    private bool isInteracted = false;

    private void Awake()
    {
        // 自動獲取組件
        if (impulseSource == null) 
            impulseSource = GetComponent<CinemachineImpulseSource>();
            
        if (obstacleParent != null)
        {
            foreach (Transform child in obstacleParent)
            {
                obstacleFragments.Add(child);
            }
        }
    }

    [ContextMenu("Test Absorb Animation")]
    public void OnInteract()
    {
        if (isInteracted || obstacleFragments.Count == 0) return;
        isInteracted = true;

        // 觸發相機震動
        ExecuteCinemachineShake();

        foreach (Transform fragment in obstacleFragments)
        {
            AnimateFragment(fragment);
        }
    }

    private void ExecuteCinemachineShake()
    {
        if (impulseSource == null) return;

        // 使用 Sequence 稍微延後震動，配合碎塊啟動的時機
        DOTween.Sequence()
            .AppendInterval(globalShakeDelay)
            .OnComplete(() =>
            {
                // 產生震動訊號
                impulseSource.GenerateImpulse();
            });
    }

    private void AnimateFragment(Transform fragment)
    {
        float randomDelay = Random.Range(0f, maxDelay);
        float randomHeight = Random.Range(minFloatHeight, maxFloatHeight);
        float randomDuration = Random.Range(minDuration, maxDuration);
        Vector3 randomRotation = new Vector3(
            Random.Range(-maxRotation, maxRotation),
            Random.Range(-maxRotation, maxRotation),
            Random.Range(-maxRotation, maxRotation)
        );

        Sequence s = DOTween.Sequence();

        s.PrependInterval(randomDelay);

        s.AppendCallback(() =>
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(SFXType.RockShake);

            if (absorbParticlePrefab != null)
            {
                ParticleSystem pfx = Instantiate(absorbParticlePrefab, fragment.position, Quaternion.identity);
                pfx.Play();
                Destroy(pfx.gameObject, 2f);
            }
        });

        s.Append(fragment.DOShakePosition(shakeDuration, shakeStrength, 10, 90));

        s.Append(fragment.DOMoveY(fragment.position.y + randomHeight, randomDuration).SetEase(Ease.InCubic));
        s.Join(fragment.DORotate(randomRotation, randomDuration, RotateMode.WorldAxisAdd).SetEase(Ease.Linear));

        s.Join(fragment.DOScale(Vector3.zero, randomDuration * 0.7f).SetEase(Ease.InQuad).SetDelay(0.2f));

        s.OnComplete(() => fragment.gameObject.SetActive(false));
    }
}