using UnityEngine;
using System;

namespace BossFight
{
    [RequireComponent(typeof(LineRenderer))]
    public class LandingTelegraph : MonoBehaviour
    {
        [Header("Visual")]
        [SerializeField] private int circleSegments = 64;
        [SerializeField] private Color lineColor = new Color(1f, 0.2f, 0.2f, 0.9f);
        [SerializeField] private float lineWidth = 0.12f;

        [Header("Timing")]
        public float duration = 1.5f;
        public float startRadius = 3.0f;
        public float endRadius = 0.6f;

        public event Action OnTelegraphFinished;

        private LineRenderer _lr;
        private float _t;

        private void Awake()
        {
            _lr = GetComponent<LineRenderer>();
            _lr.positionCount = circleSegments + 1;
            _lr.loop = true;
            _lr.startWidth = lineWidth;
            _lr.endWidth = lineWidth;
            _lr.useWorldSpace = false;
            _lr.material = new Material(Shader.Find("Sprites/Default"));
            _lr.startColor = _lr.endColor = lineColor;
        }

        private void Update()
        {
            _t += Time.deltaTime;
            float pct = Mathf.Clamp01(_t / duration);
            float r = Mathf.Lerp(startRadius, endRadius, pct);
            DrawCircle(r);

            if (_t >= duration)
            {
                OnTelegraphFinished?.Invoke();
                Destroy(gameObject);
            }
        }

        private void DrawCircle(float radius)
        {
            for (int i = 0; i <= circleSegments; i++)
            {
                float ang = i * Mathf.PI * 2f / circleSegments;
                _lr.SetPosition(i, new Vector3(Mathf.Cos(ang) * radius, 0f, Mathf.Sin(ang) * radius));
            }
        }

        public static LandingTelegraph Spawn(Vector3 groundPos, LandingTelegraph prefab, float duration, float startR, float endR)
        {
            var inst = Instantiate(prefab, groundPos + Vector3.up * 0.02f, Quaternion.identity);
            inst.duration = duration;
            inst.startRadius = startR;
            inst.endRadius = endR;
            return inst;
        }
    }

}