using UnityEditor;
using UnityEngine;

public static class FixImport
{
    [MenuItem("M4Cruce/Arreglar Importación FBX")]
    public static void FixFbxImport()
    {
        string[] guids = AssetDatabase.FindAssets("t:Model", new[] { "Assets/Models" });
        int count = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) continue;

            importer.importCameras = false;
            importer.importLights = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.bakeAxisConversion = true;
            importer.globalScale = 1f;
            importer.useFileScale = true;

            importer.SaveAndReimport();
            count++;
        }

        EditorUtility.DisplayDialog("M4 Cruce",
            "FBX reimportados con bakeAxisConversion=true.\nLos modelos ahora deben estar orientados correctamente.",
            "OK");
    }
}
