using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class EnableGrabInZone : MonoBehaviour
{
    public XRGrabInteractable target;
    public InteractionLayerMask allowedLayers;
    InteractionLayerMask originalLayers;
    int handsInside = 0;

    void Awake()
    {
        if (target) originalLayers = target.interactionLayers;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!target) return;
        if (other.GetComponentInParent<XRBaseInteractor>())
        {
            handsInside++;
            target.interactionLayers = allowedLayers; // enable grab
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!target) return;
        if (other.GetComponentInParent<XRBaseInteractor>())
        {
            handsInside = Mathf.Max(0, handsInside - 1);
            if (handsInside == 0) target.interactionLayers = originalLayers; // disable grab
        }
    }
}
