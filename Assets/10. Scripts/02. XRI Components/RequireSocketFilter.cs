using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace StudyXR.XRIComponents
{
    public class RequireSocketFilter : MonoBehaviour, IXRSelectFilter
    {
        [Header("연결")]
        [SerializeField] private XRSocketInteractor requiredSocket;

        public bool canProcess => isActiveAndEnabled;

        public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
        {
            return requiredSocket == null || requiredSocket.hasSelection || interactor.IsSelecting(interactable);
        }
    }
}
