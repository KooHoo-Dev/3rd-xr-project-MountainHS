using UnityEngine;

namespace StudyXR.Strike
{
    /// <summary>
    /// 주사위 씬이 켜져 있는 동안만 전역 물리 값(Physics.bounceThreshold)을 주사위용으로 바꾼다.
    /// 끌 때 원래 값으로 되돌려 다른 씬과 프로젝트 설정에 남지 않게 한다 (<c>SceneTimestep</c>과 같은 방식).
    /// </summary>
    public class DiceScenePhysics : MonoBehaviour
    {
        [Tooltip("씬 전용 물리 값을 읽을 튜닝 에셋")]
        [SerializeField] private DicePhysicsSettings settings;

        // 이 컴포넌트가 켜지기 전 값 (m/s). 끌 때 되돌린다.
        private float previousBounceThreshold;

        /// <summary>
        /// 원래 값을 기억하고 주사위용 값을 적용한다.
        /// </summary>
        private void OnEnable()
        {
            previousBounceThreshold = Physics.bounceThreshold;
            ApplySettings(settings);
        }

        /// <summary>
        /// 기억한 원래 값으로 되돌린다.
        /// </summary>
        private void OnDisable()
        {
            Physics.bounceThreshold = previousBounceThreshold;
        }

        /// <summary>
        /// 튜닝 에셋의 씬 전용 물리 값을 적용한다. OnEnable이 처음 한 번, 테스트 키(F6)가 다시 부른다.
        /// </summary>
        /// <param name="newSettings">적용할 튜닝 에셋</param>
        public void ApplySettings(DicePhysicsSettings newSettings)
        {
            settings = newSettings;
            Physics.bounceThreshold = settings.bounceThresholdMetersPerSecond;
        }
    }
}
