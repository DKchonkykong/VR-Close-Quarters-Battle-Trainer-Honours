using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using DoorScript;

//made for VR interaction instead
[RequireComponent(typeof(XRSimpleInteractable))]
public class DoorXR : MonoBehaviour
{
    [SerializeField] private Door doorScript;
    private XRSimpleInteractable interactable;

    private void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();

        if (doorScript == null)
        {
            doorScript = GetComponentInParent<Door>();
        }
    }

    private void OnEnable()
    {
        interactable.selectEntered.AddListener(OnGrab);
    }

    private void OnDisable()
    {
        interactable.selectEntered.RemoveListener(OnGrab);
    }

    public void OnGrab(SelectEnterEventArgs args)
    {
        if (doorScript != null)
        {
            doorScript.OpenDoor();
        }
    }
}
