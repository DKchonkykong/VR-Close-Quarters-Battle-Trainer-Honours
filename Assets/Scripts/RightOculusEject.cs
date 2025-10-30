using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

public class RightOculusEject : MonoBehaviour
{
    public GunXR gun;
    List<InputDevice> rightDevices = new List<InputDevice>();

    void Start()
    {
        var desired = InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller;
        InputDevices.GetDevicesWithCharacteristics(desired, rightDevices);
    }

    void Update()
    {
        if (gun == null) return;
        if (rightDevices.Count == 0) return;

        // We’ll just use the first right-hand device we found
        var device = rightDevices[0];

        // On Oculus Touch: primary button = A (right)
        if (device.TryGetFeatureValue(CommonUsages.primaryButton, out bool pressed) && pressed)
        {
            gun.EjectPressed();
        }
    }
}
