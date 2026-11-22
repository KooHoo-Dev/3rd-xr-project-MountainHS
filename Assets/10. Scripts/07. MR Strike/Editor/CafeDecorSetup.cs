using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace StudyXR.Strike.EditorTools
{
    /// <summary>
    /// 에디터 메뉴 한 번으로 주사위 테이블 씬에 밤 skybox와 따뜻한 앰비언트를 넣고, 탁자 둘레를 보드게임 카페처럼 꾸민다.
    /// 꾸밈은 모두 'Cafe Decor' 루트 아래 프리미티브이고 충돌체가 없다 (주사위·플레이어 물리에 끼어들지 않음).
    /// 이미 있는 재질과 'Cafe Decor'는 덮어쓰지 않는다. 다시 만들려면 'Cafe Decor'를 지우고 메뉴를 다시 실행한다.
    /// </summary>
    /// <remarks>
    /// 가운데 원형 탁자(반지름 약 0.75m)와 플레이어가 서는 자리(탁자에서 1.2m 안)는 비워 둔다.
    /// 꾸밈의 중심은 씬에 있는 DiceTable 위치다. 없으면 원점을 쓴다.
    /// </remarks>
    public static class CafeDecorSetup
    {
        private const string StudyFolder = "Assets/00. Study/07. MR Strike";
        private const string MaterialFolder = StudyFolder + "/Materials";
        private const string CafeMaterialFolder = MaterialFolder + "/Cafe";
        private const string ScenePath = StudyFolder + "/00. Dice Table.unity";
        private const string SkyboxTexturePath = "Assets/99. External Assets/Skybox/lakeside_night_2k.exr";
        private const string DecorRootName = "Cafe Decor";

        // ── 방 크기 (가정, m). 중심 = 탁자 ──
        private const float RoomHalfSize = 4f;
        private const float WallHeight = 3f;
        private const float WallThickness = 0.12f;
        private const float LowWallHeight = 1.1f;
        private const float WindowBottomHeight = 0.9f;
        private const float WindowTopHeight = 2.4f;
        private const float WindowWidth = 4.2f;

        // ── 비워 둘 자리 (가정, m) ──
        // 탁자 반지름 0.75 + 플레이어 자리 1.2 = 1.95. 러그는 그 안쪽을 덮지만 바닥이라 걸리지 않는다.
        private const float RugRadius = 2.2f;

        // ── 조명 (가정) ──
        private static readonly Color WarmLightColor = new Color(1f, 0.72f, 0.42f);
        private const float PendantHangHeight = 2.15f;

        /// <summary>
        /// 재질 만들기 → 주사위 테이블 씬 열기 → skybox·앰비언트 → 카페 꾸밈 → 저장 순서.
        /// </summary>
        [MenuItem("StudyXR/MR Strike/7. 보드게임 카페 꾸미기")]
        public static void DecorateCafe()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() == false) return;

            if (AssetDatabase.IsValidFolder(MaterialFolder) == false) AssetDatabase.CreateFolder(StudyFolder, "Materials");
            if (AssetDatabase.IsValidFolder(CafeMaterialFolder) == false) AssetDatabase.CreateFolder(MaterialFolder, "Cafe");

            CafeMaterials materials = LoadOrCreateCafeMaterials();
            Material skyboxMaterial = LoadOrCreateSkyboxMaterial();
            AssetDatabase.SaveAssets();

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ApplySkyboxAndAmbient(skyboxMaterial);

            if (FindRoot(scene, DecorRootName) != null)
            {
                Debug.Log($"[CafeDecorSetup] '{DecorRootName}'가 이미 씬에 있어 꾸밈은 건너뜀 (skybox·앰비언트만 다시 적용). 다시 만들려면 그 오브젝트를 지우고 메뉴를 다시 실행한다.");
            }
            else
            {
                CreateDecor(materials);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[CafeDecorSetup] 보드게임 카페 꾸미기 완료: {ScenePath}");
        }

        /// <summary>
        /// 카페 꾸밈에 쓰는 재질 묶음. 만들 때 한 번에 모아 두고 넘겨준다.
        /// </summary>
        private class CafeMaterials
        {
            public Material floorWood;
            public Material floorPlankLine;
            public Material rug;
            public Material wall;
            public Material wallTrim;
            public Material furnitureWood;
            public Material darkWood;
            public Material lampShade;
            public Material lampBulb;
            public Material cord;
            public Material[] gameBoxes;
        }

        private static CafeMaterials LoadOrCreateCafeMaterials()
        {
            CafeMaterials materials = new CafeMaterials();
            materials.floorWood = LoadOrCreateCafeMaterial("M_CafeFloorWood", new Color(0.52f, 0.34f, 0.2f), 0.35f);
            materials.floorPlankLine = LoadOrCreateCafeMaterial("M_CafeFloorPlankLine", new Color(0.32f, 0.2f, 0.11f), 0.2f);
            materials.rug = LoadOrCreateCafeMaterial("M_CafeRug", new Color(0.55f, 0.14f, 0.12f), 0.05f);
            materials.wall = LoadOrCreateCafeMaterial("M_CafeWall", new Color(0.86f, 0.78f, 0.64f), 0.1f);
            materials.wallTrim = LoadOrCreateCafeMaterial("M_CafeWallTrim", new Color(0.24f, 0.3f, 0.22f), 0.3f);
            materials.furnitureWood = LoadOrCreateCafeMaterial("M_CafeFurnitureWood", new Color(0.62f, 0.42f, 0.24f), 0.4f);
            materials.darkWood = LoadOrCreateCafeMaterial("M_CafeDarkWood", new Color(0.28f, 0.18f, 0.1f), 0.35f);
            materials.lampShade = LoadOrCreateCafeMaterial("M_CafeLampShade", new Color(0.12f, 0.12f, 0.1f), 0.6f);
            materials.lampBulb = LoadOrCreateEmissiveMaterial("M_CafeLampBulb", WarmLightColor, 2.5f);
            materials.cord = LoadOrCreateCafeMaterial("M_CafeCord", new Color(0.08f, 0.08f, 0.08f), 0.2f);

            Color[] boxColors =
            {
                new Color(0.8f, 0.2f, 0.18f),
                new Color(0.18f, 0.42f, 0.75f),
                new Color(0.95f, 0.75f, 0.2f),
                new Color(0.25f, 0.6f, 0.3f),
                new Color(0.55f, 0.3f, 0.65f),
                new Color(0.95f, 0.5f, 0.2f),
                new Color(0.9f, 0.88f, 0.82f),
                new Color(0.15f, 0.15f, 0.18f),
            };
            materials.gameBoxes = new Material[boxColors.Length];
            for (int colorIndex = 0; colorIndex < boxColors.Length; colorIndex++)
            {
                materials.gameBoxes[colorIndex] = LoadOrCreateCafeMaterial($"M_CafeGameBox{colorIndex}", boxColors[colorIndex], 0.45f);
            }
            return materials;
        }

        /// <summary>
        /// Materials/Cafe 폴더의 URP Lit 재질. 있으면 그대로 쓴다 (튜닝한 값 보호).
        /// </summary>
        /// <param name="smoothness">0 = 거칠게, 1 = 반들반들</param>
        private static Material LoadOrCreateCafeMaterial(string assetName, Color color, float smoothness)
        {
            string path = $"{CafeMaterialFolder}/{assetName}.mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.name = assetName;
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        /// <summary>
        /// 스스로 빛나는 전구 재질. 실제 빛은 옆에 둔 Point Light가 낸다.
        /// </summary>
        /// <param name="intensity">발광 세기 배수</param>
        private static Material LoadOrCreateEmissiveMaterial(string assetName, Color color, float intensity)
        {
            string path = $"{CafeMaterialFolder}/{assetName}.mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.name = assetName;
            material.SetColor("_BaseColor", color);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * intensity);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        /// <summary>
        /// 밤 호숫가 skybox 재질. 텍스처가 큐브맵으로 임포트되어 있어 Skybox/Cubemap을 쓴다.
        /// 텍스처를 못 찾으면 따뜻한 Procedural skybox로 대신한다.
        /// </summary>
        private static Material LoadOrCreateSkyboxMaterial()
        {
            string path = $"{CafeMaterialFolder}/M_CafeSkybox.mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            Material material;
            Cubemap cubemap = AssetDatabase.LoadAssetAtPath<Cubemap>(SkyboxTexturePath);
            if (cubemap != null)
            {
                material = new Material(Shader.Find("Skybox/Cubemap"));
                material.SetTexture("_Tex", cubemap);
                material.SetFloat("_Exposure", 1f);
                material.SetFloat("_Rotation", 0f);
            }
            else
            {
                Debug.LogWarning($"[CafeDecorSetup] 큐브맵을 못 찾아 Procedural skybox로 대신함: {SkyboxTexturePath}");
                material = new Material(Shader.Find("Skybox/Procedural"));
                material.SetColor("_SkyTint", new Color(0.75f, 0.55f, 0.45f));
                material.SetColor("_GroundColor", new Color(0.3f, 0.22f, 0.18f));
                material.SetFloat("_AtmosphereThickness", 1.4f);
                material.SetFloat("_Exposure", 0.9f);
            }
            material.name = "M_CafeSkybox";
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        /// <summary>
        /// skybox를 바꾸고, 앰비언트는 skybox 대신 따뜻한 3색 그라데이션으로 둔다 (밤 하늘의 푸른 빛이 실내를 물들이지 않게).
        /// </summary>
        private static void ApplySkyboxAndAmbient(Material skyboxMaterial)
        {
            RenderSettings.skybox = skyboxMaterial;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.45f, 0.36f);
            RenderSettings.ambientEquatorColor = new Color(0.42f, 0.32f, 0.24f);
            RenderSettings.ambientGroundColor = new Color(0.2f, 0.14f, 0.1f);
            DynamicGI.UpdateEnvironment();
        }

        /// <summary>
        /// 탁자 위치를 중심으로 바닥·러그·벽·선반·조명·옆 테이블을 놓는다.
        /// </summary>
        private static void CreateDecor(CafeMaterials materials)
        {
            GameObject root = new GameObject(DecorRootName);
            root.transform.position = FindTableFloorPosition();
            Transform parent = root.transform;

            CreateFloorAndRug(parent, materials);
            CreateWalls(parent, materials);
            CreateShelf(parent, materials, new Vector3(RoomHalfSize - 0.25f, 0f, 0.4f), -90f);
            CreatePendantLight(parent, materials, "Pendant Center", new Vector3(0f, PendantHangHeight, 0f), 2.2f, 4.5f);
            CreateSideTableSet(parent, materials, "Side Table A", new Vector3(-2.7f, 0f, 2.5f));
            CreateSideTableSet(parent, materials, "Side Table B", new Vector3(2.3f, 0f, -2.6f));
            CreateSideTableSet(parent, materials, "Side Table C", new Vector3(-2.7f, 0f, -2.5f));
            CreateCeilingBeams(parent, materials);
        }

        /// <summary>
        /// 씬의 DiceTable 바닥 위치 (y = 0). 탁자가 없으면 원점.
        /// </summary>
        private static Vector3 FindTableFloorPosition()
        {
            DiceTable table = Object.FindFirstObjectByType<DiceTable>();
            if (table == null)
            {
                Debug.LogWarning("[CafeDecorSetup] 씬에 DiceTable이 없어 원점을 중심으로 꾸민다.");
                return Vector3.zero;
            }
            Vector3 tablePosition = table.transform.position;
            return new Vector3(tablePosition.x, 0f, tablePosition.z);
        }

        /// <summary>
        /// 나무 바닥(널빤지 줄 포함)과 탁자 아래 둥근 러그. 바닥은 기존 Temp Floor 바로 위에 얇게 깐다.
        /// </summary>
        private static void CreateFloorAndRug(Transform parent, CafeMaterials materials)
        {
            const float floorThickness = 0.02f;
            const float floorTopHeight = 0.003f;
            float roomSize = RoomHalfSize * 2f;
            CreateBox(parent, "Wood Floor", new Vector3(0f, floorTopHeight - floorThickness * 0.5f, 0f), new Vector3(roomSize, floorThickness, roomSize), materials.floorWood);

            // 널빤지 사이 홈: 바닥 위에 얇고 어두운 줄을 0.4m 간격으로 깐다.
            const float plankWidth = 0.4f;
            GameObject plankLines = new GameObject("Plank Lines");
            plankLines.transform.SetParent(parent, false);
            for (float x = -RoomHalfSize + plankWidth; x < RoomHalfSize; x += plankWidth)
            {
                CreateBox(plankLines.transform, "Plank Line", new Vector3(x, floorTopHeight + 0.0005f, 0f), new Vector3(0.01f, 0.001f, roomSize), materials.floorPlankLine);
            }

            const float rugThickness = 0.006f;
            CreateCylinder(parent, "Rug", new Vector3(0f, floorTopHeight + rugThickness * 0.5f, 0f), RugRadius * 2f, rugThickness, materials.rug);
        }

        /// <summary>
        /// 뒤(+Z)는 큰 창이 난 벽, 오른쪽(+X)은 선반을 기댄 벽, 왼쪽(-X)은 허리 높이 벽, 앞(-Z)은 트였다.
        /// 창과 트인 곳으로 skybox가 보인다.
        /// </summary>
        private static void CreateWalls(Transform parent, CafeMaterials materials)
        {
            GameObject walls = new GameObject("Walls");
            walls.transform.SetParent(parent, false);
            Transform wallParent = walls.transform;
            float roomSize = RoomHalfSize * 2f;

            // 뒤 벽: 창 아래·위 띠, 창 양옆 기둥
            float backZ = RoomHalfSize;
            float sideWidth = (roomSize - WindowWidth) * 0.5f;
            float sideCenterX = (WindowWidth + sideWidth) * 0.5f;
            float topBandHeight = WallHeight - WindowTopHeight;
            CreateBox(wallParent, "Back Wall Below Window", new Vector3(0f, WindowBottomHeight * 0.5f, backZ), new Vector3(WindowWidth, WindowBottomHeight, WallThickness), materials.wall);
            CreateBox(wallParent, "Back Wall Above Window", new Vector3(0f, WindowTopHeight + topBandHeight * 0.5f, backZ), new Vector3(WindowWidth, topBandHeight, WallThickness), materials.wall);
            CreateBox(wallParent, "Back Wall Left", new Vector3(-sideCenterX, WallHeight * 0.5f, backZ), new Vector3(sideWidth, WallHeight, WallThickness), materials.wall);
            CreateBox(wallParent, "Back Wall Right", new Vector3(sideCenterX, WallHeight * 0.5f, backZ), new Vector3(sideWidth, WallHeight, WallThickness), materials.wall);
            CreateWindowFrame(wallParent, materials, backZ);

            // 오른쪽 벽: 꽉 막힘 (선반 뒤)
            float rightX = RoomHalfSize;
            CreateBox(wallParent, "Right Wall", new Vector3(rightX, WallHeight * 0.5f, 0f), new Vector3(WallThickness, WallHeight, roomSize), materials.wall);
            CreateBox(wallParent, "Right Wall Trim", new Vector3(rightX - WallThickness * 0.5f - 0.01f, LowWallHeight, 0f), new Vector3(0.02f, 0.06f, roomSize), materials.wallTrim);

            // 왼쪽 벽: 허리 높이까지만 (위로 skybox가 보임)
            float leftX = -RoomHalfSize;
            CreateBox(wallParent, "Left Low Wall", new Vector3(leftX, LowWallHeight * 0.5f, 0f), new Vector3(WallThickness, LowWallHeight, roomSize), materials.wall);
            CreateBox(wallParent, "Left Low Wall Cap", new Vector3(leftX, LowWallHeight + 0.02f, 0f), new Vector3(WallThickness + 0.08f, 0.04f, roomSize), materials.darkWood);
        }

        /// <summary>
        /// 창 둘레 나무 틀과 가운데 세로 창살. 유리는 넣지 않는다 (skybox가 그대로 보이게).
        /// </summary>
        private static void CreateWindowFrame(Transform parent, CafeMaterials materials, float wallZ)
        {
            const float frameThickness = 0.08f;
            float frameDepth = WallThickness + 0.04f;
            float windowHeight = WindowTopHeight - WindowBottomHeight;
            float windowCenterHeight = (WindowTopHeight + WindowBottomHeight) * 0.5f;
            float halfWidth = WindowWidth * 0.5f;

            CreateBox(parent, "Window Sill", new Vector3(0f, WindowBottomHeight, wallZ - 0.04f), new Vector3(WindowWidth + 0.1f, frameThickness, frameDepth + 0.08f), materials.darkWood);
            CreateBox(parent, "Window Head", new Vector3(0f, WindowTopHeight, wallZ), new Vector3(WindowWidth, frameThickness, frameDepth), materials.darkWood);
            CreateBox(parent, "Window Jamb Left", new Vector3(-halfWidth, windowCenterHeight, wallZ), new Vector3(frameThickness, windowHeight, frameDepth), materials.darkWood);
            CreateBox(parent, "Window Jamb Right", new Vector3(halfWidth, windowCenterHeight, wallZ), new Vector3(frameThickness, windowHeight, frameDepth), materials.darkWood);
            CreateBox(parent, "Window Mullion", new Vector3(0f, windowCenterHeight, wallZ), new Vector3(frameThickness * 0.6f, windowHeight, frameDepth * 0.6f), materials.darkWood);
        }

        /// <summary>
        /// 보드게임 상자가 꽂힌 책장 하나. 상자 색·크기는 고정 시드라 메뉴를 다시 돌려도 같다.
        /// </summary>
        /// <param name="localPosition">책장 바닥 가운데 (m)</param>
        /// <param name="yawDegrees">책장 앞면이 바라볼 방향. -90 = -X쪽(방 안쪽)</param>
        private static void CreateShelf(Transform parent, CafeMaterials materials, Vector3 localPosition, float yawDegrees)
        {
            const float shelfWidth = 2.4f;
            const float shelfHeight = 2.1f;
            const float shelfDepth = 0.36f;
            const float boardThickness = 0.03f;
            const int boardCount = 5;

            GameObject shelf = new GameObject("Game Shelf");
            shelf.transform.SetParent(parent, false);
            shelf.transform.localPosition = localPosition;
            shelf.transform.localRotation = Quaternion.Euler(0f, yawDegrees, 0f);
            Transform shelfParent = shelf.transform;

            // 책장 로컬: +Z = 앞(방 안쪽), X = 너비
            CreateBox(shelfParent, "Back Panel", new Vector3(0f, shelfHeight * 0.5f, -shelfDepth * 0.5f), new Vector3(shelfWidth, shelfHeight, 0.02f), materials.darkWood);
            CreateBox(shelfParent, "Side Left", new Vector3(-shelfWidth * 0.5f, shelfHeight * 0.5f, 0f), new Vector3(boardThickness, shelfHeight, shelfDepth), materials.furnitureWood);
            CreateBox(shelfParent, "Side Right", new Vector3(shelfWidth * 0.5f, shelfHeight * 0.5f, 0f), new Vector3(boardThickness, shelfHeight, shelfDepth), materials.furnitureWood);

            float boardGap = (shelfHeight - boardThickness) / (boardCount - 1);
            float innerWidth = shelfWidth - boardThickness * 2f;
            System.Random random = new System.Random(7);
            for (int boardIndex = 0; boardIndex < boardCount; boardIndex++)
            {
                float boardHeight = boardThickness * 0.5f + boardGap * boardIndex;
                CreateBox(shelfParent, $"Board {boardIndex}", new Vector3(0f, boardHeight, 0f), new Vector3(innerWidth, boardThickness, shelfDepth), materials.furnitureWood);

                bool isTopBoard = boardIndex == boardCount - 1;
                if (isTopBoard) continue;
                float boxBaseHeight = boardHeight + boardThickness * 0.5f;
                FillShelfRow(shelfParent, materials, boxBaseHeight, innerWidth, shelfDepth, boardGap - boardThickness, random);
            }
        }

        /// <summary>
        /// 선반 한 칸을 왼쪽부터 보드게임 상자로 채운다. 대부분 세워 꽂고, 가끔 납작하게 눕혀 쌓는다.
        /// </summary>
        /// <param name="baseHeight">칸 바닥 높이 (m)</param>
        /// <param name="rowWidth">칸 안쪽 너비 (m)</param>
        /// <param name="clearHeight">칸 안쪽 높이 (m)</param>
        private static void FillShelfRow(Transform shelfParent, CafeMaterials materials, float baseHeight, float rowWidth, float shelfDepth, float clearHeight, System.Random random)
        {
            float cursorX = -rowWidth * 0.5f + 0.02f;
            float rowEndX = rowWidth * 0.5f - 0.02f;
            while (cursorX < rowEndX)
            {
                Material boxMaterial = materials.gameBoxes[random.Next(materials.gameBoxes.Length)];
                bool isLyingStack = random.NextDouble() < 0.2;
                if (isLyingStack)
                {
                    // 눕힌 상자 2~3개 더미
                    float stackWidth = 0.3f;
                    if (cursorX + stackWidth > rowEndX) break;
                    int stackCount = random.Next(2, 4);
                    float stackHeight = baseHeight;
                    for (int stackIndex = 0; stackIndex < stackCount; stackIndex++)
                    {
                        float boxThickness = 0.06f + (float)random.NextDouble() * 0.04f;
                        Material stackMaterial = materials.gameBoxes[random.Next(materials.gameBoxes.Length)];
                        CreateBox(shelfParent, "Game Box", new Vector3(cursorX + stackWidth * 0.5f, stackHeight + boxThickness * 0.5f, 0.01f), new Vector3(stackWidth - 0.02f, boxThickness, shelfDepth - 0.06f), stackMaterial);
                        stackHeight += boxThickness;
                    }
                    cursorX += stackWidth + 0.01f;
                }
                else
                {
                    // 세워 꽂은 상자: 얇은 면이 앞으로
                    float boxThickness = 0.05f + (float)random.NextDouble() * 0.06f;
                    if (cursorX + boxThickness > rowEndX) break;
                    float boxHeight = Mathf.Min(clearHeight - 0.03f, 0.26f + (float)random.NextDouble() * 0.12f);
                    CreateBox(shelfParent, "Game Box", new Vector3(cursorX + boxThickness * 0.5f, baseHeight + boxHeight * 0.5f, 0.01f), new Vector3(boxThickness, boxHeight, shelfDepth - 0.06f), boxMaterial);
                    cursorX += boxThickness + 0.004f;
                }
            }
        }

        /// <summary>
        /// 천장에서 내려오는 줄, 갓, 빛나는 전구, 따뜻한 Point Light 하나.
        /// </summary>
        /// <param name="bulbPosition">전구 위치 (m)</param>
        /// <param name="intensity">Point Light 세기</param>
        /// <param name="range">Point Light 닿는 거리 (m)</param>
        private static void CreatePendantLight(Transform parent, CafeMaterials materials, string name, Vector3 bulbPosition, float intensity, float range)
        {
            GameObject pendant = new GameObject(name);
            pendant.transform.SetParent(parent, false);
            pendant.transform.localPosition = bulbPosition;
            Transform pendantParent = pendant.transform;

            float cordLength = WallHeight - bulbPosition.y - 0.1f;
            CreateCylinder(pendantParent, "Cord", new Vector3(0f, 0.1f + cordLength * 0.5f, 0f), 0.012f, cordLength, materials.cord);
            CreateCylinder(pendantParent, "Shade", new Vector3(0f, 0.06f, 0f), 0.34f, 0.12f, materials.lampShade);
            CreateSphere(pendantParent, "Bulb", new Vector3(0f, -0.02f, 0f), 0.09f, materials.lampBulb);

            GameObject lightObject = new GameObject("Point Light");
            lightObject.transform.SetParent(pendantParent, false);
            lightObject.transform.localPosition = new Vector3(0f, -0.06f, 0f);
            Light pointLight = lightObject.AddComponent<Light>();
            pointLight.type = LightType.Point;
            pointLight.color = WarmLightColor;
            pointLight.intensity = intensity;
            pointLight.range = range;
            pointLight.shadows = LightShadows.None;
        }

        /// <summary>
        /// 옆 테이블 하나(사각 윗판, 다리 넷), 의자 둘, 위에 작은 펜던트 조명과 게임 상자 하나.
        /// </summary>
        /// <param name="localPosition">테이블 바닥 가운데 (m)</param>
        private static void CreateSideTableSet(Transform parent, CafeMaterials materials, string name, Vector3 localPosition)
        {
            const float tableSize = 0.8f;
            const float tableTopHeight = 0.74f;
            const float topThickness = 0.04f;
            const float legSize = 0.05f;

            GameObject set = new GameObject(name);
            set.transform.SetParent(parent, false);
            set.transform.localPosition = localPosition;
            Transform setParent = set.transform;

            CreateBox(setParent, "Table Top", new Vector3(0f, tableTopHeight - topThickness * 0.5f, 0f), new Vector3(tableSize, topThickness, tableSize), materials.furnitureWood);
            float legHeight = tableTopHeight - topThickness;
            float legOffset = tableSize * 0.5f - legSize;
            for (int legIndex = 0; legIndex < 4; legIndex++)
            {
                float signX = legIndex % 2 == 0 ? -1f : 1f;
                float signZ = legIndex < 2 ? -1f : 1f;
                CreateBox(setParent, "Table Leg", new Vector3(signX * legOffset, legHeight * 0.5f, signZ * legOffset), new Vector3(legSize, legHeight, legSize), materials.darkWood);
            }

            // 탁자 위 게임 상자 하나 (펼쳐 둔 느낌)
            CreateBox(setParent, "Game Box On Table", new Vector3(0.1f, tableTopHeight + 0.03f, -0.05f), new Vector3(0.3f, 0.06f, 0.3f), materials.gameBoxes[1]);

            CreateChair(setParent, materials, new Vector3(0f, 0f, -0.6f), 0f);
            CreateChair(setParent, materials, new Vector3(0f, 0f, 0.6f), 180f);

            CreatePendantLight(setParent, materials, "Pendant", new Vector3(0f, PendantHangHeight - 0.1f, 0f), 1.2f, 3f);
        }

        /// <summary>
        /// 앉는 판, 다리 넷, 등받이 하나인 의자.
        /// </summary>
        /// <param name="yawDegrees">0 = 앉은 사람이 +Z를 봄</param>
        private static void CreateChair(Transform parent, CafeMaterials materials, Vector3 localPosition, float yawDegrees)
        {
            const float seatSize = 0.42f;
            const float seatHeight = 0.45f;
            const float seatThickness = 0.04f;
            const float legSize = 0.04f;
            const float backHeight = 0.45f;

            GameObject chair = new GameObject("Chair");
            chair.transform.SetParent(parent, false);
            chair.transform.localPosition = localPosition;
            chair.transform.localRotation = Quaternion.Euler(0f, yawDegrees, 0f);
            Transform chairParent = chair.transform;

            CreateBox(chairParent, "Seat", new Vector3(0f, seatHeight - seatThickness * 0.5f, 0f), new Vector3(seatSize, seatThickness, seatSize), materials.furnitureWood);
            float legHeight = seatHeight - seatThickness;
            float legOffset = seatSize * 0.5f - legSize;
            for (int legIndex = 0; legIndex < 4; legIndex++)
            {
                float signX = legIndex % 2 == 0 ? -1f : 1f;
                float signZ = legIndex < 2 ? -1f : 1f;
                CreateBox(chairParent, "Chair Leg", new Vector3(signX * legOffset, legHeight * 0.5f, signZ * legOffset), new Vector3(legSize, legHeight, legSize), materials.darkWood);
            }
            CreateBox(chairParent, "Back", new Vector3(0f, seatHeight + backHeight * 0.5f, -seatSize * 0.5f + legSize * 0.5f), new Vector3(seatSize, backHeight, legSize), materials.darkWood);
        }

        /// <summary>
        /// 천장 대신 나무 들보 셋. 들보 사이로 skybox가 보이고, 펜던트 줄이 매달릴 자리가 된다.
        /// </summary>
        private static void CreateCeilingBeams(Transform parent, CafeMaterials materials)
        {
            const float beamSize = 0.18f;
            float roomSize = RoomHalfSize * 2f;
            float[] beamZs = { -2.6f, 0f, 2.6f };
            foreach (float beamZ in beamZs)
            {
                CreateBox(parent, "Ceiling Beam", new Vector3(0f, WallHeight + beamSize * 0.5f, beamZ), new Vector3(roomSize, beamSize, beamSize), materials.darkWood);
            }
        }

        /// <summary>
        /// 충돌체 없는 상자. 꾸밈 전용이라 물리에 끼어들지 않는다.
        /// </summary>
        private static void CreateBox(Transform parent, string name, Vector3 localCenter, Vector3 size, Material material)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = localCenter;
            box.transform.localScale = size;
            box.GetComponent<MeshRenderer>().sharedMaterial = material;
            Object.DestroyImmediate(box.GetComponent<Collider>());
        }

        /// <summary>
        /// 충돌체 없는 원통. 유니티 기본 원통은 높이 2, 지름 1.
        /// </summary>
        private static void CreateCylinder(Transform parent, string name, Vector3 localCenter, float diameter, float height, Material material)
        {
            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = name;
            cylinder.transform.SetParent(parent, false);
            cylinder.transform.localPosition = localCenter;
            cylinder.transform.localScale = new Vector3(diameter, height * 0.5f, diameter);
            cylinder.GetComponent<MeshRenderer>().sharedMaterial = material;
            Object.DestroyImmediate(cylinder.GetComponent<Collider>());
        }

        private static void CreateSphere(Transform parent, string name, Vector3 localCenter, float diameter, Material material)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = name;
            sphere.transform.SetParent(parent, false);
            sphere.transform.localPosition = localCenter;
            sphere.transform.localScale = Vector3.one * diameter;
            MeshRenderer renderer = sphere.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            Object.DestroyImmediate(sphere.GetComponent<Collider>());
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
