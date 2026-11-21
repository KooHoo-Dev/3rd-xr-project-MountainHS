using System.Collections.Generic;
using UnityEngine;

namespace StudyXR.Strike
{
    /// <summary>
    /// 주사위를 던지는 경기장. 사각 트레이 안에 동심 타원 계단 그릇이 있고 바닥은 평평한 타원이다 (참고 사진 Sample/arena-example.png).
    /// 그릇 안이 경기장이고, 그릇 밖 트레이 가장자리에 멈춘 주사위는 장외다.
    /// 턴 진행(#5)이 <see cref="Contains"/>로 장외를 판정하고, AI(#7)가 <see cref="RandomFloorPoint"/>로 빈 곳을 겨냥한다.
    /// </summary>
    /// <remarks>
    /// 모양은 <see cref="Build"/>가 만든다. Inspector 값을 바꾼 뒤 컴포넌트 메뉴 "경기장 다시 만들기"를 실행하면 Edit 모드에서도 다시 만들어지고 씬에 저장된다.
    /// 만들어진 물체는 자식 "Generated" 아래에 모이며 다시 만들 때 통째로 지워진다.
    /// 이 오브젝트의 위치·회전이 경기장 기준이다. 로컬 원점 = 트레이 윗면 한가운데, 로컬 +Y = 위.
    /// </remarks>
    public class DiceArena : MonoBehaviour
    {
        // ── 상수 ──
        private const string GeneratedName = "Generated";

        [Header("트레이 (가정)")]
        [Tooltip("트레이 안쪽 가로 길이 (m, 로컬 X)")]
        [SerializeField] private float trayWidthMeters = 0.5f;
        [Tooltip("트레이 안쪽 세로 길이 (m, 로컬 Z)")]
        [SerializeField] private float trayDepthMeters = 0.34f;
        [Tooltip("트레이 윗면 위로 솟은 벽 높이 (m). 주사위가 책상 밖으로 튀지 않게 막는다")]
        [SerializeField] private float wallHeightMeters = 0.06f;
        [Tooltip("벽 두께 (m). 트레이 바깥쪽으로 붙는다")]
        [SerializeField] private float wallThicknessMeters = 0.02f;

        [Header("그릇 (가정)")]
        [Tooltip("평평한 타원 바닥의 가로 반지름 (m)")]
        [SerializeField] private float floorRadiusXMeters = 0.13f;
        [Tooltip("평평한 타원 바닥의 세로 반지름 (m)")]
        [SerializeField] private float floorRadiusZMeters = 0.08f;
        [Tooltip("그릇 맨 위 가장자리의 가로 반지름 (m). 이 타원 밖은 장외")]
        [SerializeField] private float rimRadiusXMeters = 0.19f;
        [Tooltip("그릇 맨 위 가장자리의 세로 반지름 (m). 이 타원 밖은 장외")]
        [SerializeField] private float rimRadiusZMeters = 0.125f;
        [Tooltip("계단 수 (턱 수). 2 이상")]
        [SerializeField] private int stepCount = 5;
        [Tooltip("트레이 윗면에서 바닥까지 깊이 (m)")]
        [SerializeField] private float bowlDepthMeters = 0.03f;
        [Tooltip("타원 둘레를 나누는 조각 수. 클수록 매끈하다")]
        [SerializeField] private int ellipseSegmentCount = 64;

        [Header("재질 (씬 연결은 StudyXR/MR Strike/2. 경기장 추가 메뉴가 한다)")]
        [Tooltip("그릇 안(바닥과 계단) 보이는 재질")]
        [SerializeField] private Material bowlMaterial;
        [Tooltip("그릇 밖 트레이 가장자리(장외 구역) 보이는 재질. 그릇과 다른 색으로 장외를 눈에 띄게 한다")]
        [SerializeField] private Material marginMaterial;
        [Tooltip("벽 보이는 재질")]
        [SerializeField] private Material wallMaterial;
        [Tooltip("바닥·계단·가장자리 물리 재질 (PM_ArenaFloor)")]
        [SerializeField] private PhysicsMaterial floorPhysicsMaterial;
        [Tooltip("벽 물리 재질 (PM_ArenaWall)")]
        [SerializeField] private PhysicsMaterial wallPhysicsMaterial;

        /// <summary>
        /// 그릇 가장자리 타원 안에 주사위가 있는지 본다. 높이는 보지 않는다.
        /// 턴 진행이 굴림이 끝난 주사위마다 불러, false이면 그 주사위를 장외로 제거한다.
        /// </summary>
        /// <param name="worldPosition">주사위 월드 위치</param>
        /// <returns>true = 경기장 안, false = 장외 (트레이 가장자리 포함)</returns>
        public bool Contains(Vector3 worldPosition)
        {
            Vector3 local = transform.InverseTransformPoint(worldPosition);
            float xRatio = local.x / rimRadiusXMeters;
            float zRatio = local.z / rimRadiusZMeters;
            return xRatio * xRatio + zRatio * zRatio <= 1f;
        }

        /// <summary>
        /// 평평한 타원 바닥의 한가운데를 돌려준다. AI가 가운데를 겨냥할 때 쓴다.
        /// </summary>
        /// <returns>바닥 중심의 월드 위치</returns>
        public Vector3 FloorCenterPoint()
        {
            return transform.TransformPoint(new Vector3(0f, -bowlDepthMeters, 0f));
        }

        /// <summary>
        /// 평평한 타원 바닥 위의 무작위 점을 돌려준다. 바닥 어디든 같은 확률이다. AI가 빈 곳을 겨냥할 때 쓴다.
        /// </summary>
        /// <returns>바닥 면 위의 월드 위치</returns>
        public Vector3 RandomFloorPoint()
        {
            float angleRadians = Random.Range(0f, Mathf.PI * 2f);
            float radiusRatio = Mathf.Sqrt(Random.value);
            Vector3 local = new Vector3(
                Mathf.Cos(angleRadians) * floorRadiusXMeters * radiusRatio,
                -bowlDepthMeters,
                Mathf.Sin(angleRadians) * floorRadiusZMeters * radiusRatio);
            return transform.TransformPoint(local);
        }

        /// <summary>
        /// 이전에 만든 것을 지우고 그릇(바닥·계단·가장자리)과 벽을 새로 만든다.
        /// 컴포넌트 메뉴로 Edit 모드에서 실행하면 결과가 씬에 저장된다.
        /// </summary>
        [ContextMenu("경기장 다시 만들기")]
        public void Build()
        {
            Transform generatedRoot = CreateEmptyGeneratedRoot();
            BuildBowl(generatedRoot);
            BuildWalls(generatedRoot);
        }

        /// <summary>
        /// 자식 "Generated"를 메쉬까지 지우고 같은 이름의 빈 자식을 새로 만든다.
        /// </summary>
        /// <returns>새로 만든 빈 루트</returns>
        private Transform CreateEmptyGeneratedRoot()
        {
            Transform oldRoot = transform.Find(GeneratedName);
            if (oldRoot != null)
            {
                foreach (MeshFilter meshFilter in oldRoot.GetComponentsInChildren<MeshFilter>())
                {
                    if (meshFilter.sharedMesh.name == "Arena Bowl Mesh") DestroyObject(meshFilter.sharedMesh);
                }
                DestroyObject(oldRoot.gameObject);
            }

            GameObject newRoot = new GameObject(GeneratedName);
            newRoot.transform.SetParent(transform, false);
            return newRoot.transform;
        }

        private void DestroyObject(Object target)
        {
            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        // ───────────── 그릇 ─────────────

        /// <summary>
        /// 바닥 타원, 계단(턱 + 수직면), 그릇 밖 가장자리를 한 메쉬로 만들어 렌더러와 충돌체를 붙인다.
        /// 부분 메쉬 0 = 그릇 안(bowlMaterial), 1 = 가장자리(marginMaterial).
        /// </summary>
        /// <param name="generatedRoot">만든 물체를 모을 부모</param>
        private void BuildBowl(Transform generatedRoot)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<int> bowlTriangles = new List<int>();
            List<int> marginTriangles = new List<int>();

            AddFloor(vertices, bowlTriangles);
            AddSteps(vertices, bowlTriangles);
            AddMargin(vertices, marginTriangles);

            Mesh mesh = new Mesh();
            mesh.name = "Arena Bowl Mesh";
            mesh.SetVertices(vertices);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(bowlTriangles, 0);
            mesh.SetTriangles(marginTriangles, 1);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            GameObject bowlObject = new GameObject("Bowl");
            bowlObject.transform.SetParent(generatedRoot, false);
            bowlObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            bowlObject.AddComponent<MeshRenderer>().sharedMaterials = new Material[] { bowlMaterial, marginMaterial };
            MeshCollider meshCollider = bowlObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh;
            meshCollider.sharedMaterial = floorPhysicsMaterial;
        }

