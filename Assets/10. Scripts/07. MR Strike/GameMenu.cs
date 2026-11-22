using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace StudyXR.Strike
{
    /// <summary>
    /// 왼손 메뉴(≡) 버튼으로 여닫는 작은 메뉴. 지금은 "다시 시작" 버튼 하나만 있고, 누르면 게임 전체를 새 판으로 시작한다.
    /// 다시 시작을 메뉴에만 두어 게임 중 버튼 오입력으로 판이 날아가지 않게 한다.
    /// </summary>
    /// <remarks>
    /// 패널은 열 때마다 왼손 앞에 놓이고 머리를 바라본다. 버튼은 <see cref="XRSimpleInteractable"/>이라 손으로 쥐거나 레이로 선택한다.
    /// 턴제라 일시정지는 없다. 메뉴가 열려 있어도 게임은 그대로 진행된다.
    /// </remarks>
    public class GameMenu : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private TurnManager turnManager;

        [Tooltip("메뉴 패널 루트. 닫으면 꺼진다")]
        [SerializeField] private GameObject panelRoot;

        [Tooltip("다시 시작 버튼 (XRSimpleInteractable)")]
        [SerializeField] private XRSimpleInteractable restartButton;

        [Tooltip("패널을 띄울 기준 손 (Left Controller)")]
        [SerializeField] private Transform hand;

        [Tooltip("패널이 바라볼 머리 (Main Camera)")]
        [SerializeField] private Transform head;

        [Header("배치 (가정)")]
        [Tooltip("손에서 패널까지 거리 (m). 손 앞쪽 + 약간 위")]
        [SerializeField] private Vector3 offsetFromHandMeters = new Vector3(0f, 0.08f, 0.15f);

        private InputAction menuAction;

        private void OnEnable()
        {
            menuAction = new InputAction("Game Menu", InputActionType.Button, "<XRController>{LeftHand}/{MenuButton}");
            menuAction.AddBinding("<Keyboard>/f12");
            menuAction.Enable();
            restartButton.selectEntered.AddListener(OnRestartSelected);
            panelRoot.SetActive(false);
        }

        private void OnDisable()
        {
            menuAction.Dispose();
            restartButton.selectEntered.RemoveListener(OnRestartSelected);
        }

        /// <summary>
        /// 메뉴 버튼을 누르면 메뉴를 열거나 닫는다.
        /// </summary>
        private void Update()
        {
            if (menuAction.WasPressedThisFrame() == false) return;

            bool isOpen = panelRoot.activeSelf;
            if (isOpen)
            {
                panelRoot.SetActive(false);
                return;
            }
            OpenNearHand();
        }

        /// <summary>
        /// 패널을 손 앞에 놓고 머리를 바라보게 한 뒤 켠다.
        /// </summary>
        private void OpenNearHand()
        {
            panelRoot.transform.position = hand.TransformPoint(offsetFromHandMeters);
            Vector3 awayFromHead = panelRoot.transform.position - head.position;
            panelRoot.transform.rotation = Quaternion.LookRotation(awayFromHead, Vector3.up);
            panelRoot.SetActive(true);
        }

        /// <summary>
        /// 다시 시작 버튼을 선택하면 새 판을 시작하고 메뉴를 닫는다.
        /// </summary>
        private void OnRestartSelected(SelectEnterEventArgs args)
        {
            turnManager.StartGame();
            panelRoot.SetActive(false);
        }
    }
}
