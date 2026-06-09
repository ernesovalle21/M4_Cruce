using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Construye (de cero, limpiando duplicados) el corredor Av. Luis Elizondo:
///   - Avenida recta de UN sentido y 3 carriles a lo largo del eje X.
///     Dir = -1 -> los carros van de DERECHA a IZQUIERDA (-X).
///   - 3 cruces con semáforo (poste con 3 focos) coordinados por onda verde.
///   - Tráfico transversal con semáforos en CONTRAFASE.
///   - Junco (centro) es una T de doble sentido: el carril que baja da vuelta
///     e se incorpora a Elizondo.
///   - Banquetas, cebras, flechas, escenografía (edificios + árboles).
///
/// Menú: M4Cruce > Construir Corredor (Limpio)
/// </summary>
public static class CorridorBuilder
{
    private const string MaterialsPath = "Assets/Materials/";
    private const string PrefabsPath = "Assets/Prefabs/";
    private const string RootName = "Corredor_Elizondo";

    // ---- Parámetros del corredor ----
    private const int Dir = -1;                 // sentido de Elizondo: -1 = derecha->izquierda
    private const float CarSpeed = 5f;
    private const float GreenDur = 10f;         // Elizondo
    private const float YellowDur = 2f;
    private const float RedDur = 8f;
    private const float CrossGreen = 6f;        // transversal (contrafase, ciclo 20)
    private const float CrossYellow = 2f;
    private const float CrossRed = 12f;

    private const float RoadXMin = -120f;       // tramo más largo
    private const float RoadXMax = 130f;
    private const float RoadWidth = 14f;
    private static readonly float[] LaneZ = { -4.5f, 0f, 4.5f };
    private static readonly float[] CrossLaneX = { -3f, 3f };
    private const float HalfInter = 7f;
    private const float Bz = RoadWidth * 0.5f + 2f;

    private struct Cross
    {
        public string name;
        public float x;
        public int side; // +1 = brazo al norte, -1 = al sur
        public Cross(string n, float x, int s) { name = n; this.x = x; side = s; }
    }

    private static readonly Cross[] Crosses =
    {
        new Cross("S1_GarciaRoel", -34.5f, +1),
        new Cross("S2_JuncoT",       0.0f, +1),
        new Cross("S3_GarzaSada",   50.0f, -1),
    };

    private const string JuncoName = "S2_JuncoT";

    // Lado por el que los carros se aproximan a un cruce (aguas arriba)
    private static float AnteX(float x) => x - Dir * (HalfInter + 7f);
    private static float PostX(float x) => x + Dir * (HalfInter + 3f);
    private static float StopX(float x) => x - Dir * (HalfInter + 4f);

