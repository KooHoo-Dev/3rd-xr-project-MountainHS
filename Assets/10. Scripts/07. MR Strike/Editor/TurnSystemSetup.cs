using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
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
            if (GameObject.Find(TurnSystemObjectName) != null)
            {
                Debug.Log($"[TurnSystemSetup] '{TurnSystemObjectName}'가 이미 씬에 있어 건너뜀.");
                return;
            }

            AiPersonality[] personalities = CreatePersonalities();
            AssetDatabase.SaveAssets();

            DicePhysicsSettings settings = AssetDatabase.LoadAssetAtPath<DicePhysicsSettings>(SettingsPath);
            GameObject root = new GameObject(TurnSystemObjectName);
            TurnManager turnManager = root.AddComponent<TurnManager>();
            PlayerTurnInput playerInput = root.AddComponent<PlayerTurnInput>();
            AiPlayer[] aiPlayers = CreateAiPlayers(root.transform, table, turnManager, personalities);

            ConnectTurnManager(turnManager, table, settings, playerInput, aiPlayers);
            ConnectPlayerInput(playerInput, turnManager);
            ConnectTestKeys(turnManager, table);

            EditorSceneManager.MarkSceneDirty(root.scene);
            Debug.Log($"[TurnSystemSetup] 턴 시스템 추가: AI {aiPlayers.Length}명. 씬을 저장한다.");
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
                personality.launchAngleDegrees = 15f;
                personality.throwSpeedScale = 1.4f;
                personality.aimErrorRadiusMeters = 0.08f;
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
                personality.launchAngleDegrees = 40f;
                personality.throwSpeedScale = 1.15f;
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
        /// 씬에 TestTurnKeys가 있으면 새 TurnManager와 탁자를 연결해 바로 시험할 수 있게 한다.
        /// </summary>
        private static void ConnectTestKeys(TurnManager turnManager, DiceTable table)
        {
            TestTurnKeys testKeys = Object.FindFirstObjectByType<TestTurnKeys>();
            if (testKeys == null) return;

            SerializedObject serializedKeys = new SerializedObject(testKeys);
            serializedKeys.FindProperty("turnManager").objectReferenceValue = turnManager;
            serializedKeys.FindProperty("table").objectReferenceValue = table;
            serializedKeys.ApplyModifiedPropertiesWithoutUndo();
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
