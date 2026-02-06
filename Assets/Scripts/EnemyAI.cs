using UnityEngine;
using UnityEngine.AI;
using Unity.XR.CoreUtils;   // XR Origin helper

public class EnemyAI : MonoBehaviour
{
    public NavMeshAgent agent;
    public Transform player;
    public EnemyPerception perception;

    void Awake()
    {
        // Auto-wire components on THIS enemy
        if (!agent) agent = GetComponent<NavMeshAgent>();
        if (!perception) perception = GetComponent<EnemyPerception>();

        // Auto-find XR player (HMD camera)
        if (!player)
        {
            // Try XR Origin
            var xrOrigin = FindAnyObjectByType<XROrigin>();
            if (xrOrigin && xrOrigin.Camera)
                player = xrOrigin.Camera.transform;
            else
            {
                // fallback: MainCamera tag
                var cam = Camera.main;
                if (cam) player = cam.transform;
            }
        }
    }
}
