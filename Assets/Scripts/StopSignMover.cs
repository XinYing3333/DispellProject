using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class StopSignMover : MonoBehaviour
{
    private Vector3 originalPosition;
    private Vector3 offsetPosition;
    private bool movedOut = false;

    [SerializeField] private float moveDistance = 5f;
    [SerializeField] private float moveDuration = 0.5f;

    void Start()
    {
        originalPosition = transform.position;
        offsetPosition = originalPosition + transform.forward * moveDistance;
    }

    public void TriggerMove()
    {
        StopAllCoroutines();

        if (!movedOut)
        {
            StartCoroutine(MoveToPosition(offsetPosition));
        }
        else
        {
            StartCoroutine(MoveToPosition(originalPosition));
        }

        movedOut = !movedOut;
    }

    IEnumerator MoveToPosition(Vector3 targetPos)
    {
        Vector3 start = transform.position;
        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            transform.position = Vector3.Lerp(start, targetPos, elapsed / moveDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPos;
    }
    
#if UNITY_EDITOR

    private void OnDrawGizmos()
    {
        // 起始位置
        Vector3 start = transform.position;
    
        // 目標位置（面前方向的 offset）
        Vector3 end = start + transform.forward * moveDistance;

        // 畫一條線：從原始位置到 offset 位置
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(start, end);

        // 畫個球表示目標位置
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(end, 0.2f);
    }
    
#endif
}