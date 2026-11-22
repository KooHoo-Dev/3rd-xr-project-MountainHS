using System.Collections.Generic;
using UnityEngine;

namespace StudyXR.Strike
{
    /// <summary>
    /// 한 판의 흐름 단계. 차례마다 ThrowingDice → Rolling → (ChoosingContinue → Rolling)... → 차례 끝으로 돌고, 승패가 나면 GameEnd.
    /// </summary>
    public enum TurnState
    {
        ThrowingDice,
        Rolling,
        ChoosingContinue,
        GameEnd,
    }

    /// <summary>
    /// 던진다 → 굴러서 멈춘다 → 판정한다 → (더 던진다 / 멈춘다) → 넘긴다 → 탈락·승패를 반복하는 한 판의 진행자.
    /// 플레이어 입력(#6)과 AI(#7)가 <see cref="OnDieThrown"/>·<see cref="StopTurn"/>으로 선택을 알려 오고, 이 클래스가 상태를 바꾼다.
    /// </summary>
    /// <remarks>
    /// 판 위 주사위는 따이지 않으면 그대로 남아 다음 차례 사람의 판정에도 쓰인다 (잃기 = 판에 남김).
    /// 손에 든 개수의 진실은 <see cref="Participant.DiceCount"/>이고, 트레이는 개수가 바뀔 때 <see cref="DiceTray.Show"/>로 맞춘다.
    /// 던질 주사위는 호출자가 먼저 트레이에서 떼어 낸다 (<see cref="DiceTray.TakeOne"/> 또는 플레이어가 직접 잡음).
    /// </remarks>
    public class TurnManager : MonoBehaviour
    {
        /// <summary>
        /// 한 번의 판정에서 일어난 일. 장외로 지운 수, X로 지운 수, 따서 가져간 수.
        /// </summary>
        public readonly struct JudgeResult
        {
            public readonly int RemovedOutside;
            public readonly int RemovedX;
            public readonly int Taken;

            public JudgeResult(int removedOutside, int removedX, int taken)
            {
                RemovedOutside = removedOutside;
                RemovedX = removedX;
                Taken = taken;
            }
        }

        [Header("연결")]
        [Tooltip("씬에 놓은 원형 탁자 (경기장과 자리별 트레이를 가진다)")]
        [SerializeField] private DiceTable table;

        [Tooltip("굴림 시간초과(rollTimeoutSeconds)를 읽을 튜닝 에셋")]
        [SerializeField] private DicePhysicsSettings settings;

        [Tooltip("플레이어 차례에 주사위 집기와 멈춤을 받는 입력")]
        [SerializeField] private PlayerTurnInput playerInput;

        [Tooltip("AI 참가자들. 탁자 1번 자리부터 순서대로 (자리 수 - 1개 이상)")]
        [SerializeField] private AiPlayer[] aiPlayers;

        [Header("규칙 (가정)")]
        [Tooltip("시작할 때 모두가 가지는 주사위 개수")]
        [SerializeField] private int startDiceCount = 5;

        // ── 한 판의 상태 ──
        private readonly List<Participant> participants = new List<Participant>();
        private readonly List<Dice> boardDice = new List<Dice>();
        private TurnOrder turnOrder;
        private float rollElapsedSeconds;

        public TurnState State { get; private set; } = TurnState.GameEnd;
        public JudgeResult LastJudgeResult { get; private set; }
        public bool DidPlayerWin { get; private set; }
        public IReadOnlyList<Participant> Participants => participants;
        public Participant CurrentParticipant => turnOrder.Current;
        public int BoardDiceCount => boardDice.Count;
        public IReadOnlyList<Dice> BoardDice => boardDice;

        /// <summary>
        /// 새 판을 시작한다. 판을 비우고 모두에게 시작 개수를 주고 첫 사람(플레이어)의 차례로 간다.
        /// 시작·재시작 버튼이 부른다. 진행 중이던 AI 차례와 플레이어가 손에 든 주사위도 함께 정리한다.
        /// </summary>
        public void StartGame()
        {
            CancelAiTurns();
            playerInput.ResetForNewGame();
            ClearBoard();
            CreateParticipants();
            turnOrder = new TurnOrder(participants);
            LastJudgeResult = default;
            DidPlayerWin = false;
            BeginChoice(TurnState.ThrowingDice);
        }

