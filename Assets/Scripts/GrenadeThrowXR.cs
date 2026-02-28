using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(XRGrabInteractable))]
public class GrenadeThrowXR : MonoBehaviour
{
    // headers for modifing it in inspector
    // this is not glue code as before since I am trying to have it so it tracks the velocity while helded and applying that velocity when relase.
    [Header("Throw Tuning")]
    [Tooltip("Scales how fast the grenade is thrown (controller velocity * multiplier).")]
    public float throwMultiplier = 1.2f;

    [Tooltip("Clamps max throw speed to avoid unrealistic launches.")]
    public float maxThrowSpeed = 10f;

    [Tooltip("Small upward bias helps throws feel natural in VR.")]
    public float upwardBias = 0.3f;

    [Header("Arc Preview (Optional)")]
    public bool showArcWhileHeld = true;
    public LineRenderer arcLine;
    public int arcSteps = 30;
    public float arcTimeStep = 0.05f;
    public LayerMask arcCollisionMask = ~0; // what the arc can hit (walls, floor)

    Rigidbody rb;
    XRGrabInteractable grab;

    bool held;
    Transform handTransform; // the interactor's attach transform

    // Simple velocity estimator (code-driven throw)
    Vector3 lastHandPos;
    Vector3 estimatedHandVel;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grab = GetComponent<XRGrabInteractable>();

        // Make sure Unity/XRI doesn't apply its own throw (we want OUR code to do it)
        // Some XRI versions expose this; if not present, ignore.
        // grab.throwOnDetach = false;  // Uncomment if your version has it

        if (!arcLine && showArcWhileHeld)
            arcLine = GetComponent<LineRenderer>();

        if (arcLine)
            arcLine.enabled = false;

        grab.selectEntered.AddListener(OnGrab);
        grab.selectExited.AddListener(OnRelease);
    }

    void OnDestroy()
    {
        // Clean up listeners (good practice)
        grab.selectEntered.RemoveListener(OnGrab);
        grab.selectExited.RemoveListener(OnRelease);
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        held = true;

        // Use the interactor's attach transform if it exists; otherwise use interactor transform
        handTransform = args.interactorObject.GetAttachTransform(grab);
        if (!handTransform) handTransform = args.interactorObject.transform;

        lastHandPos = handTransform.position;
        estimatedHandVel = Vector3.zero;

        if (arcLine && showArcWhileHeld)
        {
            arcLine.positionCount = arcSteps;
            arcLine.enabled = true;
        }

        // While held, you generally want RB controlled by grab.
        // But keep it non-kinematic if your grab movement type expects it.
        // We'll just ensure no leftover velocity:
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    void OnRelease(SelectExitEventArgs args)
    {
        held = false;

        if (arcLine) arcLine.enabled = false;

        // Convert hand velocity into throw velocity
        Vector3 v = estimatedHandVel * throwMultiplier;

        // Upward bias (helps in VR)
        v += Vector3.up * upwardBias;

        // Clamp speed for consistency (important for grading/demo)
        v = Vector3.ClampMagnitude(v, maxThrowSpeed);

        // Apply to grenade
        rb.velocity = v;

        // Optional: add a little spin based on lateral movement
        // rb.angularVelocity = new Vector3(0f, 6f, 0f);
    }

    void Update()
    {
        if (!held || !handTransform)
            return;

        // Estimate controller velocity from position change
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        Vector3 currentPos = handTransform.position;
        Vector3 frameVel = (currentPos - lastHandPos) / dt;

        // Light smoothing to reduce jitter (simple low-pass filter)
        estimatedHandVel = Vector3.Lerp(estimatedHandVel, frameVel, 0.5f);

        lastHandPos = currentPos;

        // Arc preview using projectile equation
        if (arcLine && showArcWhileHeld)
        {
            DrawArcPreview(currentPos, estimatedHandVel * throwMultiplier + Vector3.up * upwardBias);
        }
    }

    void DrawArcPreview(Vector3 startPos, Vector3 startVel)
    {
        Vector3 g = Physics.gravity;
        Vector3 prev = startPos;

        for (int i = 0; i < arcSteps; i++)
        {
            float t = i * arcTimeStep;
            Vector3 p = startPos + startVel * t + 0.5f * g * t * t;

            // Optional collision prediction between segments
            if (i > 0)
            {
                Vector3 dir = p - prev;
                float dist = dir.magnitude;
                if (dist > 0.0001f)
                {
                    if (Physics.Raycast(prev, dir.normalized, out RaycastHit hit, dist, arcCollisionMask, QueryTriggerInteraction.Ignore))
                    {
                        // Stop arc at collision point
                        arcLine.SetPosition(i, hit.point);

                        // Fill remaining points with the hit point so line doesn't stretch weirdly
                        for (int j = i + 1; j < arcSteps; j++)
                            arcLine.SetPosition(j, hit.point);

                        return;
                    }
                }
            }

            arcLine.SetPosition(i, p);
            prev = p;
        }
    }
}