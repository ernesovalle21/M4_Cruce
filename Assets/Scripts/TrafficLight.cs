using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Semáforo determinista: la fase se calcula directamente desde el reloj global
/// (Time.time) más un desfase (startOffset). Esto garantiza que varios semáforos
/// queden coordinados en "onda verde" sin estados internos que se desincronicen.
/// </summary>
public class TrafficLight : MonoBehaviour
{
    public enum Phase { Green, Yellow, Red }

    [Header("Duraciones del ciclo (s)")]
    public float greenDuration = 10f;
    public float yellowDuration = 2f;
    public float redDuration = 8f;

    [Header("Coordinación onda verde")]
    [Tooltip("Desfase en segundos respecto al inicio del ciclo global.")]
    public float startOffset = 0f;

    [Header("Visual")]
    public Renderer lampRenderer;
    public Material matGreen;
    public Material matYellow;
    public Material matRed;

    [Header("Compatibilidad (no usados por el modelo determinista)")]
    public Phase startPhase = Phase.Green;   // conservado por compatibilidad con builders previos
    public TrafficLight partnerLight;        // conservado por compatibilidad con builders previos

    private Phase currentPhase = Phase.Red;
    private bool initialized = false;
    private readonly List<WaypointMover> carsAtLine = new List<WaypointMover>();

    public Phase CurrentPhase => currentPhase;
    public float CycleTime => greenDuration + yellowDuration + redDuration;

    void Update()
    {
        Phase p = ComputePhase(Time.time);
        if (!initialized || p != currentPhase)
        {
            initialized = true;
            currentPhase = p;
            ApplyMaterial();
            UpdateCars();
        }
    }

    private Phase ComputePhase(float time)
    {
        float cycle = CycleTime;
        if (cycle <= 0f) return Phase.Green;
        float t = Mathf.Repeat(time - startOffset, cycle);
        if (t < greenDuration) return Phase.Green;
        if (t < greenDuration + yellowDuration) return Phase.Yellow;
        return Phase.Red;
    }

    /// <summary>Aplica el estado actual a los carros detenidos en la línea de alto.</summary>
    private void UpdateCars()
    {
        bool stop = (currentPhase == Phase.Red);
        for (int i = carsAtLine.Count - 1; i >= 0; i--)
        {
            if (carsAtLine[i] == null) { carsAtLine.RemoveAt(i); continue; }
            carsAtLine[i].SetStopped(stop);
        }
    }

    public void RegisterCar(WaypointMover car)
    {
        if (car == null) return;
        if (!carsAtLine.Contains(car)) carsAtLine.Add(car);
        car.SetStopped(currentPhase == Phase.Red);
    }

    public void UnregisterCar(WaypointMover car)
    {
        if (car == null) return;
        carsAtLine.Remove(car);
        car.SetStopped(false);
    }

    private void ApplyMaterial()
    {
        if (lampRenderer == null) return;
        Material m = currentPhase == Phase.Green ? matGreen
                   : currentPhase == Phase.Yellow ? matYellow
                   : matRed;
        if (m != null) lampRenderer.material = m;
    }
}
