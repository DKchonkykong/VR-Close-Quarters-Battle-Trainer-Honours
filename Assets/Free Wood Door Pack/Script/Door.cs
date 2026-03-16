using System.Collections;
using UnityEngine;
using UnityEngine.AI;

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

        private NavMeshObstacle obstacle;
        private Collider[] doorColliders;

        public bool IsOpen => isOpen;

        private void Start()
        {
            targetRotation = transform.localRotation;

            obstacle = GetComponent<NavMeshObstacle>();
            doorColliders = GetComponents<Collider>();

            UpdateDoorBlocking();
        }

        public void OpenDoor()
        {
            if (isMoving) return;

            isOpen = !isOpen;
            float angle = isOpen ? openAngle : closeAngle;
            targetRotation = Quaternion.Euler(0f, angle, 0f);

            UpdateDoorBlocking();
            StartCoroutine(RotateDoor());
        }

        private void UpdateDoorBlocking()
        {
            if (obstacle != null)
                obstacle.enabled = !isOpen;

            if (doorColliders != null)
            {
                foreach (var col in doorColliders)
                {
                    if (col != null)
                        col.enabled = !isOpen;
                }
            }
        }

        private IEnumerator RotateDoor()
        {
            isMoving = true;

            while (Quaternion.Angle(transform.localRotation, targetRotation) > 0.1f)
            {
                transform.localRotation = Quaternion.Slerp(
                    transform.localRotation,
                    targetRotation,
                    Time.deltaTime * openSpeed
                );
                yield return null;
            }

            transform.localRotation = targetRotation;
            isMoving = false;
        }
    }
}