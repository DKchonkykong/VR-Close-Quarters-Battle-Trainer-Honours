using System.Collections.Generic;
using UnityEngine;

public class RoomController : MonoBehaviour
{
    [Header("Room Settings")]
    public bool isCombatRoom = true;

    [Header("Runtime (auto)")]
    public List<EnemyAI> enemiesInRoom = new();
    public List<HostageFollower> hostagesInRoom = new();

    public bool roomCleared;

    void OnTriggerEnter(Collider other)
    {
        // Enemies
        var enemy = other.GetComponentInParent<EnemyAI>();
        if (enemy && !enemiesInRoom.Contains(enemy))
            enemiesInRoom.Add(enemy);

        // Hostages
        var hostage = other.GetComponentInParent<HostageFollower>();
        if (hostage && !hostagesInRoom.Contains(hostage))
            hostagesInRoom.Add(hostage);
    }

    void OnTriggerExit(Collider other)
    {
        var enemy = other.GetComponentInParent<EnemyAI>();
        if (enemy) enemiesInRoom.Remove(enemy);

        var hostage = other.GetComponentInParent<HostageFollower>();
        if (hostage) hostagesInRoom.Remove(hostage);
    }

    void Update()
    {
        if (!isCombatRoom || roomCleared) return;

        // Remove nulls / dead enemies (EnemyAI sets Dead state)
        enemiesInRoom.RemoveAll(e => e == null || e.currentState == EnemyAI.State.Dead);

        // If no enemies left, clear the room and start escort
        if (enemiesInRoom.Count == 0)
        {
            roomCleared = true;
            Debug.Log($"{name} cleared! Hostages can follow.");

            foreach (var h in hostagesInRoom)
            {
                if (h) h.StartFollowing();
            }
        }
    }
}