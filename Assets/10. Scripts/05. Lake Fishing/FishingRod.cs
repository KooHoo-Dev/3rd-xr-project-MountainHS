using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace StudyXR.LakeFishing
{
    [DefaultExecutionOrder(-20)]
    [RequireComponent(typeof(XRGrabInteractable))]
    public class FishingRod : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private Transform tip;

        [Header("초리 속도")]
        [SerializeField] private int sampleCount = 5;

        [Header("놓았을 때")]
        [SerializeField] private bool returnOnRelease = true;

        [Header("햅틱")]
        [SerializeField] private float grabAmplitude = 0.25f;
        [SerializeField] private float grabDuration = 0.06f;

        public event Action TriggerPressed;
        public event Action TriggerReleased;

        private XRGrabInteractable grab;
        private HapticImpulsePlayer haptics;
        private Vector3 restPosition;
        private Quaternion restRotation;
        private Vector3[] samples;
        private int sampleIndex;
        private Vector3 previousTip;
        private int patternLeft;
        private float patternAmplitude;
        private float patternDuration;
        private float patternGap;
        private float nextPatternTime;

        private bool triggerHeld;
        public Transform Tip => tip;
        public Vector3 TipVelocity { get; private set; }
        public float LastHapticAmplitude { get; private set; }
        public float LastHapticTime { get; private set; } = -100f;

        private void Awake()
        {
            grab = GetComponent<XRGrabInteractable>();
            restPosition = transform.position;
            restRotation = transform.rotation;
            samples = new Vector3[Mathf.Max(1, sampleCount)];
            previousTip = tip.position;
        }

        private void OnEnable()
        {
            grab.selectEntered.AddListener(OnSelectEntered);
            grab.selectExited.AddListener(OnSelectExited);
            grab.activated.AddListener(OnActivated);
            grab.deactivated.AddListener(OnDeactivated);
        }

        private void OnDisable()
        {
            grab.selectEntered.RemoveListener(OnSelectEntered);
            grab.selectExited.RemoveListener(OnSelectExited);
            grab.activated.RemoveListener(OnActivated);
            grab.deactivated.RemoveListener(OnDeactivated);
        }

        public void Pulse(float amplitude, float duration)
        {
            LastHapticAmplitude = amplitude;
            LastHapticTime = Time.time;
            if (haptics != null) haptics.SendHapticImpulse(amplitude, duration);
        }

        public void PulsePattern(
            float amplitude,
            float duration,
            int count,
            float gap)
        {
            patternAmplitude = amplitude;
            patternDuration = duration;
            patternGap = gap;
            patternLeft = count;
            nextPatternTime = Time.time;
        }

        public void ClearSamples()
        {
            Array.Clear(samples, 0, samples.Length);
            previousTip = tip.position;
            TipVelocity = Vector3.zero;
        }

        private void Update()
        {
            if (patternLeft > 0 && Time.time >= nextPatternTime)
            {
                Pulse(patternAmplitude, patternDuration);
                patternLeft--;
                nextPatternTime = Time.time + patternGap;
            }
        }

        private void LateUpdate()
        {
            Vector3 current = tip.position;

            if (Time.deltaTime > 0f)
            {
                samples[sampleIndex] = (current - previousTip) / Time.deltaTime;
                sampleIndex = (sampleIndex + 1) % samples.Length;
                Vector3 sum = Vector3.zero;
                for (int i = 0; i < samples.Length; ++i) sum += samples[i];
                TipVelocity = sum / samples.Length;
            }
            previousTip = current;
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            haptics = args.interactorObject.transform.GetComponentInParent<HapticImpulsePlayer>();
            Pulse(grabAmplitude, grabDuration);
        }

        private void OnSelectExited(SelectExitEventArgs args)
        {
            if (triggerHeld) OnDeactivated(null);
            haptics = null;
            if (returnOnRelease) transform.SetPositionAndRotation(restPosition, restRotation);
        }

        private void OnActivated(ActivateEventArgs args)
        {
            triggerHeld = true;
            if (TriggerPressed != null) TriggerPressed.Invoke();
        }

        private void OnDeactivated(DeactivateEventArgs args)
        {
            if (triggerHeld == false) return;
            triggerHeld = false;
            if (TriggerReleased != null) TriggerReleased.Invoke();
        }
    }
}
