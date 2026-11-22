using System;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Object = UnityEngine.Object;

namespace StudyXR.Strike.EditorTools
{
    /// <summary>
    /// 에디터 메뉴 한 번으로 열려 있는 씬의 원형 탁자에 턴 시스템(TurnManager, PlayerTurnInput, AI와 아바타들)을 붙이고 연결한다.
    /// AI 성격 에셋 3개(불도저·신중이·도박사)도 만든다. 이미 있는 에셋과 "Turn System" 오브젝트는 덮어쓰지 않는다.
    /// </summary>
    public static class TurnSystemSetup
    {
        private const string StudyFolder = "Assets/00. Study/07. MR Strike";
        private const string SettingsFolder = StudyFolder + "/Settings";
        private const string SettingsPath = SettingsFolder + "/DicePhysicsSettings.asset";
        private const string TurnSystemObjectName = "Turn System";
        private const string TablePlacerObjectName = "Table Placer";
        private const string StatusBoardObjectName = "Turn Status Board";
        private const string KoreanFontPath = "Assets/99. External Assets/Font/Noto_Sans_KR/NotoSansKR-Regular SDF.asset";

        /// <summary>
        /// 성격 에셋 만들기 → 씬의 탁자 찾기 → Turn System 오브젝트에 컴포넌트 붙이고 연결.
        /// </summary>
        [MenuItem("StudyXR/MR Strike/4. 턴 시스템 추가")]
        public static void AddTurnSystem()
        {
            DiceTable table = Object.FindFirstObjectByType<DiceTable>();
            if (table == null)
            {
                Debug.LogError("[TurnSystemSetup] 열린 씬에 원형 탁자(DiceTable)가 없다. 메뉴 3으로 프리팹을 만들고 씬에 놓은 뒤 다시 실행한다.");
                return;
            }

            TurnManager turnManager;
            bool isTurnSystemExisting = GameObject.Find(TurnSystemObjectName) != null;
            if (isTurnSystemExisting)
            {
                turnManager = Object.FindFirstObjectByType<TurnManager>();
                Debug.Log($"[TurnSystemSetup] '{TurnSystemObjectName}'가 이미 씬에 있어 만들기는 건너뛰고 시험 키·책상 배치만 확인한다.");
            }
            else
            {
                turnManager = CreateTurnSystem(table);
            }

            AddTestKeys(turnManager, table);
            AddTablePlacer(turnManager, table);
            AddStatusBoard(turnManager, table);
            EditorSceneManager.MarkSceneDirty(turnManager.gameObject.scene);
            Debug.Log("[TurnSystemSetup] 완료. 씬을 저장한다.");
        }

        /// <summary>
        /// 성격 에셋을 만들고 Turn System 오브젝트에 TurnManager·PlayerTurnInput·AI들을 붙여 서로 연결한다.
        /// </summary>
        /// <param name="table">씬의 원형 탁자</param>
        /// <returns>새로 만든 TurnManager</returns>
        private static TurnManager CreateTurnSystem(DiceTable table)
        {
            AiPersonality[] personalities = CreatePersonalities();
            AssetDatabase.SaveAssets();

            DicePhysicsSettings settings = AssetDatabase.LoadAssetAtPath<DicePhysicsSettings>(SettingsPath);
            GameObject root = new GameObject(TurnSystemObjectName);
            TurnManager turnManager = root.AddComponent<TurnManager>();
            PlayerTurnInput playerInput = root.AddComponent<PlayerTurnInput>();
            AiPlayer[] aiPlayers = CreateAiPlayers(root.transform, table, turnManager, personalities);

            ConnectTurnManager(turnManager, table, settings, playerInput, aiPlayers);
            ConnectPlayerInput(playerInput, turnManager);
            Debug.Log($"[TurnSystemSetup] 턴 시스템 추가: AI {aiPlayers.Length}명.");
            return turnManager;
        }