        /// <summary>
        /// 그릇 바닥의 평평한 타원 한 장 (가운데에서 퍼지는 삼각형 부채).
        /// </summary>
        private void AddFloor(List<Vector3> vertices, List<int> triangles)
        {
            Vector3 center = new Vector3(0f, -bowlDepthMeters, 0f);
            for (int segment = 0; segment < ellipseSegmentCount; segment++)
            {
                Vector3 current = EllipsePoint(floorRadiusXMeters, floorRadiusZMeters, segment, -bowlDepthMeters);
                Vector3 next = EllipsePoint(floorRadiusXMeters, floorRadiusZMeters, segment + 1, -bowlDepthMeters);
                AddPolygon(vertices, triangles, Vector3.up, center, current, next);
            }
        }

        /// <summary>
        /// 바닥 가장자리에서 그릇 가장자리까지 계단을 만든다.
        /// 단계마다 수직면(riser)으로 한 칸 올라가고, 그 높이에서 다음 수직면까지 평평한 턱(tread)이 이어진다.
        /// 맨 위 수직면은 트레이 윗면 높이(0)까지 올라가 가장자리와 이어진다.
        /// </summary>
        private void AddSteps(List<Vector3> vertices, List<int> triangles)
        {
            for (int step = 0; step < stepCount; step++)
            {
                float lowerHeight = -bowlDepthMeters + bowlDepthMeters * step / stepCount;
                float upperHeight = -bowlDepthMeters + bowlDepthMeters * (step + 1) / stepCount;
                Vector2 riserRadius = StepRadius(step);

                for (int segment = 0; segment < ellipseSegmentCount; segment++)
                {
                    Vector3 lowerCurrent = EllipsePoint(riserRadius.x, riserRadius.y, segment, lowerHeight);
                    Vector3 lowerNext = EllipsePoint(riserRadius.x, riserRadius.y, segment + 1, lowerHeight);
                    Vector3 upperCurrent = EllipsePoint(riserRadius.x, riserRadius.y, segment, upperHeight);
                    Vector3 upperNext = EllipsePoint(riserRadius.x, riserRadius.y, segment + 1, upperHeight);

                    Vector3 towardCenter = -(lowerCurrent + lowerNext) * 0.5f;
                    towardCenter.y = 0f;
                    AddPolygon(vertices, triangles, towardCenter, lowerCurrent, lowerNext, upperNext, upperCurrent);

                    bool isTopStep = step == stepCount - 1;
                    if (isTopStep) continue;

                    Vector2 nextRiserRadius = StepRadius(step + 1);
                    Vector3 treadInnerCurrent = upperCurrent;
                    Vector3 treadInnerNext = upperNext;
                    Vector3 treadOuterCurrent = EllipsePoint(nextRiserRadius.x, nextRiserRadius.y, segment, upperHeight);
                    Vector3 treadOuterNext = EllipsePoint(nextRiserRadius.x, nextRiserRadius.y, segment + 1, upperHeight);
                    AddPolygon(vertices, triangles, Vector3.up, treadInnerCurrent, treadInnerNext, treadOuterNext, treadOuterCurrent);
                }
            }
        }

