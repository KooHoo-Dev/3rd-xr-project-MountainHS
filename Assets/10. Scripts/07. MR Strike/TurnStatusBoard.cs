using System.Text;
using TMPro;
using UnityEngine;

namespace StudyXR.Strike
{
    /// <summary>
    /// 경기장 위 허공에 지금 누구 차례인지, 내가 무엇을 해야 하는지, 사람별 주사위 개수를 글자로 띄운다.
    /// 헤드셋에서는 화면 왼쪽 위 디버그 글자(OnGUI)가 안 보이므로, 손으로 던지며 턴을 진행할 때의 안내판으로 쓴다.
    /// </summary>
    /// <remarks>
    /// <see cref="TurnManager"/>의 상태를 읽기만 한다. 글자는 늘 머리를 바라본다.
    /// 안내 문구는 <see cref="BuildGuideText"/> 한 곳에 모아 두었다.
    /// </remarks>
    public class TurnStatusBoard : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private TurnManager turnManager;
        [SerializeField] private DiceTable table;
        [Tooltip("글자가 바라볼 머리 (Main Camera)")]
        [SerializeField] private Transform head;
        [Tooltip("안내 글자 (TextMeshPro)")]
        [SerializeField] private TMP_Text guideText;

        [Header("배치 (가정)")]
        [Tooltip("경기장 바닥 한가운데에서 글자까지 높이 (m)")]
        [SerializeField] private float heightAboveArenaMeters = 0.5f;

        /// <summary>
        /// 글자를 경기장 위에 놓고 머리를 바라보게 한 뒤 내용을 갱신한다.
        /// </summary>
        private void LateUpdate()
        {
            transform.position = table.Arena.FloorCenterPoint() + Vector3.up * heightAboveArenaMeters;
            transform.rotation = Quaternion.LookRotation(transform.position - head.position, Vector3.up);
            guideText.text = BuildGuideText();
        }

        /// <summary>
        /// 지금 상태에 맞는 안내 한 줄과 사람별 주사위 개수 한 줄을 만든다.
        /// </summary>
        /// <returns>두 줄짜리 글자</returns>
        private string BuildGuideText()
        {
            bool isGameStarted = turnManager.Participants.Count > 0;
            if (isGameStarted == false) return "게임 시작 전";

            return GuideLine() + "\n" + DiceCountLine();
        }

        /// <summary>
        /// 상태와 차례 사람에 따른 안내 문구. 플레이어에게는 다음에 할 행동(던지기·멈춤·다시 시작)을 알려 준다.
        /// </summary>
        private string GuideLine()
        {
            if (turnManager.State == TurnState.GameEnd)
            {
                string result = turnManager.DidPlayerWin ? "승리!" : "패배...";
                return $"{result} 왼손 메뉴(≡)에서 다시 시작";
            }

            Participant current = turnManager.CurrentParticipant;
            if (turnManager.State == TurnState.Rolling) return $"{current.Name} 주사위 굴러가는 중...";
            if (turnManager.State == TurnState.WaitingBoardRest) return "주사위가 멈추길 기다리는 중...";

            int forcedThrows = turnManager.ForcedThrowsRemaining;
            bool mustThrowAll = forcedThrows > 0;
            if (current.IsPlayer == false)
            {
                if (mustThrowAll) return $"{current.Name} 차례 (판이 비어 {forcedThrows}개 전부 던짐)";
                return $"{current.Name} 차례";
            }
            if (mustThrowAll)
            {
                return $"판이 비었어요! 주사위 {forcedThrows}개를 전부 던지세요";
            }
            if (turnManager.State == TurnState.ThrowingDice)
            {
                return "내 차례! 내 트레이 주사위를 잡고 던지세요";
            }
            return "못 땄어요. 다시 던지거나 B를 길게 눌러 멈추세요";
        }

        /// <summary>
        /// "나 5 · AI 1 5 · AI 2 탈락" 처럼 사람별 주사위 개수.
        /// </summary>
        private string DiceCountLine()
        {
            StringBuilder line = new StringBuilder();
            foreach (Participant participant in turnManager.Participants)
            {
                if (line.Length > 0) line.Append(" · ");

                string count = participant.IsEliminated ? "탈락" : participant.DiceCount.ToString();
                line.Append($"{participant.Name} {count}");
            }
            return line.ToString();
        }
    }
}
