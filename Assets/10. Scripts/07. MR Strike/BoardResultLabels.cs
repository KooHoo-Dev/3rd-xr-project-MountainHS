using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace StudyXR.Strike
{
    /// <summary>
    /// 판 위에 멈춘 주사위마다 윗면 숫자를 주사위 위에 띄운다. 멀리서나 비스듬히 봐도 결과를 바로 읽게 하는 보조 표시다.
    /// 판 위 주사위 목록은 <see cref="TurnManager.BoardDice"/>를 읽기만 하고, 판정에는 관여하지 않는다.
    /// </summary>
    /// <remarks>
    /// 트레이 주사위에는 띄우지 않는다 (판 위 주사위만 결과다). 굴러가는 동안은 숨기고, 멈추면 보인다.
    /// 글자는 필요한 만큼 만들어 두고 다시 쓴다. 글자는 늘 머리를 바라본다.
    /// </remarks>
    public class BoardResultLabels : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private TurnManager turnManager;

        [Tooltip("글자가 바라볼 머리 (Main Camera)")]
        [SerializeField] private Transform head;

        [Header("표시 (가정)")]
        [Tooltip("주사위 중심에서 글자까지 높이 (m)")]
        [SerializeField] private float heightAboveDiceMeters = 0.04f;
        [Tooltip("글자 크기 (TextMeshPro fontSize)")]
        [SerializeField] private float fontSize = 0.25f;
        [SerializeField] private Color numberColor = Color.white;
        [SerializeField] private Color xColor = new Color(1f, 0.35f, 0.3f);

        // 만들어 둔 글자들. 앞에서부터 판 위 주사위 순서대로 쓰고 남는 것은 끈다.
        private readonly List<TextMeshPro> labels = new List<TextMeshPro>();

        /// <summary>
        /// 주사위가 이번 프레임 움직인 뒤 글자를 따라 붙인다.
        /// </summary>
        private void LateUpdate()
        {
            IReadOnlyList<Dice> boardDice = turnManager.BoardDice;
            int usedLabelCount = 0;
            foreach (Dice dice in boardDice)
            {
                if (dice.IsResting == false) continue;

                ShowLabel(GetOrCreateLabel(usedLabelCount), dice);
                usedLabelCount++;
            }
            HideLabelsFrom(usedLabelCount);
        }

        /// <summary>
        /// 글자를 주사위 위에 놓고 윗면 값을 쓰고 머리를 바라보게 한다.
        /// </summary>
        /// <param name="label">쓸 글자</param>
        /// <param name="dice">멈춘 판 위 주사위</param>
        private void ShowLabel(TextMeshPro label, Dice dice)
        {
            DiceFace topFace = dice.ReadTopFace();
            label.text = FaceText(topFace);
            label.color = topFace == DiceFace.X ? xColor : numberColor;
            label.transform.position = dice.transform.position + Vector3.up * heightAboveDiceMeters;
            label.transform.rotation = Quaternion.LookRotation(label.transform.position - head.position, Vector3.up);
            label.gameObject.SetActive(true);
        }

        /// <summary>
        /// 면을 글자로 바꾼다. One~Five = "1"~"5", X = "X".
        /// </summary>
        private static string FaceText(DiceFace face)
        {
            if (face == DiceFace.X) return "X";

            int faceNumber = (int)face + 1;
            return faceNumber.ToString();
        }

        private TextMeshPro GetOrCreateLabel(int index)
        {
            if (index < labels.Count) return labels[index];

            GameObject labelObject = new GameObject($"Result Label {index}");
            labelObject.transform.SetParent(transform, false);
            TextMeshPro label = labelObject.AddComponent<TextMeshPro>();
            label.fontSize = fontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.rectTransform.sizeDelta = new Vector2(0.1f, 0.06f);
            labelObject.AddComponent<TextBackPanel>();
            labels.Add(label);
            return label;
        }

        private void HideLabelsFrom(int firstUnusedIndex)
        {
            for (int index = firstUnusedIndex; index < labels.Count; index++)
            {
                labels[index].gameObject.SetActive(false);
            }
        }
    }
}
