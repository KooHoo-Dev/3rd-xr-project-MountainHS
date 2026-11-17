using TMPro;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Content.Interaction;

namespace StudyXR.XRInput
{
    public class ControllerReadout : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private HandInputReader input;
        [SerializeField] private StickPlot plot;
        [SerializeField] private XRJoystick stickGauge;
        [SerializeField] private XRSlider triggerGauge;
        [SerializeField] private XRSlider gripGauge;
        [SerializeField] private TMP_Text readoutText;

        [Header("표시")]
        [SerializeField] private string handName = "왼손";
        [SerializeField] private string primaryName = "X";
        [SerializeField] private string secondaryName = "Y";

        [Header("입력 다듬기")]
        [SerializeField, Range(0f, 0.9f)] private float triggerDeadzone = 0.1f;
        [SerializeField, Range(0f, 0.9f)] private float stickDeadzone = 0.2f;
        [SerializeField, Range(0.2f, 4f)] private float curveExponent = 2f;

        private void Awake()
        {
            if (stickGauge != null)
            {
                stickGauge.recenterOnRelease = false;
                stickGauge.interactionLayers = 0;
            }

            if (triggerGauge != null) triggerGauge.interactionLayers = 0;
            if (gripGauge != null) gripGauge.interactionLayers = 0;
        }

        private void LateUpdate()
        {
            if (input == null) return;

            float rawTrigger = input.Trigger;
            float rawGrip = input.Grip;
            Vector2 rawStick = input.Stick;

            float trigger = ApplyCurve(ApplyDeadzone(rawTrigger, triggerDeadzone), curveExponent);
            float grip = ApplyDeadzone(rawGrip, triggerDeadzone);
            Vector2 stick = ApplyRadialDeadzone(rawStick, stickDeadzone);

            if (triggerGauge != null) triggerGauge.value = Mathf.Clamp01(trigger);
            if (gripGauge != null) gripGauge.value = Mathf.Clamp01(grip);
            if (stickGauge != null) stickGauge.value = Vector2.ClampMagnitude(stick, 1f);
            if (plot != null) plot.Show(rawStick, stick, stickDeadzone);

            if (readoutText == null) return;

            int state = input.TrackingState;
            readoutText.text =
                $"{handName} 컨트롤러{TestText()}\n" +
                $"추적 : {(input.IsTracked ? "예" : "아니오")} · {TrackingText(state)} (값 {state})\n" +
                $"위치 : {input.Position.ToString("F2")} m\n" +
                $"트리거 {rawTrigger:F2} → {trigger:F2}\n" +
                $"그립 {rawGrip:F2} → {grip:F2}\n" +
                $"스틱 {rawStick.ToString("F2")} → {stick.ToString("F2")}\n" +
                $"{primaryName} {Pressed(input.PrimaryPressed)} · {secondaryName} {Pressed(input.SecondaryPressed)} · 메뉴 {Pressed(input.MenuPressed)}\n" +
                $"데드존 {triggerDeadzone:F2} · 스틱 원 {stickDeadzone:F2} · 곡선 {curveExponent:F1}\n" +
                "노랑 = 원래 값 · 하늘색 = 다듬은 값";
        }

        private string TestText()
        {
            if (input.TestStickRadius > 0f) return $" · 자동 스틱 원 {input.TestStickRadius:F2}";
            if (input.TestTriggerActive) return " · 테스트 트리거";
            return "";
        }

        private static string TrackingText(int state)
        {
            var flags = (InputTrackingState)state;
            bool hasPosition = ((flags & InputTrackingState.Position) != 0);
            bool hasRotation = ((flags & InputTrackingState.Rotation) != 0);

            if (hasPosition && hasRotation) return "위치 · 회전";
            if (hasPosition) return "위치만";
            if (hasRotation) return "회전만";
            return "없음";
        }

        private static string Pressed(bool pressed)
        {
            return pressed ? "눌림" : "뗌";
        }

        private static float ApplyDeadzone(float raw, float deadzone)
        {
            if (raw <= deadzone) return 0f;
            return Mathf.Clamp01((raw - deadzone) / (1f - deadzone));
        }

        private static Vector2 ApplyRadialDeadzone(Vector2 raw, float deadzone)
        {
            float length = raw.magnitude;

            if (length <= deadzone) return Vector2.zero;
            return raw / length * ApplyDeadzone(length, deadzone);
        }

        private static float ApplyCurve(float value, float exponent)
        {
            return Mathf.Pow(Mathf.Clamp01(value), exponent);
        }
    }
}