        /// <summary>
        /// 불도저(끝까지·세게)·신중이(바로 멈춤·살살)·도박사(중간) 성격 에셋을 만든다 (있으면 그대로 쓴다). 값은 (가정).
        /// </summary>
        private static AiPersonality[] CreatePersonalities()
        {
            AiPersonality bulldozer = LoadOrCreatePersonality("AI Bulldozer", personality =>
            {
                personality.throwStyle = ThrowStyle.Smash;
                personality.minMatchChanceToContinue = 0f;
                personality.minDiceToKeepThrowing = 1;
                personality.launchAngleDegrees = 30f;
                personality.throwSpeedScale = 1.1f;
                personality.aimErrorRadiusMeters = 0.06f;
                personality.headColor = new Color(0.9f, 0.25f, 0.2f);
                personality.windupLine = "다 부숴 주마!";
                personality.stopLine = "쳇, 멈춰?";
                personality.happyLine = "하하! 내 거다!";
                personality.disappointedLine = "으아, 한 번 더!";
            });
            AiPersonality careful = LoadOrCreatePersonality("AI Careful", personality =>
            {
                personality.throwStyle = ThrowStyle.Gentle;
                personality.minMatchChanceToContinue = 0.7f;
                personality.minDiceToKeepThrowing = 3;
                personality.launchAngleDegrees = 60f;
                personality.throwSpeedScale = 1f;
                personality.aimErrorRadiusMeters = 0.02f;
                personality.headColor = new Color(0.3f, 0.6f, 0.95f);
                personality.windupLine = "조심조심...";
                personality.stopLine = "여기서 멈출게요.";
                personality.happyLine = "다행이다.";
                personality.disappointedLine = "역시 무리였나...";
            });
            AiPersonality gambler = LoadOrCreatePersonality("AI Gambler", personality =>
            {
                personality.throwStyle = ThrowStyle.Center;
                personality.minMatchChanceToContinue = 0.4f;
                personality.minDiceToKeepThrowing = 2;
                personality.launchAngleDegrees = 45f;
                personality.throwSpeedScale = 1.1f;
                personality.aimErrorRadiusMeters = 0.04f;
                personality.headColor = new Color(0.95f, 0.8f, 0.2f);
                personality.windupLine = "운을 걸어 볼까!";
                personality.stopLine = "오늘은 여기까지.";
                personality.happyLine = "역시 운이 좋군!";
                personality.disappointedLine = "다음엔 꼭!";
            });
            return new[] { bulldozer, careful, gambler };
        }

        /// <summary>
        /// 탁자 자리 수 - 1명의 AI 오브젝트(와 아바타)를 만들고 성격을 돌려 가며 연결한다 (자리가 4개를 넘으면 성격이 반복된다).
        /// </summary>
        private static AiPlayer[] CreateAiPlayers(Transform parent, DiceTable table, TurnManager turnManager, AiPersonality[] personalities)
        {
            int aiCount = table.SeatTrays.Length - 1;
            AiPlayer[] aiPlayers = new AiPlayer[aiCount];
            for (int index = 0; index < aiCount; index++)
            {
                int seatNumber = index + 1;
                AiPersonality personality = personalities[index % personalities.Length];

                GameObject aiObject = new GameObject($"AI {seatNumber}");
                aiObject.transform.SetParent(parent);
                AiPlayer aiPlayer = aiObject.AddComponent<AiPlayer>();
                AiAvatar avatar = CreateAvatar(parent, table.SeatTrays[seatNumber], table, personality, seatNumber);

                SerializedObject serializedAi = new SerializedObject(aiPlayer);
                serializedAi.FindProperty("turnManager").objectReferenceValue = turnManager;
                serializedAi.FindProperty("arena").objectReferenceValue = table.Arena;
                serializedAi.FindProperty("personality").objectReferenceValue = personality;
                serializedAi.FindProperty("avatar").objectReferenceValue = avatar;
                serializedAi.ApplyModifiedPropertiesWithoutUndo();
                aiPlayers[index] = aiPlayer;
            }
            return aiPlayers;
        }

