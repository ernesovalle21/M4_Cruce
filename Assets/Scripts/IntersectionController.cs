using UnityEngine;

/// <summary>
/// Control coordinado-actuado de una intersección:
///   - La avenida mayor (Elizondo) está en VERDE por defecto -> sin tiempo muerto.
///   - La transversal solo recibe su turno en el INSTANTE coordinado de la onda
///     verde (armOffset dentro del ciclo) Y solo si hay carros esperando (demanda).
///   - Si no hay demanda en ese instante, Elizondo se queda en verde.
///   - El turno de la transversal hace gap-out: termina antes si ya no hay carros.
///
/// Así se conserva la onda verde (los turnos ocurren en su tiempo coordinado)
/// pero se elimina el tiempo muerto cuando no hay tráfico transversal.
/// </summary>
public class IntersectionController : MonoBehaviour
{
    public TrafficLight major;        // Elizondo
    public TrafficLight minor;        // transversal
    public StopLineTrigger minorStop; // demanda de la transversal
    public StopLineTrigger majorStop; // demanda de Elizondo (para ceder si está vacío)

    [Header("Tiempos (s)")]
    public float cycle = 20f;
    public float armOffset = 0f;      // instante del ciclo en que empieza el amarillo mayor
    public float yellow = 2f;
    public float allRed = 1f;
    public float minorGreen = 4f;
    public float minMinorGreen = 1.5f; // verde mínimo antes de poder hacer gap-out
    public float minMajorGreen = 4f;   // verde mínimo de Elizondo antes de poder ceder

    [Header("Extensión de verde por cola alta (heurística J)")]
    [Tooltip("El semáforo respeta la onda verde, pero si al momento de ceder hay una " +
             "cola alta en Elizondo, EXTIENDE su verde unos segundos para vaciarla.")]
    public int extendQueueThreshold = 3;   // # de carros en cola para extender
    public float maxGreenExtension = 6f;   // segundos máximos de extensión

    private enum S { MajorGreen, MajorYellow, AllRed1, MinorGreen, MinorYellow, AllRed2 }
    private S state;
    private float t;
    private float lastArmPhase;
    private bool extendArmed;     // ya llegó el instante coordinado de ceder
    private float extendUsed;     // segundos de extensión consumidos

    void Start()
    {
        if (major != null) { major.externalControl = true; major.SetPhase(TrafficLight.Phase.Green); }
        if (minor != null) { minor.externalControl = true; minor.SetPhase(TrafficLight.Phase.Red); }
        state = S.MajorGreen;
        t = 0f;
        lastArmPhase = ArmPhase();
    }

    // Fase del reloj dentro del ciclo, relativa al instante de "armado" de la transversal.
    private float ArmPhase() => Mathf.Repeat(Time.time - armOffset, cycle);

    void Update()
    {
        t += Time.deltaTime;
        float ap = ArmPhase();
        bool armTick = ap < lastArmPhase; // se acaba de cruzar el instante coordinado
        lastArmPhase = ap;

        switch (state)
        {
            case S.MajorGreen:
                // Cede el paso a la transversal cuando hay demanda Y:
                //   - es el instante coordinado de la onda verde (armTick), o
                //   - Elizondo no tiene tráfico cerca (no tiene sentido hacerlos esperar).
                bool minorDemanda = minorStop != null && minorStop.HasCars;
                bool elizondoVacio = majorStop == null || !majorStop.HasCars;
                if (t >= minMajorGreen && minorDemanda && (armTick || elizondoVacio))
                    extendArmed = true;   // llegó el instante de ceder

                if (extendArmed)
                {
                    // HEURÍSTICA (pizarra): si hay cola alta en Elizondo, extiende el
                    // verde unos segundos para vaciarla antes de ceder.
                    bool colaAlta = majorStop != null && majorStop.WaitingCount >= extendQueueThreshold;
                    if (colaAlta && extendUsed < maxGreenExtension)
                        extendUsed += Time.deltaTime;
                    else
                        Go(S.MajorYellow, major, TrafficLight.Phase.Yellow);
                }
                break;

            case S.MajorYellow:
                if (t >= yellow) Go(S.AllRed1, major, TrafficLight.Phase.Red);
                break;

            case S.AllRed1:
                if (t >= allRed) Go(S.MinorGreen, minor, TrafficLight.Phase.Green);
                break;

            case S.MinorGreen:
                // Gap-out: termina si ya no hay carros (tras el mínimo) o al cumplir el verde.
                if (t >= minorGreen || (t >= minMinorGreen && (minorStop == null || !minorStop.HasCars)))
                    Go(S.MinorYellow, minor, TrafficLight.Phase.Yellow);
                break;

            case S.MinorYellow:
                if (t >= yellow) Go(S.AllRed2, minor, TrafficLight.Phase.Red);
                break;

            case S.AllRed2:
                if (t >= allRed) Go(S.MajorGreen, major, TrafficLight.Phase.Green);
                break;
        }
    }

    private void Go(S next, TrafficLight tl, TrafficLight.Phase p)
    {
        state = next;
        t = 0f;
        if (next == S.MajorGreen) { extendArmed = false; extendUsed = 0f; }
        if (tl != null) tl.SetPhase(p);
    }
}
