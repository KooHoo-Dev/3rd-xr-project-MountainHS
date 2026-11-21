using System.Collections;
using TMPro;
using UnityEngine;

namespace StudyXR.Strike
{
    /// <summary>
    /// AI 얼굴 표정.
    /// </summary>
    public enum Expression
    {
        Neutral,
        Thinking,
        Eager,
        Happy,
        Disappointed,
        Worried,
    }

    /// <summary>
    /// AI의 겉모습. 구체 머리 위 얼굴 글자(표정), 손 구체(예고 동작), 말풍선으로 플레이어가 성격을 알아차리게 한다.
    /// 판단은 하지 않는다. <see cref="AiPlayer"/>가 언제 어떤 표정·동작·대사를 보일지 정해 부른다.
    /// </summary>
    /// <remarks>
    /// 구조(머리·얼굴·손·말풍선)는 메뉴 "4. 턴 시스템 추가"가 만든다. 얼굴은 간단히 글자 이모티콘으로 보인다 (가정).
    /// </remarks>
    public class AiAvatar : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private TMP_Text faceText;
        [SerializeField] private TMP_Text bubbleText;
        [Tooltip("예고 동작에서 움직이는 손")]
        [SerializeField] private Transform hand;

        [Header("예고 동작 (가정)")]
        [Tooltip("예고 동작 한 번의 길이 (초)")]
        [SerializeField] private float windupSeconds = 0.8f;
        [Tooltip("손을 들어 올리는 높이 (m)")]
        [SerializeField] private float windupHeightMeters = 0.2f;

        // 손의 기본 위치 (로컬). 예고 동작이 끝나면 여기로 돌아온다.
        private Vector3 restHandPosition;
        private Coroutine bubbleRoutine;

        private void Awake()
        {
            restHandPosition = hand.localPosition;
            bubbleText.text = "";
            ShowExpression(Expression.Neutral);
        }

        /// <summary>
        /// 얼굴 글자를 표정에 맞게 바꾼다.
        /// </summary>
        /// <param name="expression">보일 표정</param>
        public void ShowExpression(Expression expression)
        {
            switch (expression)
            {
                case Expression.Thinking: faceText.text = "o_O ?"; break;
                case Expression.Eager: faceText.text = ">w<"; break;
                case Expression.Happy: faceText.text = "^_^"; break;
                case Expression.Disappointed: faceText.text = "T_T"; break;
                case Expression.Worried: faceText.text = ";_;"; break;
                default: faceText.text = "o_o"; break;
            }
        }

        /// <summary>
        /// 던지기 전 예고 동작. 스타일마다 다르게 움직여 어떤 던지기를 할지 미리 보여 준다.
        /// Smash = 손을 번쩍 들었다 내려찍기, Gentle = 작게 천천히 흔들기, Center = 손으로 원 그리기.
        /// </summary>
        /// <param name="style">AI의 던지기 방식</param>
        public IEnumerator PlayWindup(ThrowStyle style)
        {
            float elapsedSeconds = 0f;
            while (elapsedSeconds < windupSeconds)
            {
                elapsedSeconds += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsedSeconds / windupSeconds);
                hand.localPosition = restHandPosition + WindupOffset(style, progress);
                yield return null;
            }
            ResetPose();
        }

        /// <summary>
        /// 말풍선에 대사를 띄우고 일정 시간 뒤 지운다. 이미 떠 있으면 새 대사로 바꾼다.
        /// </summary>
        /// <param name="line">대사 (빈 문자열이면 아무것도 하지 않는다)</param>
        /// <param name="seconds">보여 줄 시간 (초)</param>
        public void Say(string line, float seconds)
        {
            if (string.IsNullOrEmpty(line)) return;

            if (bubbleRoutine != null) StopCoroutine(bubbleRoutine);
            bubbleRoutine = StartCoroutine(ShowBubble(line, seconds));
        }

        /// <summary>
        /// 손을 기본 위치로 돌린다. 예고 동작이 중간에 끊겼을 때도 부른다.
        /// </summary>
        public void ResetPose()
        {
            hand.localPosition = restHandPosition;
        }

        private IEnumerator ShowBubble(string line, float seconds)
        {
            bubbleText.text = line;
            yield return new WaitForSeconds(seconds);
            bubbleText.text = "";
        }

        /// <summary>
        /// 예고 동작에서 기본 위치로부터 손이 떨어진 정도.
        /// </summary>
        /// <param name="style">던지기 방식</param>
        /// <param name="progress">동작 진행도 (0~1)</param>
        /// <returns>로컬 기준 손 위치 변화량 (m)</returns>
        private Vector3 WindupOffset(ThrowStyle style, float progress)
        {
            switch (style)
            {
                case ThrowStyle.Smash:
                    // 앞 70%는 위로 번쩍, 뒤 30%는 빠르게 내려찍기
                    float rise = progress < 0.7f ? progress / 0.7f : 1f - (progress - 0.7f) / 0.3f;
                    return Vector3.up * (windupHeightMeters * rise);
                case ThrowStyle.Gentle:
                    return Vector3.up * (Mathf.Sin(progress * Mathf.PI * 2f) * windupHeightMeters * 0.2f);
                default:
                    float angle = progress * Mathf.PI * 2f;
                    return new Vector3(Mathf.Cos(angle) - 1f, Mathf.Sin(angle), 0f) * (windupHeightMeters * 0.4f);
            }
        }
    }
}
