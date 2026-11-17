// ─────────────────────────────────────────────
// OptimizationItem — 최적화 비교 씬에서 실행 중에 켜고 끌 수 있는 항목 네 가지 (안내판 순서)
// 학습 포인트
//  - 모두 실행 중에 바꿀 수 있는 값만 골랐습니다. Static 배칭은 빌드 때 정해져 목록에 없습니다(안내판 '고정' 줄)
// 조작
//  - 없음
// ─────────────────────────────────────────────
namespace StudyXR.ScenePort
{
    public enum OptimizationItem
    {
        DrawDistance,       // 카메라 먼 거리 + 안개
        ShadowDistance,     // 파이프라인 그림자 거리
        RenderScale,        // 파이프라인 렌더 스케일 (눈마다 그리는 해상도)
        Foveation,          // 포비티드 렌더링 (기기에서만)
    }
}
