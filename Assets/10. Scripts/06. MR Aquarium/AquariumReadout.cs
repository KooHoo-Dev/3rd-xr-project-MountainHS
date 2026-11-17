using StudyXR.Common;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StudyXR.MRAquarium
{
    public class AquariumReadout : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private SpatialPermissionGate gate;
        [SerializeField] private WallFinder finder;
        [SerializeField] private WallRectDrawer drawer;
        [SerializeField] private AquariumWindow aquarium;
        [SerializeField] private FishSchool school;
        [SerializeField] private GlassTap tap;
        [SerializeField] private FishFeeder feeder;

        [Header("표시")]
        [SerializeField] private TMP_Text readoutText;
        [SerializeField] private GameObject panel;
        [SerializeField] private QuestButton toggleButton = QuestButton.LeftStickClick;
        [SerializeField] private bool visibleOnStart = false;

        [Header("시뮬레이터 테스트 입력")]
        [SerializeField] private bool useTestKeys = true;

        private InputAction toggleAction;

        private void OnEnable()
        {
            toggleAction = QuestButtons.Create(toggleButton, "Readout Toggle", useTestKeys ? "<Keyboard>/f6" : null);
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
            if (panel != null) panel.SetActive(visibleOnStart);
        }

        private void Update()
        {
            if (panel == null || toggleAction == null) return;
            if (toggleAction.WasPressedThisFrame()) panel.SetActive(!panel.activeSelf);
        }

        private void LateUpdate()
        {
            if (panel == null || panel.activeSelf == false || readoutText == null) return;
            if (gate == null ||
                finder == null ||
                drawer == null ||
                aquarium == null ||
                school == null ||
                tap == null ||
                feeder == null)
                return;
            string wall = aquarium.ShownByDrawer ? WallFinder.Describe(drawer.Source) : "처음 자리 (정면 1.5 m 공중)";
            readoutText.text =
                $"{gate.Describe()} · 평면 {finder.PlaneCount}개 (벽 분류 {finder.ClassifiedWalls} · 수직 {finder.VerticalWalls}) · 벽 : {wall}\n" +
                $"n {drawer.Normal.ToString("F2")} · r {drawer.Right.ToString("F2")} · u {drawer.Up.ToString("F2")}\n" +
                $"창 {aquarium.Size.x:F2} x {aquarium.Size.y:F2} m · 깊이 {aquarium.Depth:F2} m · 물고기 {school.FishCount}마리 · {school.ModeText}\n" +
                $"두드림 {tap.TapCount}번 · {tap.LastTapText} · 먹이 {feeder.FeedCount}번\n" +
                drawer.LastMessage;
        }
    }
}
