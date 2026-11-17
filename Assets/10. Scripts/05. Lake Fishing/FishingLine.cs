using UnityEngine;

namespace StudyXR.LakeFishing
{
    [DefaultExecutionOrder(50)]
    [RequireComponent(typeof(LineRenderer))]
    public class FishingLine : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private Transform tip;
        [SerializeField] private Transform lineEnd;

        [Header("처짐")]
        [SerializeField] private int segments = 16;
        [SerializeField] private float sagRatio = 0.12f;
        [SerializeField] private float minSag = 0.01f;
        [SerializeField] private float waterHeight = 0f;

        private LineRenderer lineRenderer;

        public float Tension01 { get; set; }

        private void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.useWorldSpace = true;
            lineRenderer.positionCount = segments + 1;
        }

        private void LateUpdate()
        {
            Vector3 p0 = tip.position;
            Vector3 p2 = lineEnd.position;
            float length = Vector3.Distance(p0, p2);
            float sag = Mathf.Max(minSag, length * sagRatio * (1f - Mathf.Clamp01(Tension01)));

            Vector3 p1 = (p0 + p2) * 0.5f + Vector3.down * (2f * sag);

            for (int i = 0; i <= segments; ++i)
            {
                float t = (float)i / segments;
                float u = 1f - t;
                Vector3 point = u * u * p0 + 2f * u * t * p1 + t * t * p2;

                if (i < segments) point.y = Mathf.Max(point.y, waterHeight + 0.005f);
                lineRenderer.SetPosition(i, point);
            }
        }
    }
}
