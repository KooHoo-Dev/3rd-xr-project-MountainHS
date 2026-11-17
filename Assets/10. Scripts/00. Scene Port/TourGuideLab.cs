// ─────────────────────────────────────────────
// TourGuideLab — 관람 투어 계산의 틀 : 메서드 세 개(지점 순서 · 도착 방향 · 페이드 길이)와 점검 케이스 아홉 개
// 학습 포인트
//  - 이 파일은 계산의 틀입니다. 실제 계산은 TourGuideAnswer 가 채웁니다
//  - 무대(TourGuide)는 이 타입으로만 계산을 부릅니다. 점검 케이스는 입력과 기대값이라 수업에서 '이 값이 나와야 한다'를 보여 줄 때 씁니다
// 조작
//  - 없음
// ─────────────────────────────────────────────
using UnityEngine;

namespace StudyXR.ScenePort
{
    public abstract class TourGuideLab : ExerciseLab
    {
        // 지점 count 개 중 current 번에서 step(+1 다음, -1 이전)만큼 간 번호. 끝을 넘으면 반대쪽 끝으로 돕니다
        public abstract int NextIndex(int current, int step, int count);

        // standPoint 에 서서 landmark 를 바라보려면 몸을 y 축으로 몇 도 돌려야 하는가 (+z 가 0도, +x 가 90도)
        public abstract float ArrivalYaw(Vector3 standPoint, Vector3 landmark);

        // distance(m)를 옮길 때 화면을 어둡게 하는 시간(초) : 0 m 에서 shortest, farDistance 이상에서 longest
        public abstract float FadeSeconds(float distance, float shortest, float longest, float farDistance);

        protected override void AddCases()
        {
            CheckInt("6곳 중 0번에서 다음(+1)",
                1, NextIndex(0, 1, 6),
                "지금 번호에 step 을 더합니다");

            CheckInt("6곳 중 5번(마지막)에서 다음(+1)",
                0, NextIndex(5, 1, 6),
                "마지막 다음은 처음(0)입니다. 나머지 연산 % 를 쓰세요");

            CheckInt("6곳 중 0번에서 이전(-1)",
                5, NextIndex(0, -1, 6),
                "C# 의 % 는 음수를 그대로 돌려줍니다(-1 % 6 = -1). count 를 한 번 더한 뒤 % 하세요");

            CheckAngle("오른쪽(+x)에 있는 성",
                90f, ArrivalYaw(new Vector3(0f, 0f, 0f), new Vector3(10f, 5f, 0f)),
                "Mathf.Atan2(x, z) — Unity 는 +z 가 0도, +x 가 90도입니다. 높이(y)는 쓰지 않습니다");

            CheckAngle("뒤쪽(-z)에 있는 우물",
                180f, ArrivalYaw(new Vector3(2f, 0f, 3f), new Vector3(2f, 1f, -7f)),
                "바라볼 방향 = landmark - standPoint. 서 있는 자리를 빼야 합니다");

            CheckAngle("왼쪽 뒤 대각선의 큰 나무",
                -135f, ArrivalYaw(new Vector3(1f, 0f, 1f), new Vector3(-2f, 4f, -2f)),
                "Atan2 는 라디안을 돌려줍니다. Mathf.Rad2Deg 를 곱하세요");

            CheckFloat("50 m 이동 (0.2~0.6초, 100 m 에서 가장 김)",
                0.4f, FadeSeconds(50f, 0.2f, 0.6f, 100f),
                "Mathf.Lerp(shortest, longest, distance / farDistance)");

            CheckFloat("0 m 이동",
                0.2f, FadeSeconds(0f, 0.2f, 0.6f, 100f),
                "가까워도 shortest 만큼은 어둡게 합니다");

            CheckFloat("250 m 이동 (가장 긴 거리를 넘음)",
                0.6f, FadeSeconds(250f, 0.2f, 0.6f, 100f),
                "아주 멀어도 longest 를 넘지 않게 비율을 0~1 로 자릅니다 (Mathf.Clamp01)");
        }
    }
}
