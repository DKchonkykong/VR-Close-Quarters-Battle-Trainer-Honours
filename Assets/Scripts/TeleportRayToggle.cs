using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class TeleportRayToggle : MonoBehaviour
{
    [Header("Refs")]
    public XRRayInteractor ray;
    public LineRenderer lineRenderer;
    public InputActionReference teleportAction;   // XRI Left/Right Locomotion/Teleport Mode Activate

    [Header("Settings")]
    [Range(0f, 1f)]
    public float thumbstickUpThreshold = 0.5f;

    InteractionLayerMask originalLayers;

    void Awake()
    {
        if (ray != null)
            originalLayers = ray.interactionLayers;
    }

    void OnEnable()
    {
        if (teleportAction != null)
            teleportAction.action.Enable();
    }

    void OnDisable()
    {
        if (teleportAction != null)
            teleportAction.action.Disable();
    }

    void Update()
    {
        if (ray == null || teleportAction == null) return;

        Vector2 stick = teleportAction.action.ReadValue<Vector2>();
        bool teleportMode = stick.y > thumbstickUpThreshold;

        // Only interact with Teleport areas while in teleport mode
        ray.interactionLayers = teleportMode ? originalLayers : new InteractionLayerMask();
        // Show/hide visual
        if (lineRenderer != null)
            lineRenderer.enabled = teleportMode;
    }
}