        /// <summary>
        /// 그릇 가장자리 타원과 트레이 사각 안쪽 변 사이의 평평한 띠 (장외 구역).
        /// 사각 변과 만나는 점이 다른 변으로 넘어가는 조각에는 모서리 점을 끼워 모서리가 비지 않게 한다.
        /// </summary>
        private void AddMargin(List<Vector3> vertices, List<int> triangles)
        {
            for (int segment = 0; segment < ellipseSegmentCount; segment++)
            {
                Vector3 rimCurrent = EllipsePoint(rimRadiusXMeters, rimRadiusZMeters, segment, 0f);
                Vector3 rimNext = EllipsePoint(rimRadiusXMeters, rimRadiusZMeters, segment + 1, 0f);
                Vector3 trayCurrent = TrayEdgePointInDirection(rimCurrent);
                Vector3 trayNext = TrayEdgePointInDirection(rimNext);

                bool isCornerBetween = IsOnSideEdge(trayCurrent) != IsOnSideEdge(trayNext);
                if (isCornerBetween)
                {
                    Vector3 corner = TrayCornerBetween(rimCurrent, rimNext);
                    AddPolygon(vertices, triangles, Vector3.up, rimCurrent, rimNext, trayNext, corner, trayCurrent);
                }
                else
                {
                    AddPolygon(vertices, triangles, Vector3.up, rimCurrent, rimNext, trayNext, trayCurrent);
                }
            }
        }

        /// <summary>
        /// 가운데에서 같은 방향으로 뻗은 반직선이 트레이 사각 안쪽 변과 만나는 점.
        /// </summary>
        /// <param name="directionPoint">방향을 정하는 점 (로컬, 높이는 그대로 쓴다)</param>
        private Vector3 TrayEdgePointInDirection(Vector3 directionPoint)
        {
            float halfWidth = trayWidthMeters * 0.5f;
            float halfDepth = trayDepthMeters * 0.5f;
            float scaleToSideEdge = halfWidth / Mathf.Max(Mathf.Abs(directionPoint.x), 0.0001f);
            float scaleToEndEdge = halfDepth / Mathf.Max(Mathf.Abs(directionPoint.z), 0.0001f);
            float scale = Mathf.Min(scaleToSideEdge, scaleToEndEdge);
            return new Vector3(directionPoint.x * scale, directionPoint.y, directionPoint.z * scale);
        }

        /// <summary>
        /// 트레이 가장자리 점이 좌우 변(X = ±가로/2)에 있으면 true, 앞뒤 변(Z = ±세로/2)에 있으면 false.
        /// </summary>
        private bool IsOnSideEdge(Vector3 trayEdgePoint)
        {
            return Mathf.Abs(Mathf.Abs(trayEdgePoint.x) - trayWidthMeters * 0.5f) < 0.0001f;
        }

        /// <summary>
        /// 두 가장자리 점이 서로 다른 변으로 넘어갈 때 그 사이 트레이 모서리 점.
        /// </summary>
        private Vector3 TrayCornerBetween(Vector3 rimCurrent, Vector3 rimNext)
        {
            Vector3 middle = (rimCurrent + rimNext) * 0.5f;
            float cornerX = Mathf.Sign(middle.x) * trayWidthMeters * 0.5f;
            float cornerZ = Mathf.Sign(middle.z) * trayDepthMeters * 0.5f;
            return new Vector3(cornerX, middle.y, cornerZ);
        }

        /// <summary>
        /// 계단 단계 번호의 수직면 위치 타원 반지름. 0단 = 바닥 가장자리, 마지막 단 = 그릇 가장자리.
        /// </summary>
        /// <param name="step">단계 번호 (0 ~ stepCount-1)</param>
        /// <returns>x = 가로 반지름, y = 세로 반지름 (m)</returns>
        private Vector2 StepRadius(int step)
        {
            float progress = (float)step / (stepCount - 1);
            float radiusX = Mathf.Lerp(floorRadiusXMeters, rimRadiusXMeters, progress);
            float radiusZ = Mathf.Lerp(floorRadiusZMeters, rimRadiusZMeters, progress);
            return new Vector2(radiusX, radiusZ);
        }

        /// <summary>
        /// 타원 둘레의 한 점. segment가 ellipseSegmentCount이면 한 바퀴를 돌아 0번과 같은 점.
        /// </summary>
        /// <param name="segment">둘레 조각 번호</param>
        /// <param name="height">로컬 Y (m)</param>
        private Vector3 EllipsePoint(float radiusX, float radiusZ, int segment, float height)
        {
            float angleRadians = Mathf.PI * 2f * segment / ellipseSegmentCount;
            return new Vector3(Mathf.Cos(angleRadians) * radiusX, height, Mathf.Sin(angleRadians) * radiusZ);
        }

