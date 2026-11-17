using UnityEngine;
using UnityEngine.InputSystem;

namespace StudyXR.LakeFishing
{
    public class BobberCast : MonoBehaviour
    {
        public enum Phase { Ready, Charging, Flying, Landed }

        [Header("연결")]
        [SerializeField] private FishingRod rod;
        [SerializeField] private Transform bobber;

        [Header("던지기")]
        [SerializeField] private float speedScale = 0.5f;
        [SerializeField] private float minSpeed = 1f;
        [SerializeField] private float maxSpeed = 14f;
        [SerializeField] private float launchPulse = 0.5f;

        [Header("찌와 줄")]
        [SerializeField] private float waterHeight = 0f;
        [SerializeField] private float hangLength = 0.35f;
        [SerializeField] private float retrieveDistance = 0.8f;
        [SerializeField] private float maxLineLength = 40f;
        [SerializeField] private Vector2 waterMin = new Vector2(-30f, 4.3f);
        [SerializeField] private Vector2 waterMax = new Vector2(30f, 65f);

        [Header("시뮬레이터 테스트 입력")]
        [SerializeField] private bool enableTestKeys = true;
        [SerializeField] private Vector3 testTipVelocity = new Vector3(0f, 6f, 16f);
        [SerializeField] private Vector3 weakTipVelocity = new Vector3(0f, 0.6f, 1.4f);
        [SerializeField] private Vector3 strongTipVelocity = new Vector3(0f, 15f, 40f);

        private Vector3 velocity;
        private Vector3 releasePoint;

        public Phase CurrentPhase { get; private set; }
        public float LineLength { get; private set; }
        public float HorizontalDistance { get; private set; }
        public Vector3 LastLaunchVelocity { get; private set; }
        public float LaunchAngle { get; private set; }
        public float CastDistance { get; private set; }
        public Vector3 LandingVelocity { get; private set; }
        public string LastNote { get; private set; } = "";
        public Transform Bobber => bobber;
        public bool Locked { get; set; }
        public Transform Tip => rod.Tip;

        private void OnEnable()
        {
            if (rod == null) return;
            rod.TriggerPressed += OnTriggerPressed;
            rod.TriggerReleased += OnTriggerReleased;
        }

        private void OnDisable()
        {
            if (rod == null) return;
            rod.TriggerPressed -= OnTriggerPressed;
            rod.TriggerReleased -= OnTriggerReleased;
        }

        private void Start()
        {
            Retrieve();
        }

        private void OnTriggerPressed()
        {
            if (Locked) return;
            if (CurrentPhase == Phase.Landed) { Retrieve(); return; }
            if (CurrentPhase != Phase.Ready) return;
            rod.ClearSamples();
            CurrentPhase = Phase.Charging;
        }

        private void OnTriggerReleased()
        {
            if (CurrentPhase != Phase.Charging) return;
            CurrentPhase = Phase.Ready;
            Launch(rod.TipVelocity);
        }

        private static Vector3 LaunchVelocity(
            Vector3 tipVelocity,
            float speedScale,
            float minSpeed,
            float maxSpeed)
        {
            Vector3 scaled = tipVelocity * speedScale;

            if (scaled.magnitude < minSpeed) return Vector3.zero;
            return Vector3.ClampMagnitude(scaled, maxSpeed);
        }

        public void Launch(Vector3 tipVelocity)
        {
            if (CurrentPhase != Phase.Ready || Locked) return;
            Vector3 launch = LaunchVelocity(tipVelocity, speedScale, minSpeed, maxSpeed);
            LastLaunchVelocity = launch;
            if (launch.sqrMagnitude < 0.0001f)
            {
                LastNote = "너무 느려 던지지 않음";
                return;
            }

            velocity = launch;
            releasePoint = rod.Tip.position;
            bobber.position = releasePoint;
            float speed = launch.magnitude;
            LaunchAngle = Mathf.Asin(Mathf.Clamp(launch.y / speed, -1f, 1f)) * Mathf.Rad2Deg;
            LastNote = "";
            CurrentPhase = Phase.Flying;
            rod.Pulse(launchPulse * Mathf.Clamp01(speed / maxSpeed), 0.1f);
        }

