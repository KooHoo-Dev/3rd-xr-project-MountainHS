using UnityEditor;
using UnityEngine;

namespace StudyXR.Strike.EditorTools
{
    /// <summary>
    /// 에디터 메뉴 한 번으로 #4 원형 탁자 프리팹(4인·5인)을 만든다. 가운데 경기장과 자리마다 놓인 트레이가 들어 있다.
    /// 이미 있는 프리팹과 에셋은 덮어쓰지 않는다. 다시 만들려면 프리팹 파일을 지우고 메뉴를 다시 실행한다.
    /// </summary>
    /// <remarks>
    /// 선행: 메뉴 "1. 주사위 테이블 씬 만들기"가 Dice 프리팹을 만들어 둔 상태여야 한다.
    /// 경기장 재질은 메뉴 "2. 경기장 추가"와 같은 에셋(M_Arena*, PM_Arena*)을 쓴다.
    /// </remarks>
    public static class TableSetup
    {
        private const string StudyFolder = "Assets/00. Study/07. MR Strike";
        private const string DicePrefabPath = StudyFolder + "/Prefabs/Dice.prefab";

        // ── 탁자 모양 (가정, m) ──
        private const float TableRadius = 0.65f;
        private const float TableTopThickness = 0.04f;
        private const float TableSurfaceHeight = 0.75f;
        private const float PedestalRadius = 0.08f;
        private const float BaseRadius = 0.3f;
        private const float BaseThickness = 0.03f;

        // ── 트레이 모양 (가정, m) ──
        private const float SeatDistanceFromCenter = 0.44f;
        private const float TrayWidth = 0.30f;
        private const float TrayDepth = 0.13f;
        private const float TrayPlateThickness = 0.01f;
        private const float TrayRimHeight = 0.015f;
        private const float TrayRimThickness = 0.008f;

        /// <summary>
        /// 폴더·재질 → 4인 탁자 → 5인 탁자 순서로 없는 것만 만든다.
        /// </summary>
        [MenuItem("StudyXR/MR Strike/3. 원형 탁자 프리팹 만들기")]
        public static void CreateTablePrefabs()
        {
            CreateFolderIfMissing("Physics Materials");
            CreateFolderIfMissing("Materials");
            CreateFolderIfMissing("Prefabs");

            CreateTablePrefabIfMissing(4);
            CreateTablePrefabIfMissing(5);
            AssetDatabase.SaveAssets();
        }

        private static void CreateFolderIfMissing(string name)
        {
            if (AssetDatabase.IsValidFolder(StudyFolder + "/" + name)) return;
            AssetDatabase.CreateFolder(StudyFolder, name);
        }

        /// <summary>
        /// 탁자 윗판·기둥·받침, 가운데 경기장, 자리별 트레이를 한 프리팹으로 묶어 저장한다.
        /// </summary>
        /// <param name="seatCount">앉는 사람 수 (자리 수)</param>
        /// <exception cref="System.InvalidOperationException">Dice 프리팹이 없을 때 (메뉴 1을 먼저 실행)</exception>
        private static void CreateTablePrefabIfMissing(int seatCount)
        {
            string prefabPath = $"{StudyFolder}/Prefabs/Round Table {seatCount}P.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                Debug.Log($"[TableSetup] 이미 있어 건너뜀: {prefabPath}");
                return;
            }

            Dice dicePrefab = LoadDicePrefab();
            Material topMaterial = ArenaSetup.LoadOrCreateMaterial("M_TableTop", new Color(0.55f, 0.36f, 0.2f));
            Material legMaterial = ArenaSetup.LoadOrCreateMaterial("M_TableLeg", new Color(0.3f, 0.2f, 0.12f));
            Material trayMaterial = ArenaSetup.LoadOrCreateMaterial("M_TableTray", new Color(0.12f, 0.35f, 0.25f));

            GameObject root = new GameObject($"Round Table {seatCount}P");
            DiceTable table = root.AddComponent<DiceTable>();

            CreateTableBody(root.transform, topMaterial, legMaterial);
            DiceArena arena = CreateArena(root.transform);
            DiceTray[] seatTrays = CreateSeatTrays(root.transform, seatCount, dicePrefab, trayMaterial);

