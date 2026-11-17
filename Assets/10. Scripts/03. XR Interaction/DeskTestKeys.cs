using StudyXR.Common;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Content.Interaction;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace StudyXR.XRInteraction
{
    public class DeskTestKeys : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private EscapeDesk desk;
        [SerializeField] private XRSocketInteractor keySocket;
        [SerializeField] private XRGrabInteractable keyA;
        [SerializeField] private XRGrabInteractable keyB;
        [SerializeField] private XRLever lever;
        [SerializeField] private CandleLight candle;
        [SerializeField] private Respawnable[] resetItems;

        [Header("구역")]
        [SerializeField] private ZoneHub zoneHub;
        [SerializeField] private int zoneIndex;

        [Header("Quest 버튼")]
        [SerializeField] private bool useButtons = true;

        private const QuestButton CandleButton = QuestButton.A;
        private const QuestButton KeyAButton = QuestButton.X;
        private const QuestButton KeyBButton = QuestButton.LeftStickClick;
        private const QuestButton LeverButton = QuestButton.RightStickClick;
        private const QuestButton ResetButton = QuestButton.B;

        private InputAction candleAction, keyAAction, keyBAction, leverAction, resetAction;
        private static readonly WaitForFixedUpdate PhysicsStep = new WaitForFixedUpdate();
        private bool busy;

        public static string ControlsText =>
            QuestButtons.Label(CandleButton) + " : 촛불 · " + QuestButtons.Label(KeyAButton) + " : 열쇠 A · " + QuestButtons.Label(KeyBButton) + " : 열쇠 B\n"
            + QuestButtons.Label(LeverButton) + " : 레버 · " + QuestButtons.Label(ResetButton) + " : 처음으로";

        private void OnEnable()
        {
            if (useButtons == false) return;
            candleAction = Make(CandleButton, "Desk Candle", "<Keyboard>/f3");
            keyAAction = Make(KeyAButton, "Desk Key A", "<Keyboard>/f4");
            keyBAction = Make(KeyBButton, "Desk Key B", "<Keyboard>/f5");
            leverAction = Make(LeverButton, "Desk Lever", "<Keyboard>/f6");
            resetAction = Make(ResetButton, "Desk Reset", "<Keyboard>/f7");
        }

        private void OnDisable()
        {
            busy = false;
            if (keySocket != null) keySocket.socketActive = true;
            Drop(ref candleAction);
            Drop(ref keyAAction);
            Drop(ref keyBAction);
            Drop(ref leverAction);
            Drop(ref resetAction);
        }

        private static InputAction Make(
            QuestButton button,
            string name,
            string keyboardFallback)
        {
            InputAction action = QuestButtons.Create(button, name, keyboardFallback);
            action.Enable();
            return action;
        }

        private static void Drop(ref InputAction action)
        {
            if (action == null) return;
            action.Disable();
            action.Dispose();
            action = null;
        }

        private static bool Pressed(InputAction action)
            => action != null && action.WasPressedThisFrame();

        private void Update()
        {
            if (zoneHub != null && zoneHub.Current != zoneIndex) return;
            if (busy) return;

            if (Pressed(candleAction) && candle != null) candle.SetLit(!candle.IsLit);
            if (Pressed(keyAAction)) InsertKey(keyA);
            if (Pressed(keyBAction)) InsertKey(keyB);
            if (Pressed(leverAction) && lever != null) lever.value = !lever.value;
            if (Pressed(resetAction)) StartCoroutine(ResetDesk());
        }

        private void InsertKey(XRGrabInteractable key)
        {
            if (keySocket == null || key == null) return;
            XRInteractionManager manager = keySocket.interactionManager;

            if (manager == null) return;

            if (keySocket.hasSelection == false)
            {
                Release(manager, key);
                manager.SelectEnter((IXRSelectInteractor)keySocket, (IXRSelectInteractable)key);
                return;
            }
            IXRSelectInteractable current = keySocket.interactablesSelected[0];

            if (ReferenceEquals(current, key) == false) StartCoroutine(SwapKey(manager, current, key));
        }

        private IEnumerator SwapKey(
            XRInteractionManager manager,
            IXRSelectInteractable current,
            XRGrabInteractable key)
        {
            busy = true;
            keySocket.socketActive = false;
            manager.SelectExit((IXRSelectInteractor)keySocket, current);
            Respawnable removedHome = current.transform.GetComponent<Respawnable>();

            if (removedHome != null) removedHome.RespawnNow();
            yield return PhysicsStep;
            yield return PhysicsStep;

            keySocket.socketActive = true;
            Release(manager, key);
            manager.SelectEnter((IXRSelectInteractor)keySocket, (IXRSelectInteractable)key);
            busy = false;
        }

        private static void Release(XRInteractionManager manager, XRGrabInteractable item)
        {
            if (item == null) return;
            for (int i = item.interactorsSelecting.Count - 1; i >= 0; --i)
                manager.SelectExit(item.interactorsSelecting[i], (IXRSelectInteractable)item);
        }

        private IEnumerator ResetDesk()
        {
            busy = true;
            XRInteractionManager manager = keySocket != null ? keySocket.interactionManager : null;

            if (keySocket != null) keySocket.socketActive = false;
            if (manager != null)
            {
                Release(manager, keyA);
                Release(manager, keyB);
                if (candle != null) Release(manager, candle.GetComponent<XRGrabInteractable>());
            }

            if (candle != null) candle.SetLit(false);
            if (lever != null) lever.value = false;
            foreach (Respawnable item in resetItems)
                if (item != null) item.RespawnNow();
            if (desk != null) desk.RestartTimer();

            yield return PhysicsStep;
            yield return PhysicsStep;
            if (keySocket != null) keySocket.socketActive = true;
            busy = false;
        }
    }
}
