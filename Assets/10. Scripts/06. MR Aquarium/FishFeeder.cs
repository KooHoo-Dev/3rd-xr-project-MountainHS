using StudyXR.Common;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StudyXR.MRAquarium
{
    public class FishFeeder : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private AquariumWindow aquarium;
        [SerializeField] private FishSchool school;
        [SerializeField] private ParticleSystem food;
        [SerializeField] private Transform foodTarget;
        [SerializeField] private Transform leftHand;
        [SerializeField] private QuestButton feedButton = QuestButton.X;

        [Header("먹이")]
        [SerializeField] private int pelletCount = 14;
        [SerializeField] private float sinkSpeed = 0.04f;
        [SerializeField] private float feedDuration = 7f;
        [SerializeField] private float cooldown = 1f;
        [SerializeField] private float edgeMargin = 0.08f;
        [SerializeField] private float dropDepth = 0.3f;

        [Header("시뮬레이터 테스트 입력")]
        [SerializeField] private bool useTestKeys = true;

        private InputAction feedAction;
        private float feedUntil;
        private float nextFeedTime;

        public int FeedCount { get; private set; }
        private bool IsFeeding => (Time.time < feedUntil);

        private void OnEnable()
        {
            feedAction = QuestButtons.Create(feedButton, "Feed Fish", useTestKeys ? "<Keyboard>/f5" : null);
            feedAction.Enable();
        }

        private void OnDisable()
        {
            if (feedAction == null) return;
            feedAction.Disable();
            feedAction.Dispose();
            feedAction = null;
        }

        private void Update()
        {
            if (aquarium == null || aquarium.IsShown == false) return;

            bool pressed = feedAction != null && feedAction.WasPressedThisFrame();

            if (pressed && Time.time >= nextFeedTime) Feed();

            if (IsFeeding && foodTarget != null)
            {
                Vector3 p = foodTarget.localPosition;
                float bottom = school != null ? school.FloorY + 0.03f : p.y;
                p.y = Mathf.Max(p.y - sinkSpeed * Time.deltaTime, bottom);
                foodTarget.localPosition = p;
            }
        }

        private void Feed()
        {
            Transform window = aquarium.Window;

            if (window == null) return;
            Vector2 half = aquarium.Size * 0.5f;
            float x = 0f;

            if (leftHand != null)
                x = Mathf.Clamp(window.InverseTransformPoint(leftHand.position).x, -half.x + edgeMargin, half.x - edgeMargin);
            var start = new Vector3(x, half.y - 0.04f, aquarium.Depth * dropDepth);

            if (food != null)
            {
                var emit = new ParticleSystem.EmitParams();
                emit.position = start;
                emit.applyShapeToPosition = true;
                food.Emit(emit, pelletCount);
            }
            if (foodTarget != null) foodTarget.localPosition = start;
            if (school != null && foodTarget != null) school.Gather(foodTarget, feedDuration);

            float now = Time.time;
            feedUntil = now + feedDuration;
            nextFeedTime = now + cooldown;
            FeedCount++;
        }
    }
}
