using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Construye (de cero, limpiando duplicados) el corredor Av. Luis Elizondo:
///   - Avenida recta de UN sentido y 3 carriles a lo largo del eje X.
///   - 3 cruces con semáforo coordinados por onda verde:
///       S1 García Roel (X=-34.5), S2 T-Junco (X=0), S3 Garza Sada (X=+50, T al sur).
///   - Un solo CarSpawner con una entrada por carril.
///
/// La onda verde se calcula automáticamente desde la distancia entre cruces y la
/// velocidad de los carros, así que SIEMPRE queda sincronizada.
///
/// Menú: M4Cruce > Construir Corredor (Limpio)
/// </summary>
public static class CorridorBuilder
{
    private const string MaterialsPath = "Assets/Materials/";
    private const string PrefabsPath = "Assets/Prefabs/";
    private const string RootName = "Corredor_Elizondo";

    // ---- Parámetros del corredor ----
    private const float CarSpeed = 5f;          // unidades/seg
    private const float GreenDur = 10f;
    private const float YellowDur = 2f;
    private const float RedDur = 8f;

    private const float RoadXMin = -75f;        // entrada de los carros
    private const float RoadXMax = 85f;         // salida de los carros
    private const float RoadWidth = 14f;        // ancho total (3 carriles)
    private static readonly float[] LaneZ = { -4.5f, 0f, 4.5f }; // centros de carril
    private const float HalfInter = 7f;         // media anchura de cada intersección

    private struct Cross
    {
        public string name;
        public float x;
        public int side; // +1 = calle transversal al norte, -1 = al sur
        public Cross(string n, float x, int s) { name = n; this.x = x; side = s; }
    }

    private static readonly Cross[] Crosses =
    {
        new Cross("S1_GarciaRoel", -34.5f, +1),
        new Cross("S2_JuncoT",       0.0f, +1),
        new Cross("S3_GarzaSada",   50.0f, -1),
    };

