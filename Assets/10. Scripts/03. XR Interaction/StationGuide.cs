using System.Collections.Generic;
using System.Text;
using StudyXR.Common;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace StudyXR.XRInteraction
{
    public class StationGuide : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private TMP_Text guideText;
        [SerializeField] private GameObject panel;
        [SerializeField] private Transform scope;
        [SerializeField] private ZoneHub zoneHub;
        [SerializeField] private int zoneIndex;

        [Header("안내")]
        [SerializeField] private string title = "";
        [SerializeField] [TextArea(3, 6)] private string body = "";

        [Header("이벤트 기록")]
        [SerializeField] private int maxEvents = 3;

        [Header("Quest 버튼")]
        [SerializeField] private bool useToggleButton = true;

        private readonly List<XRBaseInteractable> watched = new List<XRBaseInteractable>();
        private readonly List<string> events = new List<string>();
        private InputAction toggleAction;

        private void OnEnable()
        {
            if (useToggleButton == false) return;
            toggleAction = QuestButtons.Create(QuestButton.X, "Station Guide Toggle", "<Keyboard>/f1");
            toggleAction.Enable();
        }

        private void OnDisable()
        {
            if (toggleAction == null) return;
            toggleAction.Disable();
            toggleAction.Dispose();
            toggleAction = null;
        }

        private void Start()
        {
            XRBaseInteractable[] found = scope != null
                ? scope.GetComponentsInChildren<XRBaseInteractable>(true)
                : FindObjectsByType<XRBaseInteractable>(FindObjectsSortMode.None);
            foreach (XRBaseInteractable interactable in found)
            {
                if (interactable is BaseTeleportationInteractable) continue;
                interactable.hoverEntered.AddListener(OnHoverEntered);
                interactable.selectEntered.AddListener(OnSelectEntered);
                interactable.selectExited.AddListener(OnSelectExited);
                interactable.activated.AddListener(OnActivated);
                watched.Add(interactable);
            }
            Refresh();
        }

        private void OnDestroy()
        {
            foreach (XRBaseInteractable interactable in watched)
            {
                if (interactable == null) continue;
                interactable.hoverEntered.RemoveListener(OnHoverEntered);
                interactable.selectEntered.RemoveListener(OnSelectEntered);
                interactable.selectExited.RemoveListener(OnSelectExited);
                interactable.activated.RemoveListener(OnActivated);
            }
        }

        private void Update()
        {
            if (toggleAction == null || toggleAction.WasPressedThisFrame() == false) return;
            if (zoneHub != null && zoneHub.Current != zoneIndex) return;
            if (panel != null) panel.SetActive(!panel.activeSelf);
        }

        private void OnHoverEntered(HoverEnterEventArgs args)
            => Add("Hover 시작", args.interactableObject, args.interactorObject);
        private void OnSelectEntered(SelectEnterEventArgs args)
            => Add("Select 시작", args.interactableObject, args.interactorObject);
        private void OnSelectExited(SelectExitEventArgs args)
            => Add("Select 끝", args.interactableObject, args.interactorObject);
        private void OnActivated(ActivateEventArgs args)
            => Add("Activate", args.interactableObject, args.interactorObject);

        private void Add(
            string kind,
            IXRInteractable interactable,
            IXRInteractor interactor)
        {
            events.Insert(0, $"{Time.time:F1}초  {kind} · {interactable.transform.name} · {HandName(interactor)}");
            while (events.Count > Mathf.Max(1, maxEvents)) events.RemoveAt(events.Count - 1);
            Refresh();
        }

        private static string HandName(IXRInteractor interactor)
        {
            switch (interactor.handedness)
            {
                case InteractorHandedness.Left: return "왼손";
                case InteractorHandedness.Right: return "오른손";
                default: return interactor.transform.name;
            }
        }

        private void Refresh()
        {
            if (guideText == null) return;
            var text = new StringBuilder();
            text.Append("<b>").Append(title).Append("</b>\n").Append(body);
            text.Append("\n<color=#9FE0FF>최근 이벤트</color>");
            if (events.Count == 0) text.Append("\n(아직 없음 — 물건에 손을 가까이 대 보세요)");
            foreach (string line in events) text.Append('\n').Append(line);
            guideText.text = text.ToString();
        }
    }
}
