using TMPro;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Gravity;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

namespace StudyXR.Strike.EditorTools
{
    /// <summary>
    /// 에디터 메뉴 한 번으로 열린 씬의 XR Origin에 조작 UX(#10)를 적용한다.
    /// 왼 스틱 = 연속 이동, 오른 스틱 = 부드러운 회전으로 바꾸고, 경기장 거리 제한(<see cref="ArenaBoundary"/>)을 붙여 연결한다.
    /// 플레이어 입력에 양손 진동을 연결하고, 다시 시작 메뉴(<see cref="GameMenu"/>)와 판 위 결과 숫자(<see cref="BoardResultLabels"/>)를 만든다.
    /// 리그의 몸 충돌(CharacterController)을 빼 원형 탁자 안쪽까지 들어갈 수 있게 하고, 시작 조작법 패널(<see cref="ControlsGuide"/>)을 만든다.
    /// </summary>
    /// <remarks>
    /// 메뉴 3(탁자)·4(턴 시스템)를 먼저 실행해 씬에 <see cref="DiceTable"/>과 <see cref="PlayerTurnInput"/>이 있어야 한다. 여러 번 실행해도 컴포넌트는 하나만 붙는다.
    /// 스틱 값(속도·회전 속도)은 각 Provider 인스펙터에서 조정한다.
    /// </remarks>
    public static class ControlSetup
    {
        private const string GameMenuObjectName = "Game Menu";
        private const string BoardLabelsObjectName = "Board Result Labels";
        private const string ControlsGuideObjectName = "Controls Guide";
        private const string KoreanFontPath = "Assets/99. External Assets/Font/Noto_Sans_KR/NotoSansKR-Regular SDF.asset";

        /// <summary>
        /// XR Origin·탁자 찾기 → 스틱 모드 설정 → 거리 제한 연결.
        /// </summary>
        [MenuItem("StudyXR/MR Strike/5. 조작 UX 적용")]
        public static void ApplyControls()
        {
            XROrigin origin = Object.FindFirstObjectByType<XROrigin>();
            DiceTable table = Object.FindFirstObjectByType<DiceTable>();
            PlayerTurnInput playerInput = Object.FindFirstObjectByType<PlayerTurnInput>();
            TurnManager turnManager = Object.FindFirstObjectByType<TurnManager>();
            if (origin == null || table == null || playerInput == null || turnManager == null)
            {
                Debug.LogError("[ControlSetup] 열린 씬에 XR Origin, 원형 탁자(DiceTable), 턴 시스템(PlayerTurnInput) 중 하나가 없다. 메뉴 1·3·4를 먼저 실행한다.");
                return;
            }

            SetStickModes(origin);
            ConnectArenaBoundary(origin, table);
            ConnectPlayerHaptics(origin, playerInput);
            CreateGameMenu(origin, turnManager);
            CreateBoardResultLabels(origin, turnManager);
            FreeBodyMovement(origin);
            CreateControlsGuide(origin);

            EditorSceneManager.MarkSceneDirty(origin.gameObject.scene);
            Debug.Log("[ControlSetup] 조작 UX 적용: 왼 스틱 이동, 오른 스틱 부드러운 회전, 경기장 거리 제한, 진동, 메뉴, 결과 숫자, 탁자 안 들어가기, 조작법 패널. 씬을 저장한다.");
        }

        /// <summary>
        /// 왼손 매니저는 연속 이동을 켜고(텔레포트 대신), 오른손 매니저는 연속 이동을 끄고 부드러운 회전을 켠다.
        /// </summary>
        private static void SetStickModes(XROrigin origin)
        {
            ControllerInputActionManager[] managers = origin.GetComponentsInChildren<ControllerInputActionManager>(true);
            foreach (ControllerInputActionManager manager in managers)
            {
                bool isLeftHand = manager.gameObject.name.Contains("Left");
                SerializedObject serializedManager = new SerializedObject(manager);
                serializedManager.FindProperty("m_SmoothMotionEnabled").boolValue = isLeftHand;
                serializedManager.FindProperty("m_SmoothTurnEnabled").boolValue = isLeftHand == false;
                serializedManager.ApplyModifiedProperties();
            }
        }

