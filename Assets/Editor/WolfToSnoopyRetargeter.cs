using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Retargets all Wolf Lite animations to work with the Snoopy model.
/// Wolf animations use paths like "CG/Pelvis/Spine/..."
/// Snoopy's skeleton uses paths like "SnoopyArmature/Pelvis/Spine/..."
/// Since the bone names are identical, we just need to remap the root path.
/// </summary>
public class WolfToSnoopyRetargeter
{
    // Wolf bones that Snoopy doesn't have or that we want handled by jiggle physics
    static readonly HashSet<string> SkippedBones = new HashSet<string>
    {
        "L Cheek", "R Cheek", // Snoopy has no cheek bones
        "L Ear", "R Ear"     // Ears will be driven by SimpleBoneJiggle script instead
    };

    [MenuItem("Tools/Retarget Wolf Animations to Snoopy")]
    static void RetargetAll()
    {
        string wolfAnimFolder = "Assets/External Packages/Malbers Animations/Animal Controller/Wolf Lite/Animations";
        string outputFolder = "Assets/Animations/Snoopy_Retargeted";

        // Create output folder
        if (!AssetDatabase.IsValidFolder("Assets/Animations"))
            AssetDatabase.CreateFolder("Assets", "Animations");
        if (!AssetDatabase.IsValidFolder(outputFolder))
            AssetDatabase.CreateFolder("Assets/Animations", "Snoopy_Retargeted");

        // Find all FBX files in the wolf animation folder
        string[] fbxGuids = AssetDatabase.FindAssets("t:AnimationClip", new[] { wolfAnimFolder });
        
        // Collect unique FBX paths
        var processedPaths = new HashSet<string>();
        int totalClips = 0;
        int skippedCurves = 0;

        // Also directly scan the folder for FBX files
        string fullPath = Path.Combine(Application.dataPath, "..",  wolfAnimFolder);
        string[] fbxFiles = Directory.GetFiles(fullPath.Replace("/", "\\"), "*.fbx", SearchOption.TopDirectoryOnly);
        string[] FBXFiles = Directory.GetFiles(fullPath.Replace("/", "\\"), "*.FBX", SearchOption.TopDirectoryOnly);
        
        var allFiles = new List<string>();
        allFiles.AddRange(fbxFiles);
        allFiles.AddRange(FBXFiles);

        foreach (string file in allFiles)
        {
            // Convert to asset path
            string fileName = Path.GetFileName(file);
            string assetPath = wolfAnimFolder + "/" + fileName;

            if (processedPaths.Contains(assetPath))
                continue;
            processedPaths.Add(assetPath);

            // Load all assets from this FBX
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (Object asset in assets)
            {
                AnimationClip sourceClip = asset as AnimationClip;
                if (sourceClip == null || sourceClip.name.StartsWith("__"))
                    continue;

                AnimationClip newClip = RetargetClip(sourceClip, out int skipped);
                skippedCurves += skipped;

                if (newClip != null)
                {
                    string clipOutputPath = outputFolder + "/Snoopy_" + sourceClip.name + ".anim";
                    
                    // Check if it already exists
                    var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipOutputPath);
                    if (existing != null)
                    {
                        EditorUtility.CopySerialized(newClip, existing);
                        Debug.Log($"Updated: {clipOutputPath}");
                    }
                    else
                    {
                        AssetDatabase.CreateAsset(newClip, clipOutputPath);
                        Debug.Log($"Created: {clipOutputPath}");
                    }
                    totalClips++;
                }
            }
        }

