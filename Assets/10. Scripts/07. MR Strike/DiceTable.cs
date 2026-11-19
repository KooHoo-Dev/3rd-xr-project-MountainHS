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

        public DiceArena Arena => arena;
        public DiceTray[] SeatTrays => seatTrays;
    }
}