        /// <summary>
        /// XR Origin 루트에 거리 제한을 붙이고 경기장·머리를 연결한다.
        /// </summary>
        private static void ConnectArenaBoundary(XROrigin origin, DiceTable table)
        {
            ArenaBoundary boundary = origin.GetComponent<ArenaBoundary>();
            if (boundary == null)
            {
                boundary = Undo.AddComponent<ArenaBoundary>(origin.gameObject);
            }

            SerializedObject serializedBoundary = new SerializedObject(boundary);
            serializedBoundary.FindProperty("arena").objectReferenceValue = table.Arena;
            serializedBoundary.FindProperty("head").objectReferenceValue = origin.Camera.transform;
            serializedBoundary.ApplyModifiedProperties();
        }

        /// <summary>
        /// 리그의 양손 HapticImpulsePlayer를 찾아 플레이어 입력의 왼손·오른손 진동에 연결한다.
        /// 왼손은 조상 오브젝트 이름에 "Left"가 들어간 쪽이다 (Starter Assets 리그 이름 규칙).
        /// </summary>
        private static void ConnectPlayerHaptics(XROrigin origin, PlayerTurnInput playerInput)
        {
            SerializedObject serializedInput = new SerializedObject(playerInput);
            HapticImpulsePlayer[] hapticPlayers = origin.GetComponentsInChildren<HapticImpulsePlayer>(true);
            foreach (HapticImpulsePlayer hapticPlayer in hapticPlayers)
            {
                string fieldName = IsUnderLeftHand(hapticPlayer.transform, origin.transform) ? "leftHandHaptics" : "rightHandHaptics";
                serializedInput.FindProperty(fieldName).objectReferenceValue = hapticPlayer;
            }
            serializedInput.ApplyModifiedProperties();
        }

        /// <summary>
        /// 왼손 메뉴 버튼으로 여는 다시 시작 메뉴를 만든다: 메뉴 오브젝트 → 꺼진 패널 → 버튼 상자(XRSimpleInteractable) + 글자.
        /// 이미 있으면 건너뛴다.
        /// </summary>
        private static void CreateGameMenu(XROrigin origin, TurnManager turnManager)
        {
            if (GameObject.Find(GameMenuObjectName) != null) return;

            const float ButtonWidthMeters = 0.12f;
            const float ButtonHeightMeters = 0.05f;
            const float ButtonThicknessMeters = 0.01f;

            GameObject menuObject = new GameObject(GameMenuObjectName);
            GameObject panel = new GameObject("Menu Panel");
            panel.transform.SetParent(menuObject.transform, false);

            GameObject button = GameObject.CreatePrimitive(PrimitiveType.Cube);
            button.name = "Restart Button";
            button.transform.SetParent(panel.transform, false);
            button.transform.localScale = new Vector3(ButtonWidthMeters, ButtonHeightMeters, ButtonThicknessMeters);
            XRSimpleInteractable restartButton = button.AddComponent<XRSimpleInteractable>();

            GameObject labelObject = new GameObject("Label");
            labelObject.transform.SetParent(panel.transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 0f, -ButtonThicknessMeters);
            TextMeshPro label = labelObject.AddComponent<TextMeshPro>();
            label.text = "다시 시작";
            label.fontSize = 0.3f;
            label.color = Color.black;
            label.alignment = TextAlignmentOptions.Center;
            label.rectTransform.sizeDelta = new Vector2(ButtonWidthMeters, ButtonHeightMeters);
            TMP_FontAsset koreanFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontPath);
            if (koreanFont != null) label.font = koreanFont;

