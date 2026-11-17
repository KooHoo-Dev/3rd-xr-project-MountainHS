// ─────────────────────────────────────────────
// OptimizationBoard — 최적화 비교 안내판 : Quest 버튼으로 항목을 고르고 켜고 끄며, 지금 FPS 와 '전체 꺼짐 / 전체 켜짐' 기록을 함께 보여 준다
// 학습 포인트
//  - Quest 의 프레임은 화면 주사율(72 Hz 등)에 묶입니다. 여유가 있으면 FPS 는 그대로이고 GPU 시간(ms)만 줄어듭니다 — 둘을 함께 봅니다
//  - 값을 바꾼 직후의 FPS 는 흔들리므로 잠시(1.5초) 기다린 뒤의 값을 기록합니다
// 조작
//  - 오른손 A (시뮬레이터 F5) : 전체 켜기 / 끄기
//  - 오른손 B (시뮬레이터 F6) : 다음 항목 고르기
//  - 왼손 X (시뮬레이터 F7) : 고른 항목 켜기 / 끄기
// ─────────────────────────────────────────────
using System.Collections.Generic;
using System.Text;
using StudyXR.Common;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace StudyXR.ScenePort
{
    public class OptimizationBoard : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private OptimizationSwitcher switcher;
        [SerializeField] private PerfHud perfHud;
        [SerializeField] private TMP_Text boardText;
        [SerializeField] private WorldScaleToggle worldScale;    // 안내판에 미니어처 버튼 이름을 적을 때

        [Header("표시")]
        [SerializeField] private int staticObjects;             // 빌더가 센 Static 배칭 대상 수 ('고정' 줄)
        [SerializeField] private float refreshSeconds = 0.5f;
        [SerializeField] private float settleSeconds = 1.5f;    // 바꾼 뒤 이만큼 지나야 기록

        [Header("시뮬레이터 테스트 입력")]
        [SerializeField] private bool useTestKeys = true;       // F5 전체 · F6 다음 항목 · F7 고른 항목 (안내판에는 적지 않음)

        private static readonly string[] Names = { "그리는 거리", "그림자 거리", "렌더 해상도", "포비티드 렌더링" };
        private static readonly string[] Saves =
        {
            "먼 성 · 산을 아예 그리지 않음 (CPU 컬링 · 드로우콜)",
            "그림자 맵에 다시 그리는 물체 수 (드로우콜 · GPU)",
            "눈마다 그리는 픽셀 수 — 가로 · 세로가 함께 줄어 제곱으로 (GPU)",
            "시선 바깥을 낮은 해상도로 (GPU · 기기에서만)",
        };

        private readonly List<XRDisplaySubsystem> displays = new List<XRDisplaySubsystem>();
        private readonly StringBuilder text = new StringBuilder();
        private InputAction allAction;
        private InputAction nextAction;
        private InputAction toggleAction;
        private int selected;
        private float refreshTimer;
        private float sinceChange;
        private float gpuMs = -1f;
        private float offFps = -1f;     // 전체 꺼짐(원본) 기록
        private float offGpu = -1f;
        private float onFps = -1f;      // 전체 켜짐 기록
        private float onGpu = -1f;

        public int Selected => selected;

        private void OnEnable()
        {
            allAction = QuestButtons.Create(QuestButton.A, "Optimization All", useTestKeys ? "<Keyboard>/f5" : null);
            nextAction = QuestButtons.Create(QuestButton.B, "Optimization Next", useTestKeys ? "<Keyboard>/f6" : null);
            toggleAction = QuestButtons.Create(QuestButton.X, "Optimization Toggle", useTestKeys ? "<Keyboard>/f7" : null);
            allAction.Enable();
            nextAction.Enable();
            toggleAction.Enable();
            if (switcher != null) switcher.Changed += OnChanged;
        }

        private void OnDisable()
        {
            if (switcher != null) switcher.Changed -= OnChanged;
            Release(allAction);
            Release(nextAction);
            Release(toggleAction);
        }

        private static void Release(InputAction action)
        {
            if (action == null) return;
            action.Disable();
            action.Dispose();
        }

        private void Start()
        {
            Refresh();
        }

        private void Update()
        {
            if (switcher == null) return;
            if (allAction.WasPressedThisFrame()) switcher.SetAll(switcher.OnCount < switcher.AvailableCount);
            if (nextAction.WasPressedThisFrame()) Select((selected + 1) % OptimizationSwitcher.ItemCount);
            if (toggleAction.WasPressedThisFrame()) switcher.SetOn((OptimizationItem)selected, !switcher.IsOn((OptimizationItem)selected));

            float dt = Time.unscaledDeltaTime;
            sinceChange += dt;
            SampleGpu();
            Record();

            refreshTimer += dt;
            if (refreshTimer < refreshSeconds) return;
            refreshTimer = 0f;
            Refresh();
        }

        public void Select(int index)
        {
            selected = Mathf.Clamp(index, 0, OptimizationSwitcher.ItemCount - 1);
            Refresh();
        }

        private void OnChanged()
        {
            sinceChange = 0f;
            Refresh();
        }

        // 모두 꺼짐 · 모두 켜짐 상태가 settleSeconds 넘게 이어지면 그 상태의 FPS · GPU 시간을 기록합니다
        // '모두 켜짐' = 쓸 수 있는 항목이 모두 켜짐 (기기에서만 · 프로젝트 설정에서 꺼진 항목은 셈에서 뺌)
        private void Record()
        {
            if (perfHud == null || sinceChange < settleSeconds || perfHud.SmoothedFps <= 0f) return;
            int count = switcher.OnCount;
            if (count == 0) { offFps = perfHud.SmoothedFps; offGpu = gpuMs; }
            else if (count == switcher.AvailableCount) { onFps = perfHud.SmoothedFps; onGpu = gpuMs; }
        }

        // XR 플러그인이 알려 주는 GPU 시간(초). 에디터 · 지원하지 않는 런타임은 false — 그러면 '없음'
        private void SampleGpu()
        {
            SubsystemManager.GetSubsystems(displays);
            foreach (XRDisplaySubsystem display in displays)
            {
                if (!display.running || !display.TryGetAppGPUTimeLastFrame(out float seconds) || seconds <= 0f) continue;
                float ms = seconds * 1000f;
                gpuMs = gpuMs < 0f ? ms : Mathf.Lerp(gpuMs, ms, 0.1f);
                return;
            }
        }

        private void Refresh()
        {
            if (boardText == null || switcher == null) return;
            float fps = perfHud != null ? perfHud.SmoothedFps : 0f;
            string frame = fps > 0f ? $"FPS {fps:0.0} · {1000f / fps:0.0} ms" : "FPS 재는 중";
            text.Clear();
            text.Append("<b>01. 최적화 비교</b> — 같은 자리에서 켜고 끄며 FPS 를 보세요\n");
            text.Append($"지금 : {frame} · {Gpu(gpuMs)}\n");
            text.Append($"기록 : 전체 꺼짐 {Measured(offFps, offGpu)} → 전체 켜짐 {Measured(onFps, onGpu)}\n");
            for (int i = 0; i < OptimizationSwitcher.ItemCount; i++)
            {
                var item = (OptimizationItem)i;
                bool isOn = switcher.IsOn(item);
                string state = isOn ? "<color=#7CFC7C>켜짐</color>" : "<color=#FF9A9A>꺼짐</color>";
                string value = switcher.IsAvailable(item) ? switcher.ValueText(item, isOn) : switcher.UnavailableText(item);
                text.Append(i == selected ? "<color=#FFD54A>→ " : "    ");
                text.Append($"{i + 1}. {Names[i]} : ").Append(state).Append($" · {value}");
                text.Append(i == selected ? "</color>\n" : "\n");
            }
            var pick = (OptimizationItem)selected;
            // 값과 아끼는 것을 두 줄로 — 한 줄에 넣으면 안내판 폭을 넘어 낱말 중간에서 꺾임
            string pickValue = switcher.IsAvailable(pick)
                ? $"{switcher.ValueText(pick, false)} → {switcher.ValueText(pick, true)}"
                : switcher.UnavailableText(pick);   // 쓸 수 없는 항목에 '단계 0.00 → 0.66' 같은 가짜 값을 적지 않음
            text.Append($"<size=85%>고른 항목 : {pickValue}\n");
            text.Append($"아끼는 것 : {Saves[selected]}\n");
            text.Append($"고정 : Static 배칭 {staticObjects}개 (실행 중에 바꿀 수 없음 · 00 투어에는 없음)</size>\n");
            text.Append($"{QuestButtons.Label(QuestButton.A)} : 전체 켜기/끄기 · {QuestButtons.Label(QuestButton.B)} : 다음 항목\n");
            text.Append($"{QuestButtons.Label(QuestButton.X)} : 고른 항목 켜기/끄기 · {QuestButtons.Label(QuestButton.Y)} : 성능 표시");
            if (worldScale != null) text.Append($"\n{QuestButtons.Label(worldScale.ToggleButton)} : 미니어처 · {QuestButtons.Menu} : 런처로");
            boardText.text = text.ToString();
        }

        private static string Gpu(float ms) => ms >= 0f ? $"GPU {ms:0.0} ms" : "GPU 시간 없음";

        private static string Measured(float fps, float gpu)
        {
            if (fps < 0f) return "—";
            return gpu >= 0f ? $"{fps:0.0} fps · GPU {gpu:0.0} ms" : $"{fps:0.0} fps";
        }
    }
}
