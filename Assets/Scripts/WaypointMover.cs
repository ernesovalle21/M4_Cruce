using UnityEngine;

public class WaypointMover : MonoBehaviour
{
    public Transform[] waypoints;
    public float speed = 4f;
    public float stoppingDistance = 0.5f;

    [Header("Obstacle Avoidance")]
    public float obstacleCheckDistance = 6f;
    public float obstacleCheckHeight = 0.5f;
    public float obstacleCheckRadius = 1.2f;

    private int currentIndex = 0;
    private bool isStopped = false;

    public void SetStopped(bool val)
    {
        isStopped = val;
    }

    public int GetCurrentIndex()
    {
        return currentIndex;
    }

    void Update()
    {
        if (isStopped) return;
        if (waypoints == null || waypoints.Length == 0) return;

        if (currentIndex >= waypoints.Length)
        {
            Destroy(gameObject);
            return;
        }

        Transform target = waypoints[currentIndex];
        if (target == null)
        {
            currentIndex++;
            return;
        }

        Vector3 dir = target.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 5f * Time.deltaTime);
        }

        if (IsCarAhead())
        {
            return;
        }

        transform.position = Vector3.MoveTowards(transform.position, target.position, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target.position) < stoppingDistance)
        {
            currentIndex++;
            if (currentIndex >= waypoints.Length)
            {
                Destroy(gameObject);
            }
        }
    }

    private bool IsCarAhead()
    {
        Vector3 origin = transform.position + Vector3.up * obstacleCheckHeight + transform.forward * 1.0f;
        RaycastHit[] hits = Physics.SphereCastAll(origin, obstacleCheckRadius, transform.forward, obstacleCheckDistance);
        foreach (var hit in hits)
        {
            if (hit.collider == null) continue;
            if (hit.collider.transform == transform) continue;
            if (hit.collider.transform.IsChildOf(transform)) continue;
            if (hit.collider.CompareTag("Car") || hit.collider.transform.root.CompareTag("Car"))
            {
                return true;
            }
        }
        return false;
    }
}
