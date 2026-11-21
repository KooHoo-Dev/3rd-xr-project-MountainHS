using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StudyXR.Strike
{
    /// <summary>
    /// AI 한 명의 차례 행동. 생각하는 시간 뒤에 계속할지 판단하고, 던진다면 목표를 골라 포물선으로 던진다.
    /// <see cref="TurnManager"/>가 AI 차례에 <see cref="BeginChoosing"/>을 부르고, 이 클래스는 <see cref="TurnManager.OnDieThrown"/> 또는 <see cref="TurnManager.StopTurn"/>으로 답한다.
    /// </summary>
    /// <remarks>
    /// 차례는 몇 초짜리 순서 대본(생각 → 던지기)이라 코루틴으로 쓴다. 새 판이 시작되면 <see cref="CancelTurn"/>이 끊는다.
    /// </remarks>
    public class AiPlayer : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private TurnManager turnManager;
        [SerializeField] private DiceArena arena;
        [SerializeField] private AiPersonality personality;
        [SerializeField] private AiAvatar avatar;

        [Header("행동 (가정)")]
        [Tooltip("차례가 오고 나서 던지거나 멈추기까지 생각하는 시간 (초)")]
        [SerializeField] private float thinkSeconds = 1f;
        [Tooltip("트레이 위 몇 m에서 던지기 시작할지 (m)")]
        [SerializeField] private float throwStartHeightMeters = 0.1f;
        [Tooltip("무작위 회전 속도 (rad/s)")]
        [SerializeField] private float spinSpeed = 15f;
        [Tooltip("Smash가 '몰려 있다'고 세는 이웃 거리 (m)")]
        [SerializeField] private float clusterRadiusMeters = 0.06f;
        [Tooltip("Gentle이 빈 곳을 찾을 때 시험해 보는 후보 점 수")]
        [SerializeField] private int emptySpotCandidateCount = 20;
        [Tooltip("말풍선을 보여 주는 시간 (초)")]
        [SerializeField] private float lineSeconds = 2f;

        /// <summary>
        /// 차례를 시작한다. 생각한 뒤 멈추거나 던지는 코루틴을 돌린다.
        /// </summary>
        /// <param name="self">이 AI의 참가자 (트레이와 개수를 가진다)</param>
        /// <param name="canStop">true = 멈출 수 있음 (한 번 던져 못 딴 뒤). false면 반드시 던진다</param>
        public void BeginChoosing(Participant self, bool canStop)
        {
            StartCoroutine(TakeTurn(self, canStop));
        }

        /// <summary>
        /// 진행 중인 차례를 끊는다. 새 판이 시작될 때 TurnManager가 부른다.
        /// </summary>
        public void CancelTurn()
        {
            StopAllCoroutines();
            avatar.ResetPose();
            avatar.ShowExpression(Expression.Neutral);
        }

        /// <summary>
        /// 판정 결과에 표정과 대사로 반응한다. TurnManager가 판정 직후 모든 AI에게 알린다.
        /// 자기 차례였으면 땄는지에 따라 웃거나 아쉬워하고, 남의 차례였으면 상대가 땄을 때만 불안해한다.
        /// </summary>
        /// <param name="thrower">방금 던진 사람 (플레이어 또는 AI)</param>
        /// <param name="result">그 판정 결과 (장외·X·딴 수)</param>
        public void ReactToJudge(Participant thrower, TurnManager.JudgeResult result)
        {
            bool isMyThrow = thrower.Ai == this;
            bool hasTaken = result.Taken > 0;

            if (isMyThrow && hasTaken)
            {
                avatar.ShowExpression(Expression.Happy);
                avatar.Say(personality.happyLine, lineSeconds);
                return;
            }
            if (isMyThrow)
            {
                avatar.ShowExpression(Expression.Disappointed);
                avatar.Say(personality.disappointedLine, lineSeconds);
                return;
            }

            avatar.ShowExpression(hasTaken ? Expression.Worried : Expression.Neutral);
        }

        /// <summary>
        /// 계속할지 판단한다. 짝이 생길 확률과 손에 든 개수가 성격 기준을 모두 넘어야 계속한다.
        /// </summary>
        /// <param name="boardFaces">판 위 주사위의 윗면들 (장외·X는 이미 지워진 상태)</param>
        /// <param name="diceCount">내 손에 든 주사위 개수</param>
        /// <returns>true = 더 던진다, false = 멈춘다</returns>
        public bool ShouldKeepThrowing(IReadOnlyList<DiceFace> boardFaces, int diceCount)
        {
            float matchChance = DiceRules.MatchChance(boardFaces);
            bool isChanceHighEnough = matchChance >= personality.minMatchChanceToContinue;
            bool hasEnoughDice = diceCount >= personality.minDiceToKeepThrowing;
            return isChanceHighEnough && hasEnoughDice;
        }

        /// <summary>
        /// 성격의 던지기 방식에 따라 겨냥할 바닥 점을 고르고 조준 오차를 더한다.
        /// </summary>
        /// <param name="boardDice">판 위 주사위들</param>
        /// <returns>겨냥한 월드 위치 (경기장 바닥 높이). 오차 때문에 바닥 밖일 수 있다</returns>
        public Vector3 ChooseTarget(IReadOnlyList<Dice> boardDice)
        {
            Vector3 target;
            switch (personality.throwStyle)
            {
                case ThrowStyle.Smash:
                    target = FindMostCrowdedPoint(boardDice);
                    break;
                case ThrowStyle.Gentle:
                    target = FindEmptiestPoint(boardDice);
                    break;
                default:
                    target = arena.FloorCenterPoint();
                    break;
            }

            Vector2 aimError = Random.insideUnitCircle * personality.aimErrorRadiusMeters;
            return target + new Vector3(aimError.x, 0f, aimError.y);
        }

        /// <summary>
        /// 출발점에서 목표에 닿는 초기 속도를 포물선 공식으로 구한다. 각도가 정해져 있으면 속도가 하나로 정해진다.
        /// </summary>
        /// <param name="from">출발 월드 위치</param>
        /// <param name="to">목표 월드 위치</param>
        /// <param name="angleDegrees">던지는 각도 (도, 수평 기준 위쪽)</param>
        /// <returns>초기 속도 (m/s). 목표가 그 각도로는 닿을 수 없을 만큼 높으면 높이 차를 0으로 보고 계산한다</returns>
        public static Vector3 LaunchVelocity(Vector3 from, Vector3 to, float angleDegrees)
        {
            Vector3 horizontalOffset = new Vector3(to.x - from.x, 0f, to.z - from.z);
            float horizontalDistance = horizontalOffset.magnitude;
            Vector3 horizontalDirection = horizontalOffset / horizontalDistance;
            float heightDifference = to.y - from.y;

            float angleRadians = angleDegrees * Mathf.Deg2Rad;
            float gravity = -Physics.gravity.y;
            float cosine = Mathf.Cos(angleRadians);
            float tangent = Mathf.Tan(angleRadians);

            float reachableHeight = horizontalDistance * tangent - heightDifference;
            if (reachableHeight <= 0.001f) reachableHeight = horizontalDistance * tangent;

            float speedSquared = gravity * horizontalDistance * horizontalDistance / (2f * cosine * cosine * reachableHeight);
            float speed = Mathf.Sqrt(speedSquared);
            return horizontalDirection * (speed * cosine) + Vector3.up * (speed * Mathf.Sin(angleRadians));
        }

        /// <summary>
        /// 생각한 뒤 멈추거나(멈출 수 있고 계속할 이유가 없을 때) 던진다.
        /// </summary>
        private IEnumerator TakeTurn(Participant self, bool canStop)
        {
            avatar.ShowExpression(Expression.Thinking);
            yield return new WaitForSeconds(thinkSeconds);

            bool wantsToKeepThrowing = ShouldKeepThrowing(ReadBoardFaces(), self.DiceCount);
            bool shouldStop = canStop && wantsToKeepThrowing == false;
            if (shouldStop)
            {
                avatar.ShowExpression(Expression.Worried);
                avatar.Say(personality.stopLine, lineSeconds);
                turnManager.StopTurn();
                yield break;
            }

            avatar.ShowExpression(Expression.Eager);
            avatar.Say(personality.windupLine, lineSeconds);
            yield return avatar.PlayWindup(personality.throwStyle);

            // 물리 값은 물리 프레임에 맞춰 넣는다
            yield return new WaitForFixedUpdate();
            ThrowDice(self);
        }

        /// <summary>
        /// 트레이에서 주사위 하나를 떼어 목표를 향해 던지고 TurnManager에 알린다.
        /// </summary>
        private void ThrowDice(Participant self)
        {
            Dice dice = self.Tray.TakeOne();
            Rigidbody body = dice.GetComponent<Rigidbody>();

            Vector3 startPosition = self.Tray.transform.position + Vector3.up * throwStartHeightMeters;
            Vector3 target = ChooseTarget(turnManager.BoardDice);
            Vector3 velocity = LaunchVelocity(startPosition, target, personality.launchAngleDegrees) * personality.throwSpeedScale;

            body.position = startPosition;
            body.rotation = Random.rotation;
            body.linearVelocity = velocity;
            body.angularVelocity = Random.onUnitSphere * spinSpeed;
            turnManager.OnDieThrown(dice);
        }

        private List<DiceFace> ReadBoardFaces()
        {
            List<DiceFace> boardFaces = new List<DiceFace>();
            foreach (Dice dice in turnManager.BoardDice)
            {
                boardFaces.Add(dice.ReadTopFace());
            }
            return boardFaces;
        }

        /// <summary>
        /// 판 위 주사위 중 clusterRadiusMeters 안에 이웃이 가장 많은 주사위 위치를 고른다. 판이 비었으면 가운데.
        /// </summary>
        /// <returns>가장 몰린 곳의 월드 위치 (경기장 바닥 높이)</returns>
        private Vector3 FindMostCrowdedPoint(IReadOnlyList<Dice> boardDice)
        {
            Vector3 crowdedPoint = arena.FloorCenterPoint();
            int mostNeighbors = -1;
            foreach (Dice candidate in boardDice)
            {
                int neighborCount = 0;
                foreach (Dice other in boardDice)
                {
                    float distance = Vector3.Distance(candidate.transform.position, other.transform.position);
                    if (distance <= clusterRadiusMeters) neighborCount++;
                }

                if (neighborCount <= mostNeighbors) continue;
                mostNeighbors = neighborCount;
                crowdedPoint = candidate.transform.position;
            }
            crowdedPoint.y = arena.FloorCenterPoint().y;
            return crowdedPoint;
        }

        /// <summary>
        /// 바닥 무작위 후보 점 중 판 위 주사위와의 가장 가까운 거리가 가장 큰 점(가장 빈 곳)을 고른다.
        /// </summary>
        /// <returns>가장 빈 곳의 월드 위치 (경기장 바닥 높이)</returns>
        private Vector3 FindEmptiestPoint(IReadOnlyList<Dice> boardDice)
        {
            Vector3 emptiestPoint = arena.RandomFloorPoint();
            float largestClearance = -1f;
            for (int candidateIndex = 0; candidateIndex < emptySpotCandidateCount; candidateIndex++)
            {
                Vector3 candidate = arena.RandomFloorPoint();
                float clearance = float.MaxValue;
                foreach (Dice dice in boardDice)
                {
                    float distance = Vector3.Distance(candidate, dice.transform.position);
                    if (distance < clearance) clearance = distance;
                }

                if (clearance <= largestClearance) continue;
                largestClearance = clearance;
                emptiestPoint = candidate;
            }
            return emptiestPoint;
        }
    }
}