    [MenuItem("M4Cruce/Construir Corredor (Limpio)")]
    public static void Build()
    {
        CleanPrevious();
        EnsureCarTag();

        Material matAsfalto  = GetOrCreateMat("Mat_Asfalto",       new Color32(0x33, 0x33, 0x33, 0xFF));
        Material matInter    = GetOrCreateMat("Mat_Interseccion",  new Color32(0x3A, 0x3A, 0x3A, 0xFF));
        Material matLinea    = GetOrCreateMat("Mat_LineaBlanca",   Color.white);
        Material matSemCaja  = GetOrCreateMat("Mat_Semaforo_Caja", new Color32(0x11, 0x11, 0x11, 0xFF));
        Material matVerde    = GetOrCreateMat("Mat_Verde",         new Color32(0x00, 0xCC, 0x44, 0xFF));
        Material matAmarillo = GetOrCreateMat("Mat_Amarillo",      new Color32(0xFF, 0xCC, 0x00, 0xFF));
        Material matRojo     = GetOrCreateMat("Mat_Rojo",          new Color32(0xCC, 0x22, 0x00, 0xFF));
        Material matBanqueta = GetOrCreateMat("Mat_Banqueta",      new Color32(0xB0, 0xB0, 0xB0, 0xFF));
        Material matPasto    = GetOrCreateMat("Mat_Pasto",         new Color32(0x4C, 0x8C, 0x3F, 0xFF));
        Material matFocoOff  = GetOrCreateMat("Mat_FocoApagado",   new Color32(0x20, 0x20, 0x20, 0xFF));
        Material[] matEdificios =
        {
            GetOrCreateMat("Mat_Edificio1", new Color32(0x9E, 0x9E, 0x96, 0xFF)),
            GetOrCreateMat("Mat_Edificio2", new Color32(0xC2, 0xA9, 0x84, 0xFF)),
            GetOrCreateMat("Mat_Edificio3", new Color32(0x8A, 0x6E, 0x5D, 0xFF)),
            GetOrCreateMat("Mat_Edificio4", new Color32(0x7C, 0x8B, 0x9E, 0xFF)),
        };
        Material matTronco = GetOrCreateMat("Mat_Tronco", new Color32(0x6B, 0x4A, 0x2E, 0xFF));
        Material matHojas  = GetOrCreateMat("Mat_Hojas",  new Color32(0x2F, 0x6E, 0x2F, 0xFF));
        AssetDatabase.SaveAssets();

        GameObject root = new GameObject(RootName);
        Transform R = root.transform;

        float roadCenterX = (RoadXMin + RoadXMax) * 0.5f;
        float roadLen = RoadXMax - RoadXMin;

        Cube("Pasto", new Vector3(roadCenterX, -0.08f, 0f), new Vector3(roadLen + 140f, 0.1f, 300f), matPasto, R);
        Cube("Elizondo_Road", new Vector3(roadCenterX, -0.05f, 0f), new Vector3(roadLen, 0.1f, RoadWidth), matAsfalto, R);

        // Banquetas segmentadas
        var northGaps = new List<Vector2>();
        var southGaps = new List<Vector2>();
        foreach (var c in Crosses)
        {
            var gap = new Vector2(c.x - HalfInter, c.x + HalfInter);
            northGaps.Add(gap);
            if (c.name != JuncoName) southGaps.Add(gap); // Junco es T: banqueta sur continua
        }
        BuildSidewalk("Banqueta_Norte",  Bz, northGaps, R, matBanqueta);
        BuildSidewalk("Banqueta_Sur",   -Bz, southGaps, R, matBanqueta);

        Cube("Linea_Div_N",    new Vector3(roadCenterX, -0.045f,  2.25f), new Vector3(roadLen, 0.01f, 0.25f), matLinea, R);
        Cube("Linea_Div_S",    new Vector3(roadCenterX, -0.045f, -2.25f), new Vector3(roadLen, 0.01f, 0.25f), matLinea, R);
        Cube("Linea_Orilla_N", new Vector3(roadCenterX, -0.045f,  6.8f),  new Vector3(roadLen, 0.01f, 0.3f),  matLinea, R);
        Cube("Linea_Orilla_S", new Vector3(roadCenterX, -0.045f, -6.8f),  new Vector3(roadLen, 0.01f, 0.3f),  matLinea, R);

        // Flechas de sentido (apuntan según Dir)
        float arrowYaw = Dir < 0 ? 180f : 0f;
        for (float ax = RoadXMin + 18f; ax <= RoadXMax - 18f; ax += 24f)
        {
            bool nearCross = false;
            foreach (var c in Crosses) if (Mathf.Abs(ax - c.x) < 12f) nearCross = true;
            if (nearCross) continue;
            foreach (float z in LaneZ) BuildArrow(ax, z, arrowYaw, R, matLinea);
        }

        // Orden de cruces según el sentido de avance; el primero define la onda verde
        var ordered = new List<Cross>(Crosses);
        ordered.Sort((a, b) => Dir < 0 ? b.x.CompareTo(a.x) : a.x.CompareTo(b.x));
        float firstX = ordered[0].x;
        float cycle = GreenDur + YellowDur + RedDur;
        float entryX = Dir < 0 ? RoadXMax : RoadXMin;
        float exitX  = Dir < 0 ? RoadXMin : RoadXMax;

        var entries = new List<CarSpawner.SpawnPoint>();
        Transform wpRoot = new GameObject("Waypoints").transform;
        wpRoot.SetParent(R, false);

        // ---- Entradas de Elizondo (3 carriles) ----
        for (int lane = 0; lane < LaneZ.Length; lane++)
        {
            float z = LaneZ[lane];
            Transform laneRoot = new GameObject("Elizondo_Carril_" + lane).transform;
            laneRoot.SetParent(wpRoot, false);

            var path = new List<Transform>();
            path.Add(Wp($"E{lane}_Entrada", new Vector3(entryX, 0f, z), laneRoot));
            foreach (var c in ordered)
            {
                path.Add(Wp($"E{lane}_Ante_{c.name}", new Vector3(AnteX(c.x), 0f, z), laneRoot));
                path.Add(Wp($"E{lane}_Post_{c.name}", new Vector3(PostX(c.x), 0f, z), laneRoot));
            }
            path.Add(Wp($"E{lane}_Salida", new Vector3(exitX, 0f, z), laneRoot));

            entries.Add(new CarSpawner.SpawnPoint
            {
                spawnTransform = path[0],
                waypoints = path.ToArray(),
                interval = 3.5f
            });
        }

        // ---- Cada cruce ----
        foreach (var c in Crosses)
        {
            GameObject g = new GameObject("Cruce_" + c.name);
            g.transform.SetParent(R, false);
            Transform G = g.transform;

            bool isT = c.name == JuncoName;

            Cube(c.name + "_Road_Largo", new Vector3(c.x, -0.05f, c.side * 47f), new Vector3(14f, 0.1f, 80f), matAsfalto, G);
            Cube(c.name + "_Inter", new Vector3(c.x, -0.04f, 0f), new Vector3(14f, 0.1f, 14f), matInter, G);
            Cube(c.name + "_LineaC_L", new Vector3(c.x, -0.035f, c.side * 47f), new Vector3(0.25f, 0.01f, 70f), matLinea, G);
            Cube(c.name + "_BanqL_W", new Vector3(c.x - 9f, 0f, c.side * 47f), new Vector3(4f, 0.2f, 80f), matBanqueta, G);
            Cube(c.name + "_BanqL_E", new Vector3(c.x + 9f, 0f, c.side * 47f), new Vector3(4f, 0.2f, 80f), matBanqueta, G);

            if (!isT)
            {
                Cube(c.name + "_Road_Stub", new Vector3(c.x, -0.05f, -c.side * 20f), new Vector3(14f, 0.1f, 28f), matAsfalto, G);
                Cube(c.name + "_LineaC_S",  new Vector3(c.x, -0.035f, -c.side * 20f), new Vector3(0.25f, 0.01f, 22f), matLinea, G);
                Cube(c.name + "_BanqS_W",   new Vector3(c.x - 9f, 0f, -c.side * 20f), new Vector3(4f, 0.2f, 28f), matBanqueta, G);
                Cube(c.name + "_BanqS_E",   new Vector3(c.x + 9f, 0f, -c.side * 20f), new Vector3(4f, 0.2f, 28f), matBanqueta, G);
            }

            float offset = Mathf.Abs(c.x - firstX) / CarSpeed;

            // Semáforo de Elizondo (en el lado por el que se aproximan los carros)
            float elizLightX = StopX(c.x);
            TrafficLight tlEliz = BuildLight(c.name + "_Eliz",
                new Vector3(elizLightX, 0f, c.side * 8.5f), offset, c.side,
                GreenDur, YellowDur, RedDur, new Vector3(0f, 0f, -c.side * 0.35f),
                matSemCaja, matVerde, matAmarillo, matRojo, matFocoOff, G);

            BuildCrosswalk(c.name + "_Eliz", new Vector3(c.x - Dir * (HalfInter + 2f), -0.03f, 0f), true, matLinea, G);
            Cube(c.name + "_AltoEliz", new Vector3(elizLightX, -0.03f, 0f), new Vector3(0.9f, 0.01f, RoadWidth), matLinea, G);
            BuildStopLine(c.name + "_Eliz", new Vector3(elizLightX, 0f, 0f), new Vector3(3f, 3f, RoadWidth), tlEliz, G);

            // Semáforo transversal (contrafase)
            float crossOffset = Mathf.Repeat(offset + GreenDur + YellowDur, cycle);
            float crossStopZ = c.side * (HalfInter + 4f);
            TrafficLight tlCross = BuildLight(c.name + "_Cross",
                new Vector3(c.x + 8.5f, 0f, crossStopZ + c.side * 2f), crossOffset, c.side,
                CrossGreen, CrossYellow, CrossRed, new Vector3(-0.35f, 0f, 0f),
                matSemCaja, matVerde, matAmarillo, matRojo, matFocoOff, G);

            BuildCrosswalk(c.name + "_Cross", new Vector3(c.x, -0.03f, c.side * (HalfInter + 2f)), false, matLinea, G);
            Cube(c.name + "_AltoCross", new Vector3(c.x, -0.03f, crossStopZ), new Vector3(RoadWidth, 0.01f, 0.9f), matLinea, G);
            BuildStopLine(c.name + "_Cross", new Vector3(c.x, 0f, crossStopZ), new Vector3(RoadWidth, 3f, 3f), tlCross, G);

            // --- Tráfico transversal ---
            Transform crossWpRoot = new GameObject("WP_" + c.name).transform;
            crossWpRoot.SetParent(wpRoot, false);

            if (isT)
            {
                // Junco: T de doble sentido. El carril que baja se detiene y da vuelta
                // para incorporarse al carril norte de Elizondo (en el sentido Dir).
                const float zNorte = 4.5f;
                float mergeX = PostX(c.x) + Dir * 4f; // ya dentro del carril, aguas abajo del cruce
                var baja = new List<Transform>
                {
                    Wp("Junco_Baja_0", new Vector3(-2.3f, 0f, 60f),  crossWpRoot),
                    Wp("Junco_Baja_1", new Vector3(-2.3f, 0f, 11f),  crossWpRoot), // alto (semáforo transversal)
                    Wp("Junco_Baja_2", new Vector3(-2.3f, 0f, 6f),   crossWpRoot),
                    Wp("Junco_Baja_3", new Vector3(Dir * 1.5f, 0f, 5.0f), crossWpRoot), // arco de vuelta
                    Wp("Junco_Baja_4", new Vector3(mergeX, 0f, zNorte), crossWpRoot),   // incorporado
                };
                // Desde el merge, sigue por el carril norte hasta la salida pasando los cruces aguas abajo
                foreach (var cc in ordered)
                {
                    if (Dir < 0 ? cc.x >= c.x : cc.x <= c.x) continue; // solo cruces aguas abajo
                    baja.Add(Wp($"Junco_Baja_Ante_{cc.name}", new Vector3(AnteX(cc.x), 0f, zNorte), crossWpRoot));
                    baja.Add(Wp($"Junco_Baja_Post_{cc.name}", new Vector3(PostX(cc.x), 0f, zNorte), crossWpRoot));
                }
                baja.Add(Wp("Junco_Baja_Salida", new Vector3(exitX, 0f, zNorte), crossWpRoot));
                entries.Add(new CarSpawner.SpawnPoint { spawnTransform = baja[0], waypoints = baja.ToArray(), interval = 5f });

                // Carril que sube (se aleja por Junco)
                var sube = new List<Transform>
                {
                    Wp("Junco_Sube_0", new Vector3(2.3f, 0f, 12f), crossWpRoot),
                    Wp("Junco_Sube_1", new Vector3(2.3f, 0f, 60f), crossWpRoot),
                };
                entries.Add(new CarSpawner.SpawnPoint { spawnTransform = sube[0], waypoints = sube.ToArray(), interval = 5f });
            }
            else
            {
                for (int cl = 0; cl < CrossLaneX.Length; cl++)
                {
                    float lx = c.x + CrossLaneX[cl];
                    var path = new List<Transform>
                    {
                        Wp($"{c.name}_C{cl}_Entrada", new Vector3(lx, 0f,  c.side * 60f), crossWpRoot),
                        Wp($"{c.name}_C{cl}_Ante",    new Vector3(lx, 0f,  c.side * (HalfInter + 4f)), crossWpRoot),
                        Wp($"{c.name}_C{cl}_Post",    new Vector3(lx, 0f, -c.side * (HalfInter + 3f)), crossWpRoot),
                        Wp($"{c.name}_C{cl}_Salida",  new Vector3(lx, 0f, -c.side * 30f), crossWpRoot),
                    };
                    entries.Add(new CarSpawner.SpawnPoint { spawnTransform = path[0], waypoints = path.ToArray(), interval = 5f });
                }
            }
        }

        // ---- Escenografía ----
        BuildScenery(R, matEdificios, matTronco, matHojas);

        // ---- Spawner ----
        GameObject spawnerGO = new GameObject("CarSpawner");
        spawnerGO.transform.SetParent(R, false);
        CarSpawner spawner = spawnerGO.AddComponent<CarSpawner>();
        spawner.carPrefabs = LoadPrefabs();
        spawner.entries = entries.ToArray();
        spawner.maxCars = 36;
        spawner.carSpeed = CarSpeed;

        // ---- Cámara ----
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            cam = camGO.AddComponent<Camera>();
        }
        cam.transform.position = new Vector3(roadCenterX, 175f, -90f);
        cam.transform.rotation = Quaternion.Euler(60f, 0f, 0f);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("M4 Cruce — Corredor construido",
            $"Av. Luis Elizondo ({(Dir < 0 ? "derecha->izquierda" : "izquierda->derecha")}), 3 carriles, tramo largo.\n" +
            "Tráfico transversal en contrafase · Junco = T de doble sentido.\n\n" +
            $"Ciclo: {cycle}s · Velocidad: {CarSpeed} u/s\n\nDale Play.", "OK");
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

