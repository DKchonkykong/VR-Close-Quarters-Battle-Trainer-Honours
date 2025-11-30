using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetMovable : MonoBehaviour
{
    [Header("Points")]
    public Transform startPoint;
    public Transform endPoint;

    [Header("Movement")]
    public float moveTime = 1.5f;     // time to move between points
    public float waitTime = 0.5f;     // pause at each end

    void Start()
    {
        StartCoroutine(MoveLoop());
    }

    IEnumerator MoveLoop()
    {
        while (true)
        {
            // Move from start → end
            yield return MoveObject(startPoint.position, endPoint.position);

            // Pause at end
            yield return new WaitForSeconds(waitTime);

            // Move from end → start
            yield return MoveObject(endPoint.position, startPoint.position);

            // Pause at start
            yield return new WaitForSeconds(waitTime);
        }
    }

    IEnumerator MoveObject(Vector3 from, Vector3 to)
    {
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / moveTime;
            transform.position = Vector3.Lerp(from, to, t);
            yield return null;
        }
    }
}
