using UnityEditor;
using UnityEngine;

public class CleanSnoopyTranslationCurves
{
    [MenuItem("Tools/Malbers Animations/Fix Snoopy Melted Spaghetti")]
    static void CleanTranslationCurves()
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
                // If it's a position (translation) curve
                if (binding.propertyName.ToLower().Contains("position"))
                {
                    // Keep Root Motion and Hip Bouncing (SnoopyArmature and SnoopyArmature/Pelvis)
                    // Strip position from everything else (Spine, Legs, Neck, etc.)
                    if (binding.path != "" && 
                        binding.path != "SnoopyArmature" && 
                        binding.path != "SnoopyArmature/Pelvis")
                    {
                        AnimationUtility.SetEditorCurve(clip, binding, null);
                        modified = true;
                    }
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
            Debug.Log($"Fixed {modifiedCount} animations by removing bad position curves!");
            EditorUtility.DisplayDialog("Fixed Animations", $"Fixed {modifiedCount} animations! Snoopy's limbs will no longer rip apart.", "Awesome!");
        }
        else
        {
            Debug.Log("No bad position curves found or already fixed.");
        }
    }
}