            SerializedObject serializedTable = new SerializedObject(table);
            serializedTable.FindProperty("arena").objectReferenceValue = arena;
            SerializedProperty seatTraysProperty = serializedTable.FindProperty("seatTrays");
            seatTraysProperty.arraySize = seatTrays.Length;
            for (int seatIndex = 0; seatIndex < seatTrays.Length; seatIndex++)
            {
                seatTraysProperty.GetArrayElementAtIndex(seatIndex).objectReferenceValue = seatTrays[seatIndex];
            }
            serializedTable.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            Debug.Log($"[TableSetup] 생성: {prefabPath}");
        }

        private static Dice LoadDicePrefab()
        {
            GameObject dicePrefabObject = AssetDatabase.LoadAssetAtPath<GameObject>(DicePrefabPath);
            if (dicePrefabObject == null)
            {
                throw new System.InvalidOperationException($"Dice 프리팹이 없습니다: {DicePrefabPath}. 메뉴 '1. 주사위 테이블 씬 만들기'를 먼저 실행하세요.");
            }
            return dicePrefabObject.GetComponent<Dice>();
        }

        /// <summary>
        /// 윗판(윗면 높이 = TableSurfaceHeight), 가운데 기둥, 바닥 받침. 윗판과 받침만 충돌체를 둔다.
        /// </summary>
        private static void CreateTableBody(Transform parent, Material topMaterial, Material legMaterial)
        {
            float topCenterHeight = TableSurfaceHeight - TableTopThickness * 0.5f;
            CreateCylinder(parent, "Table Top", topCenterHeight, TableRadius * 2f, TableTopThickness, topMaterial, true);

            float pedestalHeight = TableSurfaceHeight - TableTopThickness - BaseThickness;
            float pedestalCenterHeight = BaseThickness + pedestalHeight * 0.5f;
            CreateCylinder(parent, "Table Pedestal", pedestalCenterHeight, PedestalRadius * 2f, pedestalHeight, legMaterial, false);

            CreateCylinder(parent, "Table Base", BaseThickness * 0.5f, BaseRadius * 2f, BaseThickness, legMaterial, true);
        }

        /// <summary>
        /// 가운데가 탁자 한가운데인 원통 하나. 기본 캡슐 충돌체는 지우고, 필요하면 볼록 메쉬 충돌체를 붙인다.
        /// </summary>
        /// <param name="centerHeight">원통 중심의 로컬 Y (m)</param>
        /// <param name="diameter">지름 (m)</param>
        /// <param name="height">높이 (m)</param>
        /// <param name="hasCollider">true = 충돌체 있음</param>
        private static void CreateCylinder(Transform parent, string name, float centerHeight, float diameter, float height, Material material, bool hasCollider)
        {
            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = name;
            cylinder.transform.SetParent(parent, false);
            cylinder.transform.localPosition = new Vector3(0f, centerHeight, 0f);
            // 유니티 기본 원통은 높이 2, 지름 1
            cylinder.transform.localScale = new Vector3(diameter, height * 0.5f, diameter);
            cylinder.GetComponent<MeshRenderer>().sharedMaterial = material;

            Object.DestroyImmediate(cylinder.GetComponent<CapsuleCollider>());
            if (hasCollider)
            {
                cylinder.AddComponent<MeshCollider>().convex = true;
            }
        }

        /// <summary>
        /// 탁자 한가운데에 경기장을 놓는다. 경기장 그릇 바닥이 탁자 윗면 높이에 오도록 올린다.
        /// </summary>
        private static DiceArena CreateArena(Transform parent)
        {
            GameObject arenaObject = new GameObject("Dice Arena");
            arenaObject.transform.SetParent(parent, false);
            DiceArena arena = arenaObject.AddComponent<DiceArena>();

            SerializedObject serializedArena = new SerializedObject(arena);
            serializedArena.FindProperty("bowlMaterial").objectReferenceValue = ArenaSetup.LoadOrCreateMaterial("M_ArenaBowl", new Color(1f, 0.75f, 0.1f));
            serializedArena.FindProperty("marginMaterial").objectReferenceValue = ArenaSetup.LoadOrCreateMaterial("M_ArenaMargin", new Color(0.45f, 0.3f, 0.15f));
            serializedArena.FindProperty("wallMaterial").objectReferenceValue = ArenaSetup.LoadOrCreateMaterial("M_ArenaWall", new Color(0.9f, 0.5f, 0.1f));
            serializedArena.FindProperty("floorPhysicsMaterial").objectReferenceValue = ArenaSetup.LoadOrCreatePhysicsMaterial("PM_ArenaFloor", 0.5f, 0.6f, 0.2f);
            serializedArena.FindProperty("wallPhysicsMaterial").objectReferenceValue = ArenaSetup.LoadOrCreatePhysicsMaterial("PM_ArenaWall", 0.3f, 0.3f, 0.3f);
            serializedArena.ApplyModifiedPropertiesWithoutUndo();

            float bowlDepth = serializedArena.FindProperty("bowlDepthMeters").floatValue;
            arenaObject.transform.localPosition = new Vector3(0f, TableSurfaceHeight + bowlDepth, 0f);
            arena.Build();
            return arena;
        }

