using UnityEngine;

namespace StudyXR.Strike
{
    /// <summary>
    /// 플레이어가 경기장에서 너무 멀어지지 않게 막는다. 머리가 최대 거리 밖으로 나가면 넘친 만큼 XR Origin을 경기장 쪽으로 되민다.
    /// 너무 멀리서 약하게 던져 그릇에 못 닿는 상황을 막는 것이 목적이다. 스틱 이동이든 실제로 걸어 나가든 같은 규칙이다.
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

        /// <summary>
        /// 이동 Provider가 이번 프레임 이동을 마친 뒤 거리를 검사하고 넘쳤으면 되민다.
        /// </summary>
        private void LateUpdate()
        {
            Vector3 headOffset = HorizontalOffsetFromArena();
            float distanceFromArena = headOffset.magnitude;
            if (distanceFromArena <= maxDistanceFromArenaMeters) return;

            PushBackToward(headOffset, distanceFromArena);
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
        /// 머리가 최대 거리 원 위에 오도록 XR Origin 전체를 경기장 쪽으로 옮긴다.
        /// </summary>
        /// <param name="headOffset">경기장 중심에서 머리까지 수평 벡터</param>
        /// <param name="distanceFromArena">그 벡터의 길이 (m), 최대 거리보다 크다</param>
        private void PushBackToward(Vector3 headOffset, float distanceFromArena)
        {
            float overflowMeters = distanceFromArena - maxDistanceFromArenaMeters;
            Vector3 towardArena = -headOffset / distanceFromArena;
            transform.position += towardArena * overflowMeters;
        }
    }
}
