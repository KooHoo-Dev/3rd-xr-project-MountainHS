using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace StudyXR.XRIComponents
{
    public class SpatialUIZone : ZoneReadout
    {
        [Header("구역 6 연결")]
        [SerializeField] private XRUIInputModule uiModule;
        [SerializeField] private Transform followPanel;
        [SerializeField] private UnityEngine.XR.Interaction.Toolkit.UI.LazyFollow lazyFollow;
        [SerializeField] private TrackedDeviceGraphicRaycaster[] raycasters;

        private Vector3 homePosition;
        private Quaternion homeRotation;
        private string lastClick = "아직 없음";
        private string pointing = "없음";
        private int hiddenClicks;

        protected override string ButtonALabel => "따라오는 패널";
        protected override string ButtonBLabel => "가림 검사";

        protected override void Initialize()
        {
            if (followPanel != null)
            {
                homePosition = followPanel.position;
                homeRotation = followPanel.rotation;
            }
            if (lazyFollow != null) lazyFollow.enabled = false;
            if (uiModule == null) return;
            uiModule.pointerClick += OnClick;
            uiModule.pointerEnter += OnEnter;
            uiModule.pointerExit += OnExit;
        }

        private void OnDestroy()
        {
            if (uiModule == null) return;
            uiModule.pointerClick -= OnClick;
            uiModule.pointerEnter -= OnEnter;
            uiModule.pointerExit -= OnExit;
        }

        public void CountHiddenClick()
            => hiddenClicks++;

        protected override void OnButtonA()
        {
            if (lazyFollow != null) lazyFollow.enabled = !lazyFollow.enabled;
        }

        protected override void OnButtonB()
        {
            bool on = !Occluding();

            if (raycasters == null) return;
            foreach (TrackedDeviceGraphicRaycaster r in raycasters)
                if (r != null) r.checkFor3DOcclusion = on;
        }

        protected override void OnZoneExit()
        {
            if (lazyFollow != null) lazyFollow.enabled = false;
            if (followPanel != null) followPanel.SetPositionAndRotation(homePosition, homeRotation);
        }

        private bool Occluding()
        {
            return raycasters != null &&
                   raycasters.Length > 0 &&
                   raycasters[0] != null &&
                   raycasters[0].checkFor3DOcclusion;
        }

        private void OnClick(GameObject target, PointerEventData data)
        {
            if (IsActiveZone) lastClick = Widget(target) + " · " + Who(data);
        }

        private void OnEnter(GameObject target, PointerEventData data)
        {
            if (IsActiveZone && target == data.pointerEnter) pointing = Widget(target) + " · " + Who(data);
        }

        private void OnExit(GameObject target, PointerEventData data)
        {
            if (IsActiveZone && target == data.pointerEnter) pointing = "없음";
        }

        private static string Widget(GameObject target)
        {
            if (target == null) return "없음";
            Selectable widget = target.GetComponentInParent<Selectable>();

            return widget != null ? widget.name : target.name;
        }

        private static string Who(PointerEventData data)
        {
            if (data is TrackedDeviceEventData tracked && tracked.interactor is IXRInteractor interactor)
                return HandName(interactor) + " " + interactor.transform.name;
            return "마우스";
        }

        protected override void WriteValues(StringBuilder lines)
        {
            lines.Append("마지막 클릭 : ").Append(lastClick).Append('\n');
            lines.Append("가리키는 것 : ").Append(pointing).Append('\n');
            lines.Append("따라오는 패널 : ").Append(lazyFollow != null && lazyFollow.enabled ? "켜짐 (LazyFollow)" : "꺼짐").Append('\n');
            lines.Append("가림 검사(3D) : ").Append(Occluding() ? "켜짐" : "꺼짐").Append(" · 상자 뒤 버튼 ").Append(hiddenClicks).Append("번 눌림");
        }
    }
}
