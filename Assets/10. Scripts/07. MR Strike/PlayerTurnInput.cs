using System.Collections.Generic;
using StudyXR.Common;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace StudyXR.Strike
{
    /// <summary>
    /// 플레이어 차례의 입력. 내 트레이 주사위를 빛나게 해 잡을 수 있게 하고, 던지면 알리고, B 버튼을 길게 눌러 멈춘다.
    /// 손 움직임으로 날리는 계산은 주사위의 <see cref="DiceThrow"/>가 하므로 여기서는 "집었다 / 놓았다"와 놓을 때 속도만 본다.
    /// </summary>
    /// <remarks>
    /// <see cref="TurnManager"/>가 플레이어 차례가 되면 <see cref="BeginChoosing"/>을 부른다.
    /// 놓을 때 주사위 속도가 떨어뜨림 기준보다 느리면 던진 것이 아니라 실수로 떨어뜨린 것으로 보고 트레이로 되돌린다.
    /// 새 판 시작은 메뉴(<see cref="GameMenu"/>)가 맡는다.
    /// </remarks>
    public class PlayerTurnInput : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private TurnManager turnManager;

        [Tooltip("왼손 진동 (Left Controller의 HapticImpulsePlayer)")]
        [SerializeField] private HapticImpulsePlayer leftHandHaptics;
        [Tooltip("오른손 진동 (Right Controller의 HapticImpulsePlayer)")]
        [SerializeField] private HapticImpulsePlayer rightHandHaptics;

        [Header("멈춤 (가정)")]
        [Tooltip("B 버튼을 이 시간 이상 누르고 있어야 멈춘다 (초). 실수로 스친 입력을 거른다")]
        [SerializeField] private float stopHoldSeconds = 0.6f;

        [Header("떨어뜨림 (가정)")]
        [Tooltip("놓을 때 주사위 속도가 이보다 느리면 던지기가 아니라 떨어뜨림으로 보고 트레이로 되돌린다 (m/s)")]
        [SerializeField] private float dropSpeedMetersPerSecond = 0.5f;

        [Header("진동 (가정)")]
        [Tooltip("내 차례 시작 알림 진동 세기 (0~1)")]
        [SerializeField] private float turnStartAmplitude = 0.6f;
        [Tooltip("내 차례 시작 알림 진동 길이 (초)")]
        [SerializeField] private float turnStartSeconds = 0.15f;
        [Tooltip("아직 차례 아님 진동 세기 (0~1)")]
        [SerializeField] private float notYourTurnAmplitude = 0.2f;
        [Tooltip("아직 차례 아님 진동 길이 (초)")]
        [SerializeField] private float notYourTurnSeconds = 0.05f;
        [Tooltip("내 차례가 아닐 때 손이 내 트레이에서 이 거리 안에서 그립을 쥐면 '아직 차례 아님' 진동 (m)")]
        [SerializeField] private float notYourTurnReachMeters = 0.3f;

        // ── 지금 고르는 중인 상태 ──
        private bool isChoosing;
        private bool canStop;
        private Participant choosingPlayer;
        private DiceTray choosingTray;

        // B 버튼을 계속 누르고 있는 시간 (초). 떼면 0
        private float stopHeldSeconds;

        // 잡기·놓기 이벤트를 걸어 둔 주사위들 (끝날 때 풀기 위해 기억)
        private readonly List<Dice> watchedDice = new List<Dice>();

        private InputAction stopAction;
        private InputAction leftGripAction;
        private InputAction rightGripAction;

        private void OnEnable()
        {
            stopAction = QuestButtons.Create(QuestButton.B, "Player Stop", "<Keyboard>/f9");
            leftGripAction = new InputAction("Left Grip", InputActionType.Button, "<XRController>{LeftHand}/{GripButton}");
            rightGripAction = new InputAction("Right Grip", InputActionType.Button, "<XRController>{RightHand}/{GripButton}");
            stopAction.Enable();
            leftGripAction.Enable();
            rightGripAction.Enable();
        }

        private void OnDisable()
        {
            UnwatchAllDice();
            stopAction.Dispose();
            leftGripAction.Dispose();
            rightGripAction.Dispose();
        }

        /// <summary>
        /// 멈춤 길게 누르기와 차례 아님 그립을 본다.
        /// </summary>
        private void Update()
        {
            UpdateStopHold();
            WarnIfGrippingOutOfTurn(leftGripAction, leftHandHaptics);
            WarnIfGrippingOutOfTurn(rightGripAction, rightHandHaptics);
        }

        /// <summary>
        /// 플레이어 차례를 연다. 내 트레이 주사위를 강조해 잡을 수 있게 하고 잡기·놓기를 지켜본다. 시작을 진동으로 알린다.
        /// </summary>
        /// <param name="player">플레이어 참가자</param>
        /// <param name="canStopThisTime">true = B 버튼으로 멈출 수 있음 (한 번 던져 못 딴 뒤)</param>
        public void BeginChoosing(Participant player, bool canStopThisTime)
        {
            UnwatchAllDice();
            isChoosing = true;
            canStop = canStopThisTime;
            choosingPlayer = player;
            choosingTray = player.Tray;

            choosingTray.SetSelectable(true);
            foreach (Dice dice in choosingTray.DiceInTray)
            {
                WatchDice(dice);
            }
            PulseBothHands(turnStartAmplitude, turnStartSeconds);
        }

        /// <summary>
        /// B를 누르고 있는 시간을 재고, 멈출 수 있는 때 기준 시간을 넘기면 차례를 마친다.
        /// </summary>
        private void UpdateStopHold()
        {
            bool isStopHeld = stopAction.IsPressed();
            if (isStopHeld == false)
            {
                stopHeldSeconds = 0f;
                return;
            }

            stopHeldSeconds += Time.deltaTime;
            bool isHeldLongEnough = stopHeldSeconds >= stopHoldSeconds;
            bool canStopNow = isChoosing && canStop;
            if (isHeldLongEnough && canStopNow)
            {
                stopHeldSeconds = 0f;
                StopChoosing();
                turnManager.StopTurn();
            }
        }

        /// <summary>
        /// 내 차례가 아닌데 내 트레이 근처에서 그립을 쥐면 약하게 진동해 "아직 차례 아님"을 알린다.
        /// 내 차례가 아니면 트레이 주사위는 잡기가 꺼져 있어 XRI 이벤트가 오지 않으므로 그립 입력을 직접 본다.
        /// </summary>
        /// <param name="gripAction">그 손의 그립 입력</param>
        /// <param name="handHaptics">그 손의 진동 (손 위치로도 쓴다)</param>
        private void WarnIfGrippingOutOfTurn(InputAction gripAction, HapticImpulsePlayer handHaptics)
        {
            if (gripAction.WasPressedThisFrame() == false) return;
            if (isChoosing) return;

            bool isGameRunning = turnManager.State != TurnState.GameEnd;
            if (isGameRunning == false) return;

            DiceTray playerTray = turnManager.Participants[0].Tray;
            float handToTrayMeters = Vector3.Distance(handHaptics.transform.position, playerTray.transform.position);
            if (handToTrayMeters > notYourTurnReachMeters) return;

            handHaptics.SendHapticImpulse(notYourTurnAmplitude, notYourTurnSeconds);
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
        /// 놓은 주사위가 충분히 빠르면 던진 것으로 알리고, 느리면 떨어뜨린 것으로 보고 트레이로 되돌린 뒤 다시 고르게 한다.
        /// </summary>
        /// <param name="args">XRI 놓기 이벤트. 어떤 주사위인지 들어 있다</param>
        /// <remarks>
        /// <see cref="DiceThrow"/>가 같은 놓기 이벤트에 먼저(OnEnable에서) 구독해 속도를 넣으므로, 여기서 읽는 속도는 던진 속도다.
        /// </remarks>
        private void OnReleased(SelectExitEventArgs args)
        {
            Dice releasedDice = args.interactableObject.transform.GetComponent<Dice>();
            float releaseSpeed = releasedDice.GetComponent<Rigidbody>().linearVelocity.magnitude;
            bool isDropped = releaseSpeed < dropSpeedMetersPerSecond;
            StopChoosing();
            releasedDice.SetSelectable(false);

            if (isDropped)
            {
                ReturnToTray(releasedDice);
                return;
            }
            turnManager.OnDieThrown(releasedDice);
        }

        /// <summary>
        /// 떨어뜨린 주사위를 지우고 트레이를 개수의 진실(<see cref="Participant.DiceCount"/>)에 다시 맞춘 뒤 같은 조건으로 다시 고르게 한다.
        /// 던진 것으로 치지 않았으므로 개수는 줄지 않았고, 트레이가 빈자리에 새 주사위를 놓는다.
        /// </summary>
        /// <param name="droppedDice">떨어뜨린 주사위 (이미 트레이 관리에서 빠져 있다)</param>
        private void ReturnToTray(Dice droppedDice)
        {
            Destroy(droppedDice.gameObject);
            choosingTray.Show(choosingPlayer.DiceCount);
            BeginChoosing(choosingPlayer, canStop);
        }

        private void StopChoosing()
        {
            isChoosing = false;
            UnwatchAllDice();
            choosingTray.SetSelectable(false);
        }

        private void PulseBothHands(float amplitude, float durationSeconds)
        {
            leftHandHaptics.SendHapticImpulse(amplitude, durationSeconds);
            rightHandHaptics.SendHapticImpulse(amplitude, durationSeconds);
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
