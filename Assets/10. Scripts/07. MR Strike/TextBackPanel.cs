using TMPro;
using UnityEngine;

namespace StudyXR.Strike
{
    /// <summary>
    /// 월드 글자(TextMeshPro) 뒤에 시작 조작법 패널(<see cref="ControlsGuide"/>)과 같은 검은 판을 깔아 어디서든 글자가 잘 읽히게 한다.
    /// 판은 매 프레임 실제 글자 영역(textBounds)에 여백만 더한 크기로 맞추고, 글자 영역 한가운데에 놓는다. 글자가 비면 판도 숨긴다.
    /// </summary>
    /// <remarks>
    /// 글자를 가진 쪽(<see cref="TurnStatusBoard"/>, <see cref="AiAvatar"/> 말풍선, <see cref="BoardResultLabels"/>)이 Play 때 AddComponent로 붙인다.
    /// 글자 내용은 그쪽 LateUpdate가 바꾸므로, 이 클래스는 그 뒤에 돌아 같은 프레임의 글자에 맞춘다 (DefaultExecutionOrder).
    /// </remarks>
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(TMP_Text))]
    public class TextBackPanel : MonoBehaviour
    {
        // 글자 둘레 여백 (글자 로컬 단위 = m)
        private const float PaddingMeters = 0.015f;
        // 판을 글자 뒤(+z)로 띄우는 거리 (m). 글자 앞(-z)이 보는 사람을 향한다
        private const float BehindTextMeters = 0.002f;
        // 시작 조작법 패널 배경과 같은 색
        private static readonly Color PanelColor = new Color(0.08f, 0.08f, 0.1f);

        // 모든 판이 같이 쓰는 재질. 처음 만들 때 한 번 만든다
        private static Material panelMaterial;

        private TMP_Text text;
        private Transform panel;

        /// <summary>
        /// 글자 자식으로 충돌체 없는 검은 사각 판을 만든다.
        /// </summary>
        private void Awake()
        {
            text = GetComponent<TMP_Text>();

            GameObject panelObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            panelObject.name = "Back Panel";
            Destroy(panelObject.GetComponent<Collider>());
            panelObject.GetComponent<MeshRenderer>().sharedMaterial = PanelMaterial();
            panel = panelObject.transform;
            panel.SetParent(transform, false);
        }

        /// <summary>
        /// 이번 프레임 글자에 맞춰 판의 크기와 위치를 바꾼다.
        /// </summary>
        private void LateUpdate()
        {
            text.ForceMeshUpdate();
            bool isTextEmpty = string.IsNullOrEmpty(text.text);
            panel.gameObject.SetActive(isTextEmpty == false);
            if (isTextEmpty) return;

            FitToText(text.textBounds);
        }

        /// <summary>
        /// 판을 글자 영역 한가운데에 놓고, 글자 영역 + 사방 여백 크기로 늘린다.
        /// </summary>
        /// <param name="textBounds">글자 로컬 기준 실제 글자 영역 (m)</param>
        private void FitToText(Bounds textBounds)
        {
            panel.localPosition = new Vector3(textBounds.center.x, textBounds.center.y, BehindTextMeters);
            panel.localScale = new Vector3(textBounds.size.x + PaddingMeters * 2f, textBounds.size.y + PaddingMeters * 2f, 1f);
        }

        private static Material PanelMaterial()
        {
            if (panelMaterial != null) return panelMaterial;

            panelMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            panelMaterial.name = "Text Back Panel";
            panelMaterial.color = PanelColor;
            return panelMaterial;
        }
    }
}
