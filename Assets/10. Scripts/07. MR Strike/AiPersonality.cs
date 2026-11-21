using UnityEngine;

namespace StudyXR.Strike
{
    /// <summary>
    /// AI가 어디를 겨냥할지 고르는 방식.
    /// </summary>
    public enum ThrowStyle
    {
        // 판 위 주사위가 가장 몰린 곳을 세게 때려 흩뜨린다
        Smash,
        // 판 위 주사위에서 가장 먼 빈 곳에 살살 놓는다
        Gentle,
        // 경기장 가운데를 겨냥한다
        Center,
    }

    /// <summary>
    /// AI 한 명의 성격 값 묶음. 언제 멈추는지(위험 성향), 어떻게 던지는지(세기·조준), 얼마나 정확한지(실력).
    /// <see cref="AiPlayer"/>가 읽어 쓴다. 에셋이라 Play 중에 고친 값이 남는다.
    /// </summary>
    [CreateAssetMenu(fileName = "AiPersonality", menuName = "StudyXR/MR Strike/AI Personality")]
    public class AiPersonality : ScriptableObject
    {
        [Header("멈춤 판단")]
        [Tooltip("판 위 주사위로 짝이 생길 확률이 이 값 이상이어야 계속 던진다 (0~1). 0 = 확률과 상관없이 던짐")]
        public float minMatchChanceToContinue = 0f;

        [Tooltip("손에 든 주사위가 이 개수 이상이어야 계속 던진다. 1 = 개수와 상관없이 던짐")]
        public int minDiceToKeepThrowing = 1;

        [Header("던지기")]
        public ThrowStyle throwStyle = ThrowStyle.Center;

        [Tooltip("던지는 각도 (도). 낮을수록 납작하게 날아가 세게 부딪힌다")]
        public float launchAngleDegrees = 40f;

        [Tooltip("목표에 닿는 속도에 곱하는 배율. 1보다 크면 목표를 지나쳐 날아간다")]
        public float throwSpeedScale = 1f;

        [Tooltip("조준 오차 반지름 (m). 클수록 목표에서 멀리 벗어난다")]
        public float aimErrorRadiusMeters = 0.04f;

        [Header("겉모습·대사 (AiAvatar가 보여 줌)")]
        [Tooltip("머리 색. 세 AI를 한눈에 구분하는 단서")]
        public Color headColor = Color.white;

        [Tooltip("던지기 전 예고 동작 때 하는 말")]
        public string windupLine = "간다!";

        [Tooltip("더 던지지 않고 멈출 때 하는 말")]
        public string stopLine = "여기까지.";

        [Tooltip("짝을 따서 주사위를 가져갔을 때 하는 말")]
        public string happyLine = "좋았어!";

        [Tooltip("못 땄을 때 하는 말")]
        public string disappointedLine = "아쉽네.";
    }
}
