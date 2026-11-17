using UnityEngine;

namespace StudyXR.MRAquarium
{
    public class FishSchool : MonoBehaviour
    {
        private static readonly int SwimPhaseId = Shader.PropertyToID("_SwimPhase");
        private static readonly int SwimAxisId = Shader.PropertyToID("_SwimAxis");
        private static readonly int SwimSideId = Shader.PropertyToID("_SwimSide");

        [Header("연결")]
        [SerializeField] private Transform tank;
        [SerializeField] private Transform[] fish;

        [Header("수조")]
        [SerializeField] private Vector3 tankSize = new Vector3(0.9f, 0.6f, 0.8f);
        [SerializeField] private float floorY = -0.25f;
        [SerializeField] private float frontGap = 0.1f;
        [SerializeField] private float margin = 0.07f;

        [Header("속도")]
        [SerializeField] private float minSpeed = 0.05f;
        [SerializeField] private float maxSpeed = 0.16f;
        [SerializeField] private float turnSharpness = 5f;

        [Header("무리 (boids)")]
        [SerializeField] private float neighborRadius = 0.25f;
        [SerializeField] private float separationRadius = 0.07f;
        [SerializeField] private float cohesion = 0.5f;
        [SerializeField] private float alignment = 0.8f;
        [SerializeField] private float separation = 1.6f;
        [SerializeField] private float boundsWeight = 3f;
        [SerializeField] private float wander = 0.25f;

        [Header("반응")]
        [SerializeField] private float scatterWeight = 4f;
        [SerializeField] private float scatterBoost = 2.5f;
        [SerializeField] private float scatterTime = 1.2f;
        [SerializeField] private float gatherWeight = 1.5f;

        [Header("헤엄 셰이더")]
        [SerializeField] private float tailRate = 7f;
        [SerializeField] private float tailRateBySpeed = 25f;
        [SerializeField] private int seed = 5;

        private Vector3[] pos;
        private Vector3[] vel;
        private float[] phase;
        private Renderer[] bodies;
        private MaterialPropertyBlock block;
        private float scatterUntil;
        private Vector3 scatterPoint;
        private Transform gatherTarget;
        private float gatherUntil;

        public int FishCount => fish != null ? fish.Length : 0;
        public float FloorY => floorY;
        public string ModeText => Time.time < scatterUntil ?
            "흩어짐"
            : gatherTarget != null && Time.time < gatherUntil
                ? "먹이로 모임"
                : "헤엄";

        private void Awake()
        {
            int n = FishCount;
            pos = new Vector3[n];
            vel = new Vector3[n];
            phase = new float[n];
            bodies = new Renderer[n];
            block = new MaterialPropertyBlock();
            for (int i = 0; i < n; ++i)
            {
                if (fish[i] == null) continue;
                pos[i] = fish[i].localPosition;
                vel[i] = fish[i].localRotation * Vector3.forward * minSpeed;
                phase[i] = i * 1.7f;
                bodies[i] = fish[i].GetComponentInChildren<Renderer>();
                SetupSwim(i);
            }
        }

        private void SetupSwim(int i)
        {
            Renderer body = bodies[i];

            if (body == null) return;
            MeshFilter filter = body.GetComponent<MeshFilter>();

            if (filter == null || filter.sharedMesh == null) return;
            Bounds b = filter.sharedMesh.bounds;
            Vector3 axis = body.transform.InverseTransformDirection(fish[i].forward).normalized;
            Vector3 side = body.transform.InverseTransformDirection(fish[i].right).normalized;
            float center = Vector3.Dot(b.center, axis);
            float half = Mathf.Abs(b.extents.x * axis.x) + Mathf.Abs(b.extents.y * axis.y) + Mathf.Abs(b.extents.z * axis.z);
            body.GetPropertyBlock(block);
            block.SetVector(SwimAxisId, new Vector4(axis.x, axis.y, axis.z, center));
            block.SetVector(SwimSideId, new Vector4(side.x, side.y, side.z, half));
            body.SetPropertyBlock(block);
        }

        public void SetBounds(Vector3 size, float floor)
        {
            tankSize = size;
            floorY = floor;
        }

        public void Respawn()
        {
            if (pos == null) return;
            var rng = new System.Random(seed);
            Vector3 min = MinCorner();
            Vector3 max = MaxCorner();
            for (int i = 0; i < pos.Length; ++i)
            {
                if (fish[i] == null) continue;
                pos[i] = new Vector3(
                    Mathf.Lerp(min.x, max.x, (float)rng.NextDouble()),
                    Mathf.Lerp(min.y, max.y, (float)rng.NextDouble()),
                    Mathf.Lerp(min.z, max.z, (float)rng.NextDouble()));
                vel[i] = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f) * Vector3.forward * (minSpeed + maxSpeed) * 0.5f;
                fish[i].localPosition = pos[i];
                fish[i].localRotation = Quaternion.LookRotation(vel[i], Vector3.up);
            }
        }

