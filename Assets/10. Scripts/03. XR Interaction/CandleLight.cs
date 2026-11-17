using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace StudyXR.XRInteraction
{
    public class CandleLight : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private XRGrabInteractable grab;
        [SerializeField] private GameObject flame;
        [SerializeField] private Light flameLight;

        [Header("촛불")]
        [SerializeField] private bool startLit = false;

        public event Action<bool> LitChanged;
        public bool IsLit { get; private set; }

        private void OnEnable()
        {
            if (grab != null) grab.activated.AddListener(OnActivated);
        }

        private void OnDisable()
        {
            if (grab != null) grab.activated.RemoveListener(OnActivated);
        }

        private void Start()
        {
            Apply(startLit);
        }

        private void OnActivated(ActivateEventArgs args)
        {
            SetLit(!IsLit);
        }

        public void SetLit(bool lit)
        {
            bool changed = (IsLit != lit);
            Apply(lit);
            if (changed) LitChanged?.Invoke(lit);
        }

        private void Apply(bool lit)
        {
            IsLit = lit;
            if (flame != null) flame.SetActive(lit);
            if (flameLight != null) flameLight.enabled = lit;
        }
    }
}
