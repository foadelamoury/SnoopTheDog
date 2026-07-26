using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class DumpSnoopyDogBones {
    [InitializeOnLoadMethod]
    static void Dump() {
        var assetPath = "Assets/Models/SnoopyDog.fbx";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab != null) {
            var paths = new List<string>();
            GetPaths(prefab.transform, "", paths);
            File.WriteAllLines("snoopydog_bones.txt", paths);
            Debug.Log("Dumped SnoopyDog bones");
        }
    }

    static void GetPaths(Transform t, string currentPath, List<string> paths) {
        string path = string.IsNullOrEmpty(currentPath) ? t.name : currentPath + "/" + t.name;
        paths.Add(path);
        foreach (Transform child in t) {
            GetPaths(child, path, paths);
        }
    }
}
