using System.Collections.Generic;
using StudyXR.Common;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace StudyXR.Strike
{
    /// <summary>
    /// 플레이어 차례의 입력. 내 트레이 주사위를 빛나게 해 잡을 수 있게 하고, 놓으면 던진 것으로 알리고, B 버튼으로 멈춘다.
    /// 손 움직임으로 날리는 계산은 주사위의 <see cref="DiceThrow"/>가 하므로 여기서는 "집었다 / 놓았다"만 본다.
    /// </summary>
    /// <remarks>
    /// <see cref="TurnManager"/>가 플레이어 차례가 되면 <see cref="BeginChoosing"/>을 부른다.
    /// 잡았다가 놓으면 어디에 놓였든 던진 것으로 친다 (트레이 위에 떨어뜨리면 장외로 지워진다).
    /// 새 판 시작 = 오른손 A (게임이 끝난 뒤에만).
    /// </remarks>
    public class PlayerTurnInput : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private TurnManager turnManager;

        // ── 지금 고르는 중인 상태 ──
        private bool isChoosing;
        private bool canStop;
        private DiceTray choosingTray;

        // 잡기·놓기 이벤트를 걸어 둔 주사위들 (끝날 때 풀기 위해 기억)
        private readonly List<Dice> watchedDice = new List<Dice>();

        private InputAction stopAction;
        private InputAction restartAction;

        private void OnEnable()
        {
            stopAction = QuestButtons.Create(QuestButton.B, "Player Stop", "<Keyboard>/f9");
            restartAction = QuestButtons.Create(QuestButton.A, "Player Restart", "<Keyboard>/f12");
            stopAction.Enable();
            restartAction.Enable();
        }

        private void OnDisable()
        {
            UnwatchAllDice();
            stopAction.Dispose();
            restartAction.Dispose();
        }

        /// <summary>
        /// 고르는 중이면 멈춤 버튼을, 게임이 끝났으면 새 판 버튼을 본다.
        /// </summary>
        private void Update()
        {
            bool isStopPressed = stopAction.WasPressedThisFrame();
            bool canStopNow = isChoosing && canStop;
            if (isStopPressed && canStopNow)
            {
                StopChoosing();
                turnManager.StopTurn();
            }

            bool isRestartPressed = restartAction.WasPressedThisFrame();
            bool isGameEnded = turnManager.State == TurnState.GameEnd;
            if (isRestartPressed && isGameEnded) turnManager.StartGame();
        }

        /// <summary>
        /// 플레이어 차례를 연다. 내 트레이 주사위를 강조해 잡을 수 있게 하고 잡기·놓기를 지켜본다.
        /// </summary>
        /// <param name="player">플레이어 참가자</param>
        /// <param name="canStopThisTime">true = B 버튼으로 멈출 수 있음 (한 번 던져 못 딴 뒤)</param>
        public void BeginChoosing(Participant player, bool canStopThisTime)
        {
            UnwatchAllDice();
            isChoosing = true;
            canStop = canStopThisTime;
            choosingTray = player.Tray;

            choosingTray.SetSelectable(true);
            foreach (Dice dice in choosingTray.DiceInTray)
            {
                WatchDice(dice);
            }
        }

        /// <summary>
        /// 하나를 잡으면 그 주사위를 트레이에서 떼어 내고, 남은 주사위는 더 못 잡게 한다.
        /// </summary>
        /// <param name="args">XRI 잡기 이벤트. 어떤 주사위를 잡았는지 들어 있다</param>
        private void OnGrabbed(SelectEnterEventArgs args)
        {
            Dice grabbedDice = args.interactableObject.transform.GetComponent<Dice>();
            choosingTray.Take(grabbedDice);
            choosingTray.SetSelectable(false);
        }

        /// <summary>
        /// 놓은 주사위를 던진 것으로 알린다. 강조는 끈다.
        /// </summary>
        /// <param name="args">XRI 놓기 이벤트. 어떤 주사위인지 들어 있다</param>
        private void OnReleased(SelectExitEventArgs args)
        {
            Dice releasedDice = args.interactableObject.transform.GetComponent<Dice>();
            StopChoosing();
            releasedDice.SetSelectable(false);
            turnManager.OnDieThrown(releasedDice);
        }

        private void StopChoosing()
        {
            isChoosing = false;
            UnwatchAllDice();
            choosingTray.SetSelectable(false);
        }

        private void WatchDice(Dice dice)
        {
            XRGrabInteractable grabInteractable = dice.GetComponent<XRGrabInteractable>();
            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);
            watchedDice.Add(dice);
        }

        private void UnwatchAllDice()
        {
            foreach (Dice dice in watchedDice)
            {
                if (dice == null) continue;

                XRGrabInteractable grabInteractable = dice.GetComponent<XRGrabInteractable>();
                grabInteractable.selectEntered.RemoveListener(OnGrabbed);
                grabInteractable.selectExited.RemoveListener(OnReleased);
            }
            watchedDice.Clear();
        }
    }
}
