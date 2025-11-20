using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Provides visual feedback for slide interaction:
/// - Shows a transparent blue guide rail indicating slide travel path
/// - Highlights when hovered or grabbed
/// - Shows current slide position along the rail
/// </summary>
public class SlideVisualFeedback : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The SlideRail component (usually on M1911 parent)")]
    public SlideRail slideRail;
    
    [Header("Visual Settings")]
    [Tooltip("Color when not interacting")]
    public Color idleColor = new Color(0.5f, 0.8f, 1f, 0.3f);
    
    [Tooltip("Color when hovering")]
    public Color hoverColor = new Color(0.3f, 0.6f, 1f, 0.5f);
    
    [Tooltip("Color when grabbed")]
    public Color grabbedColor = new Color(0.1f, 0.4f, 1f, 0.7f);
    
    [Tooltip("Width of the guide rail line")]
    public float railWidth = 0.005f;
    
    [Tooltip("Number of segments for smoother line")]
    public int lineSegments = 20;
    
    // Internal
    LineRenderer lineRenderer;
    XRGrabInteractable grabInteractable;
    bool isHovered;
    bool isGrabbed;

    void Awake()
    {
        // Auto-find SlideRail if not assigned
        if (!slideRail)
            slideRail = GetComponentInParent<SlideRail>();
        
        // Get or add LineRenderer
        lineRenderer = GetComponent<LineRenderer>();
        if (!lineRenderer)
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        
        SetupLineRenderer();
    }

    void Start()
    {
        // Find the SlideHandle's XRGrabInteractable
        if (slideRail && slideRail.gun)
        {
            // Search for SlideHandle in children of gun
            var slideHandles = slideRail.gun.GetComponentsInChildren<SlideHandleXR>();
            if (slideHandles.Length > 0)
            {
                grabInteractable = slideHandles[0].GetComponent<XRGrabInteractable>();
            }
        }
        
        SubscribeToEvents();
    }

    void OnEnable()
    {
        SubscribeToEvents();
    }

    void OnDisable()
    {
        UnsubscribeFromEvents();
    }

    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    void SetupLineRenderer()
    {
        if (!lineRenderer) return;
        
        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = railWidth;
        lineRenderer.endWidth = railWidth;
        
        // Create a simple transparent material
        if (!lineRenderer.material || lineRenderer.material.name == "Default-Material")
        {
            var shader = Shader.Find("Sprites/Default");
            if (!shader) shader = Shader.Find("UI/Default");
            if (!shader) shader = Shader.Find("Unlit/Color");
            
            lineRenderer.material = new Material(shader);
        }
        
        lineRenderer.startColor = idleColor;
        lineRenderer.endColor = idleColor;
        lineRenderer.useWorldSpace = true;
        lineRenderer.alignment = LineAlignment.View;
    }

    void SubscribeToEvents()
    {
        if (grabInteractable)
        {
            grabInteractable.hoverEntered.AddListener(OnHoverEnter);
            grabInteractable.hoverExited.AddListener(OnHoverExit);
            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);
        }
    }

    void UnsubscribeFromEvents()
    {
        if (grabInteractable)
        {
            grabInteractable.hoverEntered.RemoveListener(OnHoverEnter);
            grabInteractable.hoverExited.RemoveListener(OnHoverExit);
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            grabInteractable.selectExited.RemoveListener(OnReleased);
        }
    }

    void LateUpdate()
    {
        UpdateRailVisual();
    }

    void UpdateRailVisual()
    {
        if (!lineRenderer || !slideRail || !slideRail.slideClosed || !slideRail.slideOpen)
            return;
        
        // Draw line from closed to open position
        Vector3 closedPos = slideRail.slideClosed.position;
        Vector3 openPos = slideRail.slideOpen.position;
        
        lineRenderer.SetPosition(0, closedPos);
        lineRenderer.SetPosition(1, openPos);
        
        // Update color based on interaction state
        Color targetColor = GetCurrentColor();
        lineRenderer.startColor = targetColor;
        lineRenderer.endColor = targetColor;
        
        // Optional: Add pulsing effect when grabbed
        if (isGrabbed)
        {
            float pulse = (Mathf.Sin(Time.time * 8f) + 1f) * 0.5f;
            lineRenderer.widthMultiplier = Mathf.Lerp(0.8f, 1.2f, pulse);
        }
        else
        {
            lineRenderer.widthMultiplier = 1f;
        }
    }

    Color GetCurrentColor()
    {
        if (isGrabbed)
            return grabbedColor;
        else if (isHovered)
            return hoverColor;
        else if (slideRail.IsLocked)
            return Color.Lerp(idleColor, Color.yellow, 0.5f); // Yellow tint when locked
        else
            return idleColor;
    }

    void OnHoverEnter(HoverEnterEventArgs args)
    {
        isHovered = true;
        Debug.Log("Slide Handle HOVERED");
    }

    void OnHoverExit(HoverExitEventArgs args)
    {
        isHovered = false;
        Debug.Log("Slide Handle UNHOVERED");
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        isGrabbed = true;
        isHovered = false;
        Debug.Log("Slide Handle GRABBED");
    }

    void OnReleased(SelectExitEventArgs args)
    {
        isGrabbed = false;
        Debug.Log("Slide Handle RELEASED");
    }

    // Editor visualization
    void OnDrawGizmos()
    {
        if (!slideRail || !slideRail.slideClosed || !slideRail.slideOpen) return;
        
        // Draw the rail path
        Gizmos.color = new Color(0.5f, 0.8f, 1f, 0.5f);
        Gizmos.DrawLine(slideRail.slideClosed.position, slideRail.slideOpen.position);
        
        // Draw charge threshold marker
        Vector3 chargePos = Vector3.Lerp(
            slideRail.slideClosed.position, 
            slideRail.slideOpen.position, 
            slideRail.chargeThreshold01
        );
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(chargePos, 0.01f);
        
        // Draw current slide position
        if (Application.isPlaying && slideRail.slideVisual)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(slideRail.slideVisual.position, 0.008f);
        }
    }
}