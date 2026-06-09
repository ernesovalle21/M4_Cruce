using UnityEngine;

public class WaypointMover : MonoBehaviour
{
    public Transform[] waypoints;
    public float speed = 4f;            // velocidad máxima (crucero)
    public float stoppingDistance = 0.5f;

    [Header("Física de manejo")]
    public float acceleration = 8f;     // u/s² al acelerar
    public float braking = 16f;         // u/s² al frenar

    // --- Seguimiento / anti-encimamiento (medido desde el frente) ---
    private const float FrontOffset = 2.2f;   // del centro al frente del carro
    private const float CheckRadius = 1.0f;   // medio ancho del "sensor"
    private const float ScanDistance = 7f;    // qué tan adelante "ve"
    private const float SafeGap = 3.0f;       // hueco al que se detiene del todo

    private int currentIndex = 0;
    private bool isStopped = false;
    private float currentSpeed = 0f;

    public void SetStopped(bool val) { isStopped = val; }
    public int GetCurrentIndex() { return currentIndex; }

    void Update()
    {
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

        // Rotación suave hacia el waypoint (solo en Y)
        Vector3 dir = target.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 5f * Time.deltaTime);
        }

        // Velocidad objetivo: 0 si hay semáforo en rojo; si hay carro adelante,
        // frena proporcional al hueco; si no, velocidad de crucero.
        float targetSpeed = speed;
        if (isStopped)
        {
            targetSpeed = 0f;
        }
        else
        {
            float gap = DistanceToCarAhead();
            if (gap < ScanDistance)
            {
                float t = Mathf.InverseLerp(SafeGap, ScanDistance, gap); // 0 en SafeGap, 1 en ScanDistance
                targetSpeed = speed * t;
            }
        }

        // Acelera o frena gradualmente hacia la velocidad objetivo
        float rate = (targetSpeed < currentSpeed) ? braking : acceleration;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, rate * Time.deltaTime);

        if (currentSpeed > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target.position, currentSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, target.position) < stoppingDistance)
            {
                currentIndex++;
                if (currentIndex >= waypoints.Length) Destroy(gameObject);
            }
        }
    }

    /// <summary>Distancia al carro de adelante; ScanDistance+1 si no hay ninguno.</summary>
    private float DistanceToCarAhead()
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f + transform.forward * FrontOffset;
        RaycastHit[] hits = Physics.SphereCastAll(origin, CheckRadius, transform.forward, ScanDistance);
        float nearest = ScanDistance + 1f;
        foreach (var hit in hits)
        {
            if (hit.collider == null) continue;
            Transform t = hit.collider.transform;
            if (t == transform || t.IsChildOf(transform)) continue; // ignorar mis colliders
            if (hit.collider.CompareTag("Car") || t.root.CompareTag("Car"))
            {
                if (hit.distance < nearest) nearest = hit.distance;
            }
        }
        return nearest;
    }
}
