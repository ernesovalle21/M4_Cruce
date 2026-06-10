using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Arma la escena del puente Python -> Unity: limpia lo anterior y crea un objeto
/// con PythonPlayback (que reproduce Analisis/playback.json), una cámara y una luz.
/// No depende del corredor de simulación nativo.
///
/// Menú: M4Cruce > Reproducir Simulación Python (Bridge)
/// </summary>
public static class PlaybackBuilder
{
    [MenuItem("M4Cruce/Reproducir Simulación Python (Bridge)")]
    public static void Build()
    {
        Scene scene = SceneManager.GetActiveScene();
        foreach (GameObject go in scene.GetRootGameObjects())
        {
            if (go == null) continue;
            string n = go.name;
            if (n.StartsWith("PythonPlayback") || n.StartsWith("Corredor_Elizondo") ||
                n.StartsWith("CarSpawner") || n.StartsWith("Cruce_T") || n.Contains("(Clone)"))
            {
                if (go.CompareTag("MainCamera")) continue;
                Object.DestroyImmediate(go);
            }
        }

        GameObject root = new GameObject("PythonPlayback_Bridge");
        root.AddComponent<PythonPlayback>();

        // Cámara
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            cam = camGO.AddComponent<Camera>();
        }

        // Luz direccional (si no hay)
        if (Object.FindFirstObjectByType<Light>() == null)
        {
            GameObject lightGO = new GameObject("Directional Light");
            var l = lightGO.AddComponent<Light>();
            l.type = LightType.Directional;
            lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        EditorSceneManager.MarkSceneDirty(scene);

        bool jsonOk = System.IO.File.Exists(
            System.IO.Path.Combine(Application.dataPath, "..", "Analisis", "playback.json"));

        EditorUtility.DisplayDialog("M4 Cruce — Puente Python→Unity",
            "Escena de reproducción lista.\n\n" +
            (jsonOk ? "playback.json encontrado ✓\n" : "FALTA Analisis/playback.json:\ncorre  python3 Analisis/export_playback.py\n") +
            "\nDale Play: Unity reproducirá la simulación calculada en Python.\n\n" +
            "(El corredor nativo se reconstruye con 'Construir Corredor (Limpio)'.)",
            "OK");
    }
}
