using UnityEngine;

namespace StudyXR.LakeFishing
{
    [DefaultExecutionOrder(20)]
    public class BobberFloat : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private BobberCast cast;

        [Header("뜨기")]
        [SerializeField] private float waterHeight = 0f;
        [SerializeField] private float stiffness = 60f;
        [SerializeField] private float damping = 8f;
        [SerializeField] private float landingSplash = 0.08f;
        [SerializeField] private float idleBob = 0.006f;
        [SerializeField] private float idleBobSpeed = 1.8f;

        private float velocityY;
        private bool wasLanded;

        public float DipDepth { get; set; }
        public float Depth => Mathf.Max(0f, waterHeight - transform.position.y);

        private void Update()
        {
            if (cast == null || cast.CurrentPhase != BobberCast.Phase.Landed)
            {
                wasLanded = false;
                velocityY = 0f;
                return;
            }

            if (wasLanded == false)
            {
                wasLanded = true;
                velocityY = cast.LandingVelocity.y * landingSplash;
            }

            float dt = Mathf.Min(Time.deltaTime, 1f / 30f);
            Vector3 p = transform.position;
            float target = waterHeight - DipDepth + idleBob * Mathf.Sin(Time.time * idleBobSpeed);
            float accel = stiffness * (target - p.y) - damping * velocityY;
            velocityY += accel * dt;
            p.y += velocityY * dt;
            transform.position = p;
        }
    }
}
