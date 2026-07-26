using UnityEditor;
using UnityEngine;
using System.IO;

public class CleanSnoopyScaleCurves
{
    [MenuItem("Tools/Malbers Animations/Fix Snoopy Squashed Animations")]
    static void CleanScaleCurves()
    {
        string folder = "Assets/Animations/Snoopy_Retargeted";
        string[] guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { folder });
        
        int modifiedCount = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            
            if (clip == null) continue;

            bool modified = false;

            // Get all curve bindings
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
            
            foreach (var binding in bindings)
            {
                // If it's a scale curve, remove it
                if (binding.propertyName.ToLower().Contains("scale"))
                {
                    AnimationUtility.SetEditorCurve(clip, binding, null);
                    modified = true;
                }
            }

            if (modified)
            {
                EditorUtility.SetDirty(clip);
                modifiedCount++;
            }
        }

        if (modifiedCount > 0)
        {
            AssetDatabase.SaveAssets();
            Debug.Log($"Fixed {modifiedCount} animations by removing scale curves!");
            EditorUtility.DisplayDialog("Fixed Animations", $"Fixed {modifiedCount} animations! Snoopy will no longer squash.", "Awesome!");
        }
        else
        {
            Debug.Log("No scale curves found or already fixed.");
        }
    }
}