    [MenuItem("M4Cruce/Construir Corredor (Limpio)")]
    public static void Build()
    {
        CleanPrevious();
        EnsureCarTag();

        // Materiales
        Material matAsfalto  = GetOrCreateMat("Mat_Asfalto",       new Color32(0x33, 0x33, 0x33, 0xFF));
        Material matInter    = GetOrCreateMat("Mat_Interseccion",  new Color32(0x3A, 0x3A, 0x3A, 0xFF));
        Material matLinea    = GetOrCreateMat("Mat_LineaBlanca",   Color.white);
        Material matSemCaja  = GetOrCreateMat("Mat_Semaforo_Caja", new Color32(0x11, 0x11, 0x11, 0xFF));
        Material matVerde    = GetOrCreateMat("Mat_Verde",         new Color32(0x00, 0xCC, 0x44, 0xFF));
        Material matAmarillo = GetOrCreateMat("Mat_Amarillo",      new Color32(0xFF, 0xCC, 0x00, 0xFF));
        Material matRojo     = GetOrCreateMat("Mat_Rojo",          new Color32(0xCC, 0x22, 0x00, 0xFF));
        Material matBanqueta = GetOrCreateMat("Mat_Banqueta",      new Color32(0xB0, 0xB0, 0xB0, 0xFF));
        Material matPasto    = GetOrCreateMat("Mat_Pasto",         new Color32(0x4C, 0x8C, 0x3F, 0xFF));
        AssetDatabase.SaveAssets();

        GameObject root = new GameObject(RootName);
        Transform R = root.transform;

        float roadCenterX = (RoadXMin + RoadXMax) * 0.5f;
        float roadLen = RoadXMax - RoadXMin;

        // Pasto
        Cube("Pasto", new Vector3(roadCenterX, -0.08f, 0f), new Vector3(roadLen + 80f, 0.1f, 220f), matPasto, R);

        // Avenida Elizondo (asfalto)
        Cube("Elizondo_Road", new Vector3(roadCenterX, -0.05f, 0f), new Vector3(roadLen, 0.1f, RoadWidth), matAsfalto, R);

        // Banquetas a los lados de Elizondo
        float bz = RoadWidth * 0.5f + 2f; // centro de banqueta
        Cube("Banqueta_Sur",  new Vector3(roadCenterX, 0f, -bz), new Vector3(roadLen, 0.2f, 4f), matBanqueta, R);
        Cube("Banqueta_Norte",new Vector3(roadCenterX, 0f,  bz), new Vector3(roadLen, 0.2f, 4f), matBanqueta, R);

        // Líneas: 2 divisores de carril (z=±2.25) y 2 orillas (z=±6.8)
        Cube("Linea_Div_N",  new Vector3(roadCenterX, -0.045f,  2.25f), new Vector3(roadLen, 0.01f, 0.25f), matLinea, R);
        Cube("Linea_Div_S",  new Vector3(roadCenterX, -0.045f, -2.25f), new Vector3(roadLen, 0.01f, 0.25f), matLinea, R);
        Cube("Linea_Orilla_N", new Vector3(roadCenterX, -0.045f,  6.8f), new Vector3(roadLen, 0.01f, 0.3f), matLinea, R);
        Cube("Linea_Orilla_S", new Vector3(roadCenterX, -0.045f, -6.8f), new Vector3(roadLen, 0.01f, 0.3f), matLinea, R);

        // ---- Cruces (calle transversal + semáforo + línea de alto) ----
        float firstX = Crosses[0].x;
        var lights = new List<TrafficLight>();

        foreach (var c in Crosses)
        {
            GameObject g = new GameObject("Cruce_" + c.name);
            g.transform.SetParent(R, false);
            Transform G = g.transform;

            // Calle transversal (T) hacia norte o sur
            float crossZ = c.side * 47f;
            Cube(c.name + "_Road",  new Vector3(c.x, -0.05f, crossZ), new Vector3(14f, 0.1f, 80f), matAsfalto, G);
            Cube(c.name + "_Inter", new Vector3(c.x, -0.04f, 0f),     new Vector3(14f, 0.1f, 14f), matInter, G);
            Cube(c.name + "_LineaCentro", new Vector3(c.x, -0.035f, c.side * 47f), new Vector3(0.25f, 0.01f, 70f), matLinea, G);
            Cube(c.name + "_Banq_W", new Vector3(c.x - 9f, 0f, c.side * 50f), new Vector3(4f, 0.2f, 74f), matBanqueta, G);
            Cube(c.name + "_Banq_E", new Vector3(c.x + 9f, 0f, c.side * 50f), new Vector3(4f, 0.2f, 74f), matBanqueta, G);

            // Onda verde: offset = tiempo en llegar desde el primer cruce
            float cycle = GreenDur + YellowDur + RedDur;
            float offset = Mathf.Repeat((c.x - firstX) / CarSpeed, cycle);

            // Semáforo (caja + lámpara) en la orilla, antes de la intersección
            float lightX = c.x - (HalfInter + 4f);
            TrafficLight tl = BuildLight(c.name, new Vector3(lightX, 0f, c.side * 8f), offset,
                matSemCaja, matVerde, matAmarillo, matRojo, G);
            lights.Add(tl);

            // Cruce peatonal (cebra) antes de la intersección
            BuildCrosswalk(c.name, new Vector3(c.x - (HalfInter + 2f), -0.03f, 0f), matLinea, G);

            // Línea de alto (marca blanca) y trigger que detiene a los carros
            float stopX = c.x - (HalfInter + 4f);
            Cube(c.name + "_AltoMarca", new Vector3(stopX, -0.03f, 0f), new Vector3(0.6f, 0.01f, RoadWidth), matLinea, G);
            BuildStopLine(c.name, new Vector3(stopX, 0f, 0f), new Vector3(3f, 3f, RoadWidth), tl, G);
        }

        // ---- Waypoints por carril + entradas del spawner ----
        Transform wpRoot = new GameObject("Waypoints").transform;
        wpRoot.SetParent(R, false);

        var entries = new List<CarSpawner.SpawnPoint>();
        for (int lane = 0; lane < LaneZ.Length; lane++)
        {
            float z = LaneZ[lane];
            Transform laneRoot = new GameObject("Carril_" + lane).transform;
            laneRoot.SetParent(wpRoot, false);

            var path = new List<Transform>();
            path.Add(Wp($"L{lane}_Entrada", new Vector3(RoadXMin, 0f, z), laneRoot));
            foreach (var c in Crosses)
            {
                path.Add(Wp($"L{lane}_Ante_{c.name}", new Vector3(c.x - (HalfInter + 7f), 0f, z), laneRoot));
                path.Add(Wp($"L{lane}_Post_{c.name}", new Vector3(c.x + (HalfInter + 3f), 0f, z), laneRoot));
            }
            path.Add(Wp($"L{lane}_Salida", new Vector3(RoadXMax, 0f, z), laneRoot));

            entries.Add(new CarSpawner.SpawnPoint
            {
                spawnTransform = path[0],
                waypoints = path.ToArray(),
                interval = 3.5f
            });
        }

        // ---- Spawner ----
        GameObject spawnerGO = new GameObject("CarSpawner");
        spawnerGO.transform.SetParent(R, false);
        CarSpawner spawner = spawnerGO.AddComponent<CarSpawner>();
        spawner.carPrefabs = LoadPrefabs();
        spawner.entries = entries.ToArray();
        spawner.maxCars = 18;
        spawner.carSpeed = CarSpeed;

        // ---- Cámara ----
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            cam = camGO.AddComponent<Camera>();
        }
        cam.transform.position = new Vector3(5f, 130f, -55f);
        cam.transform.rotation = Quaternion.Euler(62f, 0f, 0f);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();