        /// <summary>
        /// 자리 트레이 뒤(탁자 바깥쪽)에 머리·얼굴·손·말풍선으로 된 AI 아바타를 만든다. 탁자 가운데를 바라본다.
        /// </summary>
        private static AiAvatar CreateAvatar(Transform parent, DiceTray tray, DiceTable table, AiPersonality personality, int seatNumber)
        {
            const float DistanceBehindTrayMeters = 0.35f;
            const float ChestHeightMeters = 0.45f;
            const float HeadRadiusMeters = 0.1f;

            Vector3 outward = tray.transform.position - table.transform.position;
            outward.y = 0f;
            outward.Normalize();

            GameObject avatarObject = new GameObject($"AI {seatNumber} Avatar");
            avatarObject.transform.SetParent(parent);
            avatarObject.transform.position = tray.transform.position + outward * DistanceBehindTrayMeters + Vector3.up * ChestHeightMeters;
            avatarObject.transform.rotation = Quaternion.LookRotation(-outward);

            GameObject head = CreateSphere("Head", avatarObject.transform, Vector3.zero, HeadRadiusMeters, personality.headColor);
            GameObject hand = CreateSphere("Hand", avatarObject.transform, new Vector3(0.14f, -0.12f, 0.15f), 0.04f, personality.headColor);
            TMP_Text face = CreateText("Face", avatarObject.transform, new Vector3(0f, 0f, HeadRadiusMeters + 0.002f), 0.6f, 0.2f);
            face.color = Color.black;
            TMP_Text bubble = CreateText("Bubble", avatarObject.transform, new Vector3(0f, 0.22f, 0f), 0.5f, 0.6f);

            AiAvatar avatar = avatarObject.AddComponent<AiAvatar>();
            SerializedObject serializedAvatar = new SerializedObject(avatar);
            serializedAvatar.FindProperty("faceText").objectReferenceValue = face;
            serializedAvatar.FindProperty("bubbleText").objectReferenceValue = bubble;
            serializedAvatar.FindProperty("hand").objectReferenceValue = hand.transform;
            serializedAvatar.ApplyModifiedPropertiesWithoutUndo();
            return avatar;
        }

        private static GameObject CreateSphere(string objectName, Transform parent, Vector3 localPosition, float radiusMeters, Color color)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = objectName;
            Object.DestroyImmediate(sphere.GetComponent<Collider>());
            sphere.transform.SetParent(parent);
            sphere.transform.localPosition = localPosition;
            sphere.transform.localScale = Vector3.one * (radiusMeters * 2f);

            Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", color);
            sphere.GetComponent<Renderer>().sharedMaterial = material;
            return sphere;
        }

