using StudyXR.Common;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace StudyXR.Strike.EditorTools
{
    /// <summary>
    /// 에디터 메뉴 한 번으로 #1 주사위 씬과 에셋을 만든다. 씬·프리팹 YAML을 손으로 쓰지 않기 위한 도구.
    /// 이미 있는 에셋은 덮어쓰지 않고 그대로 쓴다 (튜닝한 값 보호). 다시 만들려면 해당 파일을 지우고 메뉴를 다시 실행한다.
    /// </summary>
    public static class DiceTableSetup
    {
        private const string StudyFolder = "Assets/00. Study/07. MR Strike";
        private const string PhysicsMaterialPath = StudyFolder + "/Physics Materials/PM_Dice.asset";
        private const string SettingsPath = StudyFolder + "/Settings/DicePhysicsSettings.asset";
        private const string DicePrefabPath = StudyFolder + "/Prefabs/Dice.prefab";
        private const string ScenePath = StudyFolder + "/00. Dice Table.unity";
        private const string ThrowRangeScenePath = "Assets/00. Study/04. Throw Physics/00. Throw Range.unity";

        private const int TestDiceCount = 3;

        /// <summary>
        /// 폴더 → PM_Dice → 튜닝 에셋 → 주사위 프리팹 → 씬 순서로 없는 것만 만든다.
        /// </summary>
        [MenuItem("StudyXR/MR Strike/1. 주사위 테이블 씬 만들기")]
        public static void CreateAll()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() == false) return;

            CreateFolders();
            PhysicsMaterial diceMaterial = LoadOrCreatePhysicsMaterial();
            DicePhysicsSettings settings = LoadOrCreateSettings(diceMaterial);
            LoadOrCreateDicePrefab(settings);
            CreateSceneIfMissing();
            AssetDatabase.SaveAssets();
        }

        private static void CreateFolders()
        {
            CreateFolderIfMissing("Assets/00. Study", "07. MR Strike");
            CreateFolderIfMissing(StudyFolder, "Physics Materials");
            CreateFolderIfMissing(StudyFolder, "Settings");
            CreateFolderIfMissing(StudyFolder, "Prefabs");
        }

        private static void CreateFolderIfMissing(string parent, string name)
        {
            if (AssetDatabase.IsValidFolder(parent + "/" + name)) return;
            AssetDatabase.CreateFolder(parent, name);
        }

        /// <summary>
        /// 주사위 표면 재질. 값은 (가정) — 씬에서 굴려 보고 조절한다.
        /// </summary>
        private static PhysicsMaterial LoadOrCreatePhysicsMaterial()
        {
            PhysicsMaterial existing = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(PhysicsMaterialPath);
            if (existing != null) return existing;

            PhysicsMaterial material = new PhysicsMaterial("PM_Dice");
            material.dynamicFriction = 0.4f;
            material.staticFriction = 0.5f;
            material.bounciness = 0.3f;
            material.frictionCombine = PhysicsMaterialCombine.Average;
            material.bounceCombine = PhysicsMaterialCombine.Average;
            AssetDatabase.CreateAsset(material, PhysicsMaterialPath);
            return material;
        }

        private static DicePhysicsSettings LoadOrCreateSettings(PhysicsMaterial diceMaterial)
        {
            DicePhysicsSettings existing = AssetDatabase.LoadAssetAtPath<DicePhysicsSettings>(SettingsPath);
            if (existing != null) return existing;

            DicePhysicsSettings settings = ScriptableObject.CreateInstance<DicePhysicsSettings>();
            settings.physicsMaterial = diceMaterial;
            AssetDatabase.CreateAsset(settings, SettingsPath);
            return settings;
        }

        /// <summary>
        /// 한 변 1 크기 큐브 + 면 라벨 6개 + Rigidbody + XRGrabInteractable + DiceThrow + Dice.
        /// 실제 크기는 Dice.ApplySettings가 루트 크기로 맞춘다.
        /// </summary>
        private static void LoadOrCreateDicePrefab(DicePhysicsSettings settings)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(DicePrefabPath);
            if (existing != null) return;

            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            root.name = "Dice";
            Rigidbody body = root.AddComponent<Rigidbody>();
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            root.AddComponent<XRGrabInteractable>();
            root.AddComponent<DiceThrow>();
            Dice dice = root.AddComponent<Dice>();

            Transform[] faceLabels =
            {
                CreateFaceLabel(root.transform, "1", Vector3.up, Color.black),
                CreateFaceLabel(root.transform, "2", Vector3.back, Color.black),
                CreateFaceLabel(root.transform, "3", Vector3.right, Color.black),
                CreateFaceLabel(root.transform, "4", Vector3.left, Color.black),
                CreateFaceLabel(root.transform, "5", Vector3.forward, Color.black),
                CreateFaceLabel(root.transform, "X", Vector3.down, Color.red),
            };

            SerializedObject serializedDice = new SerializedObject(dice);
            serializedDice.FindProperty("settings").objectReferenceValue = settings;
            serializedDice.FindProperty("bodyRenderer").objectReferenceValue = root.GetComponent<MeshRenderer>();
            SerializedProperty faceLabelsProperty = serializedDice.FindProperty("faceLabels");
            faceLabelsProperty.arraySize = faceLabels.Length;
            for (int i = 0; i < faceLabels.Length; i++)
            {
                faceLabelsProperty.GetArrayElementAtIndex(i).objectReferenceValue = faceLabels[i];
            }
            serializedDice.ApplyModifiedPropertiesWithoutUndo();

            root.transform.localScale = Vector3.one * settings.diceSizeMeters;
            PrefabUtility.SaveAsPrefabAsset(root, DicePrefabPath);
            Object.DestroyImmediate(root);
        }

        /// <summary>
        /// 면 바깥쪽에 글자를 붙인다. 글자는 바깥에서 읽히도록 앞(-z)이 바깥을 향한다.
        /// </summary>
        private static Transform CreateFaceLabel(Transform parent, string text, Vector3 outwardNormal, Color color)
        {
            GameObject labelObject = new GameObject("Face " + text);
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = outwardNormal * 0.51f;

            Vector3 upHint = Vector3.up;
            if (Mathf.Abs(outwardNormal.y) > 0.5f) upHint = Vector3.forward;
            labelObject.transform.localRotation = Quaternion.LookRotation(-outwardNormal, upHint);

            TextMeshPro label = labelObject.AddComponent<TextMeshPro>();
            label.text = text;
            label.fontSize = 7f;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.rectTransform.sizeDelta = Vector2.one;
            return labelObject.transform;
        }

        /// <summary>
        /// 빈 씬에 Throw Range의 XR 리그(XR Origin + Interaction Manager + Event System)와 조명을 옮겨 오고,
        /// 물리 설정·임시 바닥·주사위·테스트 키를 놓는다. Throw Range 파일은 저장하지 않으므로 바뀌지 않는다.
        /// </summary>
        /// <remarks>
        /// NewScene(Single)이 안 쓰는 에셋을 언로드하므로, 설정과 프리팹은 씬 전환 뒤에 경로로 다시 불러온다.
        /// </remarks>
        private static void CreateSceneIfMissing()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                Debug.Log($"[DiceTableSetup] 씬이 이미 있어 건너뜀: {ScenePath}");
                return;
            }

            // XR 리그와 조명을 Throw Range에서 옮겨 온다 (프리팹 연결·오버라이드 유지)
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Scene throwRange = EditorSceneManager.OpenScene(ThrowRangeScenePath, OpenSceneMode.Additive);
            GameObject xrRoot = MoveRootToScene(throwRange, "XR", scene);
            MoveRootToScene(throwRange, "Lights", scene);
            EditorSceneManager.CloseScene(throwRange, true);

            DicePhysicsSettings settings = AssetDatabase.LoadAssetAtPath<DicePhysicsSettings>(SettingsPath);
            GameObject dicePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DicePrefabPath);

            // 씬 전용 물리 값
            GameObject physicsObject = new GameObject("Physics Settings");
            physicsObject.AddComponent<SceneTimestep>();
            DiceScenePhysics scenePhysics = physicsObject.AddComponent<DiceScenePhysics>();
            SetReference(scenePhysics, "settings", settings);

            // 임시 바닥과 받침대 (경기장은 #2)
            CreateBlock("Temp Floor", new Vector3(0f, -0.05f, 1.5f), new Vector3(4f, 0.1f, 4f));
            CreateBlock("Temp Stand", new Vector3(0f, 0.4f, 0.5f), new Vector3(0.6f, 0.8f, 0.4f));

            // 받침대 위 주사위
            Dice[] testDice = new Dice[TestDiceCount];
            for (int i = 0; i < TestDiceCount; i++)
            {
                GameObject diceObject = (GameObject)PrefabUtility.InstantiatePrefab(dicePrefab, scene);
                Dice dice = diceObject.GetComponent<Dice>();
                dice.name = "Dice " + (i + 1);
                dice.transform.position = new Vector3(-0.1f + i * 0.1f, 0.85f, 0.5f);
                testDice[i] = dice;
            }

            // 테스트 키
            GameObject testObject = new GameObject("Test Dice Keys");
            TestDiceKeys testKeys = testObject.AddComponent<TestDiceKeys>();
            SerializedObject serializedKeys = new SerializedObject(testKeys);
            serializedKeys.FindProperty("settings").objectReferenceValue = settings;
            serializedKeys.FindProperty("scenePhysics").objectReferenceValue = scenePhysics;
            serializedKeys.FindProperty("throwOrigin").objectReferenceValue = xrRoot.GetComponentInChildren<Camera>(true).transform;
            SerializedProperty testDiceProperty = serializedKeys.FindProperty("testDice");
            testDiceProperty.arraySize = testDice.Length;
            for (int i = 0; i < testDice.Length; i++)
            {
                testDiceProperty.GetArrayElementAtIndex(i).objectReferenceValue = testDice[i];
            }
            serializedKeys.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[DiceTableSetup] 씬 생성: {ScenePath}");
        }

        /// <exception cref="System.InvalidOperationException">원본 씬에 그 이름의 루트가 없을 때</exception>
        private static GameObject MoveRootToScene(Scene source, string rootName, Scene target)
        {
            foreach (GameObject root in source.GetRootGameObjects())
            {
                if (root.name != rootName) continue;
                SceneManager.MoveGameObjectToScene(root, target);
                return root;
            }
            throw new System.InvalidOperationException($"{source.path}에 루트 '{rootName}'이 없습니다.");
        }

        private static void CreateBlock(string name, Vector3 position, Vector3 size)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.position = position;
            block.transform.localScale = size;
        }

        private static void SetReference(Object target, string fieldName, Object value)
        {
            SerializedObject serializedTarget = new SerializedObject(target);
            serializedTarget.FindProperty(fieldName).objectReferenceValue = value;
            serializedTarget.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
