using System.Collections.Generic;
using UnityEngine;

namespace StudyXR.Strike
{
    /// <summary>
    /// 참가자 앞 트레이. 손에 든 주사위 개수만큼 실제 <see cref="Dice"/>를 놓아 보여 준다. 개수의 진실은 <see cref="Participant.DiceCount"/>다.
    /// 턴 진행(#5)이 개수가 바뀔 때마다 <see cref="Show"/>를, 플레이어 차례에 <see cref="SetSelectable"/>을, AI 차례에 <see cref="TakeOne"/>을 부른다.
    /// </summary>
    /// <remarks>
    /// 이 오브젝트의 위치·회전이 트레이 기준이다. 로컬 원점 = 트레이 윗면 한가운데, 로컬 +Z = 경기장 쪽, 로컬 +X = 트레이 긴 변.
    /// 주사위는 트레이의 자식이 아니라 씬 루트에 만든다 (Dice가 자기 크기를 절대 값으로 정하므로 부모 크기의 영향을 받지 않게 한다).
    /// 놓인 주사위는 물리가 켜진 채 트레이 바닥에 얹힌다. 물리를 끄면 XRI가 놓을 때 되돌려 던진 주사위가 굳어 버린다.
    /// </remarks>
    public class DiceTray : MonoBehaviour
    {
        [Header("주사위")]
        [Tooltip("트레이에 놓을 주사위 프리팹")]
        [SerializeField] private Dice dicePrefab;

        [Header("배치 (가정)")]
        [Tooltip("한 줄에 놓는 주사위 수")]
        [SerializeField] private int slotsPerRow = 8;
        [Tooltip("줄 수. 넘치는 개수는 놓을 자리가 없다 (시작 5개 × 4명 = 20개가 한 사람에게 몰려도 들어가게 잡는다)")]
        [SerializeField] private int rowCount = 3;
        [Tooltip("주사위 자리 사이 간격 (m). 주사위 크기보다 커야 겹치지 않는다")]
        [SerializeField] private float slotSpacingMeters = 0.035f;
        [Tooltip("트레이 바닥 위 몇 m에서 떨어뜨릴지 (m)")]
        [SerializeField] private float dropHeightMeters = 0.02f;

        // ── 놓인 주사위 (맨 뒤 = 가장 나중 자리) ──
        private readonly List<Dice> diceInTray = new List<Dice>();

        // 새로 놓는 주사위에 적용할 집기 허용 상태
        private bool isSelectable;

        // 지금 트레이에 놓인 주사위 수
        public int DiceCount => diceInTray.Count;

        // 지금 트레이에 놓인 주사위들 (읽기 전용). 플레이어 입력이 잡기 이벤트를 걸 때 쓴다.
        public IReadOnlyList<Dice> DiceInTray => diceInTray;

        /// <summary>
        /// 트레이에 정확히 그 개수의 주사위가 놓이도록 모자라면 만들고 남으면 지운다 (풀링 없음).
        /// 이미 놓인 주사위는 건드리지 않는다.
        /// </summary>
        /// <param name="count">손에 든 주사위 개수 (0 이상)</param>
        public void Show(int count)
        {
            while (diceInTray.Count < count)
            {
                AddDice();
            }
            while (diceInTray.Count > count)
            {
                RemoveLastDice();
            }
        }

        /// <summary>
        /// 트레이에 놓인 주사위 전부를 강조하고 집을 수 있게 하거나, 원래대로 돌린다.
        /// 이후 새로 놓이는 주사위도 같은 상태를 따른다.
        /// </summary>
        /// <param name="selectable">true = 강조 + 집기 허용, false = 기본 색 + 집기 막음</param>
        public void SetSelectable(bool selectable)
        {
            isSelectable = selectable;
            foreach (Dice dice in diceInTray)
            {
                dice.SetSelectable(selectable);
            }
        }

        /// <summary>
        /// 트레이에서 주사위 하나를 떼어 낸다. AI가 던질 주사위로 쓴다 (플레이어는 직접 잡는다).
        /// 떼어 낸 주사위는 더 이상 이 트레이가 관리하지 않으므로, 개수는 호출자가 <see cref="Participant.DiceCount"/>로 맞춘다.
        /// </summary>
        /// <returns>떼어 낸 주사위 (가장 나중에 놓인 것). 호출 전에 한 개 이상 있어야 한다</returns>
        public Dice TakeOne()
        {
            int lastIndex = diceInTray.Count - 1;
            Dice taken = diceInTray[lastIndex];
            diceInTray.RemoveAt(lastIndex);
            return taken;
        }

        /// <summary>
        /// 플레이어가 직접 집은 주사위를 트레이 관리에서 뺀다. <see cref="TakeOne"/>과 달리 어느 주사위인지 지정한다.
        /// </summary>
        /// <param name="dice">플레이어가 집은 주사위 (이 트레이에 놓여 있어야 한다)</param>
        public void Take(Dice dice)
        {
            diceInTray.Remove(dice);
        }

        /// <summary>
        /// 트레이 관리에 넣지 않는 주사위를 이 트레이의 프리팹으로 만든다. 판에 처음 놓는 주사위처럼 손에 든 개수에 속하지 않는 것에 쓴다.
        /// 집기는 막은 채로 만든다.
        /// </summary>
        /// <param name="position">만들 월드 위치</param>
        /// <param name="rotation">만들 월드 회전</param>
        /// <returns>새 주사위. 지우는 일은 호출자 몫이다</returns>
        public Dice CreateLooseDice(Vector3 position, Quaternion rotation)
        {
            Dice dice = Instantiate(dicePrefab, position, rotation);
            dice.SetSelectable(false);
            return dice;
        }

        /// <summary>
        /// 다음 빈 자리 위에 주사위를 새로 만든다.
        /// </summary>
        private void AddDice()
        {
            Vector3 spawnPosition = SlotWorldPosition(diceInTray.Count);
            Dice dice = Instantiate(dicePrefab, spawnPosition, transform.rotation);
            dice.SetSelectable(isSelectable);
            diceInTray.Add(dice);
        }

        private void RemoveLastDice()
        {
            int lastIndex = diceInTray.Count - 1;
            Destroy(diceInTray[lastIndex].gameObject);
            diceInTray.RemoveAt(lastIndex);
        }

        /// <summary>
        /// 자리 번호의 월드 위치. 줄은 가운데 줄 기준으로 펼치고, 첫 줄이 플레이어 쪽(로컬 -Z)이다.
        /// </summary>
        /// <param name="slotIndex">자리 번호 (0부터, 한 줄을 다 채운 뒤 다음 줄)</param>
        /// <returns>그 자리 바닥 위 dropHeightMeters 높이의 월드 위치</returns>
        private Vector3 SlotWorldPosition(int slotIndex)
        {
            int rowIndex = slotIndex / slotsPerRow;
            int columnIndex = slotIndex % slotsPerRow;

            float localX = (columnIndex - (slotsPerRow - 1) * 0.5f) * slotSpacingMeters;
            float localZ = (rowIndex - (rowCount - 1) * 0.5f) * slotSpacingMeters;
            return transform.TransformPoint(new Vector3(localX, dropHeightMeters, localZ));
        }
    }
}
