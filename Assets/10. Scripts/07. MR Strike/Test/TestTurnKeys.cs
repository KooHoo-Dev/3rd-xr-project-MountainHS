using StudyXR.Common;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StudyXR.Strike
{
    /// <summary>
    /// 테스트 전용. #5 턴 진행을 시뮬레이터에서 확인하는 키. 플레이어 입력(#6)·AI(#7)가 생기기 전까지 지금 차례 사람 대신 던지고 멈춘다.
    /// 시작하면 한 판을 시작한다. F8 = 지금 차례 사람이 주사위 던지기, F9 = 멈춤, F10 = 새 판, F11 = 자동 진행 토글.
    /// </summary>
    public class TestTurnKeys : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private TurnManager turnManager;
        [Tooltip("던질 목표 점을 얻을 원형 탁자 (TurnManager에 연결한 것과 같은 탁자)")]
        [SerializeField] private DiceTable table;

        [Header("시험 던지기 (가정)")]
        [Tooltip("트레이 위 몇 m에서 던지기 시작할지 (m)")]
        [SerializeField] private float throwStartHeightMeters = 0.1f;
        [Tooltip("출발에서 목표까지 나는 시간 (초). 짧을수록 빠르게 던진다")]
        [SerializeField] private float flightSeconds = 0.5f;
        [Tooltip("목표 점(경기장 바닥 무작위 점)에서 벗어나는 최대 거리 (m). 크면 장외가 자주 나온다")]
        [SerializeField] private float aimScatterMeters = 0.12f;
        [Tooltip("무작위 회전 속도 (rad/s)")]
        [SerializeField] private float spinSpeed = 15f;

        [Header("자동 진행 (가정)")]
        [Tooltip("자동 진행에서 한 번 행동하기 전 기다리는 시간 (초)")]
        [SerializeField] private float autoDelaySeconds = 0.5f;
        [Tooltip("계속할지 고를 때 멈출 확률 (0~1)")]
        [SerializeField] private float autoStopChance = 0.4f;

        private InputAction throwAction;
        private InputAction stopAction;
        private InputAction restartAction;
        private InputAction autoPlayAction;
        private bool throwQueued;
        private bool isAutoPlaying;
        private float autoWaitSeconds;

        private bool IsWaitingForChoice =>
            turnManager.State == TurnState.ThrowingDice || turnManager.State == TurnState.ChoosingContinue;

        private void OnEnable()
        {
            throwAction = QuestButtons.Create(QuestButton.B, "Test Turn Throw", "<Keyboard>/f8");
            stopAction = QuestButtons.Create(QuestButton.RightStickClick, "Test Turn Stop", "<Keyboard>/f9");
            restartAction = new InputAction("Test Turn Restart", InputActionType.Button, "<Keyboard>/f10");
            autoPlayAction = new InputAction("Test Turn Auto Play", InputActionType.Button, "<Keyboard>/f11");
            throwAction.Enable();
            stopAction.Enable();
            restartAction.Enable();
            autoPlayAction.Enable();
        }

        private void OnDisable()
        {
            throwAction.Dispose();
            stopAction.Dispose();
            restartAction.Dispose();
            autoPlayAction.Dispose();
        }

        private void Start()
        {
            turnManager.StartGame();
        }

        private void Update()
        {
            if (throwAction.WasPressedThisFrame() && IsWaitingForChoice) throwQueued = true;
            if (stopAction.WasPressedThisFrame()) turnManager.StopTurn();
            if (restartAction.WasPressedThisFrame()) RestartGame();
            if (autoPlayAction.WasPressedThisFrame()) isAutoPlaying = isAutoPlaying == false;

            RunAutoPlay();
        }

        private void FixedUpdate()
        {
            if (throwQueued == false) return;

            throwQueued = false;
            if (IsWaitingForChoice) ThrowCurrentDice();
        }

        private void RestartGame()
        {
            throwQueued = false;
            autoWaitSeconds = 0f;
            turnManager.StartGame();
        }

        /// <summary>
        /// 자동 진행 중이면 일정 시간마다 던지거나(계속할지 고르는 중에는 확률로 멈춤) 한다.
        /// </summary>
        private void RunAutoPlay()
        {
            if (isAutoPlaying == false) return;
            if (IsWaitingForChoice == false) return;
            if (throwQueued) return;

            autoWaitSeconds += Time.deltaTime;
            if (autoWaitSeconds < autoDelaySeconds) return;

            autoWaitSeconds = 0f;
            bool isChoosing = turnManager.State == TurnState.ChoosingContinue;
            bool shouldStop = isChoosing && Random.value < autoStopChance;
            if (shouldStop)
            {
                turnManager.StopTurn();
                return;
            }
            throwQueued = true;
        }

        /// <summary>
        /// 지금 차례 사람의 트레이에서 주사위 하나를 떼어 경기장 바닥 무작위 점을 향해 던지고 TurnManager에 알린다.
        /// </summary>
        private void ThrowCurrentDice()
        {
            Participant thrower = turnManager.CurrentParticipant;
            Dice dice = thrower.Tray.TakeOne();
            Rigidbody body = dice.GetComponent<Rigidbody>();

            Vector3 startPosition = thrower.Tray.transform.position + Vector3.up * throwStartHeightMeters;
            Vector2 scatter = Random.insideUnitCircle * aimScatterMeters;
            Vector3 aimPoint = table.Arena.RandomFloorPoint() + new Vector3(scatter.x, 0f, scatter.y);
            Vector3 velocity = (aimPoint - startPosition) / flightSeconds - Physics.gravity * (flightSeconds * 0.5f);

            body.position = startPosition;
            body.rotation = Random.rotation;
            body.linearVelocity = velocity;
            body.angularVelocity = Random.onUnitSphere * spinSpeed;
            turnManager.OnDieThrown(dice);
        }

        private void OnGUI()
        {
            GUI.skin.label.fontSize = 22;
            GUI.Label(new Rect(10, 200, 1300, 30), $"F8 던지기 · F9 멈춤 · F10 새 판 · F11 자동 진행({isAutoPlaying})");
            GUI.Label(new Rect(10, 234, 1300, 30), $"상태 {turnManager.State} · 판 위 주사위 {turnManager.BoardDiceCount}개");

            TurnManager.JudgeResult judge = turnManager.LastJudgeResult;
            GUI.Label(new Rect(10, 268, 1300, 30), $"마지막 판정: 장외 {judge.RemovedOutside} · X {judge.RemovedX} · 땀 {judge.Taken}");

            for (int i = 0; i < turnManager.Participants.Count; i++)
            {
                Participant participant = turnManager.Participants[i];
                bool isTurn = turnManager.State != TurnState.GameEnd && participant == turnManager.CurrentParticipant;
                string state = participant.IsEliminated ? "탈락" : $"{participant.DiceCount}개 (트레이 {participant.Tray.DiceCount}개)";
                GUI.Label(new Rect(10, 302 + i * 30, 1300, 30), $"{participant.Name}: {state} {(isTurn ? "◀ 차례" : "")}");
            }

            if (turnManager.State == TurnState.GameEnd)
            {
                string result = turnManager.DidPlayerWin ? "승리" : "패배";
                GUI.Label(new Rect(10, 302 + turnManager.Participants.Count * 30, 1300, 30), $"게임 종료: 플레이어 {result} (F10 새 판)");
            }
        }
    }
}
