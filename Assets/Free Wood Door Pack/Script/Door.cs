using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DoorScript
{
    public class Door : MonoBehaviour
    {
        [SerializeField] private bool isOpen = false;
        [SerializeField] private float openAngle = 90f;
        [SerializeField] private float closeAngle = 0f;
        [SerializeField] private float openSpeed = 2f;

        private bool isMoving = false;
        private Quaternion targetRotation;

        private void Start()
        {
            targetRotation = transform.localRotation;
        }

        public void OpenDoor()
        {
            if (!isMoving)
            {
                isOpen = !isOpen;
                float angle = isOpen ? openAngle : closeAngle;
                targetRotation = Quaternion.Euler(0, angle, 0);
                StartCoroutine(RotateDoor());
            }
        }

        private IEnumerator RotateDoor()
        {
            isMoving = true;

            while (Quaternion.Angle(transform.localRotation, targetRotation) > 0.1f)
            {
                transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * openSpeed);
                yield return null;
            }

            transform.localRotation = targetRotation;
            isMoving = false;
        }
    }
}