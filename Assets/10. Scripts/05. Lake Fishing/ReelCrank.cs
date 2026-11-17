using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace StudyXR.LakeFishing
{
    [DefaultExecutionOrder(-10)]
    public class ReelCrank : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private XRSimpleInteractable knob;
        [SerializeField] private Transform crank;
        [SerializeField] private BobberCast cast;

        [Header("릴")]
        [SerializeField] private float metersPerTurn = 0.7f;
        [SerializeField] private float minRadius = 0.02f;
        [SerializeField] private float speedSmoothing = 8f;

        [Header("시뮬레이터 테스트 입력")]
        [SerializeField] private bool enableTestKeys = true;
        [SerializeField] private float testTurnsPerSecond = 1f;

        private IXRSelectInteractor holder;
        private Vector3 previous;
        private bool hasPrevious;
        private float crankAngle;
        private float metersThisFrame;

        public float TotalMeters { get; private set; }
        public float MetersPerSecond { get; private set; }

        private void OnEnable()
        {
            if (knob == null) return;
            knob.selectEntered.AddListener(OnSelectEntered);
            knob.selectExited.AddListener(OnSelectExited);
        }

        private void OnDisable()
        {
            if (knob == null) return;
            knob.selectEntered.RemoveListener(OnSelectEntered);
            knob.selectExited.RemoveListener(OnSelectExited);
        }

        private void Update()
        {
            if (holder != null) TrackHand(holder.GetAttachTransform(knob).position);
            else if (TestKeyHeld()) Turn(testTurnsPerSecond * 360f * Time.deltaTime);

            if (Time.deltaTime > 0f)
            {
                float target = metersThisFrame / Time.deltaTime;
                MetersPerSecond = Mathf.Lerp(MetersPerSecond, target, 1f - Mathf.Exp(-speedSmoothing * Time.deltaTime));
            }
            metersThisFrame = 0f;
        }

        private bool TestKeyHeld()
        {
            if (enableTestKeys == false) return false;
            Keyboard keyboard = Keyboard.current;

            if (keyboard == null) return false;
            return keyboard.f4Key.isPressed;
        }

        public void TrackHand(Vector3 handPoint)
        {
            Vector3 axis = transform.right;
            Vector3 current = Vector3.ProjectOnPlane(handPoint - crank.position, axis);

            if (current.magnitude < minRadius) return;
            if (hasPrevious == false)
            {
                previous = current;
                hasPrevious = true;
            }

            float delta = Vector3.SignedAngle(previous, current, axis);
            previous = current;

            crankAngle = Vector3.SignedAngle(-transform.up, current, axis);
            crank.localRotation = Quaternion.AngleAxis(crankAngle, Vector3.right);
            Apply(delta);
        }

        private void Turn(float degrees)
        {
            crankAngle += degrees;
            crank.localRotation = Quaternion.AngleAxis(crankAngle, Vector3.right);
            Apply(degrees);
        }

        private void Apply(float degrees)
        {
            float meters = degrees / 360f * metersPerTurn;
            TotalMeters += meters;
            metersThisFrame += meters;
            if (cast != null) cast.ReelIn(meters);
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            holder = args.interactorObject;
            hasPrevious = false;
        }

        private void OnSelectExited(SelectExitEventArgs args)
        {
            holder = null;
            hasPrevious = false;
        }
    }
}
