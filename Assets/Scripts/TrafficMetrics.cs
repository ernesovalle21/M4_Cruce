using System.Collections.Generic;
using System.Text;
using System.Globalization;
using UnityEngine;

/// <summary>
/// Mide y reporta el desempeño del corredor (para la sección de Resultados del reto):
///   - Throughput (carros que completan su ruta por minuto)
///   - Tiempo de espera promedio (s/carro) y tiempo de viaje promedio
///   - Longitud de cola (actual, promedio y máxima), global y por intersección
/// Muestra un HUD en pantalla (OnGUI, sin dependencias de fuentes) y exporta CSV.
///
/// Teclas en Play:  E = exportar CSV ahora   |   R = reiniciar métricas
/// </summary>
public class TrafficMetrics : MonoBehaviour
{
    public static TrafficMetrics Instance;

    [Tooltip("Etiqueta de la corrida (p.ej. coordinado / sin_coordinar) para los CSV.")]
    public string runLabel = "run";
    [Tooltip("Cada cuántos segundos se muestrea la cola.")]
    public float sampleInterval = 1f;
    [Tooltip("Cada cuántos segundos se exporta el CSV automáticamente.")]
    public float autoExportInterval = 30f;
    private float exportTimer = 0f;

    // Acumuladores globales
    private float elapsed = 0f;
    private int completed = 0;
    private float sumWait = 0f;
    private float sumTravel = 0f;

    // Cola
    private int currentQueue = 0;
    private int maxQueue = 0;
    private float sumQueue = 0f;
    private int queueSamples = 0;
    private float sampleTimer = 0f;

    // Por intersección (nombre del StopLine -> acumulado)
    private readonly Dictionary<string, float> interQueueSum = new Dictionary<string, float>();
    private readonly Dictionary<string, int> interQueueMax = new Dictionary<string, int>();

    // Serie de tiempo para graficar (t, activos, cola, completados)
    private readonly List<string> timeSeries = new List<string>();

    private StopLineTrigger[] stopLines;

    void Awake()
    {
        Instance = this;
        timeSeries.Add("t_s,activos,cola,completados");
    }

    void Start()
    {
        stopLines = Object.FindObjectsByType<StopLineTrigger>(FindObjectsSortMode.None);
    }

    public void ReportCompleted(float wait, float travel)
    {
        completed++;
        sumWait += wait;
        sumTravel += travel;
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        sampleTimer += Time.deltaTime;
        if (sampleTimer >= sampleInterval)
        {
            sampleTimer -= sampleInterval;
            Sample();
        }

        exportTimer += Time.deltaTime;
        if (autoExportInterval > 0f && exportTimer >= autoExportInterval)
        {
            exportTimer -= autoExportInterval;
            ExportCSV();
        }
    }

    private int ActiveCars()
    {
        var movers = Object.FindObjectsByType<WaypointMover>(FindObjectsSortMode.None);
        return movers.Length;
    }

    private void Sample()
    {
        // Cola global = suma de carros en cola en cada línea de alto
        int q = 0;
        if (stopLines != null)
        {
            foreach (var s in stopLines)
            {
                if (s == null) continue;
                int w = s.WaitingCount;
                q += w;
                string key = s.name.Replace("StopLine_", "");
                interQueueSum[key] = (interQueueSum.TryGetValue(key, out var v) ? v : 0f) + w;
                interQueueMax[key] = Mathf.Max(interQueueMax.TryGetValue(key, out var mx) ? mx : 0, w);
            }
        }
        currentQueue = q;
        sumQueue += q;
        queueSamples++;
        if (q > maxQueue) maxQueue = q;

        int active = ActiveCars();
        timeSeries.Add($"{elapsed:F1},{active},{q},{completed}");
    }

    private float Throughput => elapsed > 1f ? completed / (elapsed / 60f) : 0f; // veh/min
    private float AvgWait => completed > 0 ? sumWait / completed : 0f;
    private float AvgTravel => completed > 0 ? sumTravel / completed : 0f;
    private float AvgQueue => queueSamples > 0 ? sumQueue / queueSamples : 0f;

    void OnGUI()
    {
        var style = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, fontSize = 13, padding = new RectOffset(10, 10, 8, 8) };
        string txt =
            $"MÉTRICAS DEL CORREDOR\n" +
            $"Tiempo: {elapsed:F0} s\n" +
            $"Carros activos: {ActiveCars()}\n" +
            $"Cola actual: {currentQueue}   (prom {AvgQueue:F1}, máx {maxQueue})\n" +
            $"Completados: {completed}\n" +
            $"Throughput: {Throughput:F1} veh/min\n" +
            $"Espera prom: {AvgWait:F1} s/carro\n" +
            $"Viaje prom: {AvgTravel:F1} s/carro\n" +
            $"CSV auto -> Analisis/ (cada {autoExportInterval:F0}s y al salir)";
        GUI.Box(new Rect(10, 10, 280, 168), txt, style);
    }

    public void ResetMetrics()
    {
        elapsed = 0f; completed = 0; sumWait = 0f; sumTravel = 0f;
        currentQueue = 0; maxQueue = 0; sumQueue = 0f; queueSamples = 0; sampleTimer = 0f;
        interQueueSum.Clear(); interQueueMax.Clear();
        timeSeries.Clear(); timeSeries.Add("t_s,activos,cola,completados");
    }

    public void ExportCSV()
    {
        var ci = CultureInfo.InvariantCulture;
        string dir = System.IO.Path.Combine(Application.dataPath, "..", "Analisis");
        try { System.IO.Directory.CreateDirectory(dir); } catch { }

        // Resumen
        var sb = new StringBuilder();
        sb.AppendLine("metrica,valor");
        sb.AppendLine($"tiempo_simulado_s,{elapsed.ToString("F1", ci)}");
        sb.AppendLine($"carros_completados,{completed}");
        sb.AppendLine($"throughput_veh_min,{Throughput.ToString("F2", ci)}");
        sb.AppendLine($"espera_promedio_s,{AvgWait.ToString("F2", ci)}");
        sb.AppendLine($"viaje_promedio_s,{AvgTravel.ToString("F2", ci)}");
        sb.AppendLine($"cola_promedio,{AvgQueue.ToString("F2", ci)}");
        sb.AppendLine($"cola_maxima,{maxQueue}");
        sb.AppendLine();
        sb.AppendLine("interseccion,cola_promedio,cola_maxima");
        foreach (var kv in interQueueSum)
        {
            float avg = queueSamples > 0 ? kv.Value / queueSamples : 0f;
            int mx = interQueueMax.TryGetValue(kv.Key, out var m) ? m : 0;
            sb.AppendLine($"{kv.Key},{avg.ToString("F2", ci)},{mx}");
        }
        string resumen = System.IO.Path.Combine(dir, $"metrics_resumen_{runLabel}.csv");
        System.IO.File.WriteAllText(resumen, sb.ToString());

        // Serie de tiempo
        string serie = System.IO.Path.Combine(dir, $"metrics_serie_{runLabel}.csv");
        System.IO.File.WriteAllText(serie, string.Join("\n", timeSeries));

        Debug.Log($"[TrafficMetrics] CSV exportado:\n  {resumen}\n  {serie}");
    }

    void OnApplicationQuit()
    {
        ExportCSV();
    }
}
