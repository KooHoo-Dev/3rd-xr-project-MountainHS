using System.Collections.Generic;
using UnityEngine;

namespace StudyXR.Strike
{
    /// <summary>
    /// 던진 주사위가 지나간 경로에 잔상 궤적을 남긴다.
    /// 주사위가 일정 속도 이상이면 매 프레임 화면에 그려지는 위치를 기록하고, LineRenderer로 이어 그린다.
    /// 각 점은 기록된 시점부터 수명만큼 남고, 나이에 따라 투명해진다. 멈추면 새 점이 없고 남은 점도 수명이 끝나면 사라진다.
    /// </summary>
    [RequireComponent(typeof(LineRenderer), typeof(Rigidbody))]
    public class DiceTrail : MonoBehaviour
    {
        // LineRenderer Gradient가 담을 수 있는 alpha 키 최대 개수
        private const int MaxGradientKeyCount = 8;

        [Header("궤적 모양")]
        [Tooltip("궤적 각 점이 기록된 뒤 사라질 때까지 시간 (초)")]
        [SerializeField] private float lifetimeSeconds = 0.4f;

        [Tooltip("궤적 머리 쪽 너비 (m)")]
        [SerializeField] private float widthMeters = 0.03f;

        [Tooltip("꼬리(0, 가장 오래된 점)에서 머리(1, 주사위)까지 너비 배율")]
        [SerializeField] private AnimationCurve widthCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Tooltip("궤적 색 (alpha는 아래 곡선이 정한다)")]
        [SerializeField] private Color color = Color.white;

        [Tooltip("점 나이(0 = 방금 기록, 1 = 수명 끝)에 따른 투명도")]
        [SerializeField] private AnimationCurve alphaOverAge = AnimationCurve.Linear(0f, 1f, 1f, 0f);

        [Header("생성 조건")]
        [Tooltip("이 속도 미만이면 새 점을 기록하지 않는다 (m/s)")]
        [SerializeField] private float minimumSpeed = 0.1f;

        /// <summary>
        /// 궤적 점 하나. 위치와 기록 시각을 같이 들고 있어야 나이별로 지우고 투명도를 정할 수 있다.
        /// </summary>
        private struct TrailPoint
        {
            public Vector3 position;
            public float recordedTime;
        }

        // ── 참조 ──
        private LineRenderer line;
        private Rigidbody body;

        // ── 궤적 ──
        // 오래된 점부터 순서대로. 0번이 꼬리, 마지막이 머리
        private readonly List<TrailPoint> trailPoints = new List<TrailPoint>();

        /// <summary>
        /// 참조를 찾고 LineRenderer를 월드 좌표 궤적용으로 맞춘다.
        /// </summary>
        private void Awake()
        {
            line = GetComponent<LineRenderer>();
            body = GetComponent<Rigidbody>();
            line.useWorldSpace = true;
            line.positionCount = 0;
        }

        /// <summary>
        /// 매 프레임 궤적을 갱신한다.
        /// </summary>
        /// <remarks>
        /// Rigidbody 보간과 손 놓기(DiceThrow.OnReleased)가 위치를 정한 뒤인 LateUpdate에서 읽어야 화면의 주사위 위치와 같다.
        /// </remarks>
        private void LateUpdate()
        {
            RemoveExpiredPoints();
            if (IsMovingFast()) RecordCurrentPosition();
            DrawTrail();
        }

        /// <summary>
        /// 수명이 끝난 점을 꼬리부터 지운다.
        /// </summary>
        private void RemoveExpiredPoints()
        {
            while (trailPoints.Count > 0 && AgeRatio(trailPoints[0]) >= 1f)
            {
                trailPoints.RemoveAt(0);
            }
        }

        /// <returns>주사위 속도가 기록 기준 이상이면 true</returns>
        private bool IsMovingFast()
        {
            return body.linearVelocity.magnitude >= minimumSpeed;
        }

        /// <summary>
        /// 지금 주사위 위치를 머리 쪽에 추가한다.
        /// </summary>
        private void RecordCurrentPosition()
        {
            TrailPoint point = new TrailPoint();
            point.position = transform.position;
            point.recordedTime = Time.time;
            trailPoints.Add(point);
        }

        /// <summary>
        /// 기록된 점을 LineRenderer에 넣고, 너비와 나이별 투명도를 적용한다. 점이 2개 미만이면 아무것도 그리지 않는다.
        /// </summary>
        private void DrawTrail()
        {
            if (trailPoints.Count < 2)
            {
                line.positionCount = 0;
                return;
            }

            line.positionCount = trailPoints.Count;
            for (int i = 0; i < trailPoints.Count; i++)
            {
                line.SetPosition(i, trailPoints[i].position);
            }

            line.widthMultiplier = widthMeters;
            line.widthCurve = widthCurve;
            line.colorGradient = BuildAgeGradient();
        }

        /// <summary>
        /// 선 위 여러 위치의 점 나이를 읽어 투명도 Gradient를 만든다.
        /// Gradient는 키가 최대 8개라서, 점 전체에서 고르게 최대 8개를 뽑아 키로 쓴다.
        /// </summary>
        /// <returns>선 길이 비율(0 = 꼬리, 1 = 머리)마다 나이에 맞는 alpha를 가진 Gradient</returns>
        private Gradient BuildAgeGradient()
        {
            int keyCount = Mathf.Min(MaxGradientKeyCount, trailPoints.Count);
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[keyCount];
            for (int keyIndex = 0; keyIndex < keyCount; keyIndex++)
            {
                float lineRatio = keyIndex / (float)(keyCount - 1);
                int pointIndex = Mathf.RoundToInt(lineRatio * (trailPoints.Count - 1));
                float alpha = alphaOverAge.Evaluate(AgeRatio(trailPoints[pointIndex]));
                alphaKeys[keyIndex] = new GradientAlphaKey(alpha, lineRatio);
            }

            GradientColorKey[] colorKeys = { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) };
            Gradient gradient = new Gradient();
            gradient.SetKeys(colorKeys, alphaKeys);
            return gradient;
        }

        /// <param name="point">나이를 잴 궤적 점</param>
        /// <returns>점 나이를 수명으로 나눈 값. 0 = 방금 기록, 1 이상 = 수명 끝</returns>
        private float AgeRatio(TrailPoint point)
        {
            return (Time.time - point.recordedTime) / lifetimeSeconds;
        }
    }
}
