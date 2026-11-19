using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StudyXR.Strike.EditorTools
{
    /// <summary>
    /// 에디터 메뉴 한 번으로 #2 경기장 에셋(물리 재질·재질)을 만들고 주사위 테이블 씬에 경기장을 놓는다.
    /// 이미 있는 에셋과 씬 오브젝트는 덮어쓰지 않는다 (튜닝한 값 보호). 모양은 DiceArena의 "경기장 다시 만들기"로 바꾼다.
    /// </summary>
    public static class ArenaSetup
    {
        private const string StudyFolder = "Assets/00. Study/07. MR Strike";
        private const string PhysicsMaterialFolder = StudyFolder + "/Physics Materials";
        private const string MaterialFolder = StudyFolder + "/Materials";
        private const string ScenePath = StudyFolder + "/00. Dice Table.unity";
        private const string ArenaObjectName = "Dice Arena";

        // 경기장이 놓일 위치 (가정). 테스트 주사위가 있는 받침대(z 0.5) 앞쪽 책상.
        private static readonly Vector3 ArenaTrayTopPosition = new Vector3(0f, 0.85f, 1.1f);

        /// <summary>
        /// 에셋 만들기 → 주사위 테이블 씬 열기 → 경기장·책상 놓기 → 저장 순서.
        /// </summary>
        [MenuItem("StudyXR/MR Strike/2. 경기장 추가")]
        public static void AddArena()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() == false) return;

            if (AssetDatabase.IsValidFolder(MaterialFolder) == false) AssetDatabase.CreateFolder(StudyFolder, "Materials");

            PhysicsMaterial floorPhysics = LoadOrCreatePhysicsMaterial("PM_ArenaFloor", 0.5f, 0.6f, 0.2f);
            PhysicsMaterial wallPhysics = LoadOrCreatePhysicsMaterial("PM_ArenaWall", 0.3f, 0.3f, 0.3f);
            Material bowlMaterial = LoadOrCreateMaterial("M_ArenaBowl", new Color(1f, 0.75f, 0.1f));
            Material marginMaterial = LoadOrCreateMaterial("M_ArenaMargin", new Color(0.45f, 0.3f, 0.15f));
            Material wallMaterial = LoadOrCreateMaterial("M_ArenaWall", new Color(0.9f, 0.5f, 0.1f));
            AssetDatabase.SaveAssets();

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (FindRoot(scene, ArenaObjectName) != null)
            {
                Debug.Log($"[ArenaSetup] '{ArenaObjectName}'가 이미 씬에 있어 건너뜀. 모양을 바꾸려면 그 오브젝트의 DiceArena 메뉴 '경기장 다시 만들기'를 쓴다.");
                return;
            }

            DiceArena arena = CreateArena(floorPhysics, wallPhysics, bowlMaterial, marginMaterial, wallMaterial);
            CreateArenaTable();
            ConnectTestKeys(arena);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[ArenaSetup] 경기장 추가: {ScenePath}");
        }

        public static PhysicsMaterial LoadOrCreatePhysicsMaterial(string assetName, float dynamicFriction, float staticFriction, float bounciness)
        {
            string path = $"{PhysicsMaterialFolder}/{assetName}.asset";
            PhysicsMaterial existing = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
            if (existing != null) return existing;

            PhysicsMaterial material = new PhysicsMaterial(assetName);
            material.dynamicFriction = dynamicFriction;
            material.staticFriction = staticFriction;
            material.bounciness = bounciness;
            material.frictionCombine = PhysicsMaterialCombine.Average;
            material.bounceCombine = PhysicsMaterialCombine.Average;
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        public static Material LoadOrCreateMaterial(string assetName, Color color)
        {
            string path = $"{MaterialFolder}/{assetName}.mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.name = assetName;
            material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        /// <summary>
        /// DiceArena를 가진 빈 오브젝트를 놓고 재질을 연결한 뒤 Build()를 한 번 실행한다.
        /// </summary>
        private static DiceArena CreateArena(PhysicsMaterial floorPhysics, PhysicsMaterial wallPhysics, Material bowlMaterial, Material marginMaterial, Material wallMaterial)
        {
            GameObject arenaObject = new GameObject(ArenaObjectName);
            arenaObject.transform.position = ArenaTrayTopPosition;
            DiceArena arena = arenaObject.AddComponent<DiceArena>();

            SerializedObject serializedArena = new SerializedObject(arena);
            serializedArena.FindProperty("bowlMaterial").objectReferenceValue = bowlMaterial;
            serializedArena.FindProperty("marginMaterial").objectReferenceValue = marginMaterial;
            serializedArena.FindProperty("wallMaterial").objectReferenceValue = wallMaterial;
            serializedArena.FindProperty("floorPhysicsMaterial").objectReferenceValue = floorPhysics;
            serializedArena.FindProperty("wallPhysicsMaterial").objectReferenceValue = wallPhysics;
            serializedArena.ApplyModifiedPropertiesWithoutUndo();

            arena.Build();
            return arena;
        }

        /// <summary>
        /// 경기장 아래 책상. 윗면이 그릇 바닥 높이(트레이 윗면 − 그릇 깊이)에 오도록 맞춘다.
        /// </summary>
        private static void CreateArenaTable()
        {
            const float tableHeight = 0.82f;
            GameObject table = GameObject.CreatePrimitive(PrimitiveType.Cube);
            table.name = "Arena Table";
            table.transform.position = new Vector3(ArenaTrayTopPosition.x, tableHeight * 0.5f, ArenaTrayTopPosition.z);
            table.transform.localScale = new Vector3(0.56f, tableHeight, 0.4f);
        }

        /// <summary>
        /// 씬의 TestDiceKeys에 경기장을 연결해 장외 표시와 바닥 무작위 점 시험이 되게 한다.
        /// </summary>
        private static void ConnectTestKeys(DiceArena arena)
        {
            TestDiceKeys testKeys = Object.FindFirstObjectByType<TestDiceKeys>();
            SerializedObject serializedKeys = new SerializedObject(testKeys);
            serializedKeys.FindProperty("arena").objectReferenceValue = arena;
            serializedKeys.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject FindRoot(Scene scene, string rootName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == rootName) return root;
            }
            return null;
        }
    }
}
