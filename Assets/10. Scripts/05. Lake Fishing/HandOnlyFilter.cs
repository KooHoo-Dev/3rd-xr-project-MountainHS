using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace StudyXR.LakeFishing
{
    public class HandOnlyFilter : MonoBehaviour, IXRHoverFilter, IXRSelectFilter
    {
        [SerializeField] private InteractorHandedness allowedHand = InteractorHandedness.Right;
        [SerializeField] private float maxDistance = 0f;

        public bool canProcess => isActiveAndEnabled;

        public bool Process(IXRHoverInteractor interactor, IXRHoverInteractable interactable)
            => IsAllowed(interactor) && IsNear(interactor);

        public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
            => IsAllowed(interactor) && IsNear(interactor);

        private bool IsNear(IXRInteractor interactor)
        {
            if (maxDistance <= 0f || interactor == null) return true;
            return (interactor.transform.position - transform.position).sqrMagnitude <= maxDistance * maxDistance;
        }

        private bool IsAllowed(IXRInteractor interactor)
        {
            if (interactor == null) return false;
            return interactor.handedness == InteractorHandedness.None || interactor.handedness == allowedHand;
        }
    }
}