        float cyc = GreenDur + YellowDur + RedDur;
        EditorUtility.DisplayDialog("M4 Cruce — Corredor construido",
            "Av. Luis Elizondo: 1 sentido, 3 carriles, 3 semáforos con onda verde.\n\n" +
            $"Ciclo: {GreenDur}s verde / {YellowDur}s amarillo / {RedDur}s rojo (total {cyc}s)\n" +
            $"Velocidad carros: {CarSpeed} u/s\n" +
            "Offsets calculados automáticamente para la onda verde.\n\n" +
            "Dale Play.", "OK");
    }

    // =====================================================================
    // Helpers
    // =====================================================================

    private static void CleanPrevious()
    {
        Scene scene = SceneManager.GetActiveScene();
        foreach (GameObject go in scene.GetRootGameObjects())
        {
            if (go == null) continue;
            string n = go.name;
            bool kill =
                n.StartsWith("Corredor_Elizondo") ||
                n.StartsWith("Cruce_T") ||
                n.StartsWith("CarSpawner") ||
                n.Contains("(Clone)") ||
                go.CompareTag("Car");
            if (kill) Object.DestroyImmediate(go);
        }
    }

    private static GameObject Cube(string name, Vector3 pos, Vector3 scale, Material mat, Transform parent)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        var col = go.GetComponent<BoxCollider>();
        if (col != null) col.enabled = false;
        var rend = go.GetComponent<Renderer>();
        if (rend != null && mat != null) rend.sharedMaterial = mat;
        return go;
    }

    private static Transform Wp(string name, Vector3 pos, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        return go.transform;
    }

    private static void BuildCrosswalk(string name, Vector3 center, Material matLinea, Transform parent)
    {
        GameObject group = new GameObject(name + "_CrucePeatonal");
        group.transform.SetParent(parent, false);
        group.transform.localPosition = center;

        int stripeCount = 6;
        float stripeWidth = 0.6f;
        float span = 12f;
        float gap = (span - stripeCount * stripeWidth) / (stripeCount - 1);
        float start = -span * 0.5f + stripeWidth * 0.5f;

        for (int i = 0; i < stripeCount; i++)
        {
            float offset = start + i * (stripeWidth + gap);
            GameObject s = GameObject.CreatePrimitive(PrimitiveType.Cube);
            s.name = "Stripe_" + i;
            s.transform.SetParent(group.transform, false);
            s.transform.localPosition = new Vector3(0f, 0f, offset);
            s.transform.localScale = new Vector3(stripeWidth, 0.01f, 3.5f);
            var col = s.GetComponent<BoxCollider>();
            if (col != null) col.enabled = false;
            s.GetComponent<Renderer>().sharedMaterial = matLinea;
        }
    }

    private static TrafficLight BuildLight(string name, Vector3 position, float startOffset,
        Material matCaja, Material matVerde, Material matAmarillo, Material matRojo, Transform parent)
    {
        GameObject root = new GameObject("Semaforo_" + name);
        root.transform.SetParent(parent, false);
        root.transform.localPosition = position;

        GameObject caja = GameObject.CreatePrimitive(PrimitiveType.Cube);
        caja.name = "Caja";
        caja.transform.SetParent(root.transform, false);
        caja.transform.localPosition = new Vector3(0f, 1.5f, 0f);
        caja.transform.localScale = new Vector3(0.5f, 3f, 0.5f);
        var cajaCol = caja.GetComponent<BoxCollider>();
        if (cajaCol != null) cajaCol.enabled = false;
        if (matCaja != null) caja.GetComponent<Renderer>().sharedMaterial = matCaja;

        GameObject lampara = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        lampara.name = "Lampara";
        lampara.transform.SetParent(root.transform, false);
        lampara.transform.localPosition = new Vector3(0f, 3.2f, 0f);
        lampara.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
        var lampCol = lampara.GetComponent<SphereCollider>();
        if (lampCol != null) lampCol.enabled = false;

        TrafficLight tl = root.AddComponent<TrafficLight>();
        tl.greenDuration = GreenDur;
        tl.yellowDuration = YellowDur;
        tl.redDuration = RedDur;
        tl.startOffset = startOffset;
        tl.lampRenderer = lampara.GetComponent<Renderer>();
        tl.matGreen = matVerde;
        tl.matYellow = matAmarillo;
        tl.matRed = matRojo;
        return tl;
    }

    private static void BuildStopLine(string name, Vector3 position, Vector3 size, TrafficLight tl, Transform parent)
    {
        GameObject go = new GameObject("StopLine_" + name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        BoxCollider bc = go.AddComponent<BoxCollider>();
        bc.size = size;
        bc.isTrigger = true;
        var slt = go.AddComponent<StopLineTrigger>();
        slt.trafficLight = tl;
    }

    private static Material GetOrCreateMat(string name, Color color)
    {
        string path = MaterialsPath + name + ".mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material mat = new Material(shader);
        mat.color = color;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    private static GameObject[] LoadPrefabs()
    {
        string[] names = { "Sedan", "Suv", "CarroJeep", "CarroPickup" };
        var list = new List<GameObject>();
        foreach (string n in names)
        {
            GameObject p = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabsPath + n + ".prefab");
            if (p != null) list.Add(p);
        }
        if (list.Count == 0)
        {
            Debug.LogWarning("[CorridorBuilder] No se encontraron prefabs en Assets/Prefabs/. " +
                             "Ejecuta 'M4Cruce > Construir Todo' una vez para generarlos.");
        }
        return list.ToArray();
    }

    private static void EnsureCarTag()
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (assets == null || assets.Length == 0) return;
        SerializedObject tagManager = new SerializedObject(assets[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");
        if (tagsProp == null) return;
        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            if (tagsProp.GetArrayElementAtIndex(i).stringValue == "Car") return;
        }
        tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
        tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = "Car";
        tagManager.ApplyModifiedProperties();
    }
}
