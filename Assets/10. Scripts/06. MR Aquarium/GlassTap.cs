using StudyXR.Common;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

namespace StudyXR.MRAquarium
{
    public class GlassTap : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [Header("연결")]
        [SerializeField] private AquariumWindow aquarium;
        [SerializeField] private FishSchool school;
        [SerializeField] private Transform rayOrigin;
        [SerializeField] private Transform head;
        [SerializeField] private Transform leftHand;
        [SerializeField] private Transform rightHand;
        [SerializeField] private HapticImpulsePlayer leftHaptics;
        [SerializeField] private HapticImpulsePlayer rightHaptics;
        [SerializeField] private QuestButton tapButton = QuestButton.B;
        [SerializeField] private Renderer glassRenderer;

        [Header("두드리기")]
        [SerializeField] private float touchDistance = 0.04f;
        [SerializeField] private float cooldown = 0.4f;
        [SerializeField] private float hapticAmplitude = 0.6f;
        [SerializeField] private float hapticDuration = 0.08f;
        [SerializeField] private float flashAlpha = 0.3f;
        [SerializeField] private float flashTime = 0.3f;

        [Header("시뮬레이터 테스트 입력")]
        [SerializeField] private bool useTestKeys = true;

        private InputAction tapAction;
        private MaterialPropertyBlock block;
        private Color glassColor = Color.clear;
        private float nextTapTime;
        private float flashUntil;
        private bool flashDirty;
        private bool leftInside;
        private bool rightInside;

        public int TapCount { get; private set; }
        public string LastTapText { get; private set; } = "아직 없음";

        private void Awake()
        {
            block = new MaterialPropertyBlock();
            if (glassRenderer != null &&
                glassRenderer.sharedMaterial != null &&
                glassRenderer.sharedMaterial.HasProperty(BaseColorId))
                glassColor = glassRenderer.sharedMaterial.GetColor(BaseColorId);
        }

        private void OnEnable()
        {
            tapAction = QuestButtons.Create(tapButton, "Tap Glass");
            tapAction.Enable();
        }

        private void OnDisable()
        {
            if (tapAction == null) return;
            tapAction.Disable();
            tapAction.Dispose();
            tapAction = null;
        }

        private void Update()
        {
            if (aquarium == null || aquarium.IsShown == false || aquarium.Window == null) return;

            if (tapAction != null && rayOrigin != null && tapAction.WasPressedThisFrame())
            {
                string button = QuestButtons.Label(tapButton);

                if (RayToGlass(new Ray(rayOrigin.position, rayOrigin.forward), out Vector3 p))
                    Tap(p, rightHaptics, button);
                else LastTapText = $"유리를 가리키고 {button}를 누르세요";
            }

            CheckTouch(leftHand, leftHaptics, ref leftInside, "왼손 터치");
            CheckTouch(rightHand, rightHaptics, ref rightInside, "오른손 터치");

            if (useTestKeys && head != null && TapKeyPressed())
            {
                Vector3 point = aquarium.Window.position;

                if (RayToGlass(new Ray(head.position, head.forward), out Vector3 hit)) point = hit;
                Tap(point, null, "시선");
            }

            UpdateFlash();
        }

        private static bool TapKeyPressed()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard == null) return false;
            return keyboard.f4Key.wasPressedThisFrame;
        }

        private void CheckTouch(
            Transform hand,
            HapticImpulsePlayer haptics,
            ref bool inside,
            string label)
        {
            if (hand == null) return;
            Transform window = aquarium.Window;
            Vector3 local = window.InverseTransformPoint(hand.position);
            Vector2 half = aquarium.Size * 0.5f;
            bool now = Mathf.Abs(local.x) < half.x &&
                       Mathf.Abs(local.y) < half.y &&
                       local.z > -touchDistance &&
                       local.z < 0.1f;

            if (now && inside == false) Tap(window.TransformPoint(new Vector3(local.x, local.y, 0f)), haptics, label);
            inside = now;
        }

        private bool RayToGlass(Ray ray, out Vector3 point)
        {
            point = Vector3.zero;
            Transform window = aquarium.Window;
            var plane = new Plane(-window.forward, window.position);

            if (plane.Raycast(ray, out float enter) == false) return false;
            Vector3 hit = ray.GetPoint(enter);
            Vector3 local = window.InverseTransformPoint(hit);
            Vector2 half = aquarium.Size * 0.5f;

            if (Mathf.Abs(local.x) > half.x || Mathf.Abs(local.y) > half.y) return false;
            point = hit;
            return true;
        }

        private void Tap(
            Vector3 point,
            HapticImpulsePlayer haptics,
            string label)
        {
            if (Time.time < nextTapTime) return;
            nextTapTime = Time.time + cooldown;
            if (school != null) school.Scatter(point);

            float amp = haptics != null ? hapticAmplitude : 0f;
            bool sent = haptics != null && haptics.SendHapticImpulse(hapticAmplitude, hapticDuration);
            LastTapText = $"{label} · 진동 {amp:F2}" + (haptics != null && sent == false ? " (전송 안 됨)" : "");
            TapCount++;
            flashUntil = Time.time + flashTime;
        }

        private void UpdateFlash()
        {
            if (glassRenderer == null) return;
            float k = Mathf.Clamp01((flashUntil - Time.time) / flashTime);

            if (k <= 0f && flashDirty == false) return;
            Color c = glassColor;
            c.a = Mathf.Clamp01(c.a + flashAlpha * k);
            glassRenderer.GetPropertyBlock(block);
            block.SetColor(BaseColorId, c);
            glassRenderer.SetPropertyBlock(block);
            flashDirty = (k > 0f);
        }
    }
}
