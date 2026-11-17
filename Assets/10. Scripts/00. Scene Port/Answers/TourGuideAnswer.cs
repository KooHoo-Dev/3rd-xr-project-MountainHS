// ─────────────────────────────────────────────
// TourGuideAnswer — 관람 투어 안내원의 계산 세 가지(지점 순서 · 도착 방향 · 페이드 길이)와 해설
// 학습 포인트
//  - 음수 나머지는 count 를 한 번 더한 뒤 다시 % 하면 0 ~ count-1 로 돌아옵니다
//  - Atan2(x, z) 는 +z 를 0도로, 시계 방향(위에서 볼 때 +x 쪽)을 + 로 잽니다 — Unity 의 y 회전과 같은 약속입니다
//  - Lerp 의 t 를 Clamp01 로 자르면 어떤 거리에서도 shortest ~ longest 사이에 머뭅니다
// 조작
//  - 없음 (계산만 합니다. 이동 조작은 TourGuide — 오른손 A : 다음 지점 · 오른손 B : 이전 지점)
// ─────────────────────────────────────────────
using UnityEngine;

namespace StudyXR.ScenePort
{
    public class TourGuideAnswer : TourGuideLab
    {
        public override int NextIndex(int current, int step, int count)
        {
            if (count <= 0) return 0;
            // (0 - 1) % 6 = -1 → (-1 + 6) % 6 = 5
            return ((current + step) % count + count) % count;
        }

        public override float ArrivalYaw(Vector3 standPoint, Vector3 landmark)
        {
            Vector3 toLandmark = landmark - standPoint;
            // 높이(y)는 쓰지 않습니다. 몸은 y 축으로만 돌기 때문입니다
            return Mathf.Atan2(toLandmark.x, toLandmark.z) * Mathf.Rad2Deg;
        }

        public override float FadeSeconds(float distance, float shortest, float longest, float farDistance)
        {
            // farDistance 가 0 이하이면 나누지 않고 가장 긴 시간을 씁니다
            float t = farDistance > 0f ? Mathf.Clamp01(distance / farDistance) : 1f;
            return Mathf.Lerp(shortest, longest, t);
        }
    }
}
