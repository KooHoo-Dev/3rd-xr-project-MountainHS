namespace StudyXR.Strike
{
    /// <summary>
    /// 주사위 한 면. 숫자 1~5와 꽝(X).
    /// <see cref="Dice.ReadTopFace"/>가 돌려주고, 판정 규칙(#3)이 같은 숫자 짝을 찾을 때 쓴다.
    /// </summary>
    /// <remarks>
    /// <c>Dice</c>의 6개 면 필드(positiveXFace 등)가 이 값을 가진다. 프리팹에 정수로 저장되므로 순서를 바꾸지 않는다 (0 = One … 5 = X).
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