        /// <summary>
        /// 볼록 다각형 한 장을 삼각형 부채로 메쉬 목록에 더한다.
        /// 점 순서가 반대여도 앞면이 facingHint 쪽을 보도록 자동으로 뒤집는다.
        /// 면마다 점을 새로 만들어 모서리가 각져 보인다.
        /// </summary>
        /// <param name="facingHint">보이는 면이 향해야 할 대략적인 방향</param>
        /// <param name="points">다각형 꼭짓점 (3개 이상, 둘레 순서)</param>
        private void AddPolygon(List<Vector3> vertices, List<int> triangles, Vector3 facingHint, params Vector3[] points)
        {
            Vector3 normal = Vector3.Cross(points[1] - points[0], points[2] - points[0]);
            bool isFlipped = Vector3.Dot(normal, facingHint) < 0f;

            int firstIndex = vertices.Count;
            vertices.AddRange(points);
            for (int corner = 1; corner < points.Length - 1; corner++)
            {
                triangles.Add(firstIndex);
                if (isFlipped)
                {
                    triangles.Add(firstIndex + corner + 1);
                    triangles.Add(firstIndex + corner);
                }
                else
                {
                    triangles.Add(firstIndex + corner);
                    triangles.Add(firstIndex + corner + 1);
                }
            }
        }

        // ───────────── 벽 ─────────────

        /// <summary>
        /// 트레이 네 변 바깥에 벽 상자 4개를 세운다. 아래는 그릇 바닥 높이까지 내려 트레이 옆면 역할도 한다.
        /// </summary>
        private void BuildWalls(Transform generatedRoot)
        {
            float wallBottom = -bowlDepthMeters;
            float wallHeight = bowlDepthMeters + wallHeightMeters;
            float wallCenterY = wallBottom + wallHeight * 0.5f;
            float halfWidth = trayWidthMeters * 0.5f;
            float halfDepth = trayDepthMeters * 0.5f;
            float halfThickness = wallThicknessMeters * 0.5f;
            float wallLengthAlongX = trayWidthMeters + wallThicknessMeters * 2f;

            CreateWall(generatedRoot, "Wall Front", new Vector3(0f, wallCenterY, -(halfDepth + halfThickness)), new Vector3(wallLengthAlongX, wallHeight, wallThicknessMeters));
            CreateWall(generatedRoot, "Wall Back", new Vector3(0f, wallCenterY, halfDepth + halfThickness), new Vector3(wallLengthAlongX, wallHeight, wallThicknessMeters));
            CreateWall(generatedRoot, "Wall Left", new Vector3(-(halfWidth + halfThickness), wallCenterY, 0f), new Vector3(wallThicknessMeters, wallHeight, trayDepthMeters));
            CreateWall(generatedRoot, "Wall Right", new Vector3(halfWidth + halfThickness, wallCenterY, 0f), new Vector3(wallThicknessMeters, wallHeight, trayDepthMeters));
        }

        private void CreateWall(Transform generatedRoot, string wallName, Vector3 localCenter, Vector3 size)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = wallName;
            wall.transform.SetParent(generatedRoot, false);
            wall.transform.localPosition = localCenter;
            wall.transform.localScale = size;
            wall.GetComponent<MeshRenderer>().sharedMaterial = wallMaterial;
            wall.GetComponent<BoxCollider>().sharedMaterial = wallPhysicsMaterial;
        }

        /// <summary>
        /// 장외 경계(그릇 가장자리 타원)를 선택했을 때만 씬 뷰에 노란 선으로 그린다.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Vector3 previous = transform.TransformPoint(EllipsePoint(rimRadiusXMeters, rimRadiusZMeters, 0, 0f));
            for (int segment = 1; segment <= ellipseSegmentCount; segment++)
            {
                Vector3 current = transform.TransformPoint(EllipsePoint(rimRadiusXMeters, rimRadiusZMeters, segment, 0f));
                Gizmos.DrawLine(previous, current);
                previous = current;
            }
        }
    }
}
