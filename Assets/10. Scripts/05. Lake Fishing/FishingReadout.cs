using System.Text;
using StudyXR.Common;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StudyXR.LakeFishing
{
    [DefaultExecutionOrder(60)]
    public class FishingReadout : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private TMP_Text label;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private FishingRod rod;
        [SerializeField] private BobberCast cast;
        [SerializeField] private ReelCrank reel;
        [SerializeField] private BiteController bite;
        [SerializeField] private FishFight fight;

        [Header("표시")]
        [SerializeField] private bool visibleOnStart = true;
        [SerializeField] private float refreshInterval = 0.1f;

        [Header("Quest 버튼")]
        [SerializeField] private bool useQuestButton = true;

        private readonly StringBuilder builder = new StringBuilder();
        private bool visible;
        private float nextRefresh;
        private InputAction toggleAction;

        public bool Visible => visible;

        private void OnEnable()
        {
            if (useQuestButton == false) return;
            toggleAction = QuestButtons.Create(QuestButton.X, "Readout Toggle", "<Keyboard>/f10");
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
            SetVisible(visibleOnStart);
        }

        private void Update()
        {
            if (toggleAction != null && toggleAction.WasPressedThisFrame()) SetVisible(!visible);
            if (visible == false || label == null || Time.time < nextRefresh) return;
            nextRefresh = Time.time + refreshInterval;
            Refresh();
        }

        private void SetVisible(bool value)
        {
            visible = value;
            if (panelRoot != null) panelRoot.SetActive(value);
        }

        private void Refresh()
        {
            builder.Clear();
            builder.Append($"초리 {rod.TipVelocity.magnitude:F1} m/s · 찌 {PhaseName(cast.CurrentPhase)}\n");
            builder.Append($"던짐 {cast.LastLaunchVelocity.magnitude:F1} m/s · {cast.LaunchAngle:F0}도 · {cast.CastDistance:F1} m {cast.LastNote}\n");
            builder.Append($"줄 {cast.LineLength:F1} m · 수평 {cast.HorizontalDistance:F1} m · 릴 {reel.MetersPerSecond:+0.00;-0.00} m/s\n");

            if (bite != null)
            {
                string result = bite.LastResult;
                builder.Append($"상태 {StateName(bite.State)} {bite.TimeInState:F1}초 · 기다림 {bite.WaitTime:F1}초 · {result}\n");
            }

            if (fight != null)
                builder.Append($"장력 {fight.Tension:F1} / {BiteController.BreakTension:F0} N · 물고기 힘 {fight.Stamina01 * 100f:F0}%\n");

            builder.Append($"햅틱 {rod.LastHapticAmplitude:F2} ({Time.time - rod.LastHapticTime:F1}초 전)");
            label.text = builder.ToString();
        }

        private static string PhaseName(BobberCast.Phase phase)
        {
            switch (phase)
            {
                case BobberCast.Phase.Ready: return "손에";
                case BobberCast.Phase.Charging: return "준비";
                case BobberCast.Phase.Flying: return "날아감";
                default: return "물 위";
            }
        }

        private static string StateName(BiteState state)
        {
            switch (state)
            {
                case BiteState.Ready: return "준비";
                case BiteState.Waiting: return "기다림";
                case BiteState.Nibble: return "입질";
                case BiteState.Hooked: return "걸림";
                case BiteState.Caught: return "잡힘";
                default: return "놓침";
            }
        }
    }
}
