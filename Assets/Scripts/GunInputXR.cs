using UnityEngine;
using UnityEngine.InputSystem;

public class GunInputXR : MonoBehaviour
{
    public GunXR gun;

    // drag these in Inspector
    public InputActionReference fireAction;   // RightHand / Activate (trigger)
    public InputActionReference ejectAction;  // RightHand / PrimaryButton (A)

    void OnEnable()
    {
        if (fireAction != null)
        {
            fireAction.action.performed += OnFire;
            fireAction.action.Enable();
        }

        if (ejectAction != null)
        {
            ejectAction.action.performed += OnEject;
            ejectAction.action.Enable();
        }
    }

    void OnDisable()
    {
        if (fireAction != null)
            fireAction.action.performed -= OnFire;
        if (ejectAction != null)
            ejectAction.action.performed -= OnEject;
    }

    void OnFire(InputAction.CallbackContext ctx)
    {
        if (gun != null)
            gun.FirePressed();
    }

    void OnEject(InputAction.CallbackContext ctx)
    {
        if (gun != null)
            gun.EjectPressed();
    }
}
