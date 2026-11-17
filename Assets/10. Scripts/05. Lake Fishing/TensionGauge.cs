using UnityEngine;
using UnityEngine.UI;

namespace StudyXR.LakeFishing
{
    [DefaultExecutionOrder(60)]
    public class TensionGauge : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private FishFight fight;
        [SerializeField] private BobberCast cast;
        [SerializeField] private Transform head;
        [SerializeField] private GameObject visualRoot;
        [SerializeField] private Image tensionFill;
        [SerializeField] private Image staminaFill;
        [SerializeField] private Image fishIcon;

        [Header("자리 (수면 위 HUD)")]
        [SerializeField] private float waterHeight = 0f;
        [SerializeField] private float gapAboveWater = 0.3f;
        [SerializeField] private bool drawOnTop = true;
        [SerializeField] private Material overlayMaterial;

        [Header("모양")]
        [SerializeField] private float angularSize = 9f;
        [SerializeField] private float minDiameter = 0.25f;
        [SerializeField] private float baseSize = 0.25f;
        [SerializeField] private Color safeColor = new Color(0.35f, 0.85f, 1.00f);
        [SerializeField] private Color warnColor = new Color(1.00f, 0.80f, 0.20f);
        [SerializeField] private Color dangerColor = new Color(1.00f, 0.25f, 0.20f);
        [SerializeField] private float shakeFrom = 0.85f;
        [SerializeField] private float shakeAmount = 0.02f;

        private float shownTension;

        private void Awake()
        {
            if (drawOnTop) UseOverlayMaterial();
        }

        private void Start()
        {
            if (visualRoot != null) visualRoot.SetActive(false);
        }

        private void UseOverlayMaterial()
        {
            if (visualRoot == null || overlayMaterial == null) return;
            foreach (Graphic graphic in visualRoot.GetComponentsInChildren<Graphic>(true))
                graphic.material = overlayMaterial;
        }

        private void LateUpdate()
        {
            bool show = fight != null && fight.IsFighting && cast.CurrentPhase == BobberCast.Phase.Landed;

            if (visualRoot != null && visualRoot.activeSelf != show) visualRoot.SetActive(show);
            if (show == false) { shownTension = 0f; return; }

            float tension01 = Mathf.Clamp01(fight.Tension / BiteController.BreakTension);
            shownTension = Mathf.Lerp(shownTension, tension01, 1f - Mathf.Exp(-10f * Time.deltaTime));

            tensionFill.fillAmount = shownTension;
            tensionFill.color = shownTension < 0.6f
                ? Color.Lerp(safeColor, warnColor, shownTension / 0.6f)
                : Color.Lerp(warnColor, dangerColor, (shownTension - 0.6f) / 0.4f);
            if (staminaFill != null) staminaFill.fillAmount = fight.Stamina01;

            Vector3 fish = cast.Bobber.position;
            Vector3 surface = new Vector3(fish.x, waterHeight + gapAboveWater, fish.z);
            float distance = Vector3.Distance(head.position, surface);
            float diameter = Mathf.Max(minDiameter, 2f * distance * Mathf.Tan(angularSize * 0.5f * Mathf.Deg2Rad));
            Vector3 anchor = surface + Vector3.up * (diameter * 0.5f);

            Vector3 shake = Vector3.zero;

            if (shownTension > shakeFrom)
            {
                float k = (shownTension - shakeFrom) / (1f - shakeFrom);
                shake = Random.insideUnitSphere * shakeAmount * distance * k;
                shake.y = Mathf.Abs(shake.y);
            }

            transform.position = anchor + shake;
            transform.rotation = Quaternion.LookRotation(transform.position - head.position, Vector3.up);
            transform.localScale = Vector3.one * (diameter / baseSize);

            if (fishIcon != null)
            {
                float pull01 = Mathf.Clamp01(fight.Pull / BiteController.BreakTension);
                fishIcon.rectTransform.localScale = Vector3.one * (0.9f + 0.25f * pull01);
            }
        }
    }
}
