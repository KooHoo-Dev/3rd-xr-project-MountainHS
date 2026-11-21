using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace StudyXR.Strike
{
    /// <summary>
    /// 찾은 책상 면이 어떤 근거인지.
    /// </summary>
    public enum TableSource
    {
        None,
        // 헤드셋이 "Table"로 분류한 면
        Classified,
        // 분류는 없지만 위를 향한 수평 면 (바닥이 아니도록 머리보다 충분히 낮은 것만)
        Horizontal,
        // 면을 못 찾아 머리 앞 허공에 잡은 가짜 높이 (권한 거절·공간 스캔 없음)
        Air,
    }

    /// <summary>
    /// 시선이 닿는 진짜 책상 윗면 위치를 찾는다. 샘플 <c>06. MR Aquarium/WallFinder</c>를 책상용으로 복사해 바꾼 것이다.
    /// 책상이 없거나 권한이 거절되면 머리 앞 허공 위치를 돌려준다 (허공 경기장).
    /// </summary>
    public class TableFinder : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private ARRaycastManager raycastManager;
        [Tooltip("머리(보통 XR 카메라)")]
        [SerializeField] private Transform head;

        [Header("찾기 (가정)")]
        [Tooltip("시선이 이 거리(m)보다 먼 면은 무시한다")]
        [SerializeField] private float maxRayDistanceMeters = 4f;
        [Tooltip("분류 없는 수평 면은 머리보다 이 높이(m) 안쪽으로 낮아야 책상으로 본다 (그보다 낮으면 바닥)")]
        [SerializeField] private float maxHeadToTableMeters = 1.1f;

        [Header("허공 대안 (가정)")]
        [SerializeField] private bool allowAirTable = true;
        [Tooltip("머리 앞 몇 m에 잡을지 (m)")]
        [SerializeField] private float airDistanceMeters = 0.8f;
        [Tooltip("머리보다 몇 m 아래에 잡을지 (m)")]
        [SerializeField] private float airDropMeters = 0.5f;

        private readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();

        /// <summary>
        /// 시선 방향의 책상 윗면 점을 찾는다. 분류된 책상 → 위를 향한 수평 면 → 허공 순서로 시도한다.
        /// </summary>
        /// <param name="ray">시선 (보통 머리 위치에서 앞 방향)</param>
        /// <param name="point">찾은 윗면 점 (월드)</param>
        /// <param name="source">어떤 근거로 찾았는지 (None = 못 찾음)</param>
        /// <returns>true = 찾음 (허공 포함), false = 못 찾음</returns>
        public bool TryFindTable(Ray ray, out Vector3 point, out TableSource source)
        {
            if (TryRaycastTable(ray, out point, out source)) return true;

            if (allowAirTable)
            {
                point = FindAirPoint();
                source = TableSource.Air;
                return true;
            }

            point = Vector3.zero;
            source = TableSource.None;
            return false;
        }

        /// <summary>
        /// 면 위로 쏜 시선 중 위를 향한 면에 처음 닿는 곳을 찾는다.
        /// </summary>
        private bool TryRaycastTable(Ray ray, out Vector3 point, out TableSource source)
        {
            point = Vector3.zero;
            source = TableSource.None;

            bool isRaycastReady = raycastManager != null && raycastManager.enabled;
            if (isRaycastReady == false) return false;
            if (raycastManager.Raycast(ray, hits, TrackableType.PlaneWithinPolygon) == false) return false;

            foreach (ARRaycastHit hit in hits)
            {
                if (hit.distance > maxRayDistanceMeters) break;

                ARPlane plane = hit.trackable as ARPlane;
                if (plane == null) continue;

                TableSource kind = Classify(plane, hit.pose.position);
                bool isFacingUp = Vector3.Dot(plane.normal, Vector3.up) > 0.9f;
                if (kind == TableSource.None || isFacingUp == false) continue;

                point = hit.pose.position;
                source = kind;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 면이 책상인지 본다. 분류가 Table이면 확실, 분류가 없으면 위를 향한 수평 면이면서 머리보다 충분히 낮은 것만 책상으로 본다.
        /// </summary>
        private TableSource Classify(ARPlane plane, Vector3 worldPoint)
        {
            bool isClassifiedTable = (plane.classifications & PlaneClassifications.Table) != 0;
            if (isClassifiedTable) return TableSource.Classified;

            bool isUnclassified = plane.classifications == PlaneClassifications.None;
            bool isHorizontalUp = plane.alignment == PlaneAlignment.HorizontalUp;
            bool isBelowHeadEnough = worldPoint.y >= head.position.y - maxHeadToTableMeters;
            bool isLikelyTable = isUnclassified && isHorizontalUp && isBelowHeadEnough;
            if (isLikelyTable) return TableSource.Horizontal;

            return TableSource.None;
        }

        /// <summary>
        /// 머리가 보는 수평 방향 앞쪽, 머리보다 조금 아래 허공 점.
        /// </summary>
        private Vector3 FindAirPoint()
        {
            Vector3 flatForward = Vector3.ProjectOnPlane(head.forward, Vector3.up);
            if (flatForward.sqrMagnitude < 0.0001f) flatForward = Vector3.ProjectOnPlane(head.up, Vector3.up);
            flatForward.Normalize();

            Vector3 point = head.position + flatForward * airDistanceMeters;
            point.y = head.position.y - airDropMeters;
            return point;
        }
    }
}
