using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class StopLineTrigger : MonoBehaviour
{
    public TrafficLight trafficLight;

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

            // Si el carro YA cruzó la línea de alto, que siga (no lo detengas
            // aunque el semáforo esté en rojo) -> evita que se congele en el cruce.
            Vector3 toLine = transform.position - mover.transform.position;
            bool alreadyPassed = Vector3.Dot(mover.transform.forward, toLine) < 0f;

            mover.SetStopped(red && !alreadyPassed);
        }
    }
}
