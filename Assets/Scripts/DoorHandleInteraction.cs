using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DoorScript;


//door handle for the door knob to open the door
[RequireComponent(typeof(Collider))]
public class DoorHandleInteraction : MonoBehaviour
{
    [SerializeField] private Door doorScript;
    [Tooltip("Tag to check for player interaction (e.g., 'Player' or 'Hand')")]
    [SerializeField] private string interactionTag = "Player";

    private void Start()
    {
        // If door script not assigned, try to get it from parent
        if (doorScript == null)
        {
            doorScript = GetComponentInParent<Door>();
            
            if (doorScript == null)
            {
                Debug.LogError("Door script not found! Please assign it in the inspector.");
            }
        }

        // Ensure the collider is set to trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the object that touched the handle has the correct tag
        if (other.CompareTag(interactionTag) && doorScript != null)
        {
            doorScript.OpenDoor();
        }
    }
}