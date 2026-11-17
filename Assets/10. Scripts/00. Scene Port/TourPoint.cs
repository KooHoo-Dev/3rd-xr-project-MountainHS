// ─────────────────────────────────────────────
// TourPoint — 관람 지점 하나 : 설 자리(텔레포트 앵커), 바라볼 곳, 안내판 글
// 학습 포인트
//  - 지점은 Transform 하나입니다. 앵커와 안내판이 자식이라 지점을 돌리면 함께 돕니다
//  - 레이로 앵커(파란 원판)를 골라 텔레포트하면 TeleportationAnchor 의 teleporting 이벤트가 옵니다
// 조작
//  - 오른손 스틱 앞 (시뮬레이터 ] 후 I) : 레이로 파란 원판을 겨누고 떼면 그 지점으로 텔레포트
// ─────────────────────────────────────────────
using System;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace StudyXR.ScenePort
{
    public class TourPoint : MonoBehaviour
    {
        [Header("안내")]
        [SerializeField] private string title = "관람 지점";
        [SerializeField, TextArea(2, 3)] private string description = "";
        [SerializeField] private Vector3 landmark;             // 도착했을 때 바라볼 곳 (월드 좌표)

        [Header("연결")]
        [SerializeField] private TeleportationAnchor anchor;
        [SerializeField] private TMP_Text boardText;

        public event Action<TourPoint> Arrived;

        public string Title => title;
        public Vector3 Landmark => landmark;
        public Vector3 StandPosition => transform.position;

        // 텔레포트 목적지 — 앵커 프리팹의 'Anchor' 자식(바닥 5 cm 위). 앵커가 없으면 지점 자신
        public Transform Destination
        {
            get
            {
                if (anchor != null && anchor.teleportAnchorTransform != null) return anchor.teleportAnchorTransform;
                return transform;
            }
        }

        private void OnEnable()
        {
            if (anchor != null) anchor.teleporting.AddListener(OnTeleporting);
        }

        private void OnDisable()
        {
            if (anchor != null) anchor.teleporting.RemoveListener(OnTeleporting);
        }

        private void OnTeleporting(TeleportingEventArgs args)
        {
            Arrived?.Invoke(this);
        }

        // y 축으로만 돌립니다. 앵커가 TargetUpAndForward 라 도착한 사람도 이 방향을 봅니다
        public void FaceYaw(float yawDegrees)
        {
            transform.rotation = Quaternion.Euler(0f, yawDegrees, 0f);
        }

        public void ShowBoard(int number, int count, string footer)
        {
            if (boardText == null) return;
            boardText.text = $"{number} / {count} · {title}\n{description}\n{footer}";
        }
    }
}