            GameMenu menu = menuObject.AddComponent<GameMenu>();
            SerializedObject serializedMenu = new SerializedObject(menu);
            serializedMenu.FindProperty("turnManager").objectReferenceValue = turnManager;
            serializedMenu.FindProperty("panelRoot").objectReferenceValue = panel;
            serializedMenu.FindProperty("restartButton").objectReferenceValue = restartButton;
            serializedMenu.FindProperty("hand").objectReferenceValue = FindLeftController(origin);
            serializedMenu.FindProperty("head").objectReferenceValue = origin.Camera.transform;
            serializedMenu.ApplyModifiedPropertiesWithoutUndo();
            Undo.RegisterCreatedObjectUndo(menuObject, "Create Game Menu");
        }

        /// <summary>
        /// 판 위 멈춘 주사위에 윗면 숫자를 띄우는 오브젝트를 만든다. 이미 있으면 건너뛴다.
        /// </summary>
        private static void CreateBoardResultLabels(XROrigin origin, TurnManager turnManager)
        {
            if (GameObject.Find(BoardLabelsObjectName) != null) return;

            GameObject labelsObject = new GameObject(BoardLabelsObjectName);
            BoardResultLabels labels = labelsObject.AddComponent<BoardResultLabels>();
            SerializedObject serializedLabels = new SerializedObject(labels);
            serializedLabels.FindProperty("turnManager").objectReferenceValue = turnManager;
            serializedLabels.FindProperty("head").objectReferenceValue = origin.Camera.transform;
            serializedLabels.ApplyModifiedPropertiesWithoutUndo();
            Undo.RegisterCreatedObjectUndo(labelsObject, "Create Board Result Labels");
        }

        /// <summary>
        /// 리그 루트의 CharacterController를 빼고 중력을 끈다. CharacterController가 있으면 스틱 이동이 탁자 모서리에서 막혀 경기장 가까이 못 간다.
        /// 빼면 이동은 막힘 없이 몸만 옮기고, 들어갈 수 있는 범위는 <see cref="ArenaBoundary"/>의 최소·최대 거리 고리가 정한다.
        /// 중력도 끄는 까닭: CharacterController 없이 GravityProvider만 남으면 땅을 못 찾아 계속 떨어졌다 (씬 확인). 이 씬은 평평한 바닥뿐이라 중력이 필요 없다.
        /// </summary>
        /// <remarks>
        /// 리그는 Starter Assets 프리팹 인스턴스라 씬에 "컴포넌트 제거"·값 오버라이드로 남는다 (원본 프리팹은 그대로).
        /// </remarks>
        private static void FreeBodyMovement(XROrigin origin)
        {
            CharacterController body = origin.GetComponent<CharacterController>();
            if (body != null) Undo.DestroyObjectImmediate(body);

            GravityProvider gravity = origin.GetComponentInChildren<GravityProvider>(true);
            SerializedObject serializedGravity = new SerializedObject(gravity);
            serializedGravity.FindProperty("m_UseGravity").boolValue = false;
            serializedGravity.ApplyModifiedProperties();
        }