        public void Scatter(Vector3 worldPoint)
        {
            if (tank == null) return;
            scatterPoint = tank.InverseTransformPoint(worldPoint);
            scatterUntil = Time.time + scatterTime;
        }

        public void Gather(Transform target, float duration)
        {
            gatherTarget = target;
            gatherUntil = Time.time + duration;
        }

        private void Update()
        {
            if (pos == null || tank == null) return;
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            float now = Time.time;
            float scatter01 = Mathf.Clamp01((scatterUntil - now) / scatterTime);
            bool gathering = gatherTarget != null && now < gatherUntil;
            Vector3 food = gathering ? tank.InverseTransformPoint(gatherTarget.position) : Vector3.zero;
            float top = maxSpeed * Mathf.Lerp(1f, scatterBoost, scatter01);

            for (int i = 0; i < pos.Length; ++i)
            {
                if (fish[i] == null) continue;
                Vector3 p = pos[i];
                Vector3 steer = Flock(i) + BoundsForce(p) * boundsWeight + Wander(i, now) * wander;

                if (gathering)
                {
                    Vector3 to = food - p;
                    steer += to.normalized * gatherWeight * Mathf.Clamp01(to.magnitude / 0.12f);
                }
                if (scatter01 > 0f) steer += (p - scatterPoint).normalized * scatterWeight * scatter01;

                Vector3 v = vel[i] + steer * dt;
                float speed = Mathf.Clamp(v.magnitude, minSpeed, top);
                v = v.sqrMagnitude > 1e-8f ? v.normalized * speed : Vector3.forward * minSpeed;
                v.y = Mathf.Clamp(v.y, -0.35f * speed, 0.35f * speed);
                p = ClampInside(p + v * dt);
                pos[i] = p;
                vel[i] = v;

                fish[i].localPosition = p;
                Quaternion look = Quaternion.LookRotation(v, Vector3.up);
                fish[i].localRotation = Quaternion.Slerp(fish[i].localRotation, look, 1f - Mathf.Exp(-turnSharpness * dt));
                phase[i] += (tailRate + speed * tailRateBySpeed) * dt;
                if (bodies[i] != null)
                {
                    bodies[i].GetPropertyBlock(block);
                    block.SetFloat(SwimPhaseId, phase[i]);
                    bodies[i].SetPropertyBlock(block);
                }
            }
        }

        private Vector3 Flock(int i)
        {
            Vector3 p = pos[i];
            Vector3 push = Vector3.zero;
            Vector3 heading = Vector3.zero;
            Vector3 middle = Vector3.zero;
            int count = 0;
            for (int j = 0; j < pos.Length; ++j)
            {
                if (j == i || fish[j] == null) continue;
                Vector3 d = pos[j] - p;
                float dist = d.magnitude;

                if (dist < neighborRadius) { heading += vel[j]; middle += pos[j]; count++; }
                if (dist < separationRadius && dist > 0.0001f) push -= d / dist * (1f - dist / separationRadius);
            }
            Vector3 steer = push * separation;

            if (count > 0) steer += (heading / count - vel[i]) * alignment + (middle / count - p) * cohesion;
            return steer;
        }

        private Vector3 BoundsForce(Vector3 p)
        {
            Vector3 min = MinCorner();
            Vector3 max = MaxCorner();

            return new Vector3(
                Push(p.x, min.x, max.x),
                Push(p.y, min.y, max.y),
                Push(p.z, min.z, max.z));
        }

        private float Push(
            float v,
            float lo,
            float hi)
        {
            if (v < lo) return (lo - v) / margin;
            if (v > hi) return -(v - hi) / margin;
            return 0f;
        }

        private static Vector3 Wander(int i, float t)
        {
            return new Vector3(
                Mathf.PerlinNoise(t * 0.3f, i * 7.1f) - 0.5f,
                (Mathf.PerlinNoise(i * 3.3f, t * 0.3f) - 0.5f) * 0.4f,
                Mathf.PerlinNoise(t * 0.3f + 40f, i * 5.7f) - 0.5f) * 2f;
        }

        private Vector3 MinCorner()
            => new Vector3(
                -tankSize.x * 0.5f + margin,
                floorY + margin * 0.5f,
                frontGap);
        private Vector3 MaxCorner()
            => new Vector3(
                tankSize.x * 0.5f - margin,
                tankSize.y * 0.5f - margin,
                tankSize.z - margin);

        private Vector3 ClampInside(Vector3 p)
        {
            return new Vector3(
                Mathf.Clamp(p.x, -tankSize.x * 0.5f + 0.02f, tankSize.x * 0.5f - 0.02f),
                Mathf.Clamp(p.y, floorY, tankSize.y * 0.5f - 0.02f),
                Mathf.Clamp(p.z, 0.05f, tankSize.z - 0.02f));
        }
    }
}
