using StudyXR.Common;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StudyXR.Strike
{
    /// <summary>
    /// 왼손 X 버튼을 누르면 시선이 닿는 진짜 책상 위(없으면 머리 앞 허공)에 원형 탁자를 옮겨 놓고 새 판을 시작한다.
    /// 책상 위에서는 가상 탁자의 겉모습을 숨기고 경기장·트레이만 보이게 한다 (물리 바닥은 남는다).
    /// </summary>
    /// <remarks>
    /// 권한 요청은 샘플 <c>06. MR Aquarium/SpatialPermissionGate</c>를 그대로 쓴다 (거절되면 ARPlaneManager가 꺼져 허공 배치만 된다).
    /// 다시 누르면 그 자리에 다시 놓고 새 판을 시작한다. 탁자를 옮기면 트레이 주사위는 비웠다가 새 자리에서 다시 놓인다.
    /// </remarks>
    public class TablePlacer : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private TableFinder tableFinder;
        [SerializeField] private DiceTable table;
        [SerializeField] private TurnManager turnManager;
        [Tooltip("머리(보통 XR 카메라). 시선과 플레이어 쪽을 정한다")]
        [SerializeField] private Transform head;

        private InputAction placeAction;

        private void OnEnable()
        {
            placeAction = QuestButtons.Create(QuestButton.X, "Place Table", "<Keyboard>/f3");
            placeAction.Enable();
        }

        private void OnDisable()
        {
            placeAction.Dispose();
        }

        private void Update()
        {
            if (placeAction.WasPressedThisFrame()) PlaceTableAtGaze();
        }

        /// <summary>
        /// 시선이 닿는 책상(또는 허공)에 탁자를 놓고, 플레이어 자리(0번)가 내 쪽이 되게 돌린 뒤 새 판을 시작한다.
        /// </summary>
        private void PlaceTableAtGaze()
        {
            Ray gazeRay = new Ray(head.position, head.forward);
            bool isFound = tableFinder.TryFindTable(gazeRay, out Vector3 surfacePoint, out TableSource source);
            if (isFound == false) return;

            // 탁자 로컬 +Z가 내게서 멀어지는 방향이어야 0번 자리(-Z)가 내 앞에 온다
            Vector3 awayFromHead = Vector3.ProjectOnPlane(surfacePoint - head.position, Vector3.up);
            Quaternion rotation = Quaternion.LookRotation(awayFromHead.normalized, Vector3.up);

            bool isRealTable = source != TableSource.Air;
            table.PlaceOnSurface(surfacePoint, rotation, isRealTable);
            turnManager.StartGame();
        }
    }
}