        // Also process standalone .anim files in the folder
        string[] animFiles = Directory.GetFiles(fullPath.Replace("/", "\\"), "*.anim", SearchOption.TopDirectoryOnly);
        foreach (string file in animFiles)
        {
            string fileName = Path.GetFileName(file);
            string assetPath = wolfAnimFolder + "/" + fileName;

            if (processedPaths.Contains(assetPath))
                continue;
            processedPaths.Add(assetPath);

            AnimationClip sourceClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
            if (sourceClip == null || sourceClip.name.StartsWith("__"))
                continue;

            AnimationClip newClip = RetargetClip(sourceClip, out int skipped);
            skippedCurves += skipped;

            if (newClip != null)
            {
                string clipOutputPath = outputFolder + "/Snoopy_" + sourceClip.name + ".anim";

                var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipOutputPath);
                if (existing != null)
                {
                    EditorUtility.CopySerialized(newClip, existing);
                    Debug.Log($"Updated: {clipOutputPath}");
                }
                else
                {
                    AssetDatabase.CreateAsset(newClip, clipOutputPath);
                    Debug.Log($"Created: {clipOutputPath}");
                }
                totalClips++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"=== Retargeting Complete! ===");
        Debug.Log($"Total clips created: {totalClips}");
        Debug.Log($"Curves skipped (no matching bone on Snoopy): {skippedCurves}");
        Debug.Log($"Output folder: {outputFolder}");
        EditorUtility.DisplayDialog("Retargeting Complete",
            $"Successfully retargeted {totalClips} animation clips!\n\n" +
            $"Output: {outputFolder}\n" +
            $"Curves skipped: {skippedCurves}",
            "OK");
    }

    static AnimationClip RetargetClip(AnimationClip source, out int skippedCount)
    {
        skippedCount = 0;
        AnimationClip newClip = new AnimationClip();
        newClip.name = "Snoopy_" + source.name;

        // Copy clip settings
        AnimationClipSettings sourceSettings = AnimationUtility.GetAnimationClipSettings(source);
        AnimationUtility.SetAnimationClipSettings(newClip, sourceSettings);
        newClip.frameRate = source.frameRate;
        newClip.wrapMode = source.wrapMode;

        // Remap float/transform curves
        EditorCurveBinding[] curveBindings = AnimationUtility.GetCurveBindings(source);
        foreach (var binding in curveBindings)
        {
            string newPath = RemapPath(binding.path);
            if (newPath == null)
            {
                skippedCount++;
                continue;
            }

            AnimationCurve curve = AnimationUtility.GetEditorCurve(source, binding);
            EditorCurveBinding newBinding = new EditorCurveBinding
            {
                path = newPath,
                propertyName = binding.propertyName,
                type = binding.type
            };
            AnimationUtility.SetEditorCurve(newClip, newBinding, curve);
        }

        // Remap object reference curves (e.g., sprite swaps)
        EditorCurveBinding[] objBindings = AnimationUtility.GetObjectReferenceCurveBindings(source);
        foreach (var binding in objBindings)
        {
            string newPath = RemapPath(binding.path);
            if (newPath == null)
            {
                skippedCount++;
                continue;
            }

            ObjectReferenceKeyframe[] keyframes = AnimationUtility.GetObjectReferenceCurve(source, binding);
            EditorCurveBinding newBinding = new EditorCurveBinding
            {
                path = newPath,
                propertyName = binding.propertyName,
                type = binding.type
            };
            AnimationUtility.SetObjectReferenceCurve(newClip, newBinding, keyframes);
        }

        // Copy animation events
        AnimationEvent[] events = AnimationUtility.GetAnimationEvents(source);
        if (events != null && events.Length > 0)
        {
            AnimationUtility.SetAnimationEvents(newClip, events);
        }

        return newClip;
    }

    /// <summary>
    /// Remaps a Wolf animation path to a Snoopy animation path.
    /// Wolf: "CG/Pelvis/Spine/L Thigh/..."
    /// Snoopy: "SnoopyArmature/Pelvis/Spine/L Thigh/..."
    /// </summary>
    static string RemapPath(string wolfPath)
    {
        // Check if any part of the path contains a skipped bone
        foreach (var skip in SkippedBones)
        {
            if (wolfPath.Contains(skip))
                return null;
        }

        // Empty path = root transform, keep as-is
        if (string.IsNullOrEmpty(wolfPath))
            return wolfPath;

        // The wolf root bone container is "CG", Snoopy's is "SnoopyArmature"
        if (wolfPath == "CG")
        {
            return "SnoopyArmature";
        }
        else if (wolfPath.StartsWith("CG/"))
        {
            return "SnoopyArmature" + wolfPath.Substring(2); // Replace "CG" with "SnoopyArmature"
        }

        // If the path doesn't start with CG, it might be a mesh or other path
        // Keep it as-is
        return wolfPath;
    }
}
