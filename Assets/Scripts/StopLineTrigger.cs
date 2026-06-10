using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class StopLineTrigger : MonoBehaviour
{
    public TrafficLight trafficLight;
    [Tooltip("Centro del cruce: solo se detiene a los carros que van HACIA él.")]
    public Vector3 intersectionCenter;

    private readonly List<WaypointMover> cars = new List<WaypointMover>();

    /// <summary>True si hay carros esperando en la zona (para control actuado).</summary>
    public bool HasCars => cars.Count > 0;

    /// <summary>Carros actualmente detenidos/en cola en esta aproximación (métrica).</summary>
    public int WaitingCount
    {
        get
        {
            int n = 0;
            foreach (var m in cars) if (m != null && m.IsQueued) n++;
            return n;
        }
    }

    void Awake()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Car")) return;
        var mover = other.GetComponent<WaypointMover>();
        if (mover != null && !cars.Contains(mover)) cars.Add(mover);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Car")) return;
        var mover = other.GetComponent<WaypointMover>();
        if (mover != null)
        {
            cars.Remove(mover);
            mover.SetStopped(false);
        }
    }

    void Update()
    {
        if (trafficLight == null) return;
        bool red = trafficLight.CurrentPhase == TrafficLight.Phase.Red;

        for (int i = cars.Count - 1; i >= 0; i--)
        {
            var mover = cars[i];
            if (mover == null) { cars.RemoveAt(i); continue; }

            Vector3 f = mover.transform.forward;
            Vector3 pos = mover.transform.position;
            // Detener SOLO si: aún no cruza la línea Y va HACIA el centro del cruce.
            // (Un carro que ya entró/salió del cruce, o que se aleja por la línea del
            //  otro lado, NO se detiene -> evita que se queden parados tras pasar.)
            bool lineAhead = Vector3.Dot(f, transform.position - pos) > 0f;
            bool headingIntoIntersection = Vector3.Dot(f, intersectionCenter - pos) > 0f;

            mover.SetStopped(red && lineAhead && headingIntoIntersection);
        }
    }
}
