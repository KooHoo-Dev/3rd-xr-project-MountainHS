namespace StudyXR.Strike
{
    /// <summary>
    /// 주사위 한 면. 숫자 1~5와 꽝(X).
    /// <see cref="Dice.ReadTopFace"/>가 돌려주고, 판정 규칙(#3)이 같은 숫자 짝을 찾을 때 쓴다.
    /// </summary>
    /// <remarks>
    /// 순서가 <c>Dice</c>의 면 라벨 배열 순서와 같아야 한다 (0 = One … 5 = X).
    /// </remarks>
    public enum DiceFace
    {
        One,
        Two,
        Three,
        Four,
        Five,
        X,
    }
}
