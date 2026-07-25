using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class DumpBoneHierarchies
{
    [MenuItem("Tools/Dump Bone Hierarchies")]
    static void Dump()
    {
        var lines = new List<string>();

        // Dump Snoopy bones
        lines.Add("=== SNOOPY BONES ===");
        var snoopyAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Snoopy.fbx");
        if (snoopyAsset != null)
        {
            DumpTransform(snoopyAsset.transform, "", lines);
        }
        else
        {
            lines.Add("Snoopy.fbx not found at Assets/Models/Snoopy.fbx");
        }

        lines.Add("");
        lines.Add("=== WOLF BONES ===");
        // Try both wolf fbx files
        var wolfAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/External Packages/Malbers Animations/Animal Controller/Wolf Lite/Wolf Lite.fbx");
        if (wolfAsset == null)
            wolfAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/External Packages/Malbers Animations/Animal Controller/Wolf Lite/Wolf Lite v2.fbx");
        if (wolfAsset != null)
        {
            DumpTransform(wolfAsset.transform, "", lines);
        }
        else
        {
            lines.Add("Wolf FBX not found");
        }

        // Dump animation curve paths from a wolf animation
        lines.Add("");
        lines.Add("=== WOLF ANIMATION CURVE PATHS (WL_Actions) ===");
        var wolfAnims = AssetDatabase.LoadAllAssetsAtPath("Assets/External Packages/Malbers Animations/Animal Controller/Wolf Lite/Animations/WL_Actions.FBX");
        foreach (var asset in wolfAnims)
        {
            if (asset is AnimationClip clip && !clip.name.StartsWith("__"))
            {
                lines.Add($"Clip: {clip.name}");
                var bindings = AnimationUtility.GetCurveBindings(clip);
                var paths = new HashSet<string>();
                foreach (var b in bindings)
                {
                    paths.Add(b.path);
                }
                foreach (var p in paths)
                {
                    lines.Add($"  Path: {p}");
                }
                lines.Add("");
            }
        }

        // Also dump Snoopy's existing animation curve paths
        lines.Add("");
        lines.Add("=== SNOOPY ANIMATION CURVE PATHS (Snoopy_Bark) ===");
        var snoopyAnims = AssetDatabase.FindAssets("Snoopy_Bark t:AnimationClip");
        foreach (var guid in snoopyAnims)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip != null)
            {
                lines.Add($"Clip: {clip.name} at {path}");
                var bindings = AnimationUtility.GetCurveBindings(clip);
                var curvedPaths = new HashSet<string>();
                foreach (var b in bindings)
                {
                    curvedPaths.Add(b.path);
                }
                foreach (var p in curvedPaths)
                {
                    lines.Add($"  Path: {p}");
                }
            }
        }

        var outputPath = Path.Combine(Application.dataPath, "..", "bone_hierarchies.txt");
        File.WriteAllLines(outputPath, lines);
        Debug.Log($"Bone hierarchies dumped to {outputPath}");
    }

    static void DumpTransform(Transform t, string indent, List<string> lines)
    {
        lines.Add($"{indent}{t.name}");
        foreach (Transform child in t)
        {
            DumpTransform(child, indent + "  ", lines);
        }
    }
}
