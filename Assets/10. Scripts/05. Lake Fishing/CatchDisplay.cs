using StudyXR.Common;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StudyXR.LakeFishing
{
    [DefaultExecutionOrder(40)]
    public class CatchDisplay : MonoBehaviour
    {
        private enum Stage { None, Lifting, Hanging, Releasing, Keeping }

        [Header("연결")]
        [SerializeField] private Transform head;
        [SerializeField] private FishingRod rod;
        [SerializeField] private BobberCast cast;
        [SerializeField] private GameObject[] visuals;
        [SerializeField] private LineRenderer leader;
        [SerializeField] private CatchLog catchLog;
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private Transform keepTarget;

        [Header("종류")]
        [SerializeField] private string[] speciesNames = { "붕어", "잉어", "송어", "블루길" };
        [SerializeField] private float[] minLengths = { 0.15f, 0.35f, 0.25f, 0.10f };
        [SerializeField] private float[] maxLengths = { 0.30f, 0.65f, 0.45f, 0.22f };
        [SerializeField] private float[] weights = { 0.4f, 0.1f, 0.2f, 0.3f };

        [Header("끌어올리기 · 매달기")]
        [SerializeField] private float liftSeconds = 0.7f;
        [SerializeField] private float liftArc = 0.6f;
        [SerializeField] private float leaderLength = 0.3f;
        [SerializeField] private float swingDamping = 0.6f;
        [SerializeField] private float kickSpeed = 0.5f;
        [SerializeField] private float flapAngle = 25f;

        [Header("결과")]
        [SerializeField] private float showSeconds = 3f;
        [SerializeField] private float autoKeepSeconds = 30f;
        [SerializeField] private float panelDistance = 1.05f;
        [SerializeField] private float panelLeft = 0.35f;
        [SerializeField] private float panelDrop = 0.02f;

        [Header("Quest 버튼")]
        [SerializeField] private bool useQuestButtons = true;

        private InputAction keepAction;
        private InputAction releaseAction;
        private float[] baseLengths;
        private float[] headOffsets;
        private int showing = -1;
        private float length;
        private float scale;
        private Stage stage;
        private float stageTime;
        private Vector3 liftFrom;
        private Vector3 mouth;
        private Vector3 mouthVelocity;
        private float nextKick;
        private float flapPhase;
        private float flapEnergy;
        private Vector3 releaseVelocity;
        private Vector3 keepFrom;
        private bool splashed;

        public bool IsShowing => (showing >= 0);

        private void Awake()
        {
            baseLengths = new float[visuals.Length];
            headOffsets = new float[visuals.Length];
            for (int i = 0; i < visuals.Length; ++i)
            {
                if (visuals[i] == null) continue;
                Measure(visuals[i], out baseLengths[i], out headOffsets[i]);
                visuals[i].SetActive(false);
            }
            if (leader != null) leader.enabled = false;
            if (resultPanel != null) resultPanel.SetActive(false);
        }

        private void OnEnable()
        {
            if (useQuestButtons == false) return;
            keepAction = QuestButtons.Create(QuestButton.A, "Keep Fish", "<Keyboard>/f12");
            releaseAction = QuestButtons.Create(QuestButton.B, "Release Fish", "<Keyboard>/f11");
            keepAction.Enable();
            releaseAction.Enable();
        }

        private void OnDisable()
        {
            DisposeAction(ref keepAction);
            DisposeAction(ref releaseAction);
        }

        private static void DisposeAction(ref InputAction action)
        {
            if (action == null) return;
            action.Disable();
            action.Dispose();
            action = null;
        }

        public string SpeciesName(int index)
        {
            return index >= 0 && index < speciesNames.Length ? speciesNames[index] : "?";
        }

        public void PickCatch(
            bool biggest,
            out int index,
            out float lengthMeters)
        {
            if (biggest)
            {
                index = 0;
                for (int i = 1; i < maxLengths.Length; ++i) if (maxLengths[i] > maxLengths[index]) index = i;
                lengthMeters = maxLengths[index] * 0.95f;
                return;
            }

            float total = 0f;
            for (int i = 0; i < weights.Length; ++i) total += weights[i];
            float roll = Random.Range(0f, total);
            index = weights.Length - 1;
            for (int i = 0; i < weights.Length; ++i)
            {
                if (roll < weights[i]) { index = i; break; }
                roll -= weights[i];
            }
            lengthMeters = Random.Range(minLengths[index], maxLengths[index]);
        }

        public static float WeightKg(float lengthMeters)
        {
            float cm = lengthMeters * 100f;

            return 0.012f * cm * cm * cm / 1000f;
        }

        public void Show(
            int index,
            float lengthMeters,
            Vector3 fromWater)
        {
            Hide();
            if (index < 0 || index >= visuals.Length || visuals[index] == null || baseLengths[index] <= 0f)
            {
                Debug.LogWarning("[LakeFishing] 보여 줄 물고기 모델이 없습니다: " + SpeciesName(index));
                if (catchLog != null) catchLog.Add(SpeciesName(index), lengthMeters, true);
                return;
            }

            showing = index;
            length = lengthMeters;
            scale = lengthMeters / baseLengths[index];
            visuals[index].transform.localScale = Vector3.one * scale;
            visuals[index].SetActive(true);

            liftFrom = fromWater;
            mouth = fromWater;
            mouthVelocity = Vector3.zero;
            flapEnergy = 1f;
            if (cast != null) cast.Locked = true;
            if (leader != null) leader.enabled = true;
            WaterFx.Splash(fromWater, 1f);
            SetStage(Stage.Lifting);
            Place();
        }

        public void Hide()
        {
            if (showing >= 0) visuals[showing].SetActive(false);
            showing = -1;
            stage = Stage.None;
            if (leader != null) leader.enabled = false;
            if (resultPanel != null) resultPanel.SetActive(false);
            if (cast != null) cast.Locked = false;
        }

        public void Keep() { Decide(true); }
        public void Release() { Decide(false); }

        private void Decide(bool keep)
        {
            if (stage != Stage.Hanging) return;

            string species = SpeciesName(showing);

            if (catchLog != null) catchLog.Add(species, length, keep);
            if (resultPanel != null) resultPanel.SetActive(false);
            if (leader != null) leader.enabled = false;
            rod.Pulse(0.3f, 0.06f);

            if (keep)
            {
                keepFrom = mouth;
                SetStage(Stage.Keeping);
            }
            else
            {
                Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
                releaseVelocity = forward * 1.6f + Vector3.up * 1.2f;
                splashed = false;
                SetStage(Stage.Releasing);
            }
        }

        private void SetStage(Stage next)
        {
            stage = next;
            stageTime = 0f;
            if (next == Stage.Hanging) ShowResult();
        }

        private void Update()
        {
            if (showing < 0) return;
            stageTime += Time.deltaTime;

            if (stage == Stage.Hanging) ReadButtons();

            if (stage == Stage.Hanging)
            {
                float limit = resultPanel != null ? autoKeepSeconds : showSeconds;

                if (stageTime >= limit) Decide(resultPanel != null);
            }
        }

        private void LateUpdate()
        {
            if (showing < 0) return;
            float dt = Mathf.Min(Time.deltaTime, 1f / 30f);
            Vector3 anchor = Anchor();

            switch (stage)
            {
                case Stage.Lifting:
                {
                    float t = Mathf.Clamp01(stageTime / liftSeconds);
                    float eased = 1f - (1f - t) * (1f - t);
                    Vector3 end = anchor + Vector3.down * leaderLength;
                    Vector3 previous = mouth;
                    mouth = Vector3.Lerp(liftFrom, end, eased) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * liftArc);
                    mouthVelocity = (mouth - previous) / Mathf.Max(dt, 0.0001f);
                    if (t >= 1f)
                    {
                        mouthVelocity *= 0.4f;
                        SetStage(Stage.Hanging);
                    }
                    break;
                }

                case Stage.Hanging:
                    Swing(anchor, dt);
                    break;

                case Stage.Releasing:
                    mouthVelocity = releaseVelocity;
                    releaseVelocity += Physics.gravity * dt;
                    mouth += releaseVelocity * dt;
                    if (splashed == false && mouth.y <= 0f)
                    {
                        splashed = true;
                        WaterFx.Splash(mouth, 0.8f);
                    }
                    if (splashed && mouth.y < -0.35f) { Hide(); return; }
                    break;

                case Stage.Keeping:
                {
                    float t = Mathf.Clamp01(stageTime / 0.6f);
                    Vector3 target = keepTarget != null ? keepTarget.position : keepFrom + Vector3.down * 0.3f;
                    mouth = Vector3.Lerp(keepFrom, target, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * 0.4f);
                    visuals[showing].transform.localScale = Vector3.one * (scale * Mathf.Lerp(1f, 0.4f, t));
                    if (t >= 1f) { Hide(); return; }
                    break;
                }
            }

            Place();
            if (leader != null && leader.enabled)
            {
                leader.SetPosition(0, anchor);
                leader.SetPosition(1, mouth);
            }
        }

        private void ReadButtons()
        {
            if (keepAction != null && keepAction.WasPressedThisFrame()) Keep();
            else if (releaseAction != null && releaseAction.WasPressedThisFrame()) Release();
        }

        private void Swing(Vector3 anchor, float dt)
        {
            if (Time.time >= nextKick)
            {
                Vector3 kick = Random.insideUnitSphere;
                kick.y = 0f;
                mouthVelocity += kick.normalized * kickSpeed * flapEnergy;
                flapEnergy = Mathf.Max(0.35f, flapEnergy * 0.85f);
                flapPhase = 0f;
                nextKick = Time.time + Random.Range(0.35f, 0.9f);
            }

            mouthVelocity += Physics.gravity * dt;
            mouthVelocity *= 1f - Mathf.Clamp01(swingDamping * dt);
            mouth += mouthVelocity * dt;

            Vector3 d = mouth - anchor;
            float distance = d.magnitude;

            if (distance > leaderLength)
            {
                Vector3 dir = d / distance;
                mouth = anchor + dir * leaderLength;
                float outward = Vector3.Dot(mouthVelocity, dir);

                if (outward > 0f) mouthVelocity -= dir * outward;
            }
        }

        private void Place()
        {
            GameObject fish = visuals[showing];
            Vector3 up = stage == Stage.Releasing ? mouthVelocity : Anchor() - mouth;

            if (stage == Stage.Keeping) up = Vector3.up;
            Vector3 forward = up.sqrMagnitude > 0.0001f ? up.normalized : Vector3.up;

            Vector3 toViewer = Vector3.ProjectOnPlane(head.position - mouth, forward);
            Vector3 right = toViewer.sqrMagnitude > 0.0001f ? toViewer.normalized : Vector3.Cross(Vector3.up, forward).normalized;
            Vector3 back = Vector3.Cross(forward, right);
            Quaternion rotation = Quaternion.LookRotation(forward, back);

            flapPhase += Time.deltaTime;
            float twist = Mathf.Sin(Time.time * 11f) * flapAngle * flapEnergy * Mathf.Exp(-flapPhase * 1.5f);
            rotation = Quaternion.AngleAxis(twist, forward) * rotation;

            float headZ = headOffsets[showing] * fish.transform.localScale.x;
            fish.transform.SetPositionAndRotation(mouth - rotation * new Vector3(0f, 0f, headZ), rotation);
        }

        private Vector3 Anchor()
        {
            if (cast != null && cast.Bobber != null) return cast.Bobber.position + Vector3.down * 0.03f;
            return rod.Tip.position;
        }

        private void ShowResult()
        {
            if (resultPanel == null) return;

            float best = catchLog != null ? catchLog.BestLength : 0f;
            bool personalBest = (length > best);
            string species = SpeciesName(showing);

            if (resultText != null)
                resultText.text = $"<size=150%><b>{species}</b></size>\n"
                    + $"길이 {length * 100f:F0} cm  ·  무게 {WeightKg(length):F2} kg\n"
                    + (personalBest ? "<color=#FFC940><b>개인 최고 기록!</b></color>\n" : $"오늘 최고 {best * 100f:F0} cm\n")
                    + $"<size=75%>{QuestButtons.Label(QuestButton.B)} : 놓아주기 · {QuestButtons.Label(QuestButton.A)} : 보관하기</size>";

            Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up);
            forward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
            Vector3 left = Vector3.Cross(forward, Vector3.up);
            Vector3 position = head.position + forward * panelDistance + left * panelLeft + Vector3.down * panelDrop;
            resultPanel.transform.SetPositionAndRotation(position, Quaternion.LookRotation(position - head.position, Vector3.up));
            resultPanel.SetActive(true);
            rod.PulsePattern(0.5f, 0.08f, 3, 0.15f);
        }

        private static void Measure(
            GameObject visual,
            out float lengthZ,
            out float headZ)
        {
            lengthZ = 0f;
            headZ = 0f;
            MeshFilter filter = visual.GetComponentInChildren<MeshFilter>(true);

            if (filter == null || filter.sharedMesh == null) return;

            Bounds b = filter.sharedMesh.bounds;
            float minZ = float.MaxValue;
            float maxZ = float.MinValue;
            for (int i = 0; i < 8; ++i)
            {
                Vector3 sign = new Vector3(
                    (i & 1) == 0 ? -1f : 1f,
                    (i & 2) == 0 ? -1f : 1f,
                    (i & 4) == 0 ? -1f : 1f);
                Vector3 corner = b.center + Vector3.Scale(b.extents, sign);
                float z = visual.transform.InverseTransformPoint(filter.transform.TransformPoint(corner)).z;
                minZ = Mathf.Min(minZ, z);
                maxZ = Mathf.Max(maxZ, z);
            }
            lengthZ = maxZ - minZ;
            headZ = maxZ;
        }
    }
}
