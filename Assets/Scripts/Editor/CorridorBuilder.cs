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
    // Transversal (contrafase, ciclo 20). Verde más corto + todo-rojo en las
    // transiciones para que la intersección se vacíe (evita choques en la vuelta).
    private const float CrossGreen = 4f;
    private const float CrossYellow = 2f;
    private const float CrossRed = 14f;
    private const float AllRed = 1f;            // colchón de todo-rojo

    private const float RoadXMin = -180f;       // Elizondo extendida también a la izquierda
    private const float RoadXMax = 210f;        // Elizondo extendida más a la derecha
    private const float RoadWidth = 14f;
    private static readonly float[] LaneZ = { -4.5f, 0f, 4.5f };
    private const float HalfInter = 7f;
    private const float Bz = RoadWidth * 0.5f + 2f;

    // Longitud de los brazos de las calles transversales (calles más largas)
    private const float ArmLen = 145f;    // longitud de los brazos transversales (calles verticales)
    private const float LeftWidth = 10f;  // calle izquierda (Garza Sada): angosta, 1 carril

    private struct Cross
    {
        public string name;
        public float x;
        public int side; // +1 = brazo al norte, -1 = al sur
        public Cross(string n, float x, int s) { name = n; this.x = x; side = s; }
    }

    // Identidades por POSICIÓN en la escena: izquierda = Garza Sada, centro = Junco,
    // derecha = García Roel. (Ajusta las X libremente.)
    private static readonly Cross[] Crosses =
    {
        new Cross("S1_Izq_GarzaSada", -130.0f, +1), // izquierda: Garza Sada (angosta, 1 carril)
        new Cross("S2_JuncoT",           0.0f, +1), // centro: Junco (T doble sentido)
        new Cross("S3_Der_GarciaRoel", 130.0f, -1), // derecha: García Roel (cruz completa)
    };

    private const string JuncoName = "S2_JuncoT";
    private const string S1Name = "S1_Izq_GarzaSada";   // calle IZQ de la escena
    private const string S3Name = "S3_Der_GarciaRoel";  // calle DER de la escena
    private const float RightLaneZ = 4.5f;  // carril derecho que gira en la izquierda (S1)

    // Lado por el que los carros se aproximan a un cruce (aguas arriba)
    private static float AnteX(float x) => x - Dir * (HalfInter + 7f);
    private static float PostX(float x) => x + Dir * (HalfInter + 3f);
    private static float StopX(float x) => x - Dir * (HalfInter + 4f);

    [MenuItem("M4Cruce/Construir Corredor (Limpio)")]
    public static void Build() => BuildCorridor(true);

    [MenuItem("M4Cruce/Construir Corredor (SIN coordinar)")]
    public static void BuildSinCoordinar() => BuildCorridor(false);

    private static void BuildCorridor(bool coordinated)
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

        Cube("Pasto", new Vector3(roadCenterX, -0.08f, 0f), new Vector3(roadLen + 180f, 0.1f, 350f), matPasto, R);
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
            bool giraEnS1 = Mathf.Approximately(z, RightLaneZ); // carril derecho gira en S1
            Transform laneRoot = new GameObject("Elizondo_Carril_" + lane).transform;
            laneRoot.SetParent(wpRoot, false);

            var path = new List<Transform> { Wp($"E{lane}_Entrada", new Vector3(entryX, 0f, z), laneRoot) };
            bool terminado = false;
            foreach (var c in ordered)
            {
                if (c.name == S1Name && giraEnS1)
                {
                    // Carril derecho: se detiene en S1 y gira (arco suave) para subir por S1 (+Z)
                    path.Add(Wp("E_S1_Ante", new Vector3(AnteX(c.x), 0f, z), laneRoot));
                    path.Add(Wp("E_S1_In",   new Vector3(c.x + 5f,   0f, z),    laneRoot));
                    path.Add(Wp("E_S1_Arc1", new Vector3(c.x + 3f,   0f, 5.5f), laneRoot));
                    path.Add(Wp("E_S1_Arc2", new Vector3(c.x + 2.5f, 0f, 8f),   laneRoot));
                    path.Add(Wp("E_S1_Up1",  new Vector3(c.x + 2.5f, 0f, 30f),  laneRoot));
                    path.Add(Wp("E_S1_Up2",  new Vector3(c.x + 2.5f, 0f, 76f),  laneRoot));
                    terminado = true;
                    break;
                }
                path.Add(Wp($"E{lane}_Ante_{c.name}", new Vector3(AnteX(c.x), 0f, z), laneRoot));
                path.Add(Wp($"E{lane}_Post_{c.name}", new Vector3(PostX(c.x), 0f, z), laneRoot));
            }
            if (!terminado) path.Add(Wp($"E{lane}_Salida", new Vector3(exitX, 0f, z), laneRoot));

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
            bool isS1 = c.name == S1Name;
            bool isS3 = c.name == S3Name;          // García Roel (derecha)
            float cw = isS1 ? LeftWidth : RoadWidth; // ancho de la calle transversal

            if (isT)
            {
                // Junco: T (un solo brazo, al norte)
                float armCtr = c.side * (HalfInter + ArmLen * 0.5f);
                Cube(c.name + "_Road_Largo", new Vector3(c.x, -0.05f, armCtr), new Vector3(14f, 0.1f, ArmLen), matAsfalto, G);
                Cube(c.name + "_Inter", new Vector3(c.x, -0.04f, 0f), new Vector3(14f, 0.1f, 14f), matInter, G);
                Cube(c.name + "_LineaC_L", new Vector3(c.x, -0.035f, armCtr), new Vector3(0.25f, 0.01f, ArmLen - 6f), matLinea, G);
                Cube(c.name + "_BanqL_W", new Vector3(c.x - 9f, 0f, armCtr), new Vector3(4f, 0.2f, ArmLen), matBanqueta, G);
                Cube(c.name + "_BanqL_E", new Vector3(c.x + 9f, 0f, armCtr), new Vector3(4f, 0.2f, ArmLen), matBanqueta, G);
            }
            else
            {
                // Cruz completa (+): dos brazos largos a ambos lados (no se cortan).
                float bq = cw * 0.5f + 2f;
                float nCtr =  (HalfInter + ArmLen * 0.5f);
                float sCtr = -(HalfInter + ArmLen * 0.5f);
                Cube(c.name + "_Road_N", new Vector3(c.x, -0.05f, nCtr), new Vector3(cw, 0.1f, ArmLen), matAsfalto, G);
                Cube(c.name + "_Road_S", new Vector3(c.x, -0.05f, sCtr), new Vector3(cw, 0.1f, ArmLen), matAsfalto, G);
                Cube(c.name + "_Inter",  new Vector3(c.x, -0.04f, 0f),   new Vector3(cw, 0.1f, 14f), matInter, G);
                Cube(c.name + "_LineaC_N", new Vector3(c.x, -0.035f, nCtr), new Vector3(0.25f, 0.01f, ArmLen - 6f), matLinea, G);
                Cube(c.name + "_LineaC_S", new Vector3(c.x, -0.035f, sCtr), new Vector3(0.25f, 0.01f, ArmLen - 6f), matLinea, G);
                Cube(c.name + "_BanqN_W", new Vector3(c.x - bq, 0f, nCtr), new Vector3(4f, 0.2f, ArmLen), matBanqueta, G);
                Cube(c.name + "_BanqN_E", new Vector3(c.x + bq, 0f, nCtr), new Vector3(4f, 0.2f, ArmLen), matBanqueta, G);
                Cube(c.name + "_BanqS_W", new Vector3(c.x - bq, 0f, sCtr), new Vector3(4f, 0.2f, ArmLen), matBanqueta, G);
                Cube(c.name + "_BanqS_E", new Vector3(c.x + bq, 0f, sCtr), new Vector3(4f, 0.2f, ArmLen), matBanqueta, G);
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

            // Semáforo transversal (contrafase). En S1 el tráfico sube (+Z): alto del lado sur.
            float crossOffset = Mathf.Repeat(offset + GreenDur + YellowDur + AllRed, cycle);
            int crossSide = isT ? c.side : -1;             // cruces completas: tráfico sube (+Z), llega por el sur
            float crossStopZ = crossSide * (HalfInter + 4f);
            TrafficLight tlCross = BuildLight(c.name + "_Cross",
                new Vector3(c.x + 8.5f, 0f, crossStopZ + crossSide * 2f), crossOffset, c.side,
                CrossGreen, CrossYellow, CrossRed, new Vector3(-0.35f, 0f, 0f),
                matSemCaja, matVerde, matAmarillo, matRojo, matFocoOff, G);

            float crossW = cw;
            BuildCrosswalk(c.name + "_Cross", new Vector3(c.x, -0.03f, crossSide * (HalfInter + 2f)), false, matLinea, G);
            Cube(c.name + "_AltoCross", new Vector3(c.x, -0.03f, crossStopZ), new Vector3(crossW, 0.01f, 0.9f), matLinea, G);
            StopLineTrigger crossStop = BuildStopLine(c.name + "_Cross", new Vector3(c.x, 0f, crossStopZ), new Vector3(crossW, 3f, 3f), tlCross, G);

            if (coordinated)
            {
                // Control coordinado-actuado: Elizondo verde por defecto; la transversal
                // toma su turno coordinado (onda verde) solo si hay demanda.
                var ctrl = g.AddComponent<IntersectionController>();
                ctrl.major = tlEliz;
                ctrl.minor = tlCross;
                ctrl.minorStop = crossStop;
                ctrl.cycle = cycle;
                ctrl.armOffset = Mathf.Repeat(offset + GreenDur, cycle);
                ctrl.yellow = YellowDur;
                ctrl.allRed = AllRed;
                ctrl.minorGreen = CrossGreen;
            }
            else
            {
                // Baseline SIN coordinar: ciclo fijo e IGUAL en todos los cruces
                // (offset 0), sin actuación -> no hay onda verde.
                tlEliz.externalControl = false;
                tlEliz.startOffset = 0f;
                tlEliz.greenDuration = 15f; tlEliz.yellowDuration = 3f; tlEliz.redDuration = 12f;
                tlCross.externalControl = false;
                tlCross.startOffset = 18f;   // verde transversal durante el rojo de Elizondo
                tlCross.greenDuration = 12f; tlCross.yellowDuration = 3f; tlCross.redDuration = 15f;
            }

            // --- Tráfico transversal ---
            Transform crossWpRoot = new GameObject("WP_" + c.name).transform;
            crossWpRoot.SetParent(wpRoot, false);

            if (isT)
            {
                // Junco: T de doble sentido. El carril que baja se detiene, cede el
                // paso y da vuelta para incorporarse al carril CENTRAL (sigue derecho).
                const float zMerge = 0f;
                var baja = new List<Transform>
                {
                    Wp("Junco_Baja_0", new Vector3(-2.3f, 0f, 140f), crossWpRoot),
                    Wp("Junco_Baja_1", new Vector3(-2.3f, 0f, 11f), crossWpRoot), // alto (semáforo)
                    Wp("Junco_Baja_2", new Vector3(-2.3f, 0f, 7f),  crossWpRoot),
                };
                // Punto de cesión: espera si el carril central de la intersección está ocupado
                Transform arco = Wp("Junco_Baja_3", new Vector3(-3f, 0f, 3f), crossWpRoot);
                var yg = arco.gameObject.AddComponent<YieldGate>();
                yg.checkOffset = new Vector3(-4f, 0f, -3f);    // zona sobre el carril central
                yg.halfExtents = new Vector3(6f, 1.5f, 1.6f);
                baja.Add(arco);
                baja.Add(Wp("Junco_Baja_4", new Vector3(-6.5f, 0f, 0.5f), crossWpRoot)); // arco
                baja.Add(Wp("Junco_Baja_5", new Vector3(-12f, 0f, zMerge), crossWpRoot)); // incorporado
                foreach (var cc in ordered)
                {
                    if (Dir < 0 ? cc.x >= c.x : cc.x <= c.x) continue; // solo cruces aguas abajo
                    baja.Add(Wp($"Junco_Baja_Ante_{cc.name}", new Vector3(AnteX(cc.x), 0f, zMerge), crossWpRoot));
                    baja.Add(Wp($"Junco_Baja_Post_{cc.name}", new Vector3(PostX(cc.x), 0f, zMerge), crossWpRoot));
                }
                baja.Add(Wp("Junco_Baja_Salida", new Vector3(exitX, 0f, zMerge), crossWpRoot));
                entries.Add(new CarSpawner.SpawnPoint { spawnTransform = baja[0], waypoints = baja.ToArray(), interval = 5f });
                // Junco es T: TODO el tráfico entra desde arriba y da vuelta hacia Elizondo.
                // (Se eliminó el carril que aparecía en medio.)
            }
            else if (isS3)
            {
                // Fernando García Roel: DOBLE sentido (derecha sube +Z, izquierda baja -Z).
                float southEnd = -(HalfInter + ArmLen) + 6f;
                float northEnd =  (HalfInter + ArmLen) - 6f;
                float northStopZ = HalfInter + 4f;

                // Carril derecho: SUBE (+Z), alto en el sur + cede el paso antes de cruzar
                var sube = new List<Transform>
                {
                    Wp("GR_Sube_0", new Vector3(c.x + 2.5f, 0f, southEnd),   crossWpRoot),
                    Wp("GR_Sube_1", new Vector3(c.x + 2.5f, 0f, crossStopZ), crossWpRoot),
                };
                Transform sg = Wp("GR_Sube_gate", new Vector3(c.x + 2.5f, 0f, -7f), crossWpRoot);
                var ygS = sg.gameObject.AddComponent<YieldGate>();
                ygS.checkOffset = new Vector3(0f, 0f, 7f);              // zona en el centro del cruce
                ygS.halfExtents = new Vector3(1.8f, 1.5f, 6.5f);       // solo su carril (no choca con el otro sentido)
                sube.Add(sg);
                sube.Add(Wp("GR_Sube_2", new Vector3(c.x + 2.5f, 0f, 11f),      crossWpRoot));
                sube.Add(Wp("GR_Sube_3", new Vector3(c.x + 2.5f, 0f, northEnd), crossWpRoot));
                entries.Add(new CarSpawner.SpawnPoint { spawnTransform = sube[0], waypoints = sube.ToArray(), interval = 6f });

                // Carril izquierdo: BAJA (-Z), alto en el norte + cede el paso antes de cruzar
                var baja = new List<Transform>
                {
                    Wp("GR_Baja_0", new Vector3(c.x - 2.5f, 0f, northEnd),   crossWpRoot),
                    Wp("GR_Baja_1", new Vector3(c.x - 2.5f, 0f, northStopZ), crossWpRoot),
                };
                Transform bg = Wp("GR_Baja_gate", new Vector3(c.x - 2.5f, 0f, 7f), crossWpRoot);
                var ygB = bg.gameObject.AddComponent<YieldGate>();
                ygB.checkOffset = new Vector3(0f, 0f, -7f);
                ygB.halfExtents = new Vector3(1.8f, 1.5f, 6.5f);
                baja.Add(bg);
                baja.Add(Wp("GR_Baja_2", new Vector3(c.x - 2.5f, 0f, -11f),     crossWpRoot));
                baja.Add(Wp("GR_Baja_3", new Vector3(c.x - 2.5f, 0f, southEnd), crossWpRoot));
                entries.Add(new CarSpawner.SpawnPoint { spawnTransform = baja[0], waypoints = baja.ToArray(), interval = 6f });

                // Alto + semáforo (trigger) del lado norte para el sentido que baja
                Cube(c.name + "_AltoCrossN", new Vector3(c.x, -0.03f, northStopZ), new Vector3(crossW, 0.01f, 0.9f), matLinea, G);
                BuildStopLine(c.name + "_CrossN", new Vector3(c.x, 0f, northStopZ), new Vector3(crossW, 3f, 3f), tlCross, G);
            }
            else
            {
                // S1 (Garza Sada): un solo carril que sube (+Z), corrido a la izquierda
                // para dejar pasar la vuelta de Elizondo.
                float southEnd = -(HalfInter + ArmLen) + 6f;
                float northEnd =  (HalfInter + ArmLen) - 6f;
                float lx = c.x - 2.5f;
                var sube = new List<Transform>
                {
                    Wp($"{c.name}_Sube_0", new Vector3(lx, 0f, southEnd),   crossWpRoot),
                    Wp($"{c.name}_Sube_1", new Vector3(lx, 0f, crossStopZ), crossWpRoot),
                    Wp($"{c.name}_Sube_2", new Vector3(lx, 0f,  11f),       crossWpRoot),
                    Wp($"{c.name}_Sube_3", new Vector3(lx, 0f, northEnd),   crossWpRoot),
                };
                entries.Add(new CarSpawner.SpawnPoint { spawnTransform = sube[0], waypoints = sube.ToArray(), interval = 6f });
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
        spawner.maxCars = 38;
        spawner.carSpeed = CarSpeed;

        // ---- Métricas (HUD + CSV) ----
        GameObject metricsGO = new GameObject("TrafficMetrics");
        metricsGO.transform.SetParent(R, false);
        var metrics = metricsGO.AddComponent<TrafficMetrics>();
        metrics.runLabel = coordinated ? "coordinado" : "sin_coordinar";

        // ---- Cámara ----
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            cam = camGO.AddComponent<Camera>();
        }
        cam.transform.position = new Vector3(15f, 285f, -185f);
        cam.transform.rotation = Quaternion.Euler(57f, 0f, 0f);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();

        string modo = coordinated ? "COORDINADO (onda verde actuada)" : "SIN COORDINAR (ciclo fijo, baseline)";
        EditorUtility.DisplayDialog("M4 Cruce — Corredor construido",
            $"Modo: {modo}\n" +
            $"Av. Luis Elizondo ({(Dir < 0 ? "derecha->izquierda" : "izquierda->derecha")}), 3 carriles, tramo largo.\n" +
            "Junco = T de doble sentido.\n\n" +
            $"Velocidad: {CarSpeed} u/s\n" +
            $"Métricas -> Analisis/metrics_*_{(coordinated ? "coordinado" : "sin_coordinar")}.csv\n\nDale Play.", "OK");
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
                n.StartsWith("PythonPlayback") ||   // quitar el bridge si estaba
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
        {
            float half = (c.name == S1Name ? LeftWidth * 0.5f + 2f : 9f);
            if (Mathf.Abs(x - c.x) <= half + margin) return true;
        }
        return false;
    }

    private static void BuildScenery(Transform parent, Material[] edificios, Material tronco, Material hojas)
    {
        GameObject sceneRoot = new GameObject("Escenografia");
        sceneRoot.transform.SetParent(parent, false);
        Transform S = sceneRoot.transform;

        var rng = new System.Random(12345);

        float[] zRows = { -25f, -52f, -82f, -112f, -142f, 25f, 52f, 82f, 112f, 142f };
        for (float bx = RoadXMin + 4f; bx <= RoadXMax - 4f; bx += 13f)
        {
            foreach (float bz in zRows)
            {
                float jx = bx + (float)(rng.NextDouble() * 4 - 2);
                float jz = bz + (float)(rng.NextDouble() * 4 - 2);
                if (OverlapsRoad(jx, jz, 4f)) continue;
                // Reservar TODA la cuadra del campus del Tec (entre Garza Sada y Junco, lado norte)
                if (jx > -122f && jx < -8f && jz > 12f && jz < 160f) continue;
                // Reservar la cuadra del teatro + parque (entre Junco y García Roel, lado sur)
                if (jx > 8f && jx < 92f && jz < -22f && jz > -72f) continue;
                if (rng.NextDouble() < 0.25) continue;

                float w = 5f + (float)rng.NextDouble() * 4f;
                float d = 5f + (float)rng.NextDouble() * 4f;
                float h = 4f + (float)rng.NextDouble() * 9f;
                Material m = edificios[rng.Next(edificios.Length)];
                Cube($"Edificio_{jx:F0}_{jz:F0}", new Vector3(jx, h * 0.5f, jz), new Vector3(w, h, d), m, S);
            }
        }

        // Campus del Tec entre Junco (x=0) y Garza Sada (x=-130)
        BuildCampus(S);

        // Teatro y parque entre Junco (x=0) y García Roel (x=130), lado sur
        BuildTheater(S);
        BuildPark(S, tronco, hojas);

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

    private static void BuildCampus(Transform parent)
    {
        Material matCampus = GetOrCreateMat("Mat_Campus", new Color32(0x53, 0x68, 0x8f, 0xFF));      // azul-gris institucional
        Material matQuad   = GetOrCreateMat("Mat_CampusVerde", new Color32(0x3f, 0x7a, 0x3a, 0xFF)); // explanada verde

        GameObject g = new GameObject("Campus_Tec");
        g.transform.SetParent(parent, false);

        // Explanada/quad verde del campus (cubre toda la cuadra hacia el fondo)
        Cube("Campus_Quad", new Vector3(-65f, -0.06f, 80f), new Vector3(112f, 0.08f, 130f), matQuad, g.transform);

        // Bloques del campus: varios cubos UNIDOS para que se vean más grandes.
        // (centroX, centroZ, ancho, profundidad, altura)
        var bloques = new (float x, float z, float w, float d, float h)[]
        {
            (-98f, 30f, 32f, 18f, 11f),   // bloque grande (varios unidos)
            (-58f, 30f, 30f, 18f, 13f),   // bloque grande
            (-24f, 30f, 16f, 18f, 9f),
            (-100f, 64f, 16f, 20f, 10f),
            (-58f, 64f, 36f, 20f, 12f),   // bloque grande (3 unidos)
            (-22f, 64f, 16f, 20f, 9f),
            (-84f, 100f, 30f, 22f, 14f),  // grande
            (-42f, 100f, 30f, 22f, 12f),  // grande
            (-62f, 134f, 42f, 18f, 11f),  // fila grande unida
        };
        for (int i = 0; i < bloques.Length; i++)
        {
            var b = bloques[i];
            Cube($"Campus_Bloque_{i}", new Vector3(b.x, b.h * 0.5f, b.z), new Vector3(b.w, b.h, b.d), matCampus, g.transform);
        }

        // Edificio principal (la escuela), grande, al fondo de la cuadra
        Cube("Campus_Principal", new Vector3(-62f, 9.5f, 152f), new Vector3(60f, 19f, 16f), matCampus, g.transform);
    }

    private static void BuildTheater(Transform parent)
    {
        Material mt    = GetOrCreateMat("Mat_Teatro",     new Color32(0x8a, 0x3b, 0x33, 0xFF)); // fachada terracota
        Material mtrim = GetOrCreateMat("Mat_TeatroTrim", new Color32(0xd8, 0xc8, 0xa0, 0xFF)); // detalles claros

        GameObject g = new GameObject("Teatro");
        g.transform.SetParent(parent, false);
        float tx = 72f, tz = -46f;

        Cube("Teatro_Sala",    new Vector3(tx, 8f, tz - 6f),  new Vector3(30f, 16f, 26f), mt, g.transform);     // sala principal
        Cube("Teatro_Techo",   new Vector3(tx, 16.6f, tz - 6f), new Vector3(33f, 1.4f, 28f), mtrim, g.transform); // cornisa
        Cube("Teatro_Fachada", new Vector3(tx, 5f, tz + 10f),  new Vector3(34f, 10f, 6f), mtrim, g.transform);  // pórtico
        for (int k = 0; k < 4; k++)
            Cube($"Teatro_Columna_{k}", new Vector3(tx - 13.5f + k * 9f, 4f, tz + 13f), new Vector3(1.6f, 8f, 1.6f), mtrim, g.transform);
        Cube("Teatro_Escalones", new Vector3(tx, 0.3f, tz + 16f), new Vector3(30f, 0.6f, 4f), mtrim, g.transform);
    }

    private static void BuildPark(Transform parent, Material tronco, Material hojas)
    {
        Material mp    = GetOrCreateMat("Mat_Parque", new Color32(0x4f, 0x8a, 0x42, 0xFF)); // césped
        Material mpath = GetOrCreateMat("Mat_Camino", new Color32(0xc9, 0xbf, 0xa6, 0xFF)); // andador

        GameObject g = new GameObject("Parque");
        g.transform.SetParent(parent, false);
        float px = 30f, pz = -46f;

        Cube("Parque_Cesped",   new Vector3(px, -0.05f, pz), new Vector3(40f, 0.1f, 40f), mp, g.transform);
        Cube("Parque_CaminoH",  new Vector3(px, -0.04f, pz), new Vector3(40f, 0.02f, 3f), mpath, g.transform);
        Cube("Parque_CaminoV",  new Vector3(px, -0.04f, pz), new Vector3(3f, 0.02f, 40f), mpath, g.transform);
        foreach (var off in new (float dx, float dz)[] { (-13f, -13f), (13f, -13f), (-13f, 13f), (13f, 13f), (-15f, 0f), (15f, 0f) })
            BuildTree(new Vector3(px + off.dx, 0f, pz + off.dz), tronco, hojas, g.transform);
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

    private static StopLineTrigger BuildStopLine(string name, Vector3 position, Vector3 size, TrafficLight tl, Transform parent)
    {
        GameObject go = new GameObject("StopLine_" + name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        BoxCollider bc = go.AddComponent<BoxCollider>();
        bc.size = size;
        bc.isTrigger = true;
        var slt = go.AddComponent<StopLineTrigger>();
        slt.trafficLight = tl;
        return slt;
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