        /// <summary>
        /// 탁자 가장자리에 자리 수만큼 트레이를 둘러놓는다. 0번 자리는 -Z쪽(플레이어)이고 시계 방향으로 늘어난다.
        /// 트레이는 경기장 쪽을 바라보며, 긴 변이 탁자 둘레 방향이다.
        /// </summary>
        /// <returns>자리 번호 순서의 트레이들</returns>
        private static DiceTray[] CreateSeatTrays(Transform parent, int seatCount, Dice dicePrefab, Material trayMaterial)
        {
            DiceTray[] seatTrays = new DiceTray[seatCount];
            for (int seatIndex = 0; seatIndex < seatCount; seatIndex++)
            {
                float angleDegrees = 360f * seatIndex / seatCount;
                Vector3 outwardDirection = Quaternion.Euler(0f, angleDegrees, 0f) * Vector3.back;

                GameObject trayObject = new GameObject($"Seat {seatIndex} Tray");
                trayObject.transform.SetParent(parent, false);
                trayObject.transform.localPosition = outwardDirection * SeatDistanceFromCenter + Vector3.up * (TableSurfaceHeight + TrayPlateThickness);
                trayObject.transform.localRotation = Quaternion.LookRotation(-outwardDirection, Vector3.up);

                DiceTray tray = trayObject.AddComponent<DiceTray>();
                SerializedObject serializedTray = new SerializedObject(tray);
                serializedTray.FindProperty("dicePrefab").objectReferenceValue = dicePrefab;
                serializedTray.ApplyModifiedPropertiesWithoutUndo();

                CreateTrayPlateAndRim(trayObject.transform, trayMaterial);
                seatTrays[seatIndex] = tray;
            }
            return seatTrays;
        }

        /// <summary>
        /// 트레이 바닥판 하나와 주사위가 굴러 나가지 않게 막는 낮은 테두리 네 개.
        /// 트레이 오브젝트 원점이 바닥판 윗면이다.
        /// </summary>
        private static void CreateTrayPlateAndRim(Transform tray, Material trayMaterial)
        {
            CreateBox(tray, "Plate", new Vector3(0f, -TrayPlateThickness * 0.5f, 0f), new Vector3(TrayWidth, TrayPlateThickness, TrayDepth), trayMaterial);

            float rimCenterHeight = TrayRimHeight * 0.5f;
            float rimOffsetZ = (TrayDepth - TrayRimThickness) * 0.5f;
            float rimOffsetX = (TrayWidth - TrayRimThickness) * 0.5f;
            float sideRimLength = TrayDepth - TrayRimThickness * 2f;

            CreateBox(tray, "Rim Near", new Vector3(0f, rimCenterHeight, -rimOffsetZ), new Vector3(TrayWidth, TrayRimHeight, TrayRimThickness), trayMaterial);
            CreateBox(tray, "Rim Far", new Vector3(0f, rimCenterHeight, rimOffsetZ), new Vector3(TrayWidth, TrayRimHeight, TrayRimThickness), trayMaterial);
            CreateBox(tray, "Rim Left", new Vector3(-rimOffsetX, rimCenterHeight, 0f), new Vector3(TrayRimThickness, TrayRimHeight, sideRimLength), trayMaterial);
            CreateBox(tray, "Rim Right", new Vector3(rimOffsetX, rimCenterHeight, 0f), new Vector3(TrayRimThickness, TrayRimHeight, sideRimLength), trayMaterial);
        }

        private static void CreateBox(Transform parent, string name, Vector3 localCenter, Vector3 size, Material material)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = localCenter;
            box.transform.localScale = size;
            box.GetComponent<MeshRenderer>().sharedMaterial = material;
        }
    }
}
