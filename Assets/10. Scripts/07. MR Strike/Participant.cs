namespace StudyXR.Strike
{
    /// <summary>
    /// 게임에 앉은 한 명(플레이어 또는 AI). 손에 든 주사위 개수가 유일한 진실이고, 트레이는 그 개수를 보여 주기만 한다.
    /// 턴 진행(#5)이 판정 결과로 <see cref="DiceCount"/>를 바꾸고, <see cref="TurnOrder"/>가 0개가 된 사람을 탈락시킨다.
    /// </summary>
    public class Participant
    {
        public string Name { get; }

        // true = 내 주사위를 직접 던지는 사람. AI(#7)가 생기면 이 값 대신 AI 연결 여부로 바꾼다.
        public bool IsPlayer { get; }

        // 이 사람 앞의 트레이. 개수를 바꾼 뒤 Tray.Show(DiceCount)로 맞춘다.
        public DiceTray Tray { get; }

        // 손에 든 주사위 개수 (진실). 0이 되면 TurnOrder가 탈락 처리한다.
        public int DiceCount { get; set; }

        // true = 탈락. TurnOrder.EliminateIfEmpty만 바꾼다.
        public bool IsEliminated { get; set; }

        /// <summary>
        /// 참가자를 만든다. 트레이는 이 시점에 아직 그리지 않는다 (호출자가 Tray.Show로 그린다).
        /// </summary>
        /// <param name="name">화면에 보일 이름</param>
        /// <param name="isPlayer">true = 직접 던지는 플레이어</param>
        /// <param name="tray">이 사람 앞의 트레이</param>
        /// <param name="startDiceCount">시작 주사위 개수</param>
        public Participant(string name, bool isPlayer, DiceTray tray, int startDiceCount)
        {
            Name = name;
            IsPlayer = isPlayer;
            Tray = tray;
            DiceCount = startDiceCount;
        }
    }
}
