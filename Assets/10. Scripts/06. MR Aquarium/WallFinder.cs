using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace StudyXR.MRAquarium
{
    public class WallFinder : MonoBehaviour
    {
        private const PlaneClassifications WallFlags = PlaneClassifications.WallFace | PlaneClassifications.InnerWallFace;

        [Header("연결")]
        [SerializeField] private ARRaycastManager raycastManager;
        [SerializeField] private ARPlaneManager planeManager;
        [SerializeField] private Transform head;

        [Header("찾기")]
        [SerializeField] private bool allowAirWindow = true;
        [SerializeField] private float airDistance = 1.5f;
        [SerializeField] private float airMaxHeight = 1f;
        [SerializeField] private float maxRayDistance = 6f;
        [SerializeField] private float wallGapTolerance = 0.5f;

        [Header("보이기")]
        [SerializeField] private bool showWalls = true;

        private readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();
        private readonly List<Renderer> rendererBuffer = new List<Renderer>();

        public int PlaneCount { get; private set; }
        public int ClassifiedWalls { get; private set; }
        public int VerticalWalls { get; private set; }

        private static WallSource Classify(ARPlane plane)
        {
            PlaneClassifications c = plane.classifications;

            if ((c & WallFlags) != 0) return WallSource.Classified;
            if (c == PlaneClassifications.None && plane.alignment == PlaneAlignment.Vertical)
                return WallSource.Vertical;
            return WallSource.None;
        }

        public static string Describe(WallSource source)
        {
            switch (source)
            {
                case WallSource.Classified: return "분류 WallFace";
                case WallSource.Vertical: return "방향 Vertical (분류 없음)";
                case WallSource.Air: return "공중 창 (정면 1.5 m)";
                default: return "-";
            }
        }

        public bool TryFindWall(
            Ray ray,
            out Vector3 point,
            out Vector3 normal,
            out WallSource source)
        {
            if (CastWalls(ray, TrackableType.PlaneWithinPolygon, false, out point, out normal, out source)) return true;
            if (CastWalls(ray, TrackableType.PlaneWithinInfinity, true, out point, out normal, out source)) return true;

            if (allowAirWindow && head != null)
            {
                Vector3 flat = Vector3.ProjectOnPlane(head.forward, Vector3.up);

                if (flat.sqrMagnitude < 0.0001f) flat = Vector3.ProjectOnPlane(head.up, Vector3.up);
                flat.Normalize();
                var air = new Plane(-flat, head.position + flat * airDistance);

                if (air.Raycast(ray, out float enter) && enter <= maxRayDistance)
                {
                    Vector3 p = ray.GetPoint(enter);

                    if (Mathf.Abs(p.y - head.position.y) <= airMaxHeight)
                    {
                        point = p;
                        normal = -flat;
                        source = WallSource.Air;
                        return true;
                    }
                }
            }

            point = Vector3.zero;
            normal = Vector3.zero;
            source = WallSource.None;
            return false;
        }

        private bool CastWalls(
            Ray ray,
            TrackableType type,
            bool nearPieceOnly,
            out Vector3 point,
            out Vector3 normal,
            out WallSource source)
        {
            point = Vector3.zero;
            normal = Vector3.zero;
            source = WallSource.None;
            if (raycastManager == null || raycastManager.Raycast(ray, hits, type) == false) return false;
            foreach (ARRaycastHit hit in hits)
            {
                if (hit.distance > maxRayDistance) break;
                var plane = hit.trackable as ARPlane;

                if (plane == null) continue;
                WallSource kind = Classify(plane);

                if (kind == WallSource.None || Vector3.Dot(plane.normal, ray.direction) >= 0f) continue;
                if (nearPieceOnly && NearPiece(plane, hit.pose.position) == false) continue;
                point = hit.pose.position;
                normal = plane.normal;
                source = kind;
                return true;
            }
            return false;
        }

        private bool NearPiece(ARPlane plane, Vector3 worldPoint)
        {
            Vector3 local = plane.transform.InverseTransformPoint(worldPoint);
            Vector2 offset = new Vector2(local.x, local.z) - plane.centerInPlaneSpace;

            return Mathf.Abs(offset.x) <= plane.extents.x + wallGapTolerance &&
                   Mathf.Abs(offset.y) <= plane.extents.y + wallGapTolerance;
        }

        private void LateUpdate()
        {
            int planes = 0, classified = 0, vertical = 0;

            if (planeManager != null)
            {
                foreach (ARPlane plane in planeManager.trackables)
                {
                    planes++;
                    WallSource kind = Classify(plane);

                    if (kind == WallSource.Classified) classified++;
                    else if (kind == WallSource.Vertical) vertical++;
                    bool visible = showWalls && kind != WallSource.None && plane.subsumedBy == null;
                    plane.GetComponentsInChildren(rendererBuffer);
                    foreach (Renderer r in rendererBuffer) r.enabled = visible;
                }
            }
            PlaneCount = planes;
            ClassifiedWalls = classified;
            VerticalWalls = vertical;
        }
    }
}
