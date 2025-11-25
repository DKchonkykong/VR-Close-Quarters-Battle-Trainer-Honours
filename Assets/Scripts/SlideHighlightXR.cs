using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(XRBaseInteractable))]
public class SlideHighlightXR : MonoBehaviour
{
	public Renderer slideRenderer;       // renderer on the Slide mesh
	public Material hoverMaterial;       // blue material

	Material[] originalMats;
	XRBaseInteractable interactable;

	void Awake()
	{
		interactable = GetComponent<XRBaseInteractable>();
		if (slideRenderer != null)
			originalMats = slideRenderer.materials;
	}

	void OnEnable()
	{
		interactable.hoverEntered.AddListener(OnHoverEntered);
		interactable.hoverExited.AddListener(OnHoverExited);
	}

	void OnDisable()
	{
		interactable.hoverEntered.RemoveListener(OnHoverEntered);
		interactable.hoverExited.RemoveListener(OnHoverExited);
	}

	void OnHoverEntered(HoverEnterEventArgs args)
	{
		if (slideRenderer == null || hoverMaterial == null) return;

		// simple version: single-material mesh
		slideRenderer.material = hoverMaterial;

		// if you have multiple sub-materials and want only index 0 highlighted,
		// you could duplicate originalMats, replace [0], and re-assign.
	}

	void OnHoverExited(HoverExitEventArgs args)
	{
		if (slideRenderer == null || originalMats == null) return;
		slideRenderer.materials = originalMats;
	}
}
