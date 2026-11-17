using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;
using UnityEngine.XR.Interaction.Toolkit.Utilities;

namespace StudyXR.XRIComponents
{
    public class InteractorCompareZone : ZoneReadout
    {
        [Header("구역 1 연결")]
        [SerializeField] private XRInteractionGroup group;
        [SerializeField] private XRBaseInteractor[] interactors;
        [SerializeField] private NearFarInteractor nearFar;
        [SerializeField] private Transform nearMarker;
        [SerializeField] private Transform farMarker;

        private static readonly string[] Kinds
            = { "Near-Far (구 + 곡선 캐스터)", "Direct (손 둘레 트리거 구)", "Ray (곧은 레이)" };
        private readonly List<IXRInteractable> targets = new List<IXRInteractable>();
        private int current;
        private bool showRange;
        private string notice = "";

        protected override string ButtonALabel => "Interactor 바꾸기";
        protected override string ButtonBLabel => "캐스터 범위";

        protected override void Initialize()
            => ShowMarkers(false);

        protected override void OnButtonA()
        {
            if (interactors == null || interactors.Length == 0) return;
            XRBaseInteractor now = interactors[current];

            if (now != null && now.hasSelection)
            {
                notice = " · 먼저 놓기";
                return;
            }
            SwitchTo((current + 1) % interactors.Length);
        }

        protected override void OnButtonB()
            => showRange = !showRange;

        protected override void OnZoneExit()
        {
            showRange = false;
            ShowMarkers(false);
            if (current != 0) SwitchTo(0);
        }

        private void SwitchTo(int next)
        {
            if (group == null || interactors == null) return;
            XRBaseInteractor from = interactors[current];
            XRBaseInteractor to = interactors[next];

            if (from == null || to == null) return;
            group.RemoveGroupMember(from);
            SetOn(from, false);
            SetOn(to, true);
            group.AddGroupMember(to);
            current = next;
            notice = "";
        }

        private static void SetOn(XRBaseInteractor i, bool on)
        {
            if (i is NearFarInteractor) i.enabled = on;
            else i.gameObject.SetActive(on);
        }

        private void LateUpdate()
        {
            bool show = IsActiveZone && showRange && current == 0 && nearFar != null;
            ShowMarkers(show);
            if (show == false) return;
            if (nearFar.nearInteractionCaster is SphereInteractionCaster sphere &&
                sphere.effectiveCastOrigin != null &&
                nearMarker != null)
            {
                nearMarker.position = sphere.effectiveCastOrigin.position;
                nearMarker.localScale = Vector3.one * (sphere.castRadius * 2f);
            }
            if (nearFar.farInteractionCaster != null && farMarker != null)
                farMarker.position = nearFar.TryGetCurveEndPoint(out Vector3 end) != EndPointType.None
                    ? end
                    : nearFar.farInteractionCaster.lastSamplePoint;
        }

        private void ShowMarkers(bool on)
        {
            if (nearMarker != null && nearMarker.gameObject.activeSelf != on) nearMarker.gameObject.SetActive(on);
            if (farMarker != null && farMarker.gameObject.activeSelf != on) farMarker.gameObject.SetActive(on);
        }

        protected override void WriteValues(StringBuilder lines)
        {
            XRBaseInteractor now = interactors != null && interactors.Length > current ? interactors[current] : null;
            lines.Append("지금 Interactor : ").Append(Kinds[Mathf.Min(current, Kinds.Length - 1)]).Append('\n');
            IXRInteractor active = group != null ? group.activeInteractor : null;
            lines.Append("그룹 활성 멤버 : ").Append(active != null ? active.transform.name : "없음 (Hover · Select 없음)").Append('\n');
            if (now == null)
            {
                lines.Append("후보 —\n잡은 물건 —");
                return;
            }

            now.GetValidTargets(targets);
            if (targets.Count == 0) lines.Append("후보 0개");
            else
            {
                float distance = SurfaceDistance(targets[0], now);
                lines.Append("후보 ").Append(targets.Count).Append("개 · 1순위 ").Append(targets[0].transform.name)
                     .Append(" (").Append(distance.ToString("F2")).Append(" m · ").Append(Region(distance)).Append(')');
            }
            lines.Append('\n');

            if (now.hasSelection == false)
            {
                lines.Append("잡은 물건 : 없음");
                notice = "";
                return;
            }
            IXRSelectInteractable held = now.interactablesSelected[0];
            float gap = Vector3.Distance(now.transform.position, held.transform.position);
            lines.Append("잡은 물건 : ").Append(held.transform.name).Append(" · 손에서 ").Append(gap.ToString("F2")).Append(" m");
            if (current == 0 && nearFar != null) lines.Append(" · ").Append(nearFar.selectionRegion.Value);
            lines.Append(notice);
        }

        private float SurfaceDistance(IXRInteractable target, XRBaseInteractor now)
        {
            Transform from = now.GetAttachTransform(target);

            if (current == 0 &&
                nearFar != null &&
                nearFar.nearInteractionCaster is SphereInteractionCaster sphere &&
                sphere.effectiveCastOrigin != null)
                from = sphere.effectiveCastOrigin;
            if (from != null &&
                XRInteractableUtility.TryGetClosestPointOnCollider(target, from.position, out DistanceInfo info))
                return Mathf.Sqrt(info.distanceSqr);
            return Mathf.Sqrt(target.GetDistanceSqrToInteractor(now));
        }

        private string Region(float distance)
        {
            if (current == 1) return "근거리";
            if (current == 2) return "원거리";
            if (nearFar != null && nearFar.nearInteractionCaster is SphereInteractionCaster sphere)
                return distance <= sphere.castRadius ? "근거리 구 안" : "원거리 곡선";
            return "—";
        }
    }
}
