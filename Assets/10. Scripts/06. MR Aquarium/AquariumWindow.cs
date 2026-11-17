using UnityEngine;

namespace StudyXR.MRAquarium
{
    public class AquariumWindow : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private WallRectDrawer drawer;
        [SerializeField] private Transform windowRoot;
        [SerializeField] private Transform portalMask;
        [SerializeField] private Transform glass;
        [SerializeField] private Transform frameTop;
        [SerializeField] private Transform frameBottom;
        [SerializeField] private Transform frameLeft;
        [SerializeField] private Transform frameRight;
        [SerializeField] private Transform shell;
        [SerializeField] private Transform sandBed;
        [SerializeField] private Transform[] fogCards;
        [SerializeField] private Transform[] decor;
        [SerializeField] private ParticleSystem[] bubbles;
        [SerializeField] private FishSchool school;

        [Header("크기")]
        [SerializeField] private Vector3 designSize = new Vector3(0.9f, 0.6f, 0.8f);
        [SerializeField] private float depthPerSize = 0.9f;
        [SerializeField] private float minDepth = 0.6f;
        [SerializeField] private float maxDepth = 1.0f;
        [SerializeField] private float frameThickness = 0.04f;
        [SerializeField] private float wallGap = 0.003f;
        [SerializeField] private float sandTop = 0.05f;
        [SerializeField] private float bubbleSpeed = 0.12f;
        [SerializeField] private float minDecorScale = 0.5f;
        [SerializeField] private float maxDecorScale = 1.3f;

        [Header("시작")]
        [SerializeField] private bool showOnStart = false;
        [SerializeField] private Vector3 startPosition = new Vector3(0f, 1.3f, 1.5f);
        [SerializeField] private Vector2 startSize = new Vector2(0.9f, 0.6f);

        private Vector2[] decorLayout;
        private float[] decorLift;
        private Vector3[] decorScale;
        private Vector2[] bubbleLayout;
        private Vector2 sandFootprint = Vector2.one;

        public bool IsShown { get; private set; }
        public bool ShownByDrawer { get; private set; }
        public Vector2 Size { get; private set; }
        public float Depth { get; private set; }
        public Transform Window => windowRoot;

        private void Awake()
        {
            int n = decor != null ? decor.Length : 0;
            decorLayout = new Vector2[n];
            decorLift = new float[n];
            decorScale = new Vector3[n];
            float designFloor = -designSize.y * 0.5f + sandTop;
            for (int i = 0; i < n; ++i)
            {
                if (decor[i] == null) continue;
                Vector3 p = decor[i].localPosition;
                decorLayout[i] = new Vector2(p.x / designSize.x, p.z / designSize.z);
                decorLift[i] = p.y - designFloor;
                decorScale[i] = decor[i].localScale;
            }
            if (sandBed != null)
            {
                MeshFilter sand = sandBed.GetComponent<MeshFilter>();

                if (sand != null && sand.sharedMesh != null)
                    sandFootprint = new Vector2(
                        Mathf.Max(sand.sharedMesh.bounds.size.x, 0.01f),
                        Mathf.Max(sand.sharedMesh.bounds.size.z, 0.01f));
            }
            int b = bubbles != null ? bubbles.Length : 0;
            bubbleLayout = new Vector2[b];
            for (int i = 0; i < b; ++i)
                if (bubbles[i] != null)
                    bubbleLayout[i] = new Vector2(
                        bubbles[i].transform.localPosition.x / designSize.x,
                        bubbles[i].transform.localPosition.z / designSize.z);
        }

        private void OnEnable()
        {
            if (drawer != null) drawer.RectFinished += OnRectFinished;
        }

        private void OnDisable()
        {
            if (drawer != null) drawer.RectFinished -= OnRectFinished;
        }

        private void Start()
        {
            if (showOnStart) Show(new Pose(startPosition, Quaternion.identity), startSize);
            else if (windowRoot != null && IsShown == false) windowRoot.gameObject.SetActive(false);
        }

        private void OnRectFinished(Pose pose, Vector2 size)
        {
            ShownByDrawer = true;
            Show(pose, size);
        }

        public void Show(Pose pose, Vector2 size)
        {
            if (windowRoot == null) return;
            windowRoot.gameObject.SetActive(true);
            float w = size.x;
            float h = size.y;
            float d = Mathf.Clamp(Mathf.Max(w, h) * depthPerSize, minDepth, maxDepth);
            float s = Mathf.Clamp(h / designSize.y, minDecorScale, maxDecorScale);
            float floor = -h * 0.5f + sandTop * s;

            windowRoot.SetPositionAndRotation(pose.position + pose.rotation * new Vector3(0f, 0f, -wallGap), pose.rotation);
            Place(portalMask, new Vector3(0f, 0f, -0.002f), new Vector3(w, h, 1f));
            Place(glass, new Vector3(0f, 0f, -0.004f), new Vector3(w, h, 1f));
            PlaceFrame(w, h);
            Place(shell, new Vector3(0f, 0f, d * 0.5f), new Vector3(w, h, d));
            if (fogCards != null)
                for (int i = 0; i < fogCards.Length; ++i)
                    Place(fogCards[i], new Vector3(0f, 0f, d * (i + 1f) / (fogCards.Length + 1f)), new Vector3(w, h, 1f));
            Place(sandBed, new Vector3(0f, -h * 0.5f, d * 0.5f), new Vector3(w / sandFootprint.x, s, d / sandFootprint.y));

            for (int i = 0; i < decorLayout.Length; ++i)
            {
                if (decor[i] == null) continue;
                decor[i].localPosition = new Vector3(
                    decorLayout[i].x * w,
                    floor + decorLift[i] * s,
                    decorLayout[i].y * d);
                decor[i].localScale = decorScale[i] * s;
            }
            for (int i = 0; i < bubbleLayout.Length; ++i)
            {
                if (bubbles[i] == null) continue;
                bubbles[i].transform.localPosition = new Vector3(bubbleLayout[i].x * w, floor, bubbleLayout[i].y * d);
                ParticleSystem.MainModule main = bubbles[i].main;
                main.startSpeed = bubbleSpeed;
                main.startLifetime = Mathf.Max(0.5f, (h * 0.5f - floor) / bubbleSpeed);
            }
            if (school != null)
            {
                school.SetBounds(new Vector3(w, h, d), floor + 0.04f);
                school.Respawn();
            }
            Size = size;
            Depth = d;
            IsShown = true;
        }

        private void PlaceFrame(float w, float h)
        {
            float t = frameThickness;
            Bar(frameTop, new Vector3(0f, h * 0.5f + t * 0.5f, -t * 0.5f), 0f, w + 2f * t);
            Bar(frameBottom, new Vector3(0f, -h * 0.5f - t * 0.5f, -t * 0.5f), 0f, w + 2f * t);
            Bar(frameLeft, new Vector3(-w * 0.5f - t * 0.5f, 0f, -t * 0.5f), 90f, h);
            Bar(frameRight, new Vector3(w * 0.5f + t * 0.5f, 0f, -t * 0.5f), 90f, h);
        }

        private static void Bar(
            Transform bar,
            Vector3 localPos,
            float roll,
            float length)
        {
            if (bar == null) return;
            bar.localPosition = localPos;
            bar.localRotation = Quaternion.Euler(0f, 0f, roll);
            bar.localScale = new Vector3(length, 1f, 1f);
        }

        private static void Place(
            Transform t,
            Vector3 localPos,
            Vector3 localScale)
        {
            if (t == null) return;
            t.localPosition = localPos;
            t.localScale = localScale;
        }
    }
}
