// ─────────────────────────────────────────────
// ExerciseLab — 문제 메서드를 정해진 입력으로 불러 기대값과 비교하고, 통과 개수와 첫 실패를 패널에 보여 준다
// 학습 포인트
//  - 채점 케이스는 입력과 기대값만 적습니다. 정답 공식은 들어 있지 않습니다
//  - 케이스마다 흔한 실수 하나를 잡도록 골랐고, 실패하면 그 실수에 맞춘 힌트를 보여 줍니다
//  - 세션 YD 부터 시연 전용이라 씬에 채점 패널이 없습니다. 빌더가 checkOnStart 를 끄고, 필요하면 RunSelfCheck 를 직접 부릅니다
// 조작
//  - 없음
// ─────────────────────────────────────────────
using TMPro;
using UnityEngine;

namespace StudyXR.ScenePort
{
    public abstract class ExerciseLab : MonoBehaviour
    {
        [Header("채점")]
        [SerializeField] private bool checkOnStart = true;    // 채점 패널이 없는 시연 씬은 끔 (세션 YD)
        [SerializeField] private TMP_Text checkText;
        [SerializeField] private string title = "채점";
        [SerializeField] private float tolerance = 0.01f;
        [SerializeField] private float angleTolerance = 0.5f;   // 도

        private int passed;
        private int total;
        private string firstFailure;

        public int Passed => passed;
        public int Total => total;

        protected virtual void Start()
        {
            if (checkOnStart) RunSelfCheck();
        }

        public void RunSelfCheck()
        {
            passed = 0;
            total = 0;
            firstFailure = null;

            try
            {
                AddCases();
            }
            catch (System.Exception e)
            {
                firstFailure = $"케이스 {total + 1} 실행 중 오류\n{e.GetType().Name} : {e.Message}";
                Debug.LogException(e);
            }

            string result = firstFailure == null ? "모든 케이스를 통과했습니다" : firstFailure;
            if (checkText != null) checkText.text = $"{title}\n채점 : {passed} / {total} 통과\n{result}";

            Debug.Log($"[채점] {GetType().Name} : {passed} / {total} 통과");
        }

        protected abstract void AddCases();

        protected void CheckInt(string label, int expected, int actual, string hint)
        {
            Record(label, expected == actual, expected.ToString(), actual.ToString(), hint);
        }

        protected void CheckFloat(string label, float expected, float actual, string hint)
        {
            bool ok = Mathf.Abs(expected - actual) <= tolerance;
            Record(label, ok, expected.ToString("F2"), actual.ToString("F2"), hint);
        }

        // 각도는 360 을 더하거나 빼도 같은 방향입니다(-90 과 270). 그래서 차이를 DeltaAngle 로 잽니다
        protected void CheckAngle(string label, float expected, float actual, string hint)
        {
            bool ok = Mathf.Abs(Mathf.DeltaAngle(expected, actual)) <= angleTolerance;
            Record(label, ok, expected.ToString("F1") + "도", actual.ToString("F1") + "도", hint);
        }

        private void Record(string label, bool ok, string expectedText, string actualText, string hint)
        {
            total++;

            if (ok)
            {
                passed++;
                Debug.Log($"[채점] 통과 · 케이스 {total} {label}");
                return;
            }

            Debug.LogWarning($"[채점] 실패 · 케이스 {total} {label} · 기대 {expectedText} · 내 값 {actualText} · 힌트 : {hint}");
            if (firstFailure == null)
                firstFailure = $"실패 : 케이스 {total} · {label}\n기대 {expectedText}  ·  내 값 {actualText}\n힌트 : {hint}";
        }
    }
}
