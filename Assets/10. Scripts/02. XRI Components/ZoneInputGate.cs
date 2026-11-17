using StudyXR.Common;
using UnityEngine;

namespace StudyXR.XRIComponents
{
    [DefaultExecutionOrder(100)]
    public class ZoneInputGate : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private ZoneHub hub;

        [Header("구역 (배열 순서 = ZoneHub 구역 번호)")]
        [SerializeField] private ZoneReadout[] zones;

        private void OnEnable()
        {
            if (hub != null) hub.ZoneChanged += Apply;
        }

        private void OnDisable()
        {
            if (hub != null) hub.ZoneChanged -= Apply;
        }

        private void Start()
        {
            Apply(hub != null ? hub.Current : 0);
        }

        private void Apply(int currentZone)
        {
            if (zones == null) return;
            for (int i = 0; i < zones.Length; ++i)
                if (zones[i] != null) zones[i].SetZoneActive(i == currentZone);
        }
    }
}
