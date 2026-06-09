using UnityEngine;

public class WaypointMover : MonoBehaviour
{
    public Transform[] waypoints;
    public float speed = 4f;
    public float stoppingDistance = 0.5f;

    // --- Anti-encimamiento (medido desde el frente del carro) ---
    // Constantes: no dependen de valores serializados en el prefab, así el
    // comportamiento es idéntico en todos los carros sin re-generar prefabs.
    private const float FrontOffset = 2.2f;   // del centro al frente del carro
    private const float CheckRadius = 1.0f;   // medio ancho del "sensor"
    private const float DesiredGap = 3.5f;    // hueco libre a mantener al frente

    private int currentIndex = 0;
    private bool isStopped = false;

    public void SetStopped(bool val) { isStopped = val; }
    public int GetCurrentIndex() { return currentIndex; }

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

        // Rotación suave hacia el waypoint (solo en Y)
        Vector3 dir = target.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 5f * Time.deltaTime);
        }

        // Si hay otro carro dentro del hueco deseado al frente, esperar
        if (IsCarAhead()) return;

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
        // El sensor arranca justo en el frente del carro y mira hacia adelante.
        Vector3 origin = transform.position + Vector3.up * 0.5f + transform.forward * FrontOffset;
        RaycastHit[] hits = Physics.SphereCastAll(origin, CheckRadius, transform.forward, DesiredGap);
        foreach (var hit in hits)
        {
            if (hit.collider == null) continue;
            Transform t = hit.collider.transform;
            if (t == transform || t.IsChildOf(transform)) continue; // ignorar mis propios colliders
            if (hit.collider.CompareTag("Car") || t.root.CompareTag("Car"))
            {
                return true;
            }
        }
        return false;
    }
}
