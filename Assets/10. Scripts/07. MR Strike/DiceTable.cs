using UnityEngine;

namespace StudyXR.Strike
{
    /// <summary>
    /// 보드게임 카페처럼 둘러앉는 원형 탁자 프리팹의 루트. 가운데 경기장과 자리마다 놓인 트레이를 한곳에서 알려 준다.
    /// 턴 진행(#5)이 씬에 놓인 탁자에서 <see cref="Arena"/>와 <see cref="SeatTrays"/>를 받아 쓰므로 Inspector 연결이 필요 없다.
    /// </summary>
    /// <remarks>
    /// 프리팹은 메뉴 "StudyXR/MR Strike/3. 원형 탁자 프리팹 만들기"가 만든다 (4인·5인).
    /// 자리 번호 0 = 플레이어(탁자 로컬 -Z쪽), 번호가 늘수록 시계 방향이며 이 순서가 그대로 차례 순서다.
    /// 탁자를 옮기고 돌려도 되지만 크기(Scale)는 1로 둔다 (Dice는 자기 크기를 절대 값으로 정하므로 탁자만 키우면 주사위와 트레이 비율이 어긋난다).
    /// </remarks>
    public class DiceTable : MonoBehaviour
    {
        [Tooltip("탁자 가운데 경기장")]
        [SerializeField] private DiceArena arena;

        [Tooltip("자리별 트레이. 0번 = 플레이어, 이후 시계 방향")]
        [SerializeField] private DiceTray[] seatTrays;

        [Tooltip("탁자 윗면의 높이 (m, 탁자 원점 기준). 진짜 책상에 놓을 때 이 높이만큼 내려서 윗면을 책상에 맞춘다. 메뉴 3의 TableSurfaceHeight와 같은 값")]
        [SerializeField] private float tableTopHeightMeters = 0.75f;

        public DiceArena Arena => arena;
        public DiceTray[] SeatTrays => seatTrays;

        /// <summary>
        /// 탁자 윗면이 주어진 점에 오도록 옮기고 돌린다. 트레이 주사위는 옮기기 전에 비우므로, 호출 뒤 새 판(StartGame)이 새 자리에 다시 놓아야 한다.
        /// MR 책상 배치(TablePlacer)가 부른다.
        /// </summary>
        /// <param name="surfacePoint">탁자 윗면 한가운데가 놓일 월드 위치 (진짜 책상 윗면 점)</param>
        /// <param name="rotation">탁자 회전 (Y 회전만 쓴다. 로컬 -Z쪽이 플레이어 자리)</param>
        /// <param name="hideFurniture">true = 가상 탁자 겉모습(윗판·기둥·받침)을 숨겨 진짜 책상이 보이게 한다 (충돌은 남는다)</param>
        public void PlaceOnSurface(Vector3 surfacePoint, Quaternion rotation, bool hideFurniture)
        {
            foreach (DiceTray tray in seatTrays)
            {
                tray.Show(0);
            }

            transform.SetPositionAndRotation(surfacePoint - Vector3.up * tableTopHeightMeters, rotation);
            SetFurnitureVisible(hideFurniture == false);
        }

        /// <summary>
        /// 경기장과 트레이를 뺀 나머지 겉모습(윗판·기둥·받침)을 켜거나 끈다.
        /// </summary>
        private void SetFurnitureVisible(bool visible)
        {
            foreach (Renderer furniture in GetComponentsInChildren<Renderer>())
            {
                bool isArenaPart = furniture.transform.IsChildOf(arena.transform);
                if (isArenaPart) continue;

                bool isTrayPart = false;
                foreach (DiceTray tray in seatTrays)
                {
                    if (furniture.transform.IsChildOf(tray.transform)) isTrayPart = true;
                }
                if (isTrayPart) continue;

                furniture.enabled = visible;
            }
        }
    }
}
