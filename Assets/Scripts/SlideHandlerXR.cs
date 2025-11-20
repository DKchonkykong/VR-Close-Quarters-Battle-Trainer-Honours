using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(XRGrabInteractable))]
public class SlideHandleXR : MonoBehaviour
{
    public SlideRail rail;  // drag the SlideRail from the gun here

    XRGrabInteractable grab;

    void Awake() 
    { 
        grab = GetComponent<XRGrabInteractable>(); 
    }

    void OnEnable()
    {
        if (!grab) return;

        grab.selectEntered.AddListener(OnGrab);
        grab.selectExited.AddListener(OnRelease);
        
        // Critical: Set movement type so the slide doesn't move the whole gun
        grab.movementType = XRBaseInteractable.MovementType.Kinematic;
        grab.throwOnDetach = false;
        
        // Prevent the slide from moving its transform - rail handles positioning
        grab.trackPosition = false;
        grab.trackRotation = false;
        
        // Make sure it's not trying to parent to the hand
        grab.retainTransformParent = true;
    }

    void OnDisable()
    {
        if (!grab) return;
        
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
