using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(XRGrabInteractable))]
public class GrenadeThrowXR : MonoBehaviour
{
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
    public LayerMask arcCollisionMask = ~0;

    Rigidbody rb;
    XRGrabInteractable grab;

    bool held;
    Transform handTransform;

    Vector3 lastHandPos;
    Vector3 estimatedHandVel;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grab = GetComponent<XRGrabInteractable>();

        if (!arcLine && showArcWhileHeld)
            arcLine = GetComponent<LineRenderer>();

        if (arcLine)
        {
            arcLine.enabled = false;
            // Set position count once in Awake
            arcLine.positionCount = arcSteps;
        }

        grab.selectEntered.AddListener(OnGrab);
        grab.selectExited.AddListener(OnRelease);
    }

    void OnDestroy()
    {
        grab.selectEntered.RemoveListener(OnGrab);
        grab.selectExited.RemoveListener(OnRelease);
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        held = true;

        handTransform = args.interactorObject.GetAttachTransform(grab);
        if (!handTransform) handTransform = args.interactorObject.transform;

        lastHandPos = handTransform.position;
        estimatedHandVel = Vector3.zero;

        if (arcLine && showArcWhileHeld)
        {
            arcLine.enabled = true;
        }

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    void OnRelease(SelectExitEventArgs args)
    {
        held = false;

        if (arcLine) arcLine.enabled = false;

        Vector3 v = estimatedHandVel * throwMultiplier;
        v += Vector3.up * upwardBias;
        v = Vector3.ClampMagnitude(v, maxThrowSpeed);

        rb.velocity = v;
    }

    void Update()
    {
        if (rb.velocity.sqrMagnitude < 0.04f && rb.angularVelocity.sqrMagnitude < 0.04f) // ~0.2 speed
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep();
        }

        if (!held || !handTransform)
            return;

        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        Vector3 currentPos = handTransform.position;
        Vector3 frameVel = (currentPos - lastHandPos) / dt;

        estimatedHandVel = Vector3.Lerp(estimatedHandVel, frameVel, 0.5f);

        lastHandPos = currentPos;

        if (arcLine && showArcWhileHeld)
        {
            DrawArcPreview(currentPos, estimatedHandVel * throwMultiplier + Vector3.up * upwardBias);
        }
    }

    void DrawArcPreview(Vector3 startPos, Vector3 startVel)
    {
        Vector3 g = Physics.gravity;
        Vector3 prev = startPos;
        bool hitDetected = false;
        Vector3 hitPoint = Vector3.zero;
        int hitIndex = 0;

        for (int i = 0; i < arcSteps; i++)
        {
            // If we already hit something, fill rest with hit point
            if (hitDetected)
            {
                arcLine.SetPosition(i, hitPoint);
                continue;
            }

            float t = i * arcTimeStep;
            Vector3 p = startPos + startVel * t + 0.5f * g * t * t;

            // Collision check between previous and current point
            if (i > 0)
            {
                Vector3 dir = p - prev;
                float dist = dir.magnitude;
                if (dist > 0.0001f)
                {
                    if (Physics.Raycast(prev, dir.normalized, out RaycastHit hit, dist, arcCollisionMask, QueryTriggerInteraction.Ignore))
                    {
                        hitDetected = true;
                        hitPoint = hit.point;
                        hitIndex = i;
                        arcLine.SetPosition(i, hitPoint);
                        continue;
                    }
                }
            }

            arcLine.SetPosition(i, p);
            prev = p;
        }
    }
}