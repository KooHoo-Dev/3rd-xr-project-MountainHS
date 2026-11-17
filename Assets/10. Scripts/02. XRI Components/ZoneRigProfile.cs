using StudyXR.Common;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Gravity;

namespace StudyXR.XRIComponents
{
    [DefaultExecutionOrder(100)]
    public class ZoneRigProfile : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private ZoneHub hub;

        [Header("구역 3 — 시선")]
        [SerializeField] private int gazeZone = 2;
        [SerializeField] private GameObject gazeInteractorObject;
        [SerializeField] private GameObject gazeStabilizedObject;

        [Header("구역 7 — 이동 확장")]
        [SerializeField] private int locomotionZone = 6;
        [SerializeField] private GameObject jumpObject;
        [SerializeField] private GameObject grabMoveObject;
        [SerializeField] private GravityProvider gravity;

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

        private void Apply(int zone)
        {
            bool locomotion = (zone == locomotionZone);

            if (gravity != null) gravity.useGravity = locomotion;
            if (gazeInteractorObject != null) gazeInteractorObject.SetActive(zone == gazeZone);
            if (gazeStabilizedObject != null) gazeStabilizedObject.SetActive(zone == gazeZone);
            if (jumpObject != null) jumpObject.SetActive(locomotion);
            if (grabMoveObject != null) grabMoveObject.SetActive(locomotion);
        }
    }
}
