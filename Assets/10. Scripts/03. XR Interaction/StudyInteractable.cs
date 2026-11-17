using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class StudyInteractable : MonoBehaviour
{
    public XRGrabInteractable grabInteractable;

    public Material red;
    public Material green;
    public Material blue;

    private Renderer myRenderer;

    private void Start()
    {
        myRenderer = GetComponent<Renderer>();
        
        grabInteractable.hoverEntered.AddListener(SetRed);
        grabInteractable.selectEntered.AddListener(SetBlue);
        grabInteractable.selectExited.AddListener(SetGreen);
    }

    public void SetRed(BaseInteractionEventArgs eventArgs)
    {
        myRenderer.material = red;
    }
    
    public void SetGreen(BaseInteractionEventArgs eventArgs)
    {
        myRenderer.material = green;
    }
    public void SetBlue(BaseInteractionEventArgs eventArgs)
    {
        myRenderer.material = blue;
    }
}
