using System.Collections.Generic;

namespace StudyXR.Strike
{
    /// <summary>
    /// 앉은 순서대로 차례를 돌리고 탈락자를 건너뛴다. Unity에 의존하지 않는다.
    /// 턴 진행(#5)이 차례를 마칠 때 <see cref="MoveToNext"/>를, 판정 뒤 <see cref="EliminateIfEmpty"/>를,
    /// 승패를 볼 때 <see cref="AliveCount"/>·<see cref="IsPlayerAlive"/>를 부른다.
    /// </summary>
    public class TurnOrder
    {
        private readonly IReadOnlyList<Participant> participants;
        private int currentIndex;

        // 지금 차례인 참가자. 시작할 때는 목록의 첫 사람.
        public Participant Current => participants[currentIndex];

        // 탈락하지 않은 참가자 수
        public int AliveCount
        {
            get
            {
                int aliveCount = 0;
                foreach (Participant participant in participants)
                {
                    if (participant.IsEliminated == false) aliveCount++;
                }
                return aliveCount;
            }
        }

        // 플레이어가 아직 탈락하지 않았으면 true
        public bool IsPlayerAlive
        {
            get
            {
                foreach (Participant participant in participants)
                {
                    bool isAlivePlayer = participant.IsPlayer && participant.IsEliminated == false;
                    if (isAlivePlayer) return true;
                }
                return false;
            }
        }

        /// <summary>
        /// 앉은 순서(시계 방향)대로 참가자를 받는다. 첫 사람부터 시작한다.
        /// </summary>
        /// <param name="participantsInSeatOrder">차례 순서대로 놓은 참가자들 (1명 이상, 시작 시 전원 생존)</param>
        public TurnOrder(IReadOnlyList<Participant> participantsInSeatOrder)
        {
            participants = participantsInSeatOrder;
            currentIndex = 0;
        }

        /// <summary>
        /// 탈락하지 않은 다음 참가자로 차례를 넘긴다. 한 바퀴를 돌아 자기 자신에게 돌아올 수도 있다 (혼자 남았을 때).
        /// </summary>
        /// <remarks>
        /// 생존자가 한 명도 없으면 제자리에 멈춘다. 호출 전에 <see cref="AliveCount"/>로 승패를 먼저 본다.
        /// </remarks>
        public void MoveToNext()
        {
            for (int step = 1; step <= participants.Count; step++)
            {
                int candidateIndex = (currentIndex + step) % participants.Count;
                if (participants[candidateIndex].IsEliminated) continue;

                currentIndex = candidateIndex;
                return;
            }
        }

        /// <summary>
        /// 주사위가 0개인 참가자를 탈락 처리한다.
        /// </summary>
        /// <param name="participant">확인할 참가자</param>
        /// <returns>true = 0개라서 탈락 처리함, false = 아직 살아 있음</returns>
        public bool EliminateIfEmpty(Participant participant)
        {
            bool hasNoDice = participant.DiceCount == 0;
            if (hasNoDice == false) return false;

            participant.IsEliminated = true;
            return true;
        }
    }
}
