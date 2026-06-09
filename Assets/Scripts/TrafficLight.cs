using System.Collections.Generic;
using UnityEngine;

public class TrafficLight : MonoBehaviour
{
    public enum Phase { Green, Yellow, Red }

    public float greenDuration = 8f;
    public float yellowDuration = 2f;
    public float redDuration = 8f;
    public Phase startPhase = Phase.Green;

    public Renderer lampRenderer;
    public Material matGreen;
    public Material matYellow;
    public Material matRed;

    public TrafficLight partnerLight;

    private Phase currentPhase;
    private float timer;
    private List<WaypointMover> stoppedCars = new List<WaypointMover>();

    public Phase CurrentPhase => currentPhase;

    void Start()
    {
        currentPhase = startPhase;
        timer = GetDuration(currentPhase);
        ApplyMaterial();
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            AdvancePhase();
        }
    }

    private void AdvancePhase()
    {
        Phase next = currentPhase;
        switch (currentPhase)
        {
            case Phase.Green:
                next = Phase.Yellow;
                break;
            case Phase.Yellow:
                next = Phase.Red;
                partnerLight?.ForcePhase(Phase.Green);
                break;
            case Phase.Red:
                next = Phase.Green;
                ReleaseAllCars();
                partnerLight?.ForcePhase(Phase.Red);
                break;
        }
        currentPhase = next;
        timer = GetDuration(currentPhase);
        ApplyMaterial();
    }

    private void ReleaseAllCars()
    {
        foreach (var car in stoppedCars)
        {
            if (car != null) car.SetStopped(false);
        }
    }

    public void ForcePhase(Phase p)
    {
        currentPhase = p;
        timer = GetDuration(p);
        if (p == Phase.Green)
        {
            ReleaseAllCars();
        }
        else
        {
            foreach (var car in stoppedCars)
            {
                if (car != null) car.SetStopped(true);
            }
        }
        ApplyMaterial();
    }

    public void RegisterCar(WaypointMover car)
    {
        if (car == null) return;
        if (!stoppedCars.Contains(car)) stoppedCars.Add(car);
        car.SetStopped(currentPhase != Phase.Green);
    }

    public void UnregisterCar(WaypointMover car)
    {
        if (car == null) return;
        stoppedCars.Remove(car);
        car.SetStopped(false);
    }

    private float GetDuration(Phase p)
    {
        switch (p)
        {
            case Phase.Green: return greenDuration;
            case Phase.Yellow: return yellowDuration;
            case Phase.Red: return redDuration;
        }
        return 1f;
    }

    private void ApplyMaterial()
    {
        if (lampRenderer == null) return;
        Material m = null;
        switch (currentPhase)
        {
            case Phase.Green: m = matGreen; break;
            case Phase.Yellow: m = matYellow; break;
            case Phase.Red: m = matRed; break;
        }
        if (m != null) lampRenderer.material = m;
    }
}