        /// <summary>
        /// 월드 글자를 만든다. 아바타(탁자 가운데를 향함)의 반대쪽을 보게 돌려, 가운데에서 바라볼 때 읽힌다. 한글 글꼴이 있으면 쓴다.
        /// </summary>
        private static TMP_Text CreateText(string objectName, Transform parent, Vector3 localPosition, float fontSize, float height)
        {
            GameObject textObject = new GameObject(objectName);
            textObject.transform.SetParent(parent, false);
            textObject.transform.localPosition = localPosition;
            textObject.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            TextMeshPro text = textObject.AddComponent<TextMeshPro>();
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.rectTransform.sizeDelta = new Vector2(1f, height);
            TMP_FontAsset koreanFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontPath);
            if (koreanFont != null) text.font = koreanFont;
            return text;
        }

        private static void ConnectTurnManager(TurnManager turnManager, DiceTable table, DicePhysicsSettings settings, PlayerTurnInput playerInput, AiPlayer[] aiPlayers)
        {
            SerializedObject serializedManager = new SerializedObject(turnManager);
            serializedManager.FindProperty("table").objectReferenceValue = table;
            serializedManager.FindProperty("settings").objectReferenceValue = settings;
            serializedManager.FindProperty("playerInput").objectReferenceValue = playerInput;

            SerializedProperty aiArray = serializedManager.FindProperty("aiPlayers");
            aiArray.arraySize = aiPlayers.Length;
            for (int index = 0; index < aiPlayers.Length; index++)
            {
                aiArray.GetArrayElementAtIndex(index).objectReferenceValue = aiPlayers[index];
            }
            serializedManager.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConnectPlayerInput(PlayerTurnInput playerInput, TurnManager turnManager)
        {
            SerializedObject serializedInput = new SerializedObject(playerInput);
            serializedInput.FindProperty("turnManager").objectReferenceValue = turnManager;
            serializedInput.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// 씬에 TestTurnKeys가 없으면 만들고, TurnManager와 탁자를 연결해 Play하면 바로 한 판이 시작되게 한다 (시험 전용).
        /// </summary>
        private static void AddTestKeys(TurnManager turnManager, DiceTable table)
        {
            TestTurnKeys testKeys = Object.FindFirstObjectByType<TestTurnKeys>();
            if (testKeys == null)
            {
                testKeys = new GameObject("TestTurnKeys").AddComponent<TestTurnKeys>();
            }

            SerializedObject serializedKeys = new SerializedObject(testKeys);
            serializedKeys.FindProperty("turnManager").objectReferenceValue = turnManager;
            serializedKeys.FindProperty("table").objectReferenceValue = table;
            serializedKeys.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// 경기장 위에 차례·할 일·주사위 개수를 띄우는 안내판(TurnStatusBoard)을 없으면 만들고 연결한다.
        /// 글자는 안내판 오브젝트 앞(+Z)을 향해 읽히므로, 안내판이 머리를 바라보면 그대로 읽힌다.
        /// </summary>
        private static void AddStatusBoard(TurnManager turnManager, DiceTable table)
        {
            XROrigin origin = Object.FindFirstObjectByType<XROrigin>();
            if (origin == null)
            {
                Debug.LogWarning("[TurnSystemSetup] XR Origin이 없어 안내판(TurnStatusBoard)은 만들지 않았다.");
                return;
            }

            TurnStatusBoard board = Object.FindFirstObjectByType<TurnStatusBoard>();
            if (board == null)
            {
                GameObject boardObject = new GameObject(StatusBoardObjectName);
                TMP_Text guideText = CreateText("Guide Text", boardObject.transform, Vector3.zero, 0.4f, 0.3f);
                guideText.transform.localRotation = Quaternion.identity;
                guideText.rectTransform.sizeDelta = new Vector2(1.2f, 0.3f);
                board = boardObject.AddComponent<TurnStatusBoard>();

                SerializedObject serializedText = new SerializedObject(board);
                serializedText.FindProperty("guideText").objectReferenceValue = guideText;
                serializedText.ApplyModifiedPropertiesWithoutUndo();
            }

            SerializedObject serializedBoard = new SerializedObject(board);
            serializedBoard.FindProperty("turnManager").objectReferenceValue = turnManager;
            serializedBoard.FindProperty("table").objectReferenceValue = table;
            serializedBoard.FindProperty("head").objectReferenceValue = origin.Camera.transform;
            serializedBoard.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// 씬에 TablePlacer·TableFinder가 없으면 "Table Placer" 오브젝트에 만들고 연결한다.
        /// 머리는 XR Origin의 카메라, 면 레이캐스트는 씬의 ARRaycastManager를 쓴다 (없으면 연결하지 않아 허공 배치만 된다).
        /// </summary>
        private static void AddTablePlacer(TurnManager turnManager, DiceTable table)
        {
            XROrigin origin = Object.FindFirstObjectByType<XROrigin>();
            if (origin == null)
            {
                Debug.LogWarning("[TurnSystemSetup] XR Origin이 없어 책상 배치(TablePlacer)는 만들지 않았다.");
                return;
            }

            TablePlacer placer = Object.FindFirstObjectByType<TablePlacer>();
            TableFinder finder = Object.FindFirstObjectByType<TableFinder>();
            if (placer == null || finder == null)
            {
                GameObject placerObject = GameObject.Find(TablePlacerObjectName);
                if (placerObject == null) placerObject = new GameObject(TablePlacerObjectName);
                if (placer == null) placer = placerObject.AddComponent<TablePlacer>();
                if (finder == null) finder = placerObject.AddComponent<TableFinder>();
            }

            Transform head = origin.Camera.transform;
            ARRaycastManager raycastManager = Object.FindFirstObjectByType<ARRaycastManager>();
            if (raycastManager == null)
            {
                Debug.LogWarning("[TurnSystemSetup] 씬에 ARRaycastManager가 없다. TableFinder는 허공 배치만 한다 (진짜 책상을 쓰려면 XR Origin에 AR 컴포넌트를 둔다).");
            }

            SerializedObject serializedFinder = new SerializedObject(finder);
            serializedFinder.FindProperty("raycastManager").objectReferenceValue = raycastManager;
            serializedFinder.FindProperty("head").objectReferenceValue = head;
            serializedFinder.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject serializedPlacer = new SerializedObject(placer);
            serializedPlacer.FindProperty("tableFinder").objectReferenceValue = finder;
            serializedPlacer.FindProperty("table").objectReferenceValue = table;
            serializedPlacer.FindProperty("turnManager").objectReferenceValue = turnManager;
            serializedPlacer.FindProperty("head").objectReferenceValue = head;
            serializedPlacer.ApplyModifiedPropertiesWithoutUndo();
        }

        private static AiPersonality LoadOrCreatePersonality(string assetName, Action<AiPersonality> configure)
        {
            string path = $"{SettingsFolder}/{assetName}.asset";
            AiPersonality existing = AssetDatabase.LoadAssetAtPath<AiPersonality>(path);
            if (existing != null) return existing;

            AiPersonality personality = ScriptableObject.CreateInstance<AiPersonality>();
            configure(personality);
            AssetDatabase.CreateAsset(personality, path);
            return personality;
        }
    }
}
