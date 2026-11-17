using System.Text;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Gaze;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;

namespace StudyXR.XRIComponents
{
    public class GazeSnapZone : ZoneReadout
    {
        [Header("구역 3 연결")]
        [SerializeField] private XRGazeInteractor gaze;
        [SerializeField] private XRGazeAssistance assistance;
        [SerializeField] private XRRayInteractor[] teleportRays;
        [SerializeField] private XRInteractableSnapVolume snapVolume;
        [SerializeField] private Collider snapCollider;
        [SerializeField] private NearFarInteractor rightHand;

        private Transform[] rayOrigins;
        private Transform[] rayAttaches;
        private XRInteractorLineVisual[] lineVisuals;
        private bool[] lineOverrides;
        private Transform[] lineOrigins;
        private Transform[] headOrigins;
        private Transform[] headAttaches;
        private Transform[] headLineOrigins;
        private IXRInteractable gazeTarget;
        private float gazeSince;
        private int gazeSelects;
        private bool wasSelecting;

        protected override string ButtonALabel => "시선 보조";
        protected override string ButtonBLabel => "스냅 볼륨";

        protected override void Initialize()
        {
            int count = teleportRays != null ? teleportRays.Length : 0;
            rayOrigins = new Transform[count];
            rayAttaches = new Transform[count];
            lineVisuals = new XRInteractorLineVisual[count];
            lineOverrides = new bool[count];
            lineOrigins = new Transform[count];
            headOrigins = new Transform[count];
            headAttaches = new Transform[count];
            headLineOrigins = new Transform[count];
            for (int i = 0; i < count; ++i)
            {
                if (teleportRays[i] == null) continue;
                IXRRayProvider provider = teleportRays[i];
                rayOrigins[i] = provider.GetOrCreateRayOrigin();
                rayAttaches[i] = provider.GetOrCreateAttachTransform();
                lineVisuals[i] = teleportRays[i].GetComponent<XRInteractorLineVisual>();
                if (lineVisuals[i] == null) continue;
                lineOverrides[i] = lineVisuals[i].overrideInteractorLineOrigin;
                lineOrigins[i] = lineVisuals[i].lineOriginTransform;
            }
            SetSnap(false);
        }

        protected override void OnButtonA()
            => SetAssistance(assistance != null && assistance.enabled == false);

        protected override void OnButtonB()
            => SetSnap(snapVolume != null && snapVolume.enabled == false);

        protected override void OnZoneExit()
        {
            SetAssistance(false);
            SetSnap(false);
            gazeTarget = null;
            gazeSelects = 0;
        }

        private void SetAssistance(bool on)
        {
            if (assistance == null) return;
            if (on) ReapplyHeadRays();
            assistance.enabled = on;
            if (on == false) RestoreRays();
        }

        private void RestoreRays()
        {
            if (rayOrigins == null) return;
            for (int i = 0; i < rayOrigins.Length; ++i)
            {
                if (teleportRays[i] == null) continue;
                IXRRayProvider provider = teleportRays[i];
                Transform current = provider.GetOrCreateRayOrigin();

                if (current != rayOrigins[i])
                {
                    headOrigins[i] = current;
                    headAttaches[i] = provider.GetOrCreateAttachTransform();
                    headLineOrigins[i] = lineVisuals[i] != null ? lineVisuals[i].lineOriginTransform : null;
                }
                if (rayOrigins[i] != null) provider.SetRayOrigin(rayOrigins[i]);
                if (rayAttaches[i] != null) provider.SetAttachTransform(rayAttaches[i]);
                if (lineVisuals[i] == null) continue;
                lineVisuals[i].overrideInteractorLineOrigin = lineOverrides[i];
                lineVisuals[i].lineOriginTransform = lineOrigins[i];
            }
        }

        private void ReapplyHeadRays()
        {
            if (headOrigins == null) return;
            for (int i = 0; i < headOrigins.Length; ++i)
            {
                if (teleportRays[i] == null || headOrigins[i] == null) continue;
                IXRRayProvider provider = teleportRays[i];
                provider.SetRayOrigin(headOrigins[i]);
                if (headAttaches[i] != null) provider.SetAttachTransform(headAttaches[i]);
                if (lineVisuals[i] != null && headLineOrigins[i] != null)
                {
                    lineVisuals[i].overrideInteractorLineOrigin = true;
                    lineVisuals[i].lineOriginTransform = headLineOrigins[i];
                }
                headOrigins[i] = null;
            }
        }

        private void SetSnap(bool on)
        {
            if (snapVolume != null) snapVolume.enabled = on;
            if (snapCollider != null) snapCollider.enabled = on;
        }

        private void LateUpdate()
        {
            if (IsActiveZone == false || gaze == null) return;
            IXRInteractable now = gaze.hasHover ? gaze.interactablesHovered[0] : null;

            if (ReferenceEquals(now, gazeTarget) == false)
            {
                gazeTarget = now;
                gazeSince = Time.time;
                gazeSelects = 0;
            }
            bool selecting = gaze.hasSelection;

            if (selecting && wasSelecting == false) gazeSelects++;
            wasSelecting = selecting;
        }

        protected override void WriteValues(StringBuilder lines)
        {
            lines.Append("시선 대상 : ").Append(gazeTarget != null ? gazeTarget.transform.name : "없음").Append(" (머리 방향)\n");
            lines.Append(DwellText()).Append('\n');
            bool assist = assistance != null && assistance.enabled;
            lines.Append("시선 보조 : ").Append(assist ? "켜짐 — 손이 시야 밖이면 텔레포트 레이가 머리에서" : "꺼짐").Append('\n');
            bool snap = snapVolume != null && snapVolume.enabled;
            string rightTarget = rightHand != null && rightHand.hasHover ? rightHand.interactablesHovered[0].transform.name : "없음";
            lines.Append("스냅 볼륨 : ").Append(snap ? "켜짐" : "꺼짐").Append(" · 오른손 레이 : ").Append(rightTarget);
            if (rightHand != null &&
                rightHand.hasSelection == false &&
                rightHand.TryGetCurveEndPoint(out _, false, true) == EndPointType.AttachPoint)
                lines.Append(" (끝이 붙음)");
        }

        private string DwellText()
        {
            if (gazeTarget == null || gaze == null) return "머문 시간 —";
            float dwell = Time.time - gazeSince;
            var target = gazeTarget as XRBaseInteractable;

            if (target == null || target.allowGazeSelect == false || gaze.hoverToSelect == false)
                return "머문 " + dwell.ToString("F1") + "초 · 시선 선택 안 함";
            float need = target.overrideGazeTimeToSelect ? target.gazeTimeToSelect : gaze.hoverTimeToSelect;

            return "머문 " + dwell.ToString("F1") + "초 / 선택 " + need.ToString("F1") + "초" + (gazeSelects > 0 ? " → 선택됨 " + gazeSelects + "번" : "");
        }
    }
}
