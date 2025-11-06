using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(XRGrabInteractable))]
public class SlideConstrainedZ : MonoBehaviour
{
    public Transform parentSpace;        // usually the gun root; slide moves in its local Z
    public float travel = 0.015f;        // meters of travel (closed → fully back)
    [Range(0.1f, 1f)] public float pullThreshold01 = 0.7f;
    public GunXR gun;                    // set in Inspector

    XRGrabInteractable grab;
    bool isGrabbed;
    bool pulledEnough;
    float closedLocalZ;                  // closed position in parent local space

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        if (!parentSpace) parentSpace = transform.parent;
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

    void Start()
    {
        closedLocalZ = parentSpace.InverseTransformPoint(transform.position).z;
    }

    void OnGrab(SelectEnterEventArgs _)
    {
        isGrabbed = true;
        pulledEnough = false;
        grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;
    }

    void OnRelease(SelectExitEventArgs _)
    {
        isGrabbed = false;
        if (pulledEnough && gun) gun.OnSlideCharged();

        // Snap forward to closed
        var lp = parentSpace.InverseTransformPoint(transform.position);
        lp.z = closedLocalZ;
        transform.position = parentSpace.TransformPoint(lp);
    }

    void LateUpdate()
    {
        if (!isGrabbed) return;

        var lp = parentSpace.InverseTransformPoint(transform.position);

        // Assume back is negative Z; tweak if your model is opposite.
        float minZ = closedLocalZ - travel; // back
        float maxZ = closedLocalZ;          // closed
        lp.z = Mathf.Clamp(lp.z, minZ, maxZ);

        transform.position = parentSpace.TransformPoint(lp);

        float t = Mathf.InverseLerp(maxZ, minZ, lp.z); // 0=closed, 1=full back
        if (t >= pullThreshold01) pulledEnough = true;
    }
}
