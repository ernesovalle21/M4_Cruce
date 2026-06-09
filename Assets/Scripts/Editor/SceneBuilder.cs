using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SceneBuilder
{
    private const string MaterialsPath = "Assets/Materials/";
    private const string PrefabsPath = "Assets/Prefabs/";
    private const string ModelsPath = "Assets/Models/";

    [MenuItem("M4Cruce/Construir Todo")]
    public static void BuildAll()
    {
        EnsureCarTag();

        Material matAsfalto       = CreateMaterial("Mat_Asfalto",       new Color32(0x33, 0x33, 0x33, 0xFF));
        Material matInter         = CreateMaterial("Mat_Interseccion",  new Color32(0x3A, 0x3A, 0x3A, 0xFF));
        Material matLinea         = CreateMaterial("Mat_LineaBlanca",   Color.white);
        Material matSemCaja       = CreateMaterial("Mat_Semaforo_Caja", new Color32(0x11, 0x11, 0x11, 0xFF));
        Material matVerde         = CreateMaterial("Mat_Verde",         new Color32(0x00, 0xCC, 0x44, 0xFF));
        Material matAmarillo      = CreateMaterial("Mat_Amarillo",      new Color32(0xFF, 0xCC, 0x00, 0xFF));
        Material matRojo          = CreateMaterial("Mat_Rojo",          new Color32(0xCC, 0x22, 0x00, 0xFF));
        Material matBanqueta      = CreateMaterial("Mat_Banqueta",      new Color32(0xB0, 0xB0, 0xB0, 0xFF));
        Material matPasto         = CreateMaterial("Mat_Pasto",         new Color32(0x4C, 0x8C, 0x3F, 0xFF));

        AssetDatabase.SaveAssets();

        // B) Calle
        GameObject cruce = new GameObject("Cruce_T");
        cruce.transform.position = Vector3.zero;

        // Pasto / suelo base muy grande, debajo de todo
        CreateRoadCube("Pasto",            new Vector3(0, -0.2f, 30),   new Vector3(260, 0.1f, 220), matPasto, cruce.transform);

        // Calles (Junco extendida hasta z=0 para tapar gap con intersección)
        CreateRoadCube("Elizondo_Road",    new Vector3(0, -0.05f, 0),   new Vector3(200, 0.1f, 14), matAsfalto, cruce.transform);
        CreateRoadCube("Junco_Road",       new Vector3(0, -0.05f, 50),  new Vector3(14, 0.1f, 100), matAsfalto, cruce.transform);
        // Intersección del mismo ancho que las calles (sin sobresalir)
        CreateRoadCube("Intersection",     new Vector3(0, -0.04f, 0),   new Vector3(14, 0.1f, 14),  matInter,   cruce.transform);

        // Banqueta sur Elizondo (continua, no hay Junco al sur)
        CreateRoadCube("Banqueta_E_S",     new Vector3(0,    0.0f, -9), new Vector3(200, 0.2f, 4), matBanqueta, cruce.transform);
        // Banquetas norte Elizondo (van hasta el borde de Junco para tapar las esquinas)
        CreateRoadCube("Banqueta_E_N_W",   new Vector3(-53.5f, 0.0f, 9), new Vector3(93, 0.2f, 4),  matBanqueta, cruce.transform);
        CreateRoadCube("Banqueta_E_N_E",   new Vector3( 53.5f, 0.0f, 9), new Vector3(93, 0.2f, 4),  matBanqueta, cruce.transform);

        // Banquetas Junco (desde z=11 (donde terminan las de Elizondo norte) hacia arriba)
        CreateRoadCube("Banqueta_J_W",     new Vector3(-9, 0.0f, 55),   new Vector3(4, 0.2f, 88), matBanqueta, cruce.transform);
        CreateRoadCube("Banqueta_J_E",     new Vector3( 9, 0.0f, 55),   new Vector3(4, 0.2f, 88), matBanqueta, cruce.transform);

        // Líneas centrales (no atraviesan la intersección)
        CreateRoadCube("Linea_Centro_E_W", new Vector3(-53.5f, -0.03f, 0), new Vector3(93, 0.01f, 0.3f), matLinea, cruce.transform);
        CreateRoadCube("Linea_Centro_E_E", new Vector3( 53.5f, -0.03f, 0), new Vector3(93, 0.01f, 0.3f), matLinea, cruce.transform);
        CreateRoadCube("Linea_Centro_J",   new Vector3(0, -0.03f, 55),  new Vector3(0.3f, 0.01f, 88), matLinea, cruce.transform);

        // Cruces peatonales (3 lados de la T)
        CreateCrosswalk("Cruce_Peatonal_N", new Vector3(0, -0.025f, 9.5f),   axisHorizontal: true,  spanLength: 12f, cruce.transform, matLinea); // Norte (al norte de la intersección, atraviesa Junco)
        CreateCrosswalk("Cruce_Peatonal_E", new Vector3(9.5f, -0.025f, 0),   axisHorizontal: false, spanLength: 12f, cruce.transform, matLinea); // Este (atraviesa Elizondo)
        CreateCrosswalk("Cruce_Peatonal_W", new Vector3(-9.5f, -0.025f, 0),  axisHorizontal: false, spanLength: 12f, cruce.transform, matLinea); // Oeste

        // Líneas de "alto" gruesas justo antes de cada cruce peatonal
        CreateRoadCube("Stop_Line_J", new Vector3(0,    -0.024f, 11f),  new Vector3(7,    0.01f, 0.5f), matLinea, cruce.transform);
        CreateRoadCube("Stop_Line_E", new Vector3(11f,  -0.024f, 0),    new Vector3(0.5f, 0.01f, 7),   matLinea, cruce.transform);
        CreateRoadCube("Stop_Line_W", new Vector3(-11f, -0.024f, 0),    new Vector3(0.5f, 0.01f, 7),   matLinea, cruce.transform);

        // C) Waypoints
        GameObject wpRoot = new GameObject("Waypoints");
        wpRoot.transform.SetParent(cruce.transform, false);

        GameObject wpJ = new GameObject("WP_Junco");
        wpJ.transform.SetParent(wpRoot.transform, false);
        Transform wpJ0 = CreateWaypoint("WP_J0", new Vector3(  0, 0, 95),  wpJ.transform);
        Transform wpJ1 = CreateWaypoint("WP_J1", new Vector3(  0, 0, 20),  wpJ.transform);
        Transform wpJ2 = CreateWaypoint("WP_J2", new Vector3(  0, 0,  0),  wpJ.transform);
        Transform wpJ3 = CreateWaypoint("WP_J3", new Vector3(-40, 0,  0),  wpJ.transform);
        Transform wpJ4 = CreateWaypoint("WP_J4", new Vector3(-100,0,  0),  wpJ.transform);

        GameObject wpE = new GameObject("WP_Elizondo");
        wpE.transform.SetParent(wpRoot.transform, false);
        Transform wpE0 = CreateWaypoint("WP_E0", new Vector3( 95, 0, 0),  wpE.transform);
        Transform wpE1 = CreateWaypoint("WP_E1", new Vector3( 18, 0, 0),  wpE.transform);
        Transform wpE2 = CreateWaypoint("WP_E2", new Vector3(-20, 0, 0),  wpE.transform);
        Transform wpE3 = CreateWaypoint("WP_E3", new Vector3(-100,0, 0),  wpE.transform);

        // D) Semáforos
        TrafficLight semJunco    = CreateTrafficLight("Semaforo_Junco",    new Vector3(-1, 0, 18), TrafficLight.Phase.Green, matSemCaja, matVerde, matAmarillo, matRojo, cruce.transform);
        TrafficLight semElizondo = CreateTrafficLight("Semaforo_Elizondo", new Vector3(18, 0,  1), TrafficLight.Phase.Red,   matSemCaja, matVerde, matAmarillo, matRojo, cruce.transform);
        semJunco.partnerLight = semElizondo;
        semElizondo.partnerLight = semJunco;

        // E) Stop lines
        CreateStopLine("StopLine_Junco",    new Vector3(0, 0, 17), new Vector3(14, 3, 2),  semJunco,    cruce.transform);
        CreateStopLine("StopLine_Elizondo", new Vector3(17, 0, 0), new Vector3(2, 3, 14),  semElizondo, cruce.transform);

        // F) Spawner y cámara
        GameObject spawnerGO = new GameObject("CarSpawner");
        spawnerGO.transform.position = Vector3.zero;
        CarSpawner spawner = spawnerGO.AddComponent<CarSpawner>();

        spawner.juncoEntry = new CarSpawner.SpawnPoint
        {
            spawnTransform = wpJ0,
            waypoints = new Transform[] { wpJ0, wpJ1, wpJ2, wpJ3, wpJ4 },
            interval = 5f
        };
        spawner.elizondoEntry = new CarSpawner.SpawnPoint
        {
            spawnTransform = wpE0,
            waypoints = new Transform[] { wpE0, wpE1, wpE2, wpE3 },
            interval = 4f
        };
        spawner.maxCars = 8;

        // G) Prefabs
        List<GameObject> prefabs = new List<GameObject>();
        string[] modelNames = { "Sedan", "Suv", "CarroJeep", "CarroPickup" };
        foreach (string name in modelNames)
        {
            GameObject p = BuildCarPrefab(name);
            if (p != null) prefabs.Add(p);
        }
        spawner.carPrefabs = prefabs.ToArray();

        // Cámara
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            cam = camGO.AddComponent<Camera>();
        }
        cam.transform.position = new Vector3(0, 55, -15);
        cam.transform.rotation = Quaternion.Euler(65, 0, 0);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("M4 Cruce", "¡Escena construida!\nAhora dale Play.", "OK");
    }

    private static Material CreateMaterial(string name, Color color)
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
        AssetDatabase.SaveAssets();
        return mat;
    }

    private static GameObject CreateRoadCube(string name, Vector3 pos, Vector3 scale, Material mat, Transform parent)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        var col = go.GetComponent<BoxCollider>();
        if (col != null) col.enabled = false;
        var rend = go.GetComponent<Renderer>();
        if (rend != null) rend.sharedMaterial = mat;
        return go;
    }

    private static void CreateCrosswalk(string name, Vector3 center, bool axisHorizontal, float spanLength, Transform parent, Material matLinea)
    {
        // Cebra: 6 stripes blancas
        GameObject group = new GameObject(name);
        group.transform.SetParent(parent, false);
        group.transform.localPosition = center;

        int stripeCount = 6;
        float stripeWidth = 0.6f;
        float gap = (spanLength - stripeCount * stripeWidth) / (stripeCount - 1);
        float start = -spanLength * 0.5f + stripeWidth * 0.5f;

        for (int i = 0; i < stripeCount; i++)
        {
            float offset = start + i * (stripeWidth + gap);
            GameObject s = GameObject.CreatePrimitive(PrimitiveType.Cube);
            s.name = "Stripe_" + i;
            s.transform.SetParent(group.transform, false);
            var col = s.GetComponent<BoxCollider>();
            if (col != null) col.enabled = false;
            s.GetComponent<Renderer>().sharedMaterial = matLinea;

            if (axisHorizontal)
            {
                // Cruce que atraviesa una calle vertical (Junco): stripes a lo largo de X
                s.transform.localPosition = new Vector3(offset, 0f, 0f);
                s.transform.localScale = new Vector3(stripeWidth, 0.01f, 3.5f);
            }
            else
            {
                // Cruce que atraviesa una calle horizontal (Elizondo): stripes a lo largo de Z
                s.transform.localPosition = new Vector3(0f, 0f, offset);
                s.transform.localScale = new Vector3(3.5f, 0.01f, stripeWidth);
            }
        }
    }

    private static Transform CreateWaypoint(string name, Vector3 pos, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        return go.transform;
    }

    private static TrafficLight CreateTrafficLight(string name, Vector3 pos, TrafficLight.Phase startPhase, Material matCaja, Material matVerde, Material matAmarillo, Material matRojo, Transform parent)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.localPosition = pos;

        GameObject caja = GameObject.CreatePrimitive(PrimitiveType.Cube);
        caja.name = "Caja";
        caja.transform.SetParent(root.transform, false);
        caja.transform.localPosition = Vector3.zero;
        caja.transform.localScale = new Vector3(0.4f, 1.2f, 0.4f);
        var cajaCol = caja.GetComponent<BoxCollider>();
        if (cajaCol != null) cajaCol.enabled = false;
        caja.GetComponent<Renderer>().sharedMaterial = matCaja;

        GameObject lampara = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        lampara.name = "Lampara";
        lampara.transform.SetParent(root.transform, false);
        lampara.transform.localPosition = new Vector3(0, 0.7f, 0.2f);
        lampara.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
        var lampCol = lampara.GetComponent<SphereCollider>();
        if (lampCol != null) lampCol.enabled = false;

        TrafficLight tl = root.AddComponent<TrafficLight>();
        tl.startPhase = startPhase;
        tl.lampRenderer = lampara.GetComponent<Renderer>();
        tl.matGreen = matVerde;
        tl.matYellow = matAmarillo;
        tl.matRed = matRojo;
        return tl;
    }

    private static void CreateStopLine(string name, Vector3 pos, Vector3 size, TrafficLight tl, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        BoxCollider bc = go.AddComponent<BoxCollider>();
        bc.size = size;
        bc.isTrigger = true;
        var slt = go.AddComponent<StopLineTrigger>();
        slt.trafficLight = tl;
    }

    private static GameObject BuildCarPrefab(string modelName)
    {
        // Root vacío que será el prefab final
        GameObject root = new GameObject(modelName);
        root.transform.position = Vector3.zero;
        root.tag = "Car";

        string fbxPath = ModelsPath + modelName + ".fbx";
        GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        GameObject visual;
        if (fbx != null)
        {
            visual = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
        }
        else
        {
            visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.transform.localScale = new Vector3(2f, 1f, 4f);
        }
        visual.name = "Modelo";
        // Wrapper intermedio para no destruir el transform local original del FBX
        GameObject pivot = new GameObject("Pivot");
        pivot.transform.SetParent(root.transform, false);
        pivot.transform.localPosition = Vector3.zero;

        // Corrección de orientación por modelo (cada FBX viene exportado distinto)
        float yFix = 180f;
        if (modelName == "CarroJeep" || modelName == "CarroPickup")
        {
            yFix = 0f; // estos ya orientan correctamente a +Z sin rotación extra
        }
        pivot.transform.localRotation = Quaternion.Euler(0f, yFix, 0f);

        visual.transform.SetParent(pivot.transform, false);
        // Preservamos el transform local que trajo el FBX (no resetear)

        // Componentes en el root
        var mover = root.AddComponent<WaypointMover>();
        mover.speed = 4f;

        var rb = root.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // Collider del carro (en el root, dimensiones razonables)
        var bc = root.AddComponent<BoxCollider>();
        bc.center = new Vector3(0f, 0.75f, 0f);
        bc.size = new Vector3(2f, 1.5f, 4f);

        root.AddComponent<CarCollision>();

        string prefabPath = PrefabsPath + modelName + ".prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);
        return prefab;
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
            SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
            if (t.stringValue == "Car") return;
        }

        tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
        SerializedProperty newTag = tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1);
        newTag.stringValue = "Car";
        tagManager.ApplyModifiedProperties();
    }
}
