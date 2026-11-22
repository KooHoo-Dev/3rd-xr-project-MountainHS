using StudyXR.Common;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StudyXR.Strike
{
    /// <summary>
    /// 씬을 시작하면 눈앞에 조작법·규칙 요약 패널을 띄운다. 오른손 B를 꾹 누르고 있으면 닫힌다.
    /// 패널은 머리를 부드럽게 따라와 늘 시야 앞에 있다. 한 번 닫으면 그 Play 동안 다시 뜨지 않는다.
    /// </summary>
    /// <remarks>
    /// 게임은 패널 뒤에서 그대로 시작한다 (턴제라 플레이어 차례는 기다린다).
    /// 첫 차례에는 아직 던지지 않아 B 멈춤(<see cref="PlayerTurnInput"/>)이 막혀 있으므로, B로 닫아도 차례가 멈추지 않는다.
    /// 구조(패널·배경·글자)는 메뉴 "5. 조작 UX 적용"이 만든다.
    /// </remarks>
    public class ControlsGuide : MonoBehaviour
    {
        [Header("연결")]
        [Tooltip("패널 루트. 닫으면 꺼진다")]
        [SerializeField] private GameObject panelRoot;
        [Tooltip("조작법 본문 글자")]
        [SerializeField] private TMP_Text bodyText;
        [Tooltip("닫기 안내와 진행 막대 글자")]
        [SerializeField] private TMP_Text skipText;
        [Tooltip("패널이 따라갈 머리 (Main Camera)")]
        [SerializeField] private Transform head;

        [Header("배치 (가정)")]
        [Tooltip("머리 앞 거리 (m)")]
        [SerializeField] private float distanceFromHeadMeters = 0.9f;
        [Tooltip("눈높이보다 낮게 둘 거리 (m)")]
        [SerializeField] private float belowEyeMeters = 0.1f;
        [Tooltip("머리를 따라가는 빠르기. 클수록 바로 붙는다")]
        [SerializeField] private float followSharpness = 3f;

        [Header("닫기 (가정)")]
        [Tooltip("B를 이 시간 이상 누르고 있어야 닫힌다 (초)")]
        [SerializeField] private float skipHoldSeconds = 1.5f;

        // 진행 막대 칸 수
        private const int ProgressCells = 10;

        // B를 계속 누르고 있는 시간 (초). 떼면 0
        private float skipHeldSeconds;

        private InputAction skipAction;

        private void OnEnable()
        {
            skipAction = QuestButtons.Create(QuestButton.B, "Skip Controls Guide", "<Keyboard>/f1");
            skipAction.Enable();
        }

        private void OnDisable()
        {
            skipAction.Dispose();
        }

        /// <summary>
        /// 본문을 채우고 패널을 머리 앞에 바로 놓아 켠다.
        /// </summary>
        private void Start()
        {
            bodyText.text = BuildBody();
            ShowSkipProgress();
            panelRoot.transform.position = TargetPosition();
            FaceHead();
            panelRoot.SetActive(true);
        }

        /// <summary>
        /// 패널이 떠 있는 동안 머리를 따라가고, B 길게 누르기를 센다.
        /// </summary>
        private void Update()
        {
            if (panelRoot.activeSelf == false) return;

            FollowHead();
            UpdateSkipHold();
        }

        /// <summary>
        /// 조작법과 규칙 요약. 버튼 이름은 <see cref="QuestButtons.Label"/>로 다른 씬 안내와 맞춘다.
        /// </summary>
        /// <returns>패널 본문 (TMP 서식 포함)</returns>
        private static string BuildBody()
        {
            return "<b>MR 스트라이크 조작법</b>\n\n" +
                   "왼 스틱 : 이동 · 오른 스틱 : 회전\n" +
                   "그립 : 내 트레이 주사위 잡기\n" +
                   "잡은 채 휘두르고 놓기 : 던지기\n" +
                   QuestButtons.Label(QuestButton.B) + " 꾹 : 그만 던지기 (못 땄을 때)\n" +
                   QuestButtons.Label(QuestButton.X) + " : 보는 책상 위에 탁자 놓기\n" +
                   QuestButtons.Menu + " : 다시 시작 메뉴\n\n" +
                   "<b>규칙</b>\n" +
                   "같은 숫자가 2개 이상 → 모두 가져감\n" +
                   "X 면·경기장 밖 → 사라짐\n" +
                   "판이 비면 다음 사람이 전부 던짐\n" +
                   "주사위를 다 잃으면 탈락";
        }

        /// <summary>
        /// 누르는 동안 시간을 쌓고, 다 차면 패널을 닫는다. 떼면 처음부터 다시 센다.
        /// </summary>
        private void UpdateSkipHold()
        {
            if (skipAction.IsPressed() == false)
            {
                skipHeldSeconds = 0f;
                ShowSkipProgress();
                return;
            }

            skipHeldSeconds += Time.deltaTime;
            ShowSkipProgress();
            if (skipHeldSeconds >= skipHoldSeconds) panelRoot.SetActive(false);
        }

        /// <summary>
        /// 닫기 안내 아래에 누른 정도를 칸 막대로 보여 준다.
        /// </summary>
        private void ShowSkipProgress()
        {
            float progress = Mathf.Clamp01(skipHeldSeconds / skipHoldSeconds);
            int filledCells = Mathf.RoundToInt(progress * ProgressCells);
            string bar = "[" + new string('#', filledCells) + new string('-', ProgressCells - filledCells) + "]";
            skipText.text = QuestButtons.Label(QuestButton.B) + " 꾹 눌러 닫기\n" + bar;
        }

        /// <summary>
        /// 목표 위치로 조금씩 다가가고 머리를 바라본다. 고개를 돌리면 천천히 시야 앞으로 돌아온다.
        /// </summary>
        private void FollowHead()
        {
            float followRatio = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
            panelRoot.transform.position = Vector3.Lerp(panelRoot.transform.position, TargetPosition(), followRatio);
            FaceHead();
        }

        /// <summary>
        /// 머리가 보는 수평 방향으로 일정 거리 앞, 눈높이보다 조금 아래.
        /// </summary>
        /// <returns>패널이 있어야 할 월드 위치</returns>
        private Vector3 TargetPosition()
        {
            Vector3 flatForward = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
            return head.position + flatForward * distanceFromHeadMeters + Vector3.down * belowEyeMeters;
        }

        /// <summary>
        /// 글자가 머리 쪽에서 읽히도록 패널 앞(-z)을 머리로 돌린다 (<see cref="GameMenu"/>와 같은 방식).
        /// </summary>
        private void FaceHead()
        {
            Vector3 awayFromHead = panelRoot.transform.position - head.position;
            awayFromHead.y = 0f;
            panelRoot.transform.rotation = Quaternion.LookRotation(awayFromHead, Vector3.up);
        }
    }
}
