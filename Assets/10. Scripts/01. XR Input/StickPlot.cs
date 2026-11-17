using UnityEngine;

namespace StudyXR.XRInput
{
    public class StickPlot : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private RectTransform deadzoneRing;
        [SerializeField] private RectTransform rawDot;
        [SerializeField] private RectTransform processedDot;

        [Header("크기")]
        [SerializeField] private float radius = 110f;

        public void Show(
            Vector2 raw,
            Vector2 processed,
            float deadzone)
        {
            if (deadzoneRing != null) deadzoneRing.sizeDelta = Vector2.one * (2f * radius * Mathf.Max(0f, deadzone));

            if (rawDot != null) rawDot.anchoredPosition = Vector2.ClampMagnitude(raw, 1.5f) * radius;
            if (processedDot != null) processedDot.anchoredPosition = Vector2.ClampMagnitude(processed, 1.5f) * radius;
        }
    }
}
