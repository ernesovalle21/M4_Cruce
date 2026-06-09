using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Extiende la escena base (cruce S2/Junco) al corredor completo Av. Luis Elizondo
/// con 3 semáforos coordinados por onda verde:
///   S1 Covarrubias  (offset 0)
///   S2 Junco         (offset 31.1)  -- ya existe, solo se le asigna el offset
///   S3 Garza Sada   (offset 10.1)
/// </summary>
public static class ExtendCorridor
{
    private const string MaterialsPath = "Assets/Materials/";

    // Distancias respecto a S2 (que está en el origen)
    private const float DistS1 = -34.5f; // 34.5 unidades a la IZQUIERDA de S2
    private const float DistS3 =  50.0f; // 50  unidades a la DERECHA  de S2

    // Offsets de onda verde (segundos)
    private const float OffsetS1 = 0f;
    private const float OffsetS2 = 31.1f;
    private const float OffsetS3 = 10.1f;

    [MenuItem("M4Cruce/Extend To Corridor")]
    public static void Extend()
    {
        // 1. Detectar el semáforo S2 existente
        GameObject s2JuncoObj    = GameObject.Find("Semaforo_Junco");
        GameObject s2ElizondoObj = GameObject.Find("Semaforo_Elizondo");
        if (s2JuncoObj == null || s2ElizondoObj == null)
        {
            EditorUtility.DisplayDialog("Extend Corridor",
                "No se encontró el cruce base (Semaforo_Junco / Semaforo_Elizondo).\n" +
                "Ejecuta primero 'M4Cruce > Construir Todo'.",
                "OK");
            return;
        }

        Transform cruceRoot = s2JuncoObj.transform.parent != null
            ? s2JuncoObj.transform.parent
            : s2JuncoObj.transform.root;

        // Posiciones X del centro de cada cruce (mismo eje Z que S2)
        Vector3 s2Center = Vector3.zero;
        float xS1 = s2Center.x + DistS1;
        float xS3 = s2Center.x + DistS3;

        // 2. Cargar materiales existentes
        Material matAsfalto   = LoadMat("Mat_Asfalto");
        Material matInter     = LoadMat("Mat_Interseccion");
        Material matLinea     = LoadMat("Mat_LineaBlanca");
        Material matSemCaja   = LoadMat("Mat_Semaforo_Caja");
        Material matVerde     = LoadMat("Mat_Verde");
        Material matAmarillo  = LoadMat("Mat_Amarillo");
        Material matRojo      = LoadMat("Mat_Rojo");
        Material matBanqueta  = LoadMat("Mat_Banqueta");

        // 3. Asegurar que la calle Elizondo cubre el corredor completo
        // (40 izq + 55 der desde S2). La calle existente ya cubre ±100 en x,
        // pero para garantizarlo agregamos parches en los extremos por si fue acortada.
        EnsureElizondoCoverage(cruceRoot, matAsfalto, matLinea, matBanqueta);

        // 4. Crear el cruce S1 (Covarrubias) — calle transversal hacia el norte
        GameObject s1Cross = BuildCrossStreet(
            crossName: "Cruce_Covarrubias",
            centerX: xS1,
            cruceRoot: cruceRoot,
            matAsfalto: matAsfalto, matInter: matInter, matLinea: matLinea,
            matBanqueta: matBanqueta);

        TrafficLight s1Light = BuildElizondoLight(
            name: "Semaforo_Covarrubias",
            position: new Vector3(xS1 + 17f, 0f, 1f),
            startPhase: TrafficLight.Phase.Green,
            startOffset: OffsetS1,
            matCaja: matSemCaja, matVerde: matVerde, matAmarillo: matAmarillo, matRojo: matRojo,
            parent: cruceRoot);

        BuildStopLine(
            name: "StopLine_Covarrubias",
            position: new Vector3(xS1 + 17f, 0f, 0f),
            size: new Vector3(2f, 3f, 14f),
            tl: s1Light,
            parent: cruceRoot);

        BuildCrosswalkOnElizondo(xS1, cruceRoot, matLinea);

        // 5. Crear el cruce S3 (Garza Sada)
        GameObject s3Cross = BuildCrossStreet(
            crossName: "Cruce_GarzaSada",
            centerX: xS3,
            cruceRoot: cruceRoot,
            matAsfalto: matAsfalto, matInter: matInter, matLinea: matLinea,
            matBanqueta: matBanqueta);

        TrafficLight s3Light = BuildElizondoLight(
            name: "Semaforo_GarzaSada",
            position: new Vector3(xS3 + 17f, 0f, 1f),
            startPhase: TrafficLight.Phase.Green,
            startOffset: OffsetS3,
            matCaja: matSemCaja, matVerde: matVerde, matAmarillo: matAmarillo, matRojo: matRojo,
            parent: cruceRoot);

        BuildStopLine(
            name: "StopLine_GarzaSada",
            position: new Vector3(xS3 + 17f, 0f, 0f),
            size: new Vector3(2f, 3f, 14f),
            tl: s3Light,
            parent: cruceRoot);

        BuildCrosswalkOnElizondo(xS3, cruceRoot, matLinea);

        // 6. Actualizar S2 (Elizondo) con su offset de onda verde
        TrafficLight s2ElizondoLight = s2ElizondoObj.GetComponent<TrafficLight>();
        if (s2ElizondoLight != null)
        {
            s2ElizondoLight.startOffset = OffsetS2;
        }

        // 7. Crear los 8 waypoints de la ruta completa en Elizondo
        Transform wpRoot = FindOrCreateChild(cruceRoot, "Waypoints");
        GameObject corridorGroupGO = new GameObject("WP_Corridor");
        corridorGroupGO.transform.SetParent(wpRoot, false);
        Transform corridorGroup = corridorGroupGO.transform;

        // Westbound: cars enter at x=+95 and salen en x=-100
        // - AnteSx: 3 unidades antes del StopLine (StopLine está en xCentro+17)
        // - PostSx: 3 unidades después del cruce (cruce ±7, así que xCentro-10)
        Transform wpEntrada = CreateWp("WP_Entrada", new Vector3( 95f,        0f, 0f), corridorGroup);
        Transform wpAnteS3  = CreateWp("WP_AnteS3",  new Vector3(xS3 + 20f,   0f, 0f), corridorGroup);
        Transform wpPostS3  = CreateWp("WP_PostS3",  new Vector3(xS3 - 10f,   0f, 0f), corridorGroup);
        Transform wpAnteS2  = CreateWp("WP_AnteS2",  new Vector3( 20f,        0f, 0f), corridorGroup);
        Transform wpPostS2  = CreateWp("WP_PostS2",  new Vector3(-10f,        0f, 0f), corridorGroup);
        Transform wpAnteS1  = CreateWp("WP_AnteS1",  new Vector3(xS1 + 20f,   0f, 0f), corridorGroup);
        Transform wpPostS1  = CreateWp("WP_PostS1",  new Vector3(xS1 - 10f,   0f, 0f), corridorGroup);
        Transform wpSalida  = CreateWp("WP_Salida",  new Vector3(-100f,       0f, 0f), corridorGroup);

        // 8. Reasignar la entrada Elizondo del spawner a la nueva ruta del corredor
        var spawner = Object.FindFirstObjectByType<CarSpawner>();
        if (spawner != null && spawner.elizondoEntry != null)
        {
            spawner.elizondoEntry.spawnTransform = wpEntrada;
            spawner.elizondoEntry.waypoints = new Transform[]
            {
                wpEntrada, wpAnteS3, wpPostS3,
                wpAnteS2,  wpPostS2,
                wpAnteS1,  wpPostS1, wpSalida
            };
        }

        // 9. Reposicionar la cámara para ver los 3 semáforos
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            cam = camGO.AddComponent<Camera>();
        }
        // Centro horizontal entre S1 (-35.5) y S3 (49) ≈ x=7
        cam.transform.position = new Vector3(7f, 95f, -30f);
        cam.transform.rotation = Quaternion.Euler(60f, 0f, 0f);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("M4 Cruce",
            "Corredor extendido: S1 (Covarrubias), S2 (Junco) y S3 (Garza Sada) con onda verde.\n" +
            $"Offsets: S1={OffsetS1}s · S2={OffsetS2}s · S3={OffsetS3}s",
            "OK");
    }

    // =========================================================================
    // Helpers de geometría
    // =========================================================================

    private static Material LoadMat(string name)
    {
        Material m = AssetDatabase.LoadAssetAtPath<Material>(MaterialsPath + name + ".mat");
        if (m == null)
        {
            Debug.LogWarning($"[ExtendCorridor] No se encontró material {name}. Ejecuta 'M4Cruce > Construir Todo' primero.");
        }
        return m;
    }

    private static Transform FindOrCreateChild(Transform parent, string name)
    {
        Transform t = parent.Find(name);
        if (t != null) return t;
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static GameObject CreateCube(string name, Vector3 pos, Vector3 scale, Material mat, Transform parent)
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

    private static Transform CreateWp(string name, Vector3 pos, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        return go.transform;
    }

    private static void EnsureElizondoCoverage(Transform parent, Material matAsfalto, Material matLinea, Material matBanqueta)
    {
        // Parche extra por seguridad: si la calle ya cubre estos rangos, simplemente
        // se solapa (no rompe nada porque comparten y de asfalto).
        // Cubrir x ∈ [xS1-50, xS3+50] aproximadamente.
        float xMin = DistS1 - 50f;      // ~ -84.5
        float xMax = DistS3 + 50f;      // ~  100
        float centerX = (xMin + xMax) * 0.5f;
        float length = xMax - xMin;

        // Sólo agregamos los parches si NO encontramos el GO existente con esos nombres.
        if (parent.Find("Elizondo_Road_Ext") == null)
        {
            CreateCube("Elizondo_Road_Ext", new Vector3(centerX, -0.055f, 0f), new Vector3(length, 0.1f, 14f), matAsfalto, parent);
        }
    }

    private static GameObject BuildCrossStreet(string crossName, float centerX, Transform cruceRoot,
        Material matAsfalto, Material matInter, Material matLinea, Material matBanqueta)
    {
        GameObject group = new GameObject(crossName);
        group.transform.SetParent(cruceRoot, false);

        // Calle transversal (hacia el norte, igual estilo que Junco)
        CreateCube($"{crossName}_Road",       new Vector3(centerX, -0.05f, 40f),  new Vector3(14f, 0.1f, 80f),  matAsfalto, group.transform);
        // Intersección
        CreateCube($"{crossName}_Inter",      new Vector3(centerX, -0.04f, 0f),   new Vector3(14f, 0.1f, 14f),  matInter,   group.transform);
        // Línea central de la calle transversal
        CreateCube($"{crossName}_LineaCentro",new Vector3(centerX, -0.03f, 45f),  new Vector3(0.3f, 0.01f, 68f), matLinea,  group.transform);
        // Banquetas de la calle transversal (oeste y este)
        CreateCube($"{crossName}_Banq_W",     new Vector3(centerX - 9f, 0f, 47f), new Vector3(4f, 0.2f, 72f),   matBanqueta, group.transform);
        CreateCube($"{crossName}_Banq_E",     new Vector3(centerX + 9f, 0f, 47f), new Vector3(4f, 0.2f, 72f),   matBanqueta, group.transform);

        return group;
    }

    private static void BuildCrosswalkOnElizondo(float centerX, Transform parent, Material matLinea)
    {
        // Cruce peatonal antes de la intersección (lado este, por donde llegan los carros westbound).
        // 6 stripes blancas a lo largo de Z, atravesando la calle Elizondo (ancho 14).
        GameObject group = new GameObject($"CrucePeatonal_X{centerX:F0}");
        group.transform.SetParent(parent, false);
        group.transform.localPosition = new Vector3(centerX + 9.5f, -0.025f, 0f);

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

    private static TrafficLight BuildElizondoLight(string name, Vector3 position, TrafficLight.Phase startPhase,
        float startOffset, Material matCaja, Material matVerde, Material matAmarillo, Material matRojo, Transform parent)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.localPosition = position;

        GameObject caja = GameObject.CreatePrimitive(PrimitiveType.Cube);
        caja.name = "Caja";
        caja.transform.SetParent(root.transform, false);
        caja.transform.localPosition = Vector3.zero;
        caja.transform.localScale = new Vector3(0.4f, 1.2f, 0.4f);
        var cajaCol = caja.GetComponent<BoxCollider>();
        if (cajaCol != null) cajaCol.enabled = false;
        if (matCaja != null) caja.GetComponent<Renderer>().sharedMaterial = matCaja;

        GameObject lampara = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        lampara.name = "Lampara";
        lampara.transform.SetParent(root.transform, false);
        lampara.transform.localPosition = new Vector3(0f, 0.7f, 0.2f);
        lampara.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
        var lampCol = lampara.GetComponent<SphereCollider>();
        if (lampCol != null) lampCol.enabled = false;

        TrafficLight tl = root.AddComponent<TrafficLight>();
        tl.startPhase = startPhase;
        tl.startOffset = startOffset;
        tl.lampRenderer = lampara.GetComponent<Renderer>();
        tl.matGreen = matVerde;
        tl.matYellow = matAmarillo;
        tl.matRed = matRojo;
        return tl;
    }

    private static void BuildStopLine(string name, Vector3 position, Vector3 size, TrafficLight tl, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        BoxCollider bc = go.AddComponent<BoxCollider>();
        bc.size = size;
        bc.isTrigger = true;
        var slt = go.AddComponent<StopLineTrigger>();
        slt.trafficLight = tl;
    }
}