        /// <summary>
        /// 막 손을 떠난 주사위를 판에 올리고 굴림을 기다린다. 던진 사람의 개수가 1 줄어든다.
        /// 플레이어 입력(#6)과 AI(#7)가 던진 직후에 부른다.
        /// </summary>
        /// <param name="dice">방금 던진 주사위 (이미 트레이에서 떼어 낸 것)</param>
        public void OnDieThrown(Dice dice)
        {
            // 어느 경로로 던졌든 플레이어의 고르기는 닫는다 (안 닫으면 굴리는 동안·AI 차례에도 트레이를 잡을 수 있다)
            playerInput.StopChoosing();
            turnOrder.Current.DiceCount--;
            boardDice.Add(dice);
            rollElapsedSeconds = 0f;
            State = TurnState.Rolling;
        }

        /// <summary>
        /// 더 던지지 않고 차례를 마친다. 계속할지 고르는 중(ChoosingContinue)에만 받는다.
        /// </summary>
        public void StopTurn()
        {
            if (State != TurnState.ChoosingContinue) return;

            EndTurn();
        }

        /// <summary>
        /// 굴리는 중이면 판 위 주사위가 모두 멈췄거나 시간이 다 됐을 때 판정으로 넘어간다.
        /// </summary>
        private void Update()
        {
            if (State != TurnState.Rolling) return;

            rollElapsedSeconds += Time.deltaTime;
            bool areAllDiceResting = AreAllBoardDiceResting();
            bool isRollTimedOut = rollElapsedSeconds >= settings.rollTimeoutSeconds;
            if (areAllDiceResting == false && isRollTimedOut == false) return;

            FinishRolling();
        }

        /// <summary>
        /// 판정하고 결과에 따라 차례를 끝내거나 계속할지 고르는 상태로 간다.
        /// 땄으면 끝, 못 땄어도 남은 개수가 0이면 끝, 남았으면 고른다.
        /// </summary>
        private void FinishRolling()
        {
            LastJudgeResult = JudgeBoard();
            NotifyAiReactions(turnOrder.Current, LastJudgeResult);

            bool hasTaken = LastJudgeResult.Taken > 0;
            if (hasTaken)
            {
                EndTurn();
                return;
            }

            bool hasNoDiceLeft = turnOrder.Current.DiceCount == 0;
            if (hasNoDiceLeft)
            {
                EndTurn();
                return;
            }

            BeginChoice(TurnState.ChoosingContinue);
        }

        /// <summary>
        /// 판 위 주사위를 ① 장외 ② X ③ 짝 있는 숫자 순서로 정리한다. 짝은 지금 차례 사람이 가져간다.
        /// </summary>
        /// <returns>장외로 지운 수, X로 지운 수, 따서 가져간 수</returns>
        private JudgeResult JudgeBoard()
        {
            int removedOutside = RemoveOutsideDice();
            int removedX = RemoveXDice();
            int taken = TakePairedDice();
            return new JudgeResult(removedOutside, removedX, taken);
        }

        /// <summary>
        /// 경기장 밖에 멈춘 주사위를 판에서 지운다.
        /// </summary>
        /// <returns>지운 수</returns>
        private int RemoveOutsideDice()
        {
            int removedCount = 0;
            for (int index = boardDice.Count - 1; index >= 0; index--)
            {
                bool isOutside = table.Arena.Contains(boardDice[index].transform.position) == false;
                if (isOutside == false) continue;

                RemoveBoardDiceAt(index);
                removedCount++;
            }
            return removedCount;
        }

        /// <summary>
        /// 윗면이 X인 주사위를 판에서 지운다 (개수는 이미 던질 때 줄었으므로 그대로 사라진다).
        /// </summary>
        /// <returns>지운 수</returns>
        private int RemoveXDice()
        {
            int removedCount = 0;
            for (int index = boardDice.Count - 1; index >= 0; index--)
            {
                bool isX = boardDice[index].ReadTopFace() == DiceFace.X;
                if (isX == false) continue;

                RemoveBoardDiceAt(index);
                removedCount++;
            }
            return removedCount;
        }

        /// <summary>
        /// 같은 숫자가 2개 이상인 주사위를 판에서 지우고 그 수만큼 지금 차례 사람의 개수를 늘려 트레이에 보여 준다.
        /// </summary>
        /// <returns>가져간 수</returns>
        private int TakePairedDice()
        {
            List<DiceFace> boardFaces = new List<DiceFace>();
            foreach (Dice dice in boardDice)
            {
                boardFaces.Add(dice.ReadTopFace());
            }
            List<DiceFace> pairedFaces = DiceRules.FindPairedFaces(boardFaces);

            int takenCount = 0;
            for (int index = boardDice.Count - 1; index >= 0; index--)
            {
                bool isPaired = pairedFaces.Contains(boardFaces[index]);
                if (isPaired == false) continue;

                RemoveBoardDiceAt(index);
                takenCount++;
            }

            Participant current = turnOrder.Current;
            current.DiceCount += takenCount;
            current.Tray.Show(current.DiceCount);
            return takenCount;
        }

