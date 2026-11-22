using UnityEngine;

namespace StudyXR.Strike
{
    /// <summary>
    /// 플레이어 머리를 경기장 둘레의 고리 안(최소~최대 거리)에 붙잡아 둔다. 고리 밖으로 넘친 만큼 XR Origin을 되민다.
    /// 최대 거리: 너무 멀리서 약하게 던져 그릇에 못 닿는 상황을 막는다.
    /// 최소 거리: 몸 충돌(CharacterController)을 뺀 리그가 원형 탁자 안쪽까지 들어와 가까이서 던지게 하되, 머리가 경기장 위로 들어가지는 않게 한다.
    /// 스틱 이동이든 실제로 걸어 움직이든 같은 규칙이다.
    /// </summary>
    /// <remarks>
    /// XR Origin 루트에 붙인다. 이동 Provider는 건드리지 않고, 이동이 끝난 뒤(LateUpdate) 위치만 바로잡는다.
    /// 경기장 위치를 매 프레임 읽으므로 책상 배치(#9)가 경기장을 옮겨도 따라간다.
    /// </remarks>
    public class ArenaBoundary : MonoBehaviour
    {
        [Header("연결")]
        [Tooltip("거리 기준이 되는 경기장 (그릇 중심)")]
        [SerializeField] private DiceArena arena;

        [Tooltip("플레이어 머리 (XR Origin의 Main Camera)")]
        [SerializeField] private Transform head;

        [Header("거리 제한 (가정)")]
        [Tooltip("경기장 중심에서 머리까지 수평 최대 거리 (m). 이 밖으로는 못 나간다")]
        [SerializeField] private float maxDistanceFromArenaMeters = 1.2f;
        [Tooltip("경기장 중심에서 머리까지 수평 최소 거리 (m). 탁자 안쪽으로 이보다 더 들어오지 못한다")]
        [SerializeField] private float minDistanceFromArenaMeters = 0.45f;

        /// <summary>
        /// 이동 Provider가 이번 프레임 이동을 마친 뒤 거리를 검사하고, 고리 밖이면 가까운 경계까지 되민다.
        /// </summary>
        private void LateUpdate()
        {
            Vector3 headOffset = HorizontalOffsetFromArena();
            float distanceFromArena = headOffset.magnitude;
            bool isTooFar = distanceFromArena > maxDistanceFromArenaMeters;
            bool isTooClose = distanceFromArena < minDistanceFromArenaMeters;

            if (isTooFar) PushToDistance(headOffset, distanceFromArena, maxDistanceFromArenaMeters);
            if (isTooClose) PushToDistance(headOffset, distanceFromArena, minDistanceFromArenaMeters);
        }

        /// <summary>
        /// 경기장 중심에서 머리까지의 수평 방향 벡터. 높이 차이는 무시한다.
        /// </summary>
        /// <returns>머리 위치 - 경기장 중심 (y = 0)</returns>
        private Vector3 HorizontalOffsetFromArena()
        {
            Vector3 offset = head.position - arena.transform.position;
            offset.y = 0f;
            return offset;
        }

        /// <summary>
        /// 머리가 경기장 중심에서 목표 거리 원 위에 오도록 XR Origin 전체를 바깥(또는 안)쪽으로 옮긴다.
        /// </summary>
        /// <param name="headOffset">경기장 중심에서 머리까지 수평 벡터</param>
        /// <param name="distanceFromArena">그 벡터의 길이 (m)</param>
        /// <param name="targetDistance">머리를 둘 거리 (m). 지금보다 작으면 경기장 쪽, 크면 바깥쪽으로 민다</param>
        private void PushToDistance(Vector3 headOffset, float distanceFromArena, float targetDistance)
        {
            Vector3 awayFromArena = headOffset / distanceFromArena;
            float moveMeters = targetDistance - distanceFromArena;
            transform.position += awayFromArena * moveMeters;
        }
    }
}
