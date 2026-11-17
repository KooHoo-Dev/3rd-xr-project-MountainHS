namespace StudyXR.XRInteraction
{
    public readonly struct DeskState
    {
        public DeskState(
            bool candleLit,
            bool keyInSocket,
            bool correctKey,
            bool leverPulled)
        {
            CandleLit = candleLit;
            KeyInSocket = keyInSocket;
            CorrectKey = correctKey;
            LeverPulled = leverPulled;
        }

        public bool CandleLit { get; }
        public bool KeyInSocket { get; }
        public bool CorrectKey { get; }
        public bool LeverPulled { get; }

        public override string ToString()
        {
            return $"촛불 {Mark(CandleLit)} · 꽂힘 {Mark(KeyInSocket)} · 맞는 열쇠 {Mark(CorrectKey)} · 레버 {Mark(LeverPulled)}";
        }

        private static string Mark(bool value)
        {
            return value ? "O" : "X";
        }
    }
}
