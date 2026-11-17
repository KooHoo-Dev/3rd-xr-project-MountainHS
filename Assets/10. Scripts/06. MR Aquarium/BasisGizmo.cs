using UnityEngine;

namespace StudyXR.MRAquarium
{
    public class BasisGizmo : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private LineRenderer normalLine;
        [SerializeField] private LineRenderer rightLine;
        [SerializeField] private LineRenderer upLine;

        [Header("모양")]
        [SerializeField] private float length = 0.2f;

        private void Awake()
        {
            Hide();
        }

        public void Show(
            Vector3 origin,
            Vector3 n,
            Vector3 r,
            Vector3 u)
        {
            Set(normalLine, origin, n);
            Set(rightLine, origin, r);
            Set(upLine, origin, u);
        }

        public void Hide()
        {
            if (normalLine != null) normalLine.enabled = false;
            if (rightLine != null) rightLine.enabled = false;
            if (upLine != null) upLine.enabled = false;
        }

        private void Set(
            LineRenderer line,
            Vector3 origin,
            Vector3 dir)
        {
            if (line == null) return;
            float sum = dir.x + dir.y + dir.z;

            if (float.IsNaN(sum) || float.IsInfinity(sum) || dir.sqrMagnitude < 1e-6f)
            {
                line.enabled = false;
                return;
            }
            line.positionCount = 2;
            line.SetPosition(0, origin);
            line.SetPosition(1, origin + dir * length);
            line.enabled = true;
        }
    }
}
