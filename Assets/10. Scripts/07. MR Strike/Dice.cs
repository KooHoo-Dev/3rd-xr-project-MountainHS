using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace StudyXR.Strike
{
    /// <summary>
    /// 경기장에 던지는 주사위 한 개. 멈췄는지(<see cref="IsResting"/>)와 위를 향한 면(<see cref="ReadTopFace"/>)을 알려 준다.
    /// 판정은 하지 않는다. 턴 진행(#5)이 이 두 값을 읽어 판정한다.
    /// </summary>
    /// <remarks>
    /// 손으로 던지는 계산은 같은 오브젝트의 <see cref="DiceThrow"/>가 한다.
    /// DiceThrow는 회전 감쇠를 건드리지 않으므로 튜닝 에셋의 angularDamping이 항상 그대로 유지된다.
    /// </remarks>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(XRGrabInteractable))]
    public class Dice : MonoBehaviour
    {
        [Header("튜닝")]
        [Tooltip("시작할 때 적용할 튜닝 에셋")]
        [SerializeField] private DicePhysicsSettings settings;

        [Header("면")]
        [Tooltip("면 라벨 6개. 순서 = DiceFace 순서 (One, Two, Three, Four, Five, X)")]
        [SerializeField] private Transform[] faceLabels;

        [Header("강조")]
        [SerializeField] private Renderer bodyRenderer;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color selectableColor = new Color(1f, 0.85f, 0.3f);

        // ── 컴포넌트 ──
        private Rigidbody body;
        private BoxCollider boxCollider;
        private XRGrabInteractable grabInteractable;

        // ── 정지 판정 ──
        // 정지 속도 이하로 연속해서 머문 시간 (초)
        private float slowSeconds;

        // 정지 속도 이하가 정지 유지 시간 이상 이어졌으면 true. 손에 쥔 동안은 항상 false.
        public bool IsResting => slowSeconds >= settings.restHoldSeconds;

        /// <summary>
        /// 컴포넌트를 찾고 프리팹에 연결된 튜닝 에셋을 적용한다.
        /// </summary>
        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            boxCollider = GetComponent<BoxCollider>();
            grabInteractable = GetComponent<XRGrabInteractable>();
            ApplySettings(settings);
        }

        /// <summary>
        /// 매 물리 프레임 정지 시간을 갱신한다. <see cref="IsResting"/>이 이 값을 읽는다.
        /// </summary>
        private void FixedUpdate()
        {
            UpdateRestTimer();
        }

        /// <summary>
        /// 위를 가장 많이 향한 면을 읽는다. 턴 진행이 주사위가 멈춘 뒤(또는 시간초과 때) 부른다.
        /// </summary>
        /// <returns>면 라벨 중 주사위 중심에서 본 방향이 월드 위쪽과 가장 가까운 면. 기울어 멈췄어도 가장 가까운 면을 돌려준다.</returns>
        public DiceFace ReadTopFace()
        {
            int topFaceIndex = 0;
            float highestUpAlignment = float.NegativeInfinity;
            for (int faceIndex = 0; faceIndex < faceLabels.Length; faceIndex++)
            {
                Vector3 faceDirection = (faceLabels[faceIndex].position - transform.position).normalized;
                float upAlignment = Vector3.Dot(faceDirection, Vector3.up);
                if (upAlignment > highestUpAlignment)
                {
                    highestUpAlignment = upAlignment;
                    topFaceIndex = faceIndex;
                }
            }
            return (DiceFace)topFaceIndex;
        }

        /// <summary>
        /// 튜닝 에셋 값을 크기·Rigidbody·Collider·잡기 컴포넌트에 복사한다.
        /// Awake가 처음 한 번, 테스트 키(F6)가 Play 중 값을 바꾼 뒤 다시 부른다.
        /// </summary>
        /// <param name="newSettings">적용할 튜닝 에셋. 이후 정지 판정도 이 에셋 값을 읽는다.</param>
        public void ApplySettings(DicePhysicsSettings newSettings)
        {
            settings = newSettings;
            ApplySize();
            ApplyRigidbody();
            ApplyCollider();
            ApplyThrow();
        }

        /// <summary>
        /// 이번 차례에 집을 수 있는 주사위인지 정한다. 강조 색과 잡기 허용을 함께 바꾼다.
        /// </summary>
        /// <param name="selectable">true = 강조 색 + 잡기 허용, false = 기본 색 + 잡기 막음</param>
        public void SetSelectable(bool selectable)
        {
            grabInteractable.enabled = selectable;
            if (selectable)
            {
                bodyRenderer.material.color = selectableColor;
            }
            else
            {
                bodyRenderer.material.color = normalColor;
            }
        }

        /// <summary>
        /// 손에 쥐지 않았고 정지 속도 이하이면 정지 시간을 늘리고, 아니면 0으로 되돌린다.
        /// </summary>
        private void UpdateRestTimer()
        {
            bool isHeld = grabInteractable.isSelected;
            bool isMoving = body.linearVelocity.magnitude > settings.restSpeedMetersPerSecond;
            if (isHeld || isMoving)
            {
                slowSeconds = 0f;
                return;
            }
            slowSeconds += Time.fixedDeltaTime;
        }

        /// <summary>
        /// 루트 크기를 주사위 한 변 길이로 맞춘다. 프리팹은 한 변 1 크기로 만들어져 있다.
        /// </summary>
        private void ApplySize()
        {
            transform.localScale = Vector3.one * settings.diceSizeMeters;
        }

        /// <summary>
        /// 질량·감쇠·회전 속도 상한을 Rigidbody에 넣는다.
        /// </summary>
        private void ApplyRigidbody()
        {
            body.mass = settings.massKilograms;
            body.linearDamping = settings.linearDamping;
            body.angularDamping = settings.angularDamping;
            body.maxAngularVelocity = settings.maxAngularSpeedRadiansPerSecond;
        }

        /// <summary>
        /// 표면 재질과 충돌 감지 거리를 Collider에 넣는다.
        /// </summary>
        private void ApplyCollider()
        {
            boxCollider.sharedMaterial = settings.physicsMaterial;
            boxCollider.contactOffset = settings.contactOffsetMeters;
        }

        /// <summary>
        /// 던지기 배율과 평균 구간을 XRGrabInteractable에 넣는다. DiceThrow가 놓는 순간 이 값을 읽는다.
        /// </summary>
        private void ApplyThrow()
        {
            grabInteractable.throwVelocityScale = settings.throwVelocityScale;
            grabInteractable.throwSmoothingDuration = settings.throwSmoothingSeconds;
        }
    }
}
