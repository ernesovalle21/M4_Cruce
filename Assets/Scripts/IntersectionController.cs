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

    [Header("Tiempos (s)")]
    public float cycle = 20f;
    public float armOffset = 0f;      // instante del ciclo en que empieza el amarillo mayor
    public float yellow = 2f;
    public float allRed = 1f;
    public float minorGreen = 4f;
    public float minMinorGreen = 1.5f; // verde mínimo antes de poder hacer gap-out

    private enum S { MajorGreen, MajorYellow, AllRed1, MinorGreen, MinorYellow, AllRed2 }
    private S state;
    private float t;
    private float lastArmPhase;

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
                // Solo cede el paso en el instante coordinado y si hay demanda transversal.
                if (armTick && minorStop != null && minorStop.HasCars)
                    Go(S.MajorYellow, major, TrafficLight.Phase.Yellow);
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
        if (tl != null) tl.SetPhase(p);
    }
}
