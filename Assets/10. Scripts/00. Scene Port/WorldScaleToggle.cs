// ─────────────────────────────────────────────
// WorldScaleToggle — XR Origin 의 크기를 1 ↔ 15 로 바꿔 마을을 디오라마처럼 내려다본다 (미니어처 모드)
// 학습 포인트
//  - XR Origin 을 15배로 키우면 눈높이와 두 눈 사이 거리가 함께 15배가 됩니다. 뇌는 '내가 커졌다'보다 '세상이 작아졌다'고 느낍니다
//  - 크기는 발밑(Origin 위치)을 중심으로 바뀝니다. 머리 아래 지점을 고정해야 옆으로 밀려 보이지 않습니다
//  - 갑자기 바뀌면 어지러우므로 화면이 완전히 어두운 순간에 바꿉니다
// 조작
//  - 오른손 스틱 누르기 (시뮬레이터 F3) : 미니어처 켜기 / 끄기 — A · B · X 는 최적화 비교 안내판이 씀
// ─────────────────────────────────────────────
using StudyXR.Common;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Gravity;

namespace StudyXR.ScenePort
{
    public class WorldScaleToggle : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private XROrigin origin;
        [SerializeField] private ScreenFade screenFade;
        [SerializeField] private GravityProvider gravity;      // 미니어처 동안 끔 — 커진 몸 캡슐이 주변에 밀리지 않게
        [SerializeField] private PerfHud perfHud;              // 미니어처 동안 숨김 — 넓어진 두 눈에 가까운 글자가 겹쳐 보임

        [Header("크기")]
        [SerializeField] private float miniScale = 15f;
        [SerializeField] private float fadeSeconds = 0.3f;

        [Header("Quest 버튼")]
        // 이 씬에는 잡을 물체가 없어 스틱 누르기(XRI 의 잡은 물체 크기 조절)와 겹치지 않습니다
        [SerializeField] private QuestButton toggleButton = QuestButton.RightStickClick;

        [Header("시뮬레이터 테스트 입력")]
        [SerializeField] private bool useTestKeys = true;      // F3 (안내판에는 적지 않음)

        private InputAction toggleAction;
        private bool hudWasVisible;

        public bool IsMini { get; private set; }
        public float MiniScale => miniScale;
        public QuestButton ToggleButton => toggleButton;

        private void OnEnable()
        {
            toggleAction = QuestButtons.Create(toggleButton, "World Scale Toggle", useTestKeys ? "<Keyboard>/f3" : null);
            toggleAction.Enable();
        }

        private void OnDisable()
        {
            if (toggleAction != null)
            {
                toggleAction.Disable();
                toggleAction.Dispose();
                toggleAction = null;
            }
            if (IsMini) ApplyScale(false);     // 씬을 나갈 때는 원래 크기로
        }

        private void Update()
        {
            if (toggleAction != null && toggleAction.WasPressedThisFrame()) Toggle();
        }

        public void Toggle()
        {
            if (screenFade != null && screenFade.IsFading) return;
            bool mini = !IsMini;
            if (screenFade == null)
            {
                ApplyScale(mini);
                return;
            }
            screenFade.FadeOutIn(() => ApplyScale(mini), fadeSeconds);
        }

        private void ApplyScale(bool mini)
        {
            Transform rig = origin.transform;
            Transform head = origin.Camera.transform;
            Vector3 headBefore = head.position;

            rig.localScale = Vector3.one * (mini ? miniScale : 1f);

            // 머리가 발 위에서 벗어나 있으면 크기를 바꿀 때 옆으로 밀립니다. 머리 아래 지점이 그대로이도록 되돌립니다
            Vector3 headAfter = head.position;
            rig.position += new Vector3(headBefore.x - headAfter.x, 0f, headBefore.z - headAfter.z);
            // CharacterController 가 옛 위치로 되돌아가지 않게 물리 쪽 위치를 바로 맞춥니다
            Physics.SyncTransforms();

            if (gravity != null) gravity.useGravity = !mini;
            if (perfHud != null)
            {
                if (mini)
                {
                    hudWasVisible = perfHud.Visible;
                    perfHud.SetVisible(false);
                }
                else
                {
                    perfHud.SetVisible(hudWasVisible);
                }
            }

            IsMini = mini;
            Debug.Log($"[ScenePort] 미니어처 {(mini ? "켜짐" : "꺼짐")} · XR Origin 크기 {rig.localScale.x:F0} · 눈높이 {head.position.y - rig.position.y:F1} m");
        }
    }
}