    private static bool OverlapsRoad(float x, float z, float margin)
    {
        if (Mathf.Abs(z) <= Bz + 2f + margin) return true;
        foreach (var c in Crosses)
            if (Mathf.Abs(x - c.x) <= 9f + margin) return true;
        return false;
    }

    private static void BuildScenery(Transform parent, Material[] edificios, Material tronco, Material hojas)
    {
        GameObject sceneRoot = new GameObject("Escenografia");
        sceneRoot.transform.SetParent(parent, false);
        Transform S = sceneRoot.transform;

        var rng = new System.Random(12345);

        float[] zRows = { -20f, -34f, -48f, 20f, 34f, 48f };
        for (float bx = RoadXMin + 4f; bx <= RoadXMax - 4f; bx += 13f)
        {
            foreach (float bz in zRows)
            {
                float jx = bx + (float)(rng.NextDouble() * 4 - 2);
                float jz = bz + (float)(rng.NextDouble() * 4 - 2);
                if (OverlapsRoad(jx, jz, 4f)) continue;
                if (rng.NextDouble() < 0.25) continue;

                float w = 5f + (float)rng.NextDouble() * 4f;
                float d = 5f + (float)rng.NextDouble() * 4f;
                float h = 4f + (float)rng.NextDouble() * 9f;
                Material m = edificios[rng.Next(edificios.Length)];
                Cube($"Edificio_{jx:F0}_{jz:F0}", new Vector3(jx, h * 0.5f, jz), new Vector3(w, h, d), m, S);
            }
        }

        for (float tx = RoadXMin + 6f; tx <= RoadXMax - 6f; tx += 9f)
        {
            foreach (float tz in new[] { -12f, 12f })
            {
                if (OverlapsRoad(tx, tz, 1.5f)) continue;
                if (rng.NextDouble() < 0.35) continue;
                BuildTree(new Vector3(tx, 0f, tz), tronco, hojas, S);
            }
        }
    }

