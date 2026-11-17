using UnityEngine;

namespace StudyXR.XRInteraction
{
    public class OpenClose : MonoBehaviour
    {
        [Header("열린 자세 (부모 기준, 닫힌 자세에서 더함)")]
        [SerializeField] private Vector3 openOffset = Vector3.zero;
        [SerializeField] private Vector3 openEuler = Vector3.zero;

        [Header("움직임")]
        [SerializeField] private float duration = 0.6f;

        private Vector3 closedPosition;
        private Quaternion closedRotation;
        private float progress;
        private bool open;

        private void Awake()
        {
            closedPosition = transform.localPosition;
            closedRotation = transform.localRotation;
        }

        public void SetOpen(bool value)
        {
            open = value;
        }

        private void Update()
        {
            float target = open ? 1f : 0f;

            if (Mathf.Approximately(progress, target)) return;

            progress = Mathf.MoveTowards(progress, target, Time.deltaTime / Mathf.Max(0.01f, duration));
            float t = Mathf.SmoothStep(0f, 1f, progress);
            transform.localPosition = closedPosition + openOffset * t;
            transform.localRotation = closedRotation * Quaternion.Euler(openEuler * t);
        }
    }
}
