using UnityEngine;

/// <summary>
/// Semáforo determinista: la fase se calcula desde el reloj global (Time.time)
/// más un desfase (startOffset), para coordinar la onda verde / contrafase.
/// La detención de los carros la maneja StopLineTrigger leyendo CurrentPhase.
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
    public Renderer lampRenderer;        // indicador único (opcional)
    public Material matGreen;
    public Material matYellow;
    public Material matRed;

    [Header("Focos (3 luces, opcional)")]
    public Renderer redLamp;
    public Renderer yellowLamp;
    public Renderer greenLamp;
    public Material lampOff;

    [Header("Control externo (coordinado-actuado)")]
    [Tooltip("Si está activo, la fase la fija un IntersectionController, no el reloj.")]
    public bool externalControl = false;

    [Header("Compatibilidad")]
    public Phase startPhase = Phase.Green;
    public TrafficLight partnerLight;

    private Phase currentPhase = Phase.Red;
    private bool initialized = false;

    public Phase CurrentPhase => currentPhase;
    public float CycleTime => greenDuration + yellowDuration + redDuration;

    void Update()
    {
        if (externalControl) return; // la fase la fija el controlador
        SetPhase(ComputePhase(Time.time));
    }

    /// <summary>Fija la fase (usado por el control externo o el reloj).</summary>
    public void SetPhase(Phase p)
    {
        if (!initialized || p != currentPhase)
        {
            initialized = true;
            currentPhase = p;
            ApplyMaterial();
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

    private void ApplyMaterial()
    {
        if (redLamp != null || yellowLamp != null || greenLamp != null)
        {
            if (redLamp != null)    redLamp.material    = currentPhase == Phase.Red    ? matRed    : lampOff;
            if (yellowLamp != null) yellowLamp.material = currentPhase == Phase.Yellow ? matYellow : lampOff;
            if (greenLamp != null)  greenLamp.material  = currentPhase == Phase.Green  ? matGreen  : lampOff;
        }

        if (lampRenderer != null)
        {
            Material m = currentPhase == Phase.Green ? matGreen
                       : currentPhase == Phase.Yellow ? matYellow
                       : matRed;
            if (m != null) lampRenderer.material = m;
        }
    }
}
