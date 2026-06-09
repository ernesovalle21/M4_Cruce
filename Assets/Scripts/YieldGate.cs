using UnityEngine;

/// <summary>
/// Punto de "ceder el paso". Se coloca en un waypoint de una ruta que DEBE ceder
/// (vueltas / incorporaciones). Cuando un carro se aproxima a ese waypoint, espera
/// si hay otro carro dentro de la zona de conflicto (una caja).
///
/// El tráfico con prioridad NO lleva YieldGate, así que la cesión es de un solo
/// lado y no se producen bloqueos mutuos.
/// </summary>
public class YieldGate : MonoBehaviour
{
    [Tooltip("Desplazamiento (mundo) del centro de la zona respecto al waypoint.")]
    public Vector3 checkOffset = Vector3.zero;
    [Tooltip("Medias dimensiones de la caja de conflicto.")]
    public Vector3 halfExtents = new Vector3(5f, 1.5f, 1.5f);

    public bool IsBlocked(Transform self)
    {
        Vector3 center = transform.position + checkOffset + Vector3.up * 0.75f;
        Collider[] hits = Physics.OverlapBox(center, halfExtents, Quaternion.identity);
        foreach (var h in hits)
        {
            if (h == null || h.isTrigger) continue;
            Transform r = h.transform.root;
            if (r == self.root) continue; // ignorarme a mí mismo
            if (h.CompareTag("Car") || r.CompareTag("Car")) return true;
        }
        return false;
    }
}
