using UnityEngine;

namespace StudyXR.LakeFishing
{
    public class WaterFx : MonoBehaviour
    {
        private const int RippleCount = 6;
        private static readonly int RipplesId = Shader.PropertyToID("_WaterRipples");
        private static readonly Vector4[] ripples = new Vector4[RippleCount];
        private static int nextSlot;

        [Header("연결")]
        [SerializeField] private ParticleSystem droplets;

        [Header("물방울")]
        [SerializeField] private int dropletsAtFull = 28;
        [SerializeField] private float waterHeight = 0f;

        public static WaterFx Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            for (int i = 0; i < RippleCount; ++i) ripples[i] = new Vector4(0f, 0f, -1000f, 0f);
            nextSlot = 0;
            Instance = null;
        }

        private void Awake()
        {
            Instance = this;
            for (int i = 0; i < RippleCount; ++i) ripples[i] = new Vector4(0f, 0f, -1000f, 0f);
            Shader.SetGlobalVectorArray(RipplesId, ripples);
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            Instance = null;
            for (int i = 0; i < RippleCount; ++i) ripples[i] = new Vector4(0f, 0f, -1000f, 0f);
            Shader.SetGlobalVectorArray(RipplesId, ripples);
        }

        public static void Ripple(Vector3 position, float strength)
        {
            ripples[nextSlot] = new Vector4(position.x, position.z, Time.time, Mathf.Clamp01(strength));
            nextSlot = (nextSlot + 1) % RippleCount;
            Shader.SetGlobalVectorArray(RipplesId, ripples);
        }

        public static void Splash(Vector3 position, float strength)
        {
            Ripple(position, strength);
            if (Instance != null) Instance.Emit(position, strength);
        }

        private void Emit(Vector3 position, float strength)
        {
            if (droplets == null) return;
            int count = Mathf.RoundToInt(dropletsAtFull * Mathf.Clamp01(strength));

            if (count <= 0) return;

            droplets.transform.position = new Vector3(position.x, waterHeight + 0.02f, position.z);
            for (int i = 0; i < count; ++i)
            {
                Vector3 dir = Random.insideUnitSphere;
                dir.y = Mathf.Abs(dir.y) * 2.2f + 0.6f;
                var emit = new ParticleSystem.EmitParams
                {
                    applyShapeToPosition = true,
                    velocity = dir.normalized * Random.Range(1.0f, 2.6f) * Mathf.Lerp(0.6f, 1.2f, strength),
                    startSize = Random.Range(0.012f, 0.035f) * Mathf.Lerp(0.7f, 1.3f, strength),
                };
                droplets.Emit(emit, 1);
            }
        }
    }
}
