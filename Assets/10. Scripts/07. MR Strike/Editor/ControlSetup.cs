using TMPro;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

namespace StudyXR.Strike.EditorTools
{
    /// <summary>
    /// 에디터 메뉴 한 번으로 열린 씬의 XR Origin에 조작 UX(#10)를 적용한다.
    /// 왼 스틱 = 연속 이동, 오른 스틱 = 부드러운 회전으로 바꾸고, 경기장 거리 제한(<see cref="ArenaBoundary"/>)을 붙여 연결한다.
    /// 플레이어 입력에 양손 진동을 연결하고, 다시 시작 메뉴(<see cref="GameMenu"/>)를 만든다.
    /// </summary>
    /// <remarks>
    /// 메뉴 3(탁자)·4(턴 시스템)를 먼저 실행해 씬에 <see cref="DiceTable"/>과 <see cref="PlayerTurnInput"/>이 있어야 한다. 여러 번 실행해도 컴포넌트는 하나만 붙는다.
    /// 스틱 값(속도·회전 속도)은 각 Provider 인스펙터에서 조정한다.
    /// </remarks>
    public static class ControlSetup
    {
        private const string GameMenuObjectName = "Game Menu";
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

            EditorSceneManager.MarkSceneDirty(origin.gameObject.scene);
            Debug.Log("[ControlSetup] 조작 UX 적용: 왼 스틱 이동, 오른 스틱 부드러운 회전, 경기장 거리 제한, 진동, 메뉴. 씬을 저장한다.");
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
