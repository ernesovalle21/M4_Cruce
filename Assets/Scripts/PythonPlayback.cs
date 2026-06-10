using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Puente Python -> Unity: lee Analisis/playback.json (generado por la simulación
/// AgentPy en Python) y REPRODUCE en 3D las posiciones de los vehículos y el estado
/// de los semáforos, frame por frame.
///
/// Es independiente del corredor de simulación nativo de Unity (no lo modifica):
/// aquí Unity solo DIBUJA lo que calculó Python.
/// </summary>
public class PythonPlayback : MonoBehaviour
{
    [System.Serializable] public class Frame { public float t; public int[] lights; public float[] cars; }
    [System.Serializable] public class PlaybackData { public float[] xs; public float xEntry; public float xExit; public float dt; public Frame[] frames; }

    [Tooltip("Ruta del JSON. Si se deja vacío usa Analisis/playback.json del proyecto.")]
    public string jsonPath = "";
    [Tooltip("Modelos de carro (Sedan/Suv). Si está vacío usa cubos.")]
    public GameObject[] carPrefabs;
    public float scale = 0.5f;             // metros -> unidades de escena
    public float secondsPerFrame = 0.13f;  // velocidad de reproducción
    public float laneZ = 0f;
    public float carLengthUnits = 2.4f;    // largo objetivo del carro
    public bool loop = true;

    private PlaybackData data;
    private int frameIdx = 0;
    private float timer = 0f;
    private readonly List<GameObject> carPool = new List<GameObject>();
    private Renderer[] lightRends;
    private Material matRed, matYel, matGrn, matCar, matRoad, matGrass, matLinea;

    private float CenterM => (data.xEntry + data.xExit) * 0.5f;
    private float X(float meters) => (meters - CenterM) * scale;

    void Start()
    {
        if (string.IsNullOrEmpty(jsonPath))
            jsonPath = Path.Combine(Application.dataPath, "..", "Analisis", "playback.json");

        if (!File.Exists(jsonPath))
        {
            Debug.LogError("[PythonPlayback] No existe el archivo:\n" + jsonPath +
                           "\nGenéralo con: python3 Analisis/export_playback.py");
            enabled = false;
            return;
        }

        data = JsonUtility.FromJson<PlaybackData>(File.ReadAllText(jsonPath));
        if (data == null || data.frames == null || data.frames.Length == 0)
        {
            Debug.LogError("[PythonPlayback] JSON vacío o inválido.");
            enabled = false;
            return;
        }

        matRed   = Mat(new Color(0.85f, 0.13f, 0f));
        matYel   = Mat(new Color(1f, 0.80f, 0f));
        matGrn   = Mat(new Color(0f, 0.85f, 0.30f));
        matCar   = Mat(new Color(0.20f, 0.40f, 0.90f));
        matRoad  = Mat(new Color(0.22f, 0.22f, 0.22f));
        matGrass = Mat(new Color(0.30f, 0.55f, 0.25f));
        matLinea = Mat(Color.white);

        BuildScene();
        PlaceCamera();
    }

    private Material Mat(Color c)
    {
        Shader s = Shader.Find("Universal Render Pipeline/Lit");
        if (s == null) s = Shader.Find("Standard");
        var m = new Material(s); m.color = c;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        return m;
    }

    private GameObject Cube(string name, Vector3 pos, Vector3 scl, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name; go.transform.SetParent(transform, false);
        go.transform.localPosition = pos; go.transform.localScale = scl;
        var col = go.GetComponent<Collider>(); if (col != null) col.enabled = false;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    private GameObject Sphere(string name, Vector3 pos, float diam, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name; go.transform.SetParent(transform, false);
        go.transform.localPosition = pos; go.transform.localScale = Vector3.one * diam;
        var col = go.GetComponent<Collider>(); if (col != null) col.enabled = false;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    private void BuildScene()
    {
        float len = (data.xExit - data.xEntry) * scale;
        float roadW = 7f;
        Cube("PB_Pasto", new Vector3(0, -0.2f, 0), new Vector3(len + 60f, 0.1f, 90f), matGrass);
        Cube("PB_Road", new Vector3(0, 0f, laneZ), new Vector3(len, 0.1f, roadW), matRoad);
        Cube("PB_Linea", new Vector3(0, 0.03f, laneZ), new Vector3(len, 0.01f, 0.2f), matLinea);

        lightRends = new Renderer[data.xs.Length];
        for (int i = 0; i < data.xs.Length; i++)
        {
            float lx = X(data.xs[i]);
            Cube("PB_Poste" + i, new Vector3(lx, 3f, laneZ + roadW * 0.5f + 1f), new Vector3(0.8f, 6f, 0.8f), matRoad);
            var foco = Sphere("PB_Foco" + i, new Vector3(lx, 6.5f, laneZ + roadW * 0.5f + 1f), 2.6f, matRed);
            lightRends[i] = foco.GetComponent<Renderer>();
        }

        int maxCars = 0;
        foreach (var f in data.frames) if (f.cars != null && f.cars.Length > maxCars) maxCars = f.cars.Length;
        for (int i = 0; i < maxCars; i++)
        {
            var car = MakeCar(i);
            car.SetActive(false);
            carPool.Add(car);
        }
    }

    private GameObject MakeCar(int idx)
    {
        if (carPrefabs != null && carPrefabs.Length > 0 && carPrefabs[idx % carPrefabs.Length] != null)
        {
            var car = Instantiate(carPrefabs[idx % carPrefabs.Length]);
            car.name = "PB_Car" + idx;

            // Solo visual: desactivar lógica de juego
            var wm = car.GetComponent<WaypointMover>(); if (wm) wm.enabled = false;
            var cc = car.GetComponent<CarCollision>(); if (cc) cc.enabled = false;
            var rb = car.GetComponent<Rigidbody>(); if (rb) rb.isKinematic = true;
            foreach (var col in car.GetComponentsInChildren<Collider>()) col.enabled = false;
            car.tag = "Untagged";

            // Normalizar tamaño al objetivo usando los bounds del modelo
            var rends = car.GetComponentsInChildren<Renderer>();
            if (rends.Length > 0)
            {
                Bounds b = rends[0].bounds;
                foreach (var r in rends) b.Encapsulate(r.bounds);
                float lengthNow = Mathf.Max(b.size.x, b.size.z);
                if (lengthNow > 0.001f) car.transform.localScale *= carLengthUnits / lengthNow;
            }

            car.transform.SetParent(transform, false);
            car.transform.localRotation = Quaternion.Euler(0f, 90f, 0f); // frente +Z -> +X
            return car;
        }
        return Cube("PB_Car" + idx, Vector3.zero, new Vector3(carLengthUnits, 0.9f, carLengthUnits * 0.55f), matCar);
    }

    private void PlaceCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;
        float len = (data.xExit - data.xEntry) * scale;
        cam.transform.position = new Vector3(0f, len * 0.42f, -len * 0.45f);
        cam.transform.rotation = Quaternion.Euler(45f, 0f, 0f);
    }

    void Update()
    {
        if (data == null) return;
        timer += Time.deltaTime;
        while (timer >= secondsPerFrame)
        {
            timer -= secondsPerFrame;
            frameIdx++;
            if (frameIdx >= data.frames.Length) frameIdx = loop ? 0 : data.frames.Length - 1;
        }

        var f = data.frames[frameIdx];
        for (int i = 0; i < lightRends.Length && i < f.lights.Length; i++)
            lightRends[i].sharedMaterial = f.lights[i] == 2 ? matGrn : (f.lights[i] == 1 ? matYel : matRed);

        int n = f.cars != null ? f.cars.Length : 0;
        for (int i = 0; i < carPool.Count; i++)
        {
            if (i < n)
            {
                if (!carPool[i].activeSelf) carPool[i].SetActive(true);
                carPool[i].transform.localPosition = new Vector3(X(f.cars[i]), 0.05f, laneZ);
            }
            else if (carPool[i].activeSelf) carPool[i].SetActive(false);
        }
    }

    void OnGUI()
    {
        int total = data != null ? data.frames.Length : 0;
        GUI.Box(new Rect(10, 10, 340, 50),
            $"PYTHON → UNITY (reproduciendo simulación AgentPy)\nFrame {frameIdx + 1}/{total}");
    }
}
