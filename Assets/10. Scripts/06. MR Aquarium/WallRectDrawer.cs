using System;
using StudyXR.Common;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StudyXR.MRAquarium
{
    public class WallRectDrawer : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private WallFinder finder;
        [SerializeField] private Transform rayOrigin;
        [SerializeField] private Transform head;
        [SerializeField] private LineRenderer outline;
        [SerializeField] private BasisGizmo gizmo;
        [SerializeField] private QuestButton drawButton = QuestButton.A;

        [Header("그리기")]
        [SerializeField] private float snap = 0.05f;
        [SerializeField] private float minSize = 0.3f;
        [SerializeField] private float maxSize = 1.5f;
        [SerializeField] private float maxRayDistance = 6f;
        [SerializeField] private float dragSmoothing = 20f;
        [SerializeField] private float outlineGap = 0.004f;
        [SerializeField] private Vector2 presetSize = new Vector2(0.9f, 0.6f);
        [SerializeField] private bool hideGuidesOnFinish = false;

        [Header("시뮬레이터 테스트 입력")]
        [SerializeField] private bool useTestKeys = true;

        private Plane wallPlane;
        private Vector3 anchor;
        private Vector3 normal;
        private Vector3 right;
        private Vector3 up;
        private Vector3 corner;
        private Vector3 center;
        private Vector2 size;
        private Quaternion rotation = Quaternion.identity;
        private bool gazeDrawing;
        private InputAction drawAction;

        public event Action<Pose, Vector2> RectFinished;

        private bool isDrawing;
        public WallSource Source { get; private set; }
        public Vector3 Normal => normal;
        public Vector3 Right => right;
        public Vector3 Up => up;
        public Vector2 Size => size;
        public Quaternion Rotation => rotation;
        public string LastMessage { get; private set; } = "벽을 가리키고 오른손 A 를 누른 채 끄세요";

        private void OnEnable()
        {
            drawAction = QuestButtons.Create(drawButton, "Draw Window");
            drawAction.Enable();
        }

        private void OnDisable()
        {
            if (drawAction == null) return;
            drawAction.Disable();
            drawAction.Dispose();
            drawAction = null;
        }

        private void Start()
        {
            if (outline != null) outline.enabled = false;
        }

        private void Update()
        {
            if (drawAction != null && rayOrigin != null && gazeDrawing == false)
            {
                var hand = new Ray(rayOrigin.position, rayOrigin.forward);

                if (drawAction.WasPressedThisFrame()) BeginRect(hand);
                else if (isDrawing && drawAction.IsPressed()) DragRect(hand);
                else if (isDrawing && drawAction.WasReleasedThisFrame()) EndRect();
            }

            if (useTestKeys == false || head == null) return;
            Keyboard keyboard = Keyboard.current;

            if (keyboard == null) return;
            var gaze = new Ray(head.position, head.forward);

            if (keyboard.f1Key.wasPressedThisFrame)
            {
                if (gazeDrawing) { gazeDrawing = false; EndRect(); }
                else if (isDrawing == false) gazeDrawing = BeginRect(gaze);
            }
            else if (gazeDrawing) DragRect(gaze);
            if (keyboard.f3Key.wasPressedThisFrame && isDrawing == false) DrawPreset(gaze);
        }

        private bool BeginRect(Ray ray)
        {
            if (finder == null ||
                finder.TryFindWall(ray, out Vector3 point, out Vector3 n, out WallSource source) == false)
            {
                LastMessage = "벽을 찾지 못함 — 벽을 가리키세요";
                return false;
            }
            anchor = point;
            normal = n;
            Source = source;
            wallPlane = new Plane(normal, anchor);
            MakeBasis();
            isDrawing = true;
            SetCorner(anchor, true);
            LastMessage = "끄는 중 — 놓으면 확정";
            return true;
        }

        private void DragRect(Ray ray)
        {
            if (isDrawing == false) return;
            if (wallPlane.Raycast(ray, out float enter) && enter <= maxRayDistance)
                SetCorner(ray.GetPoint(enter), false);
        }

        private bool EndRect()
        {
            if (isDrawing == false) return false;
            isDrawing = false;
            rotation = Quaternion.LookRotation(-normal, up);
            if (hideGuidesOnFinish)
            {
                if (outline != null) outline.enabled = false;
                if (gizmo != null) gizmo.Hide();
            }
            LastMessage = $"확정 · {size.x:F2} x {size.y:F2} m";
            RectFinished?.Invoke(new Pose(center, rotation), size);
            return true;
        }

        private bool DrawPreset(Ray gaze)
        {
            if (isDrawing || BeginRect(gaze) == false) return false;
            Vector3 first = ToWorld(-presetSize.x * 0.5f, -presetSize.y * 0.5f);
            Vector3 last = ToWorld(presetSize.x * 0.5f, presetSize.y * 0.5f);
            anchor = first;
            SetCorner(last, true);
            return EndRect();
        }

        private void MakeBasis()
        {
            right = Vector3.Cross(normal, Vector3.up).normalized;
            up = Vector3.Cross(right, normal);
            rotation = Quaternion.identity;
            if (gizmo != null) gizmo.Show(anchor, normal, right, up);
        }

        private void SetCorner(Vector3 raw, bool immediate)
        {
            corner = immediate ? raw : Vector3.Lerp(corner, raw, 1f - Mathf.Exp(-dragSmoothing * Time.deltaTime));
            Vector2 local = ToLocal(corner);
            float x = SnapClamp(local.x);
            float y = SnapClamp(local.y);
            Vector3 far = ToWorld(x, y);
            center = (anchor + far) * 0.5f;
            Vector3 diagonal = far - anchor;
            size = new Vector2(Mathf.Abs(Vector3.Dot(diagonal, right)), Mathf.Abs(Vector3.Dot(diagonal, up)));
            if (outline == null) return;
            outline.enabled = true;
            outline.positionCount = 4;
            Vector3 lift = normal * outlineGap;
            outline.SetPosition(0, ToWorld(0f, 0f) + lift);
            outline.SetPosition(1, ToWorld(x, 0f) + lift);
            outline.SetPosition(2, ToWorld(x, y) + lift);
            outline.SetPosition(3, ToWorld(0f, y) + lift);
        }

        private Vector2 ToLocal(Vector3 point)
        {
            Vector3 d = point - anchor;

            return new Vector2(Vector3.Dot(d, right), Vector3.Dot(d, up));
        }

        private Vector3 ToWorld(float x, float y)
        {
            return anchor + right * x + up * y;
        }

        private float SnapClamp(float value)
        {
            float magnitude = Mathf.Clamp(Mathf.Round(Mathf.Abs(value) / snap) * snap, minSize, maxSize);

            return value < 0f ? -magnitude : magnitude;
        }
    }
}
