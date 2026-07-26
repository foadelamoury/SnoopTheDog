using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class DumpSnoopyBones {
    [InitializeOnLoadMethod]
    static void Dump() {
        var assetPath = "Assets/Models/Snoopy.fbx";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab != null) {
            var paths = new List<string>();
            GetPaths(prefab.transform, "", paths);
            File.WriteAllLines("snoopy_bones.txt", paths);
            Debug.Log("Dumped bones");
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