        /// <summary>
        /// 차례를 끝낸다. 개수가 0이면 탈락시키고, 플레이어 탈락이면 패배, 플레이어만 남았으면 승리, 아니면 다음 사람에게 넘긴다.
        /// </summary>
        private void EndTurn()
        {
            turnOrder.EliminateIfEmpty(turnOrder.Current);

            bool isPlayerDefeated = turnOrder.IsPlayerAlive == false;
            if (isPlayerDefeated)
            {
                EndGame(false);
                return;
            }

            bool isPlayerLastAlive = turnOrder.AliveCount == 1;
            if (isPlayerLastAlive)
            {
                EndGame(true);
                return;
            }

            turnOrder.MoveToNext();
            BeginChoice(TurnState.ThrowingDice);
        }

        /// <summary>
        /// 지금 차례 사람에게 던질 기회를 준다. 던지기 전(ThrowingDice)은 멈출 수 없고, 한 번 던져 못 딴 뒤(ChoosingContinue)에는 멈출 수 있다.
        /// 플레이어면 입력 클래스가 집기를 열고, 선택은 OnDieThrown / StopTurn으로 돌아온다.
        /// </summary>
        /// <param name="choiceState">ThrowingDice 또는 ChoosingContinue</param>
        private void BeginChoice(TurnState choiceState)
        {
            State = choiceState;
            bool canStop = choiceState == TurnState.ChoosingContinue;

            Participant current = turnOrder.Current;
            if (current.IsPlayer)
            {
                playerInput.BeginChoosing(current, canStop);
                return;
            }
            current.Ai.BeginChoosing(current, canStop);
        }

        /// <summary>
        /// 판정 결과를 모든 AI에게 알려 표정·대사로 반응하게 한다.
        /// </summary>
        /// <param name="thrower">방금 던진 사람</param>
        /// <param name="result">판정 결과</param>
        private void NotifyAiReactions(Participant thrower, JudgeResult result)
        {
            foreach (AiPlayer ai in aiPlayers)
            {
                ai.ReactToJudge(thrower, result);
            }
        }

        /// <summary>
        /// 새 판을 시작할 때 이전 판에서 진행 중이던 AI 차례(코루틴)를 끊는다. 안 끊으면 새 판에서 옛 AI가 던진다.
        /// </summary>
        private void CancelAiTurns()
        {
            foreach (AiPlayer ai in aiPlayers)
            {
                ai.CancelTurn();
            }
        }

        private void EndGame(bool playerWon)
        {
            DidPlayerWin = playerWon;
            State = TurnState.GameEnd;
        }

        private bool AreAllBoardDiceResting()
        {
            foreach (Dice dice in boardDice)
            {
                if (dice.IsResting == false) return false;
            }
            return true;
        }

        private void RemoveBoardDiceAt(int index)
        {
            Destroy(boardDice[index].gameObject);
            boardDice.RemoveAt(index);
        }

        /// <summary>
        /// 이전 판에 남은 판 위 주사위를 지운다.
        /// </summary>
        private void ClearBoard()
        {
            foreach (Dice dice in boardDice)
            {
                Destroy(dice.gameObject);
            }
            boardDice.Clear();
        }

        /// <summary>
        /// 탁자 자리 순서대로 참가자를 만든다 (0번 = 플레이어). 트레이에는 시작 개수를 보여 주고 집기는 막아 둔다.
        /// 트레이는 한 번 비운 뒤 다시 채워, 이전 판에 밀리거나 떨어진 주사위 없이 모두 제자리에서 시작한다.
        /// </summary>
        private void CreateParticipants()
        {
            participants.Clear();
            for (int seatIndex = 0; seatIndex < table.SeatTrays.Length; seatIndex++)
            {
                bool isPlayer = seatIndex == 0;
                string participantName = isPlayer ? "나" : $"AI {seatIndex}";
                AiPlayer ai = isPlayer ? null : aiPlayers[seatIndex - 1];
                DiceTray tray = table.SeatTrays[seatIndex];

                tray.SetSelectable(false);
                tray.Show(0);
                tray.Show(startDiceCount);
                participants.Add(new Participant(participantName, ai, tray, startDiceCount));
            }
        }
    }
}
