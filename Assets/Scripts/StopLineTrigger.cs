using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class StopLineTrigger : MonoBehaviour
{
    public TrafficLight trafficLight;

    void Awake()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Car"))
        {
            var mover = other.GetComponent<WaypointMover>();
            if (mover != null) trafficLight?.RegisterCar(mover);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Car"))
        {
            var mover = other.GetComponent<WaypointMover>();
            if (mover != null) trafficLight?.UnregisterCar(mover);
        }
    }
}