        public void LandAt(Vector3 point)
        {
            Retrieve();
            releasePoint = rod.Tip.position;
            LastLaunchVelocity = Vector3.zero;
            LaunchAngle = 0f;
            Land(point, Vector3.zero);
        }

        public void Retrieve()
        {
            CurrentPhase = Phase.Ready;
            LineLength = hangLength;
            HangBobber();
        }

        public void ReelIn(float meters)
        {
            if (CurrentPhase != Phase.Landed) return;
            LineLength = Mathf.Min(LineLength - meters, maxLineLength);
            if (HorizontalDistance <= retrieveDistance) Retrieve();
        }

        public void NudgeBobber(Vector3 delta)
        {
            if (CurrentPhase != Phase.Landed) return;
            delta.y = 0f;
            bobber.position += delta;
        }

        private void Update()
        {
            ReadTestKeys();
            if (CurrentPhase != Phase.Flying) return;

            float dt = Time.deltaTime;
            velocity += Physics.gravity * dt;
            Vector3 p = bobber.position + velocity * dt;
            bobber.position = p;
            if (p.y <= waterHeight) Land(p, velocity);
        }

        private void Land(Vector3 point, Vector3 landingVelocity)
        {
            point.y = waterHeight;
            bobber.position = point;
            LandingVelocity = landingVelocity;
            CastDistance = Vector2.Distance(new Vector2(releasePoint.x, releasePoint.z), new Vector2(point.x, point.z));
            if (point.x < waterMin.x || point.x > waterMax.x || point.z < waterMin.y || point.z > waterMax.y)
            {
                LastNote = "물 밖에 떨어짐 — 거둠";
                Retrieve();
                return;
            }

            CurrentPhase = Phase.Landed;
            LineLength = Vector3.Distance(rod.Tip.position, point);
            UpdateDistance();
            rod.Pulse(0.15f, 0.05f);
            WaterFx.Splash(point, Mathf.Clamp(landingVelocity.magnitude / 10f, 0.25f, 1f));
        }

        private void LateUpdate()
        {
            if (CurrentPhase == Phase.Ready || CurrentPhase == Phase.Charging) HangBobber();
            else if (CurrentPhase == Phase.Flying) LineLength = Vector3.Distance(rod.Tip.position, bobber.position);
            else KeepWithinLine();
            UpdateDistance();
        }

        private void KeepWithinLine()
        {
            Vector3 tipPosition = rod.Tip.position;
            Vector3 position = bobber.position;
            float dy = tipPosition.y - position.y;
            Vector3 flat = new Vector3(position.x - tipPosition.x, 0f, position.z - tipPosition.z);
            float reach = Mathf.Sqrt(Mathf.Max(0f, LineLength * LineLength - dy * dy));

            if (flat.magnitude <= reach) return;

            Vector3 pulled = tipPosition + flat.normalized * reach;
            pulled.y = position.y;
            bobber.position = pulled;
        }

        private void HangBobber()
        {
            bobber.SetPositionAndRotation(rod.Tip.position + Vector3.down * hangLength, Quaternion.identity);
        }

        private void UpdateDistance()
        {
            Vector3 d = bobber.position - rod.Tip.position;
            HorizontalDistance = new Vector2(d.x, d.z).magnitude;
        }

        private void ReadTestKeys()
        {
            if (enableTestKeys == false) return;
            Keyboard keyboard = Keyboard.current;

            if (keyboard == null) return;

            if (keyboard.f1Key.wasPressedThisFrame) TestCast(testTipVelocity);
            if (keyboard.f3Key.wasPressedThisFrame) TestCast(weakTipVelocity);
            if (keyboard.f5Key.wasPressedThisFrame) TestCast(strongTipVelocity);
            if (keyboard.f9Key.wasPressedThisFrame) Retrieve();
        }

        private void TestCast(Vector3 localTipVelocity)
        {
            Retrieve();
            Vector3 forward = Vector3.ProjectOnPlane(rod.Tip.position - rod.transform.position, Vector3.up);
            Quaternion yaw = forward.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(forward) : Quaternion.identity;
            Launch(yaw * localTipVelocity);
        }
    }
}
