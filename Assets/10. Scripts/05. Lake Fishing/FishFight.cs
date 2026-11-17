using UnityEngine;

namespace StudyXR.LakeFishing
{
    [DefaultExecutionOrder(10)]
    public class FishFight : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private BobberCast cast;
        [SerializeField] private BobberFloat bobberFloat;
        [SerializeField] private ReelCrank reel;
        [SerializeField] private FishingRod rod;
        [SerializeField] private FishingLine line;

        [Header("물고기 힘")]
        [SerializeField] private float pullPerMeter = 12f;
        [SerializeField] private float pullWobble = 0.25f;
        [SerializeField] private float wobbleSpeed = 1.5f;
        [SerializeField] private float staminaSeconds = 8f;
        [SerializeField] private float tiredPull = 0.3f;

        [Header("줄")]
        [SerializeField] private float reelResistance = 4f;
        [SerializeField] private float tensionResponse = 6f;
        [SerializeField] private float runSpeed = 0.5f;
        [SerializeField] private float swayWidth = 0.4f;
        [SerializeField] private float swaySpeed = 0.8f;
        [SerializeField] private float hookedDip = 0.12f;

        [Header("햅틱")]
        [SerializeField] private float maxPulse = 0.7f;
        [SerializeField] private float pulseInterval = 0.1f;

        private float fishLength;
        private float nextPulseTime;
        private float nextSplashTime;

        public bool IsFighting { get; private set; }
        public float Tension { get; private set; }
        public float Pull { get; private set; }
        public float Stamina01 { get; private set; }

        public void Begin(float lengthMeters)
        {
            fishLength = lengthMeters;
            Stamina01 = 1f;
            Tension = 0f;
            nextPulseTime = 0f;
            nextSplashTime = Time.time + 0.8f;
            IsFighting = true;
        }

        public void Stop()
        {
            IsFighting = false;
            Tension = 0f;
            Pull = 0f;
            if (bobberFloat != null) bobberFloat.DipDepth = 0f;
            if (line != null) line.Tension01 = 0f;
        }

        private void Update()
        {
            if (IsFighting == false) return;
            float dt = Time.deltaTime;

            Stamina01 = Mathf.Max(0f, Stamina01 - dt / staminaSeconds);
            float strength = Mathf.Lerp(tiredPull, 1f, Stamina01);
            Pull = fishLength * pullPerMeter * strength * (1f + pullWobble * Mathf.Sin(Time.time * wobbleSpeed * 2f * Mathf.PI));

            float reelSpeed = Mathf.Max(0f, reel.MetersPerSecond);
            float target = Pull + reelSpeed * reelResistance;
            Tension = Mathf.Lerp(Tension, target, 1f - Mathf.Exp(-tensionResponse * dt));

            Vector3 away = Vector3.ProjectOnPlane(cast.Bobber.position - cast.Tip.position, Vector3.up).normalized;
            Vector3 side = Vector3.Cross(Vector3.up, away);

            if (reelSpeed < 0.05f)
            {
                float run = runSpeed * strength * dt;
                cast.ReelIn(-run);
                cast.NudgeBobber(away * run);
            }
            cast.NudgeBobber(side * (Mathf.Sin(Time.time * swaySpeed) * swayWidth * dt));

            bobberFloat.DipDepth = hookedDip;

            float pull01 = Mathf.Clamp01(Pull / BiteController.BreakTension);

            if (Time.time >= nextSplashTime)
            {
                WaterFx.Splash(cast.Bobber.position, 0.25f + 0.5f * pull01);
                nextSplashTime = Time.time + Random.Range(0.6f, 1.5f) / (0.5f + pull01);
            }
            line.Tension01 = Tension / BiteController.BreakTension;
            if (Time.time >= nextPulseTime)
            {
                rod.Pulse(maxPulse * Mathf.Clamp01(line.Tension01), pulseInterval);
                nextPulseTime = Time.time + pulseInterval;
            }
        }
    }
}
