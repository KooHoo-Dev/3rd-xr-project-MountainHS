using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace StudyXR.Strike
{
    /// <summary>
    /// 손으로 쥔 주사위를 놓는 순간, 최근 손 움직임에서 속도와 회전을 추정해 주사위에 넣는다.
    /// 샘플 <c>04. Throw Physics/PhysicalThrow</c>를 복사한 것이다. 원본과 달리 회전 감쇠(angularDamping)를 건드리지 않는다.
    /// </summary>
    /// <remarks>
    /// 회전 감쇠는 <see cref="Dice.ApplySettings"/>가 튜닝 에셋 값으로 한 번 넣은 그대로 유지된다.
    /// 던지기 배율(throwVelocityScale)과 평균 구간(throwSmoothingDuration)은 같은 오브젝트의 XRGrabInteractable 값을 읽는다.
    /// </remarks>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(XRGrabInteractable))]
    public class DiceThrow : MonoBehaviour
    {
        // ── 상수 ──
        // 손 움직임 기록 최대 개수 (프레임)
        private const int SampleCapacity = 64;

        // 이보다 빠르게 손이 순간이동하면 기록을 버린다 (m/s)
        private const float HandTeleportSpeed = 30f;

        [Header("놓기")]
        [Tooltip("놓는 순간 주사위 속도 상한 (m/s)")]
        [SerializeField] private float maxReleaseSpeed = 25f;

        // ── 컴포넌트 ──
        private Rigidbody body;
        private XRGrabInteractable grabInteractable;

        // ── 쥐고 있는 동안 ──
        // 지금 쥔 손(인터랙터) Transform. null = 안 쥐고 있음
        private Transform grabbingHand;

        // 쥔 손 기준으로 본 주사위 위치 (손 로컬 좌표). 손을 돌리면 주사위가 반지름만큼 휘두르는 것을 계산하는 데 쓴다.
        private Vector3 handToDiceLocalOffset;

        // ── 손 움직임 기록 (원형 버퍼, 오래된 것부터 덮어씀) ──
        private readonly float[] sampleTimes = new float[SampleCapacity];
        private readonly Vector3[] handPositions = new Vector3[SampleCapacity];
        private readonly Quaternion[] handRotations = new Quaternion[SampleCapacity];
        private int sampleCount;
        private int newestSampleIndex = -1;

        // ── 추정 중간값 (기록 사이 구간마다 하나) ──
        private readonly float[] intervalMidTimes = new float[SampleCapacity];
        private readonly Vector3[] intervalHandVelocities = new Vector3[SampleCapacity];
        private readonly Vector3[] intervalHandSpins = new Vector3[SampleCapacity];

        // ── 놓은 직후 보간 끄기 ──
        private bool isInterpolationSuspended;
        private RigidbodyInterpolation savedInterpolation;

        /// <summary>
        /// 컴포넌트를 찾고, XRI 기본 던지기를 꺼서 이 클래스가 속도를 넣도록 한다.
        /// </summary>
        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            grabInteractable = GetComponent<XRGrabInteractable>();
            grabInteractable.throwOnDetach = false;
        }

        private void OnEnable()
        {
            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);
        }

        /// <summary>
        /// 이벤트를 풀고, 놓은 직후 꺼 둔 보간이 있으면 되돌린다.
        /// </summary>
        private void OnDisable()
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            grabInteractable.selectExited.RemoveListener(OnReleased);
            grabbingHand = null;
            if (isInterpolationSuspended)
            {
                isInterpolationSuspended = false;
                body.interpolation = savedInterpolation;
            }
        }

        /// <summary>
        /// 잡은 손을 기억하고 이전 기록을 비운다.
        /// </summary>
        /// <param name="args">XRI 잡기 이벤트. 어떤 인터랙터(손)가 잡았는지 들어 있다</param>
        private void OnGrabbed(SelectEnterEventArgs args)
        {
            grabbingHand = args.interactorObject.transform;
            handToDiceLocalOffset = Quaternion.Inverse(grabbingHand.rotation) * (transform.position - grabbingHand.position);
            sampleCount = 0;
            newestSampleIndex = -1;
        }

        /// <summary>
        /// 쥐고 있는 동안 매 프레임 손 위치·회전을 기록한다.
        /// </summary>
        private void LateUpdate()
        {
            bool isHeld = grabbingHand != null && grabInteractable.isSelected;
            if (isHeld == false) return;

            handToDiceLocalOffset = Quaternion.Inverse(grabbingHand.rotation) * (transform.position - grabbingHand.position);
            RecordHandSample(Time.time, grabbingHand.position, grabbingHand.rotation);
        }

        /// <summary>
        /// 손 위치·회전 한 프레임을 원형 버퍼에 넣는다. 시간이 안 흘렀으면 무시하고, 손이 순간이동했으면 이전 기록을 버린다.
        /// </summary>
        /// <param name="time">기록 시각 (초)</param>
        /// <param name="handPosition">손 월드 위치 (m)</param>
        /// <param name="handRotation">손 월드 회전</param>
        private void RecordHandSample(float time, Vector3 handPosition, Quaternion handRotation)
        {
            if (sampleCount > 0)
            {
                float secondsSinceNewest = time - sampleTimes[newestSampleIndex];
                if (secondsSinceNewest <= 0f) return;

                float handSpeed = (handPosition - handPositions[newestSampleIndex]).magnitude / secondsSinceNewest;
                bool isHandTeleported = handSpeed > HandTeleportSpeed;
                if (isHandTeleported) sampleCount = 0;
            }

            newestSampleIndex = (newestSampleIndex + 1) % SampleCapacity;
            sampleTimes[newestSampleIndex] = time;
            handPositions[newestSampleIndex] = handPosition;
            handRotations[newestSampleIndex] = handRotation;
            sampleCount = Mathf.Min(sampleCount + 1, SampleCapacity);
        }

        /// <summary>
        /// 놓는 순간 손 속도·회전을 추정해 주사위 위치·속도·회전 속도를 정한다.
        /// 손을 돌리며 놓으면 주사위가 손에서 떨어진 거리만큼 더 빠르게 날아간다.
        /// </summary>
        /// <param name="args">XRI 놓기 이벤트. 취소(canceled)면 던지지 않는다</param>
        private void OnReleased(SelectExitEventArgs args)
        {
            Transform releaseHand = grabbingHand;
            grabbingHand = null;

            if (releaseHand == null) return;
            if (args.isCanceled) return;
            if (body.isKinematic) return;

            RecordHandSample(Time.time, releaseHand.position, releaseHand.rotation);

            float averagingWindowSeconds = Mathf.Max(0.005f, grabInteractable.throwSmoothingDuration);
            EstimateHandMotionAtRelease(averagingWindowSeconds, out Vector3 handVelocity, out Vector3 handSpin);

            Vector3 handToDiceWorldOffset = releaseHand.rotation * handToDiceLocalOffset;
            Vector3 turnVelocity = Vector3.Cross(handSpin, handToDiceWorldOffset);
            Vector3 releaseVelocity = (handVelocity + turnVelocity) * grabInteractable.throwVelocityScale;
            releaseVelocity = Vector3.ClampMagnitude(releaseVelocity, maxReleaseSpeed);
            Vector3 releaseSpin = handSpin * grabInteractable.throwAngularVelocityScale;
            Vector3 releasePosition = releaseHand.position + handToDiceWorldOffset;

            SuspendInterpolationUntilNextFixedUpdate();
            body.position = releasePosition;
            transform.position = releasePosition;
            body.linearVelocity = releaseVelocity;
            body.angularVelocity = releaseSpin;
        }

        /// <summary>
        /// 놓은 프레임에 위치를 직접 옮기므로, 다음 물리 프레임까지 Rigidbody 보간을 끈다.
        /// </summary>
        private void SuspendInterpolationUntilNextFixedUpdate()
        {
            if (isInterpolationSuspended) return;

            savedInterpolation = body.interpolation;
            body.interpolation = RigidbodyInterpolation.None;
            isInterpolationSuspended = true;
        }

        /// <summary>
        /// 다음 물리 프레임이 오면 꺼 둔 보간을 되돌린다.
        /// </summary>
        private void FixedUpdate()
        {
            if (isInterpolationSuspended == false) return;

            isInterpolationSuspended = false;
            body.interpolation = savedInterpolation;
        }

        /// <summary>
        /// 기록된 손 움직임에서 놓는 순간의 손 속도와 회전 속도를 추정한다.
        /// 기록 사이 구간마다 속도를 구한 뒤, 최근 평균 구간 안의 값들로 추세선을 그어 놓은 시각의 값을 읽는다.
        /// </summary>
        /// <param name="averagingWindowSeconds">최근 몇 초의 구간을 쓸지 (초). 구간 2개까지는 항상 쓴다</param>
        /// <param name="handVelocity">놓는 순간 손 속도 (m/s). 기록이 2개 미만이면 0</param>
        /// <param name="handSpin">놓는 순간 손 회전 속도 (rad/s, 월드 축). 기록이 2개 미만이면 0</param>
        private void EstimateHandMotionAtRelease(float averagingWindowSeconds, out Vector3 handVelocity, out Vector3 handSpin)
        {
            handVelocity = Vector3.zero;
            handSpin = Vector3.zero;
            if (sampleCount < 2) return;

            float releaseTime = sampleTimes[newestSampleIndex];
            int intervalCount = 0;

            // 최신 기록부터 거꾸로, 이웃한 두 기록 사이 구간마다 손 속도·회전 속도를 구한다
            for (int intervalNumber = 0; intervalNumber < sampleCount - 1; intervalNumber++)
            {
                int laterIndex = (newestSampleIndex - intervalNumber + SampleCapacity) % SampleCapacity;
                int earlierIndex = (laterIndex - 1 + SampleCapacity) % SampleCapacity;
                float intervalSeconds = sampleTimes[laterIndex] - sampleTimes[earlierIndex];
                float intervalMidTime = (sampleTimes[earlierIndex] + sampleTimes[laterIndex]) * 0.5f;

                bool hasEnoughIntervals = intervalCount >= 2;
                bool isOutsideWindow = releaseTime - intervalMidTime > averagingWindowSeconds;
                if (hasEnoughIntervals && isOutsideWindow) break;

                Vector3 intervalSpin = CalculateSpin(handRotations[earlierIndex], handRotations[laterIndex], intervalSeconds);

                intervalMidTimes[intervalCount] = intervalMidTime;
                intervalHandVelocities[intervalCount] = (handPositions[laterIndex] - handPositions[earlierIndex]) / intervalSeconds;
                intervalHandSpins[intervalCount] = intervalSpin;
                intervalCount++;
            }

            handVelocity = FitTrendAt(intervalHandVelocities, intervalCount, releaseTime);
            handSpin = FitTrendAt(intervalHandSpins, intervalCount, releaseTime);
        }

        /// <summary>
        /// 두 회전 사이를 주어진 시간에 돌았다면 월드 축 기준 회전 속도가 얼마인지 구한다.
        /// </summary>
        /// <param name="earlierRotation">이전 회전</param>
        /// <param name="laterRotation">나중 회전</param>
        /// <param name="seconds">두 회전 사이 시간 (초)</param>
        /// <returns>회전 속도 벡터 (rad/s). 방향 = 회전축, 길이 = 속도. 계산할 수 없으면 0</returns>
        private Vector3 CalculateSpin(Quaternion earlierRotation, Quaternion laterRotation, float seconds)
        {
            Quaternion rotationDelta = laterRotation * Quaternion.Inverse(earlierRotation);
            rotationDelta.ToAngleAxis(out float degrees, out Vector3 axis);
            if (degrees > 180f) degrees -= 360f;

            Vector3 spin = axis * (degrees * Mathf.Deg2Rad / seconds);
            bool isSpinInvalid = float.IsNaN(spin.x) || float.IsInfinity(spin.x);
            if (isSpinInvalid) return Vector3.zero;
            return spin;
        }

        /// <summary>
        /// 구간별 값들에 시간에 따른 직선 추세(최소제곱)를 맞춰 지정한 시각의 값을 읽는다.
        /// 구간이 3개 미만이거나 시간 차이가 거의 없으면 평균을 돌려준다.
        /// </summary>
        /// <param name="values">구간별 값 (이 클래스의 intervalMidTimes와 같은 순서)</param>
        /// <param name="intervalCount">쓸 구간 개수 (1 이상)</param>
        /// <param name="atTime">값을 읽을 시각 (초). 보통 놓은 시각</param>
        /// <returns>atTime에서의 추정 값</returns>
        private Vector3 FitTrendAt(Vector3[] values, int intervalCount, float atTime)
        {
            float meanTime = 0f;
            Vector3 meanValue = Vector3.zero;
            for (int i = 0; i < intervalCount; i++)
            {
                meanTime += intervalMidTimes[i];
                meanValue += values[i];
            }
            meanTime /= intervalCount;
            meanValue /= intervalCount;

            if (intervalCount < 3) return meanValue;

            float timeVariance = 0f;
            Vector3 timeValueCovariance = Vector3.zero;
            for (int i = 0; i < intervalCount; i++)
            {
                float timeOffset = intervalMidTimes[i] - meanTime;
                timeVariance += timeOffset * timeOffset;
                timeValueCovariance += timeOffset * (values[i] - meanValue);
            }

            if (timeVariance <= 1e-8f) return meanValue;
            return meanValue + timeValueCovariance / timeVariance * (atTime - meanTime);
        }
    }
}
