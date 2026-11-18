using UnityEngine;

namespace StudyXR.Strike
{
    /// <summary>
    /// 주사위 손맛 튜닝 값을 한곳에 모은 에셋. 주사위 물리, 던지기, 정지 판정, 씬 전용 물리 값.
    /// <see cref="Dice.ApplySettings"/>와 <see cref="DiceScenePhysics.ApplySettings"/>가 읽어 적용한다.
    /// </summary>
    /// <remarks>
    /// 에셋이라 Play 중에 바꾼 값이 Play를 멈춘 뒤에도 남는다.
    /// Rigidbody·Collider·잡기 값은 적용 시점에만 복사되므로 Play 중 수정은 다시 적용(F6)해야 반영된다.
    /// 정지 판정 값은 주사위가 매 물리 프레임 읽으므로 바로 반영된다.
    /// </remarks>
    [CreateAssetMenu(fileName = "DicePhysicsSettings", menuName = "StudyXR/MR Strike/Dice Physics Settings")]
    public class DicePhysicsSettings : ScriptableObject
    {
        [Header("주사위 물리")]
        [Tooltip("주사위 한 변 길이 (m)")]
        public float diceSizeMeters = 0.035f;

        [Tooltip("주사위 질량 (kg). 주사위끼리 같은 값이면 체감 차이가 거의 없다")]
        public float massKilograms = 0.02f;

        [Tooltip("공기 저항처럼 이동 속도를 줄이는 값. 클수록 덜 날아가고 덜 미끄러진다")]
        public float linearDamping = 0.1f;

        [Tooltip("회전 속도를 줄이는 값. 클수록 덜 구른다. 던지는 동안과 구르는 동안 항상 이 값이 쓰인다")]
        public float angularDamping = 0.3f;

        [Tooltip("회전 속도 상한 (rad/s). Unity 기본 7은 주사위에 너무 낮다")]
        public float maxAngularSpeedRadiansPerSecond = 50f;

        [Tooltip("충돌을 미리 감지하는 거리 (m). 작은 주사위가 바닥에 떠 보이면 줄인다")]
        public float contactOffsetMeters = 0.002f;

        [Tooltip("주사위 표면 재질 (마찰·튕김)")]
        public PhysicsMaterial physicsMaterial;

        [Header("던지기 (XRGrabInteractable에 적용, DiceThrow가 읽음)")]
        [Tooltip("손 속도에 곱하는 배율. 1 = 손 속도 그대로")]
        public float throwVelocityScale = 1f;

        [Tooltip("놓는 순간 손 속도를 평균 내는 구간 (초). 길수록 부드럽고 짧을수록 손끝 속도에 민감하다")]
        public float throwSmoothingSeconds = 0.1f;

        [Header("정지 판정")]
        [Tooltip("이 속도 이하이면 멈춘 것으로 본다 (m/s)")]
        public float restSpeedMetersPerSecond = 0.02f;

        [Tooltip("정지 속도 이하로 이 시간 이상 머물면 IsResting = true (초)")]
        public float restHoldSeconds = 0.4f;

        [Tooltip("이 시간이 지나도 안 멈추면 지금 면으로 판정한다 (초). 턴 진행(#5)이 쓴다")]
        public float rollTimeoutSeconds = 6f;

        [Header("씬 전용 물리 (DiceScenePhysics가 적용, 씬을 끄면 복원)")]
        [Tooltip("이 속도보다 느리게 부딪히면 튕기지 않는다 (m/s). 프로젝트 기본 2는 작은 주사위에 너무 크다")]
        public float bounceThresholdMetersPerSecond = 0.3f;
    }
}
