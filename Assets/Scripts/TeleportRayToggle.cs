using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class TeleportRayToggle : MonoBehaviour
{
    public XRRayInteractor ray;
    public Behaviour lineVisual; // XRInteractorLineVisual OR LineRenderer
    public InputActionReference moveAction; // <-- set to XRI .../Move (Vector2)

    [Range(0f, 1f)] public float thumbstickUpThreshold = 0.5f;

    void OnEnable() => moveAction?.action.Enable();
    void OnDisable() => moveAction?.action.Disable();

    void Update()
    {
        if (!ray || moveAction == null) return;

        Vector2 stick = moveAction.action.ReadValue<Vector2>();
        bool teleportMode = stick.y > thumbstickUpThreshold;

        ray.enabled = teleportMode;
        if (lineVisual) lineVisual.enabled = teleportMode;
    }
}