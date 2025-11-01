using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(ConfigurableJoint))]
[RequireComponent(typeof(XRGrabInteractable))]
public class SlideJointXR : MonoBehaviour
{
    public GunXR gun;                 // assign your gun here
    public float pullThreshold01 = 0.7f;  // 0..1 of the joint limit

    ConfigurableJoint joint;
    XRGrabInteractable grab;
    bool isGrabbed;
    bool pulledEnough;

    float maxZ;   // joint linear limit (how far it can move)

    void Awake()
    {
        joint = GetComponent<ConfigurableJoint>();
        grab = GetComponent<XRGrabInteractable>();

        // joint.linearLimit.limit is the distance in meters it can move along X
        maxZ = joint.linearLimit.limit;   // e.g. 0.01
    }

    void OnEnable()
    {
        grab.selectEntered.AddListener(OnGrab);
        grab.selectExited.AddListener(OnRelease);
    }

    void OnDisable()
    {
        grab.selectEntered.RemoveListener(OnGrab);
        grab.selectExited.RemoveListener(OnRelease);
    }

    void OnGrab(SelectEnterEventArgs _)
    {
        isGrabbed = true;
        pulledEnough = false;
    }

    void OnRelease(SelectExitEventArgs _)
    {
        isGrabbed = false;

        if (pulledEnough && gun != null)
        {
            gun.OnSlideCharged();   // <- you add this to your gun script
        }
    }

    void Update()
    {
        // current slide position in joint space = localPosition.x relative to anchor
        // since Axis is X=1, movement is along local X
        float currentX = transform.localPosition.x;  // should be 0 (forward) to +maxX (back) or maybe negative – check!

        // depending on your model, the slide might move in negative X.
        // let's normalize it:
        float dist = Mathf.Abs(currentX);  // 0 .. maxX
        float t = Mathf.Clamp01(dist / maxZ);

        if (isGrabbed && t >= pullThreshold01)
        {
            pulledEnough = true;
        }
    }

    // called by gun when we fire last shot
    public void LockSlideOpen()
    {
        // easiest: just LOCK x motion
        joint.zMotion = ConfigurableJointMotion.Locked;
    }

    public void UnlockSlide()
    {
        joint.zMotion = ConfigurableJointMotion.Limited;
    }
}
