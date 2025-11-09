using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(XRGrabInteractable))]
public class SlideHandleXR : MonoBehaviour
{
    public SlideRail rail;  // drag the SlideRail from the gun here

    XRGrabInteractable grab;

    void Awake() { grab = GetComponent<XRGrabInteractable>(); }

    void OnEnable()
    {
        grab.selectEntered.AddListener(OnGrab);
        grab.selectExited.AddListener(OnRelease);
        grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;
        grab.throwOnDetach = false;
    }

    void OnDisable()
    {
        grab.selectEntered.RemoveListener(OnGrab);
        grab.selectExited.RemoveListener(OnRelease);
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        rail?.OnGrab(args.interactorObject as XRBaseInteractor);
    }

    void OnRelease(SelectExitEventArgs _)
    {
        rail?.OnRelease();
    }
}
