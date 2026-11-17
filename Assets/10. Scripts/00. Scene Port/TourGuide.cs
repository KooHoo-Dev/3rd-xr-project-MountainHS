// ─────────────────────────────────────────────
// TourGuide — 관람 지점을 차례로 옮겨 다니고, 옮길 때 화면을 어둡게 했다가 밝힌다
// 학습 포인트
//  - VR 의 이동은 카메라가 아니라 XR Origin(몸)을 옮기는 것입니다. 텔레포트 요청을 TeleportationProvider 에 넣습니다
//  - 시야가 갑자기 바뀌면 멀미가 납니다. 완전히 어두워진 순간에 옮기면 뇌가 '이동'을 보지 않습니다
//  - 순서 · 도착 방향 · 페이드 길이는 TourGuideLab(씬에는 TourGuideAnswer)이 정합니다
// 조작
//  - 오른손 A (시뮬레이터 F1) : 다음 지점
//  - 오른손 B (시뮬레이터 F4) : 이전 지점
//  - 트리거 (오른손 레이로 안내판 버튼, 시뮬레이터 ] 후 T) : 이전 지점 / 다음 지점
// ─────────────────────────────────────────────
using StudyXR.Common;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace StudyXR.ScenePort
{
    public class TourGuide : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private TourGuideLab lab;
        [SerializeField] private TourPoint[] points;
        [SerializeField] private TeleportationProvider teleportation;
        [SerializeField] private ScreenFade screenFade;
        [SerializeField] private Transform head;                 // XR 카메라 — 옮길 거리를 잴 때

        [Header("페이드")]
        [SerializeField] private float shortestFade = 0.2f;     // 초
        [SerializeField] private float longestFade = 0.6f;
        [SerializeField] private float farDistance = 100f;      // m — 이보다 멀면 longestFade

        [Header("시작")]
        [SerializeField] private int startIndex;

        [Header("시뮬레이터 테스트 입력")]
        [SerializeField] private bool useTestKeys = true;       // F1 다음 · F4 이전 (안내판에는 적지 않음)

        // 안내판 아래 조작 두 줄 — Quest 버튼 이름만 적습니다(세션 YF)
        private static readonly string ControlsText =
            "<size=85%>" + QuestButtons.Label(QuestButton.A) + " : 다음 지점 · " + QuestButtons.Label(QuestButton.B) + " : 이전 지점\n" +
            QuestButtons.Label(QuestButton.Y) + " : 성능 표시 · " + QuestButtons.Menu + " : 런처로</size>";

        private InputAction nextAction;
        private InputAction previousAction;

        private int current;
        private float lastFade;

        public int Current => current;
        public float LastFade => lastFade;

        private void OnEnable()
        {
            foreach (TourPoint point in points) point.Arrived += OnArrived;
            nextAction = QuestButtons.Create(QuestButton.A, "Tour Next", useTestKeys ? "<Keyboard>/f1" : null);
            previousAction = QuestButtons.Create(QuestButton.B, "Tour Previous", useTestKeys ? "<Keyboard>/f4" : null);
            nextAction.Enable();
            previousAction.Enable();
        }

        private void OnDisable()
        {
            foreach (TourPoint point in points) point.Arrived -= OnArrived;
            nextAction.Disable();
            nextAction.Dispose();
            previousAction.Disable();
            previousAction.Dispose();
        }

        private void Start()
        {
            if (points.Length == 0) return;

            // 지점마다 도착했을 때 바라볼 방향을 lab 으로 정합니다 — 앵커와 안내판이 자식이라 함께 돕니다
            foreach (TourPoint point in points) point.FaceYaw(lab.ArrivalYaw(point.StandPosition, point.Landmark));

            // 처음 자리는 페이드 없이 (빌더는 XR Origin 을 첫 지점에 북쪽을 보게 놓았습니다)
            current = Mathf.Clamp(startIndex, 0, points.Length - 1);
            Teleport(current);
        }

        private void Update()
        {
            if (nextAction.WasPressedThisFrame()) Next();
            if (previousAction.WasPressedThisFrame()) Previous();
        }

        // 오른손 A · 안내판 버튼이 부릅니다 (버튼은 빌더가 onClick 에 연결)
        public void Next()
        {
            GoTo(lab.NextIndex(current, 1, points.Length));
        }

        public void Previous()
        {
            GoTo(lab.NextIndex(current, -1, points.Length));
        }

        private void GoTo(int index)
        {
            if (screenFade != null && screenFade.IsFading) return;     // 페이드 도중에 또 누르면 무시
            if (index < 0 || index >= points.Length)
            {
                Debug.LogWarning($"[ScenePort] NextIndex 가 범위 밖 번호 {index} 를 돌려주었습니다 (0 ~ {points.Length - 1})");
                return;
            }

            Vector3 from = head != null ? head.position : transform.position;
            float distance = Vector3.Distance(from, points[index].Destination.position);
            lastFade = lab.FadeSeconds(distance, shortestFade, longestFade, farDistance);

            // 0 초면 페이드 없이 바로 옮깁니다(컷). ScreenFade 에 0 을 넘기면 한 프레임 깜빡임이 됩니다
            if (lastFade <= 0f || screenFade == null)
            {
                Teleport(index);
                return;
            }
            screenFade.FadeOutIn(() => Teleport(index), lastFade);
        }

        // XR Origin 을 지점의 앵커 자리 · 방향으로 옮겨 달라고 요청합니다 (실제 이동은 Provider 의 다음 Update)
        private void Teleport(int index)
        {
            Transform destination = points[index].Destination;
            var request = new TeleportRequest
            {
                destinationPosition = destination.position,
                destinationRotation = destination.rotation,
                matchOrientation = MatchOrientation.TargetUpAndForward,
                requestTime = Time.time,
            };
            teleportation.QueueTeleportRequest(request);

            current = index;
            RefreshBoards();
        }

        // 레이로 원판을 골라 직접 텔레포트한 경우 : 순서 번호만 맞춥니다 (페이드 없음 — 버튼 이동과 비교)
        private void OnArrived(TourPoint point)
        {
            int index = System.Array.IndexOf(points, point);
            if (index < 0) return;
            current = index;
            lastFade = 0f;
            RefreshBoards();
        }

        private void RefreshBoards()
        {
            for (int i = 0; i < points.Length; i++)
            {
                string footer = i == current ? $"여기 있습니다 · 이동 페이드 {lastFade:F2}초\n" : "\n";
                points[i].ShowBoard(i + 1, points.Length, footer + ControlsText);
            }
        }
    }
}
