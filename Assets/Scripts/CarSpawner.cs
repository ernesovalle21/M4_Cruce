using System;
using UnityEngine;

public class CarSpawner : MonoBehaviour
{
    [Serializable]
    public class SpawnPoint
    {
        public Transform spawnTransform;
        public Transform[] waypoints;
        public float interval = 5f;
        [HideInInspector] public float _timer;
    }

    public GameObject[] carPrefabs;

    [Header("Entradas (genérico, una por carril)")]
    public SpawnPoint[] entries;

    [Header("Entradas legadas (se usan solo si 'entries' está vacío)")]
    public SpawnPoint juncoEntry;
    public SpawnPoint elizondoEntry;

    [Header("Config")]
    public int maxCars = 8;
    [Tooltip("Velocidad que se asigna a cada carro al instanciarlo (0 = usar la del prefab).")]
    public float carSpeed = 0f;
    [Tooltip("Frenado que se asigna a cada carro (0 = usar el del prefab).")]
    public float carBraking = 0f;
    [Tooltip("Aceleración que se asigna a cada carro (0 = usar la del prefab).")]
    public float carAccel = 0f;

    [Header("Variación de tráfico (horas pico cíclicas)")]
    public bool rushHourCycle = true;
    [Tooltip("Segundos de un ciclo completo valle->pico->valle.")]
    public float rushPeriod = 50f;
    [Tooltip("Factor de densidad en el valle (tráfico bajo).")]
    public float minDensity = 0.4f;
    [Tooltip("Factor de densidad en el pico (tráfico alto).")]
    public float maxDensity = 1.5f;

    private int _activeCars = 0;

    private float DensityFactor()
    {
        if (!rushHourCycle) return 1f;
        float p = (Mathf.Sin(Time.time * 2f * Mathf.PI / Mathf.Max(1f, rushPeriod)) + 1f) * 0.5f;
        return Mathf.Lerp(minDensity, maxDensity, p);
    }

    void Update()
    {
        float factor = DensityFactor();
        int effMax = Mathf.Max(1, Mathf.RoundToInt(maxCars * factor));

        if (entries != null && entries.Length > 0)
        {
            foreach (var e in entries)
            {
                if (_activeCars >= effMax) return;
                TrySpawn(e, factor);
            }
            return;
        }

        // Comportamiento legado (cruce base de 2 entradas)
        if (_activeCars >= effMax) return;
        TrySpawn(juncoEntry, factor);
        if (_activeCars >= effMax) return;
        TrySpawn(elizondoEntry, factor);
    }

    private void TrySpawn(SpawnPoint sp, float factor)
    {
        if (sp == null || sp.spawnTransform == null) return;
        if (carPrefabs == null || carPrefabs.Length == 0) return;

        sp._timer -= Time.deltaTime;
        if (sp._timer > 0f) return;

        // Si la entrada del carril está ocupada por otro carro, NO instanciar
        // encima: reintentar en breve. Evita el apelmazamiento.
        if (IsSpawnBlocked(sp.spawnTransform))
        {
            sp._timer = 0.5f;
            return;
        }

        // Más densidad (factor alto) = menor intervalo = más carros.
        sp._timer = sp.interval / Mathf.Max(0.05f, factor);

        GameObject prefab = carPrefabs[UnityEngine.Random.Range(0, carPrefabs.Length)];
        if (prefab == null) return;

        GameObject car = Instantiate(prefab, sp.spawnTransform.position, sp.spawnTransform.rotation);
        car.tag = "Car";

        var mover = car.GetComponent<WaypointMover>();
        if (mover == null) mover = car.AddComponent<WaypointMover>();
        mover.waypoints = sp.waypoints;
        if (carSpeed > 0f) mover.speed = carSpeed;
        if (carBraking > 0f) mover.braking = carBraking;
        if (carAccel > 0f) mover.acceleration = carAccel;

        var notifier = car.AddComponent<CarDestroyNotifier>();
        notifier.spawner = this;

        _activeCars++;
    }

    /// <summary>True si ya hay un carro cerca del punto de spawn.</summary>
    private bool IsSpawnBlocked(Transform t)
    {
        Vector3 center = t.position + Vector3.up * 0.75f;
        Vector3 halfExtents = new Vector3(1.5f, 1f, 3f); // ~3 ancho x 6 largo
        Collider[] hits = Physics.OverlapBox(center, halfExtents, t.rotation);
        foreach (var h in hits)
        {
            if (h == null || h.isTrigger) continue;
            if (h.CompareTag("Car") || h.transform.root.CompareTag("Car")) return true;
        }
        return false;
    }

    public void OnCarDestroyed()
    {
        _activeCars = Mathf.Max(0, _activeCars - 1);
    }
}

public class CarDestroyNotifier : MonoBehaviour
{
    public CarSpawner spawner;

    void OnDestroy()
    {
        if (spawner != null) spawner.OnCarDestroyed();
    }
}
