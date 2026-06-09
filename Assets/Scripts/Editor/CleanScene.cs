using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Limpia la escena de duplicados generados por correr Construir Todo / Extend
/// múltiples veces. Borra todos los Cruce_T, CarSpawner y carros instanciados
/// (objetos con sufijo "(Clone)" o con tag "Car" que no sean prefab assets).
///
/// Después de correr esto, ejecuta de nuevo:
///   1. M4Cruce > Construir Todo
///   2. M4Cruce > Extend To Corridor
/// para tener una escena limpia desde cero.
/// </summary>
public static class CleanScene
{
    [MenuItem("M4Cruce/Limpiar Escena")]
    public static void Clean()
    {
        bool ok = EditorUtility.DisplayDialog(
            "M4 Cruce — Limpiar escena",
            "Esto borrará TODOS los GameObjects llamados Cruce_T, CarSpawner y los carros instanciados (terminados en \"(Clone)\") de la escena activa.\n\n" +
            "Los prefabs en Assets/Prefabs y los scripts no se tocan.\n\n" +
            "¿Continuar?",
            "Sí, limpiar",
            "Cancelar");

        if (!ok) return;

        Scene scene = SceneManager.GetActiveScene();
        GameObject[] roots = scene.GetRootGameObjects();

        int cruceCount = 0;
        int spawnerCount = 0;
        int carCount = 0;

        List<GameObject> toDestroy = new List<GameObject>();

        foreach (GameObject go in roots)
        {
            if (go == null) continue;
            string n = go.name;

            if (n == "Cruce_T" || n.StartsWith("Cruce_T "))
            {
                toDestroy.Add(go);
                cruceCount++;
                continue;
            }
            if (n == "CarSpawner" || n.StartsWith("CarSpawner "))
            {
                toDestroy.Add(go);
                spawnerCount++;
                continue;
            }
            if (n.Contains("(Clone)"))
            {
                toDestroy.Add(go);
                carCount++;
                continue;
            }
            // También borramos cualquier objeto raíz con tag Car que se haya quedado suelto
            if (go.CompareTag("Car"))
            {
                toDestroy.Add(go);
                carCount++;
                continue;
            }
        }

        foreach (GameObject go in toDestroy)
        {
            Object.DestroyImmediate(go);
        }

        EditorSceneManager.MarkSceneDirty(scene);

        EditorUtility.DisplayDialog(
            "M4 Cruce — Limpieza completa",
            $"Eliminados:\n" +
            $"  • {cruceCount} × Cruce_T\n" +
            $"  • {spawnerCount} × CarSpawner\n" +
            $"  • {carCount} × carros instanciados\n\n" +
            "Ahora ejecuta:\n" +
            "  1. M4Cruce > Construir Todo\n" +
            "  2. M4Cruce > Extend To Corridor",
            "OK");
    }
}
