using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(XRGrabInteractable))]
public class GrenadeThrowXR : MonoBehaviour
{
    [Header("Throw Tuning")]
    public float throwMultiplier = 1.2f;
    public float maxThrowSpeed = 10f;
    public float upwardBias = 0.3f;

    [Header("Arc Preview")]
    public bool showArcWhileHeld = true;
    public LineRenderer arcLine;
    public int arcSteps = 30;
    public float arcTimeStep = 0.05f;
    public LayerMask arcCollisionMask = ~0;

    private Rigidbody rb;
    private XRGrabInteractable grab;

    private bool held;
    private bool hasBeenThrown;

    private Transform handTransform;
    private InputDevice device;
    private XRNode controllerNode = XRNode.RightHand; // default; updated on grab

    private Vector3 deviceVelWorld;
    private Vector3 deviceAngVelWorld;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grab = GetComponent<XRGrabInteractable>();

        // IMPORTANT: prevents "stuck to hand" after release
        grab.retainTransformParent = false;

        if (!arcLine && showArcWhileHeld)
            arcLine = GetComponentInChildren<LineRenderer>();

        if (arcLine)
        {
            arcLine.enabled = false;
            arcLine.useWorldSpace = true;
            arcLine.positionCount = arcSteps;
            // Add these for smoother arc visuals:
            arcLine.numCapVertices = 2;
            arcLine.numCornerVertices = 5;
            arcLine.alignment = LineAlignment.View;
        }

        grab.selectEntered.AddListener(OnGrab);
        grab.selectExited.AddListener(OnRelease);
    }

    void OnDestroy()
    {
        grab.selectEntered.RemoveListener(OnGrab);
        grab.selectExited.RemoveListener(OnRelease);
    }

    void OnEnable()
    {
        // Reset device tracking when script is enabled (e.g., after scene reload)
        device = default;
        held = false;
        hasBeenThrown = false;
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        held = true;
        hasBeenThrown = false;

        // Use the interactor's attach transform as "hand"
        handTransform = args.interactorObject.GetAttachTransform(grab);
        if (!handTransform) handTransform = args.interactorObject.transform;

        // Determine which controller node (left/right) by checking the interactor's name
        if (args.interactorObject is XRBaseControllerInteractor ci && ci.xrController != null)
        {
            // Infer from the GameObject name (common naming: "LeftHand Controller" / "RightHand Controller")
            string controllerName = ci.xrController.gameObject.name.ToLower();
            if (controllerName.Contains("left"))
            {
                controllerNode = XRNode.LeftHand;
            }
            else if (controllerName.Contains("right"))
            {
                controllerNode = XRNode.RightHand;
            }
            // else keep default (RightHand)

            device = InputDevices.GetDeviceAtXRNode(controllerNode);
        }
        else
        {
            // fallback: try to get any valid device
            device = default;
            var devices = new System.Collections.Generic.List<InputDevice>();
            InputDevices.GetDevices(devices);
            if (devices.Count > 0)
            {
                device = devices[0];
            }
        }

        // While held: it's ok if XRI drives it, but keep RB clean
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        if (arcLine && showArcWhileHeld)
            arcLine.enabled = true;
    }

    void OnRelease(SelectExitEventArgs args)
    {
        held = false;
        hasBeenThrown = true;

        if (arcLine) arcLine.enabled = false;

        // Make sure we are NOT parented to the hand/socket anymore
        transform.SetParent(null, true);

        // Force RB back to physics mode
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.None;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.WakeUp();

        // Apply custom throw
        Vector3 v = deviceVelWorld * throwMultiplier + Vector3.up * upwardBias;
        v = Vector3.ClampMagnitude(v, maxThrowSpeed);
        rb.velocity = v;

        // optional spin
        rb.angularVelocity = deviceAngVelWorld * 0.5f;
    }

    void Update()
    {
        // Only do tracking + arc when held
        if (!held || handTransform == null)
            return;

        // Refresh device if it became invalid
        if (!device.isValid)
            device = InputDevices.GetDeviceAtXRNode(controllerNode);

        // Get velocity in device/local space, then convert to world
        Vector3 velLocal = Vector3.zero;
        Vector3 angLocal = Vector3.zero;

        if (device.isValid)
        {
            device.TryGetFeatureValue(CommonUsages.deviceVelocity, out velLocal);
            device.TryGetFeatureValue(CommonUsages.deviceAngularVelocity, out angLocal);
        }

        deviceVelWorld = handTransform.TransformDirection(velLocal);
        deviceAngVelWorld = handTransform.TransformDirection(angLocal);

        if (arcLine && showArcWhileHeld)
        {
            if (arcLine.positionCount != arcSteps)
                arcLine.positionCount = arcSteps;

            Vector3 previewVel = deviceVelWorld * throwMultiplier + Vector3.up * upwardBias;
            DrawArcPreview(handTransform.position, previewVel);
        }
    }

    void FixedUpdate()
    {
        // Optional: sleep when it fully stops after throw
        if (hasBeenThrown && !held)
        {
            if (rb.velocity.sqrMagnitude < 0.01f && rb.angularVelocity.sqrMagnitude < 0.01f)
                rb.Sleep();
        }
    }

    void DrawArcPreview(Vector3 startPos, Vector3 startVel)
    {
        // Clamp the velocity for preview (same as throw)
        Vector3 clampedVel = Vector3.ClampMagnitude(startVel, maxThrowSpeed);
        
        Vector3 g = Physics.gravity;
        Vector3 prev = startPos;
        bool hitDetected = false;
        Vector3 hitPoint = Vector3.zero;

        for (int i = 0; i < arcSteps; i++)
        {
            if (hitDetected)
            {
                // Fill remaining points at hit location
                arcLine.SetPosition(i, hitPoint);
                continue;
            }

            float t = i * arcTimeStep;
            Vector3 p = startPos + clampedVel * t + 0.5f * g * t * t;

            if (i > 0)
            {
                Vector3 dir = p - prev;
                float dist = dir.magnitude;
                if (dist > 0.0001f &&
                    Physics.Raycast(prev, dir.normalized, out RaycastHit hit, dist, arcCollisionMask, QueryTriggerInteraction.Ignore))
                {
                    hitDetected = true;
                    hitPoint = hit.point;
                    arcLine.SetPosition(i, hitPoint);
                    continue;
                }
            }

            arcLine.SetPosition(i, p);
            prev = p;
        }
    }
}