using UnityEngine;
using System.Collections;

public class SinkingPlatform : MonoBehaviour
{
    [Header("位移參數")]
    public float sinkDistance = 0.5f;
    public float sinkSpeed = 2f;
    public float recoverSpeed = 1f;
    public float recoverDelay = 2f;

    private Vector3 initialPosition;
    private Vector3 targetPosition;
    private bool isPlayerOn = false;
    private Coroutine recoverRoutine;

    void Start()
    {
        initialPosition = transform.position;
        targetPosition = initialPosition + Vector3.down * sinkDistance;
    }

    void Update()
    {
        if (isPlayerOn)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, sinkSpeed * Time.deltaTime);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // 檢查所有碰撞點
            foreach (ContactPoint contact in collision.contacts)
            {
                // 法線向上 (0, 1, 0) 表示玩家從上方壓在平台表面
                // 判斷 contact.normal.y 是否小於 -0.5 (表示碰撞力來自上方)
                if (contact.normal.y < -0.5f)
                {
                    isPlayerOn = true;
                    if (recoverRoutine != null) StopCoroutine(recoverRoutine);
                    break; 
                }
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            isPlayerOn = false;
            if (recoverRoutine != null) StopCoroutine(recoverRoutine);
            recoverRoutine = StartCoroutine(RecoverSequence());
        }
    }

    IEnumerator RecoverSequence()
    {
        yield return new WaitForSeconds(recoverDelay);

        while (transform.position != initialPosition && !isPlayerOn)
        {
            transform.position = Vector3.MoveTowards(transform.position, initialPosition, recoverSpeed * Time.deltaTime);
            yield return null;
        }
    }
}