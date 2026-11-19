using System.Collections.Generic;
using StudyXR.Common;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StudyXR.Strike
{
    /// <summary>
    /// 테스트 전용. #4 원형 탁자의 트레이와 차례 순서를 시뮬레이터에서 확인하는 키.
    /// 시작하면 탁자 자리마다 참가자를 만들어 주사위를 시작 개수만큼 보여 준다.
    /// F8 = 다음 차례, F9 = 지금 차례 +1개, F10 = 지금 차례 -1개(0개면 탈락), F11 = 플레이어 트레이 집기 허용 토글.
    /// </summary>
    public class TestTurnKeys : MonoBehaviour
    {
        [Header("연결")]
        [Tooltip("씬에 놓은 원형 탁자 (Round Table 4P / 5P 프리팹)")]
        [SerializeField] private DiceTable table;

        [Header("시작 값 (가정)")]
        [SerializeField] private int startDiceCount = 5;

        private readonly List<Participant> participants = new List<Participant>();
        private TurnOrder turnOrder;
        private bool isPlayerTraySelectable;

        private InputAction nextTurnAction;
        private InputAction addDiceAction;
        private InputAction removeDiceAction;
        private InputAction selectableAction;

        private void OnEnable()
        {
            nextTurnAction = QuestButtons.Create(QuestButton.B, "Test Turn Next", "<Keyboard>/f8");
            addDiceAction = QuestButtons.Create(QuestButton.RightStickClick, "Test Turn Add Dice", "<Keyboard>/f9");
            removeDiceAction = new InputAction("Test Turn Remove Dice", InputActionType.Button, "<Keyboard>/f10");
            selectableAction = new InputAction("Test Turn Selectable", InputActionType.Button, "<Keyboard>/f11");
            nextTurnAction.Enable();
            addDiceAction.Enable();
            removeDiceAction.Enable();
            selectableAction.Enable();
        }

        private void OnDisable()
        {
            nextTurnAction.Dispose();
            addDiceAction.Dispose();
            removeDiceAction.Dispose();
            selectableAction.Dispose();
        }

        /// <summary>
        /// 탁자 자리 순서대로 참가자를 만들고(0번 = 플레이어) 트레이에 시작 개수를 보여 준다.
        /// </summary>
        private void Start()
        {
            for (int seatIndex = 0; seatIndex < table.SeatTrays.Length; seatIndex++)
            {
                bool isPlayer = seatIndex == 0;
                string participantName = isPlayer ? "나" : $"AI {seatIndex}";
                Participant participant = new Participant(participantName, isPlayer, table.SeatTrays[seatIndex], startDiceCount);
                participants.Add(participant);
                participant.Tray.Show(participant.DiceCount);
            }
            turnOrder = new TurnOrder(participants);
        }

        private void Update()
        {
            if (nextTurnAction.WasPressedThisFrame()) turnOrder.MoveToNext();
            if (addDiceAction.WasPressedThisFrame()) ChangeCurrentDiceCount(+1);
            if (removeDiceAction.WasPressedThisFrame()) ChangeCurrentDiceCount(-1);
            if (selectableAction.WasPressedThisFrame()) TogglePlayerTraySelectable();
        }

        /// <summary>
        /// 지금 차례 참가자의 개수를 바꾸고 트레이를 맞춘 뒤, 0개면 탈락시킨다. 이미 탈락했거나 0 아래로 내려가는 요청은 무시한다.
        /// </summary>
        /// <param name="delta">늘릴 개수 (+1) 또는 줄일 개수 (-1)</param>
        private void ChangeCurrentDiceCount(int delta)
        {
            Participant current = turnOrder.Current;
            if (current.IsEliminated) return;

            int newCount = current.DiceCount + delta;
            if (newCount < 0) return;

            current.DiceCount = newCount;
            current.Tray.Show(current.DiceCount);
            turnOrder.EliminateIfEmpty(current);
        }

        private void TogglePlayerTraySelectable()
        {
            isPlayerTraySelectable = isPlayerTraySelectable == false;
            participants[0].Tray.SetSelectable(isPlayerTraySelectable);
        }

        private void OnGUI()
        {
            GUI.skin.label.fontSize = 22;
            GUI.Label(new Rect(10, 200, 1100, 30), $"F8 다음 차례 · F9 +1개 · F10 -1개 · F11 내 트레이 집기 허용({isPlayerTraySelectable}) · 생존 {turnOrder.AliveCount}명 · 플레이어 생존 {turnOrder.IsPlayerAlive}");
            for (int i = 0; i < participants.Count; i++)
            {
                Participant participant = participants[i];
                string turnMark = participant == turnOrder.Current ? "◀ 차례" : "";
                string state = participant.IsEliminated ? "탈락" : $"{participant.DiceCount}개 (트레이 {participant.Tray.DiceCount}개)";
                GUI.Label(new Rect(10, 234 + i * 30, 1100, 30), $"{participant.Name}: {state} {turnMark}");
            }
        }
    }
}