        /// <summary>
        /// 시작 조작법 패널을 만든다: 안내 오브젝트 → 꺼진 패널 → 어두운 배경 판 + 본문 글자 + 닫기 안내 글자.
        /// 본문 내용은 <see cref="ControlsGuide"/>가 Play 때 채운다. 이미 있으면 건너뛴다.
        /// </summary>
        private static void CreateControlsGuide(XROrigin origin)
        {
            if (GameObject.Find(ControlsGuideObjectName) != null) return;

            const float PanelWidthMeters = 0.5f;
            const float PanelHeightMeters = 0.6f;
            const float SkipTextHeightMeters = 0.08f;

            GameObject guideObject = new GameObject(ControlsGuideObjectName);
            GameObject panel = new GameObject("Guide Panel");
            panel.transform.SetParent(guideObject.transform, false);

            // 배경 판은 글자 뒤(+z)에 둔다. 패널 앞(-z)이 머리를 향한다
            GameObject background = GameObject.CreatePrimitive(PrimitiveType.Quad);
            background.name = "Background";
            background.transform.SetParent(panel.transform, false);
            background.transform.localPosition = new Vector3(0f, 0f, 0.005f);
            background.transform.localScale = new Vector3(PanelWidthMeters, PanelHeightMeters, 1f);
            Object.DestroyImmediate(background.GetComponent<Collider>());
            Material backgroundMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            backgroundMaterial.name = "Controls Guide Background";
            backgroundMaterial.color = new Color(0.08f, 0.08f, 0.1f);
            background.GetComponent<MeshRenderer>().sharedMaterial = backgroundMaterial;

            float bodyHeightMeters = PanelHeightMeters - SkipTextHeightMeters - 0.04f;
            TextMeshPro bodyText = CreatePanelText(panel.transform, "Body", 0.2f, TextAlignmentOptions.TopLeft,
                new Vector2(PanelWidthMeters - 0.04f, bodyHeightMeters),
                new Vector3(0f, (PanelHeightMeters - bodyHeightMeters) * 0.5f - 0.02f, 0f));
            TextMeshPro skipText = CreatePanelText(panel.transform, "Skip", 0.2f, TextAlignmentOptions.Center,
                new Vector2(PanelWidthMeters - 0.04f, SkipTextHeightMeters),
                new Vector3(0f, -(PanelHeightMeters - SkipTextHeightMeters) * 0.5f + 0.01f, 0f));
            skipText.color = new Color(1f, 0.85f, 0.3f);
            panel.SetActive(false);

            ControlsGuide guide = guideObject.AddComponent<ControlsGuide>();
            SerializedObject serializedGuide = new SerializedObject(guide);
            serializedGuide.FindProperty("panelRoot").objectReferenceValue = panel;
            serializedGuide.FindProperty("bodyText").objectReferenceValue = bodyText;
            serializedGuide.FindProperty("skipText").objectReferenceValue = skipText;
            serializedGuide.FindProperty("head").objectReferenceValue = origin.Camera.transform;
            serializedGuide.ApplyModifiedPropertiesWithoutUndo();
            Undo.RegisterCreatedObjectUndo(guideObject, "Create Controls Guide");
        }

        /// <summary>
        /// 패널 위 흰 글자 하나를 만든다. 한글 글꼴이 있으면 쓴다.
        /// </summary>
        /// <param name="parent">패널</param>
        /// <param name="name">오브젝트 이름</param>
        /// <param name="fontSize">TMP 글자 크기 (월드 기준)</param>
        /// <param name="alignment">정렬</param>
        /// <param name="sizeMeters">글자 상자 크기 (m)</param>
        /// <param name="localPosition">패널 기준 위치 (m)</param>
        /// <returns>만든 글자</returns>
        private static TextMeshPro CreatePanelText(Transform parent, string name, float fontSize, TextAlignmentOptions alignment, Vector2 sizeMeters, Vector3 localPosition)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            textObject.transform.localPosition = localPosition;
            TextMeshPro text = textObject.AddComponent<TextMeshPro>();
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = alignment;
            text.rectTransform.sizeDelta = sizeMeters;
            TMP_FontAsset koreanFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontPath);
            if (koreanFont != null) text.font = koreanFont;
            return text;
        }

        /// <summary>
        /// 추적되는 왼손 컨트롤러 오브젝트("Left Controller", Starter Assets 리그 이름)를 찾는다.
        /// </summary>
        /// <returns>왼손 컨트롤러 Transform. 없으면 null</returns>
        private static Transform FindLeftController(XROrigin origin)
        {
            foreach (Transform child in origin.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "Left Controller") return child;
            }
            return null;
        }

        private static bool IsUnderLeftHand(Transform child, Transform originRoot)
        {
            for (Transform current = child; current != originRoot; current = current.parent)
            {
                if (current.name.Contains("Left")) return true;
            }
            return false;
        }
    }
}