    private static void BuildTree(Vector3 pos, Material tronco, Material hojas, Transform parent)
    {
        GameObject t = new GameObject("Arbol");
        t.transform.SetParent(parent, false);
        t.transform.localPosition = pos;

        Cube("Tronco", new Vector3(0f, 1f, 0f), new Vector3(0.4f, 2f, 0.4f), tronco, t.transform);

        GameObject leaves = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        leaves.name = "Copa";
        leaves.transform.SetParent(t.transform, false);
        leaves.transform.localPosition = new Vector3(0f, 2.6f, 0f);
        leaves.transform.localScale = Vector3.one * 2.4f;
        var col = leaves.GetComponent<SphereCollider>();
        if (col != null) col.enabled = false;
        leaves.GetComponent<Renderer>().sharedMaterial = hojas;
    }

    private static void BuildSidewalk(string name, float z, List<Vector2> gaps, Transform parent, Material mat)
    {
        gaps.Sort((a, b) => a.x.CompareTo(b.x));
        float cursor = RoadXMin;
        int i = 0;
        foreach (var gap in gaps)
        {
            if (gap.x > cursor)
            {
                AddSidewalkSeg($"{name}_{i}", cursor, gap.x, z, parent, mat);
                i++;
            }
            cursor = Mathf.Max(cursor, gap.y);
        }
        if (cursor < RoadXMax)
            AddSidewalkSeg($"{name}_{i}", cursor, RoadXMax, z, parent, mat);
    }

