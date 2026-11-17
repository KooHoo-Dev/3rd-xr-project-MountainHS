using StudyXR.Common;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StudyXR.XRInput
{
    public class HandInputReader : MonoBehaviour
    {
        [Header("XRI 기본 액션")]
        [SerializeField] private InputActionReference triggerValue;
        [SerializeField] private InputActionReference gripValue;
        [SerializeField] private InputActionReference thumbstick;
        [SerializeField] private InputActionReference isTracked;
        [SerializeField] private InputActionReference trackingState;
        [SerializeField] private InputActionReference devicePosition;

        [Header("직접 만드는 버튼 액션 (용도 바인딩)")]
        [SerializeField] private string handUsage = "LeftHand";

        [Header("테스트 입력 (자동 스틱 — Quest 버튼, 테스트 트리거 — 시뮬레이터 전용)")]
        [SerializeField] private bool useTestInput = false;
        [SerializeField] private QuestButton testStickButton = QuestButton.RightStickClick;
        [SerializeField] private float testTriggerSpeed = 0.5f;
        [SerializeField] private float testStickTurnSpeed = 60f;

        private static readonly float[] TestStickRadii = { 0.15f, 0.5f, 1f };

        private InputAction primaryButton;
        private InputAction secondaryButton;
        private InputAction menuButton;
        private InputAction testStickAction;

        private bool triggerOverridden;
        private float testTrigger;
        private int testStickIndex = -1;
        private float testStickAngle;

        public float Trigger => triggerOverridden ? testTrigger : ReadFloat(triggerValue);
        public float Grip => ReadFloat(gripValue);
        public Vector2 Stick => testStickIndex >= 0 ? TestStick() : ReadVector2(thumbstick);
        public bool IsTracked => (ReadFloat(isTracked) > 0.5f);
        public int TrackingState => trackingState != null && trackingState.action != null ? trackingState.action.ReadValue<int>() : 0;
        public Vector3 Position => devicePosition != null && devicePosition.action != null ? devicePosition.action.ReadValue<Vector3>() : Vector3.zero;
        public bool PrimaryPressed => IsPressed(primaryButton);
        public bool SecondaryPressed => IsPressed(secondaryButton);
        public bool MenuPressed => IsPressed(menuButton);
        public bool TestTriggerActive => triggerOverridden;
        public float TestStickRadius => testStickIndex >= 0 ? TestStickRadii[testStickIndex] : 0f;

        private void OnEnable()
        {
            primaryButton = CreateButton("Primary", "{PrimaryButton}");
            secondaryButton = CreateButton("Secondary", "{SecondaryButton}");
            menuButton = CreateButton("Menu", "{MenuButton}");

            if (useTestInput)
            {
                testStickAction = QuestButtons.Create(testStickButton, handUsage + " Test Stick", "<Keyboard>/f7");
                testStickAction.Enable();
            }
        }

        private void OnDisable()
        {
            DisposeButton(ref primaryButton);
            DisposeButton(ref secondaryButton);
            DisposeButton(ref menuButton);
            DisposeButton(ref testStickAction);
        }

        private void Update()
        {
            if (useTestInput == false) return;

            if (testStickAction != null && testStickAction.WasPressedThisFrame())
            {
                testStickIndex++;
                if (testStickIndex >= TestStickRadii.Length) StopTestInput();
            }

            if (testStickIndex >= 0) testStickAngle += testStickTurnSpeed * Time.deltaTime;

            UpdateSimulatorTrigger();
        }

        private void UpdateSimulatorTrigger()
        {
            var keyboard = Keyboard.current;

            if (keyboard == null) return;

            bool down = keyboard.f5Key.isPressed;
            bool up = keyboard.f6Key.isPressed;

            if (down || up)
            {
                if (triggerOverridden == false)
                {
                    triggerOverridden = true;
                    testTrigger = ReadFloat(triggerValue);
                }

                float direction = up ? 1f : -1f;
                testTrigger = Mathf.Clamp01(testTrigger + direction * testTriggerSpeed * Time.deltaTime);
            }

            if (keyboard.f8Key.wasPressedThisFrame) StopTestInput();
        }

        private void StopTestInput()
        {
            triggerOverridden = false;
            testStickIndex = -1;
        }

        private InputAction CreateButton(string label, string usage)
        {
            var action = new InputAction(
                handUsage + " " + label,
                InputActionType.Button,
                "<XRController>{" + handUsage + "}/" + usage);
            action.Enable();
            return action;
        }

        private static void DisposeButton(ref InputAction action)
        {
            if (action == null) return;
            action.Disable();
            action.Dispose();
            action = null;
        }

        private static bool IsPressed(InputAction action)
        {
            return action != null && action.IsPressed();
        }

        private static float ReadFloat(InputActionReference reference)
        {
            if (reference == null || reference.action == null) return 0f;
            return reference.action.ReadValue<float>();
        }

        private static Vector2 ReadVector2(InputActionReference reference)
        {
            if (reference == null || reference.action == null) return Vector2.zero;
            return reference.action.ReadValue<Vector2>();
        }

        private Vector2 TestStick()
        {
            float a = testStickAngle * Mathf.Deg2Rad;

            return new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * TestStickRadii[testStickIndex];
        }
    }
}
