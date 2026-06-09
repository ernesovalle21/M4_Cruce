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

    private int _activeCars = 0;

    void Update()
    {
        if (entries != null && entries.Length > 0)
        {
            foreach (var e in entries)
            {
                if (_activeCars >= maxCars) return;
                TrySpawn(e);
            }
            return;
        }

        // Comportamiento legado (cruce base de 2 entradas)
        if (_activeCars >= maxCars) return;
        TrySpawn(juncoEntry);
        if (_activeCars >= maxCars) return;
        TrySpawn(elizondoEntry);
    }

    private void TrySpawn(SpawnPoint sp)
    {
        if (sp == null || sp.spawnTransform == null) return;
        if (carPrefabs == null || carPrefabs.Length == 0) return;

        sp._timer -= Time.deltaTime;
        if (sp._timer > 0f) return;

        sp._timer = sp.interval;

        GameObject prefab = carPrefabs[UnityEngine.Random.Range(0, carPrefabs.Length)];
        if (prefab == null) return;

        GameObject car = Instantiate(prefab, sp.spawnTransform.position, sp.spawnTransform.rotation);
        car.tag = "Car";

        var mover = car.GetComponent<WaypointMover>();
        if (mover == null) mover = car.AddComponent<WaypointMover>();
        mover.waypoints = sp.waypoints;
        if (carSpeed > 0f) mover.speed = carSpeed;

        var notifier = car.AddComponent<CarDestroyNotifier>();
        notifier.spawner = this;

        _activeCars++;
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