    private static void AddSidewalkSeg(string name, float x0, float x1, float z, Transform parent, Material mat)
    {
        float cx = (x0 + x1) * 0.5f;
        float len = x1 - x0;
        Cube(name, new Vector3(cx, 0f, z), new Vector3(len, 0.2f, 4f), mat, parent);
    }

    private static void BuildArrow(float x, float z, float yaw, Transform parent, Material mat)
    {
        GameObject g = new GameObject($"Flecha_{x:F0}_{z:F0}");
        g.transform.SetParent(parent, false);
        g.transform.localPosition = new Vector3(x, -0.043f, z);
        g.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

        Cube("Shaft", new Vector3(-0.2f, 0f, 0f), new Vector3(2.4f, 0.01f, 0.35f), mat, g.transform);
        GameObject hR = Cube("HeadR", new Vector3(0.7f, 0f, 0.4f), new Vector3(1.4f, 0.01f, 0.35f), mat, g.transform);
        hR.transform.localRotation = Quaternion.Euler(0f, 50f, 0f);
        GameObject hL = Cube("HeadL", new Vector3(0.7f, 0f, -0.4f), new Vector3(1.4f, 0.01f, 0.35f), mat, g.transform);
        hL.transform.localRotation = Quaternion.Euler(0f, -50f, 0f);
    }

