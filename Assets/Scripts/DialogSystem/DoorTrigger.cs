using System.Collections;
using UnityEngine;

namespace World
{
    public class DoorTrigger : MonoBehaviour
    {
        [Header("Target Renderers (same material instances ok)")]
        [SerializeField] private Renderer[] targetRenderers;

        [Header("ShaderGraph Reference Names (NOT Display Names)")]
        [SerializeField] private string fillAmountRef ;     // 改成你 Blackboard 的 Reference
        [SerializeField] private string visibleAmountRef ; // 改成你 Blackboard 的 Reference

        [Header("Lerp Targets")]
        [SerializeField] private float fillTarget = 20f;
        [SerializeField] private float visibleTarget = 1f;

        [Header("Durations")]
        [SerializeField] private float fillDuration = 1.0f;
        [SerializeField] private float visibleDuration = 0.5f;

        [Header("Door Object To Disable")]
        [SerializeField] private GameObject doorObject;

        [Header("Behavior")]
        [SerializeField] private bool oneShot = true;
        [SerializeField] private bool resetOnAwake = true;
        [SerializeField] private float fillStartValue = 0f;
        [SerializeField] private float visibleStartValue = 0f;

        int _fillId;
        int _visibleId;
        MaterialPropertyBlock _mpb;

        bool _triggered;
        bool _running;

        void Awake()
        {
            _fillId = Shader.PropertyToID(fillAmountRef);
            _visibleId = Shader.PropertyToID(visibleAmountRef);
            _mpb = new MaterialPropertyBlock();

            if (resetOnAwake)
            {
                SetFloatAll(_fillId, fillStartValue);
                SetFloatAll(_visibleId, visibleStartValue);
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (_running) return;
            if (oneShot && _triggered) return;
            if (!other.CompareTag("Player")) return;

            _triggered = true;
            StartCoroutine(CoRun());
        }

        IEnumerator CoRun()
        {
            _running = true;

            // 1) Fill Amount -> 20
            yield return LerpFloat(_fillId, fillTarget, fillDuration);

            // 2) visible amount -> 1
            yield return LerpFloat(_visibleId, visibleTarget, visibleDuration);

            // 3) 關閉大門物件
            if (doorObject) doorObject.SetActive(false);

            _running = false;
        }

        IEnumerator LerpFloat(int propId, float to, float duration)
        {
            float from = GetFloatFirst(propId);

            if (duration <= 0.0001f)
            {
                SetFloatAll(propId, to);
                yield break;
            }

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float a = Mathf.Clamp01(t / duration);
                float v = Mathf.Lerp(from, to, a);
                SetFloatAll(propId, v);
                yield return null;
            }

            SetFloatAll(propId, to);
        }

        float GetFloatFirst(int propId)
        {
            if (targetRenderers == null || targetRenderers.Length == 0 || !targetRenderers[0])
                return 0f;

            var r = targetRenderers[0];
            r.GetPropertyBlock(_mpb);
            return _mpb.GetFloat(propId);
        }

        void SetFloatAll(int propId, float value)
        {
            if (targetRenderers == null) return;

            for (int i = 0; i < targetRenderers.Length; i++)
            {
                var r = targetRenderers[i];
                if (!r) continue;

                r.GetPropertyBlock(_mpb);
                _mpb.SetFloat(propId, value);
                r.SetPropertyBlock(_mpb);
            }
        }
    }
}
