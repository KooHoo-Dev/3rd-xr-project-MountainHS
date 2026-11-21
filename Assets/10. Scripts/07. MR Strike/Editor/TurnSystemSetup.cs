using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace StudyXR.Strike.EditorTools
{
    /// <summary>
    /// 에디터 메뉴 한 번으로 열려 있는 씬의 원형 탁자에 턴 시스템(TurnManager, PlayerTurnInput, AI들)을 붙이고 연결한다.
    /// AI 성격 에셋 3개(불도저·신중이·도박사)도 만든다. 이미 있는 에셋과 "Turn System" 오브젝트는 덮어쓰지 않는다.
    /// </summary>
    public static class TurnSystemSetup
    {
        private const string StudyFolder = "Assets/00. Study/07. MR Strike";
        private const string SettingsFolder = StudyFolder + "/Settings";
        private const string SettingsPath = SettingsFolder + "/DicePhysicsSettings.asset";
        private const string TurnSystemObjectName = "Turn System";

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

            AiPersonality[] personalities =
            {
                LoadOrCreatePersonality("AI Bulldozer", ThrowStyle.Smash, 0f, 1, 15f, 1.4f, 0.08f),
                LoadOrCreatePersonality("AI Careful", ThrowStyle.Gentle, 0.7f, 3, 60f, 1f, 0.02f),
                LoadOrCreatePersonality("AI Gambler", ThrowStyle.Center, 0.4f, 2, 40f, 1.15f, 0.04f),
            };
            AssetDatabase.SaveAssets();

            DicePhysicsSettings settings = AssetDatabase.LoadAssetAtPath<DicePhysicsSettings>(SettingsPath);
            GameObject root = new GameObject(TurnSystemObjectName);
            TurnManager turnManager = root.AddComponent<TurnManager>();
            PlayerTurnInput playerInput = root.AddComponent<PlayerTurnInput>();
            AiPlayer[] aiPlayers = CreateAiPlayers(root.transform, table, turnManager, personalities);

            Connect(turnManager, table, settings, playerInput, aiPlayers);
            Connect(playerInput, turnManager);
            ConnectTestKeys(turnManager, table);

            EditorSceneManager.MarkSceneDirty(root.scene);
            Debug.Log($"[TurnSystemSetup] 턴 시스템 추가: AI {aiPlayers.Length}명. 씬을 저장한다.");
        }

        /// <summary>
        /// 탁자 자리 수 - 1명의 AI 오브젝트를 만들고 성격을 돌려 가며 연결한다 (자리가 3개를 넘으면 성격이 반복된다).
        /// </summary>
        private static AiPlayer[] CreateAiPlayers(Transform parent, DiceTable table, TurnManager turnManager, AiPersonality[] personalities)
        {
            int aiCount = table.SeatTrays.Length - 1;
            AiPlayer[] aiPlayers = new AiPlayer[aiCount];
            for (int index = 0; index < aiCount; index++)
            {
                GameObject aiObject = new GameObject($"AI {index + 1}");
                aiObject.transform.SetParent(parent);
                AiPlayer aiPlayer = aiObject.AddComponent<AiPlayer>();

                SerializedObject serializedAi = new SerializedObject(aiPlayer);
                serializedAi.FindProperty("turnManager").objectReferenceValue = turnManager;
                serializedAi.FindProperty("arena").objectReferenceValue = table.Arena;
                serializedAi.FindProperty("personality").objectReferenceValue = personalities[index % personalities.Length];
                serializedAi.ApplyModifiedPropertiesWithoutUndo();
                aiPlayers[index] = aiPlayer;
            }
            return aiPlayers;
        }

        private static void Connect(TurnManager turnManager, DiceTable table, DicePhysicsSettings settings, PlayerTurnInput playerInput, AiPlayer[] aiPlayers)
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

        private static void Connect(PlayerTurnInput playerInput, TurnManager turnManager)
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

        private static AiPersonality LoadOrCreatePersonality(
            string assetName, ThrowStyle style, float minMatchChance, int minDice, float angleDegrees, float speedScale, float aimErrorMeters)
        {
            string path = $"{SettingsFolder}/{assetName}.asset";
            AiPersonality existing = AssetDatabase.LoadAssetAtPath<AiPersonality>(path);
            if (existing != null) return existing;

            AiPersonality personality = ScriptableObject.CreateInstance<AiPersonality>();
            personality.throwStyle = style;
            personality.minMatchChanceToContinue = minMatchChance;
            personality.minDiceToKeepThrowing = minDice;
            personality.launchAngleDegrees = angleDegrees;
            personality.throwSpeedScale = speedScale;
            personality.aimErrorRadiusMeters = aimErrorMeters;
            AssetDatabase.CreateAsset(personality, path);
            return personality;
        }
    }
}