    private static void BuildCrosswalk(string name, Vector3 center, bool horizontal, Material matLinea, Transform parent)
    {
        GameObject group = new GameObject(name + "_CrucePeatonal");
        group.transform.SetParent(parent, false);
        group.transform.localPosition = center;

        int stripeCount = 7;
        float stripeWidth = 0.7f;
        float span = 13f;
        float gap = (span - stripeCount * stripeWidth) / (stripeCount - 1);
        float start = -span * 0.5f + stripeWidth * 0.5f;

        for (int i = 0; i < stripeCount; i++)
        {
            float offset = start + i * (stripeWidth + gap);
            GameObject s = GameObject.CreatePrimitive(PrimitiveType.Cube);
            s.name = "Stripe_" + i;
            s.transform.SetParent(group.transform, false);
            if (horizontal)
            {
                s.transform.localPosition = new Vector3(0f, 0f, offset);
                s.transform.localScale = new Vector3(stripeWidth, 0.01f, 4f);
            }
            else
            {
                s.transform.localPosition = new Vector3(offset, 0f, 0f);
                s.transform.localScale = new Vector3(4f, 0.01f, stripeWidth);
            }
            var col = s.GetComponent<BoxCollider>();
            if (col != null) col.enabled = false;
            s.GetComponent<Renderer>().sharedMaterial = matLinea;
        }
    }

    private static TrafficLight BuildLight(string name, Vector3 basePos, float startOffset, int side,
        float green, float yellow, float red, Vector3 focoOffset,
        Material matCaja, Material matVerde, Material matAmarillo, Material matRojo, Material matOff, Transform parent)
    {
        GameObject root = new GameObject("Semaforo_" + name);
        root.transform.SetParent(parent, false);
        root.transform.localPosition = basePos;

        Cube("Poste", new Vector3(0f, 2.5f, 0f), new Vector3(0.3f, 5f, 0.3f), matCaja, root.transform);
        Cube("Caja", new Vector3(0f, 5.2f, 0f), new Vector3(0.7f, 2.1f, 0.5f), matCaja, root.transform);

        Renderer red_ = BuildLamp("Foco_Rojo",     new Vector3(0f, 5.9f, 0f) + focoOffset, matOff, root.transform);
        Renderer yel  = BuildLamp("Foco_Amarillo", new Vector3(0f, 5.2f, 0f) + focoOffset, matOff, root.transform);
        Renderer grn  = BuildLamp("Foco_Verde",    new Vector3(0f, 4.5f, 0f) + focoOffset, matOff, root.transform);

        TrafficLight tl = root.AddComponent<TrafficLight>();
        tl.greenDuration = green;
        tl.yellowDuration = yellow;
        tl.redDuration = red;
        tl.startOffset = startOffset;
        tl.matGreen = matVerde;
        tl.matYellow = matAmarillo;
        tl.matRed = matRojo;
        tl.lampOff = matOff;
        tl.redLamp = red_;
        tl.yellowLamp = yel;
        tl.greenLamp = grn;
        return tl;
    }

    private static Renderer BuildLamp(string name, Vector3 pos, Material off, Transform parent)
    {
        GameObject s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        s.name = name;
        s.transform.SetParent(parent, false);
        s.transform.localPosition = pos;
        s.transform.localScale = Vector3.one * 0.5f;
        var col = s.GetComponent<SphereCollider>();
        if (col != null) col.enabled = false;
        var rend = s.GetComponent<Renderer>();
        if (off != null) rend.sharedMaterial = off;
        return rend;
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
        string[] names = { "Sedan", "Suv" };
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
