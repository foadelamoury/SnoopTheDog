using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class SnoopyAnimatorSetup
{
    [MenuItem("Tools/Malbers Animations/Setup Snoopy Animator Controller")]
    static void SetupSnoopyAnimator()
    {
        string originalControllerPath = "Assets/External Packages/Malbers Animations/Animal Controller/Wolf Lite/Animations/Wolf Lite Animal v2.controller";
        string newControllerFolder = "Assets/Animations/Snoopy_Retargeted";
        string newControllerPath = newControllerFolder + "/Snoopy Animal v2.controller";

        // Check if original exists
        if (!File.Exists(originalControllerPath))
        {
            Debug.LogError($"Could not find original controller at: {originalControllerPath}");
            return;
        }

        // Duplicate the controller
        if (!AssetDatabase.IsValidFolder(newControllerFolder))
        {
            AssetDatabase.CreateFolder("Assets/Animations", "Snoopy_Retargeted");
        }

        if (AssetDatabase.CopyAsset(originalControllerPath, newControllerPath))
        {
            Debug.Log($"Successfully duplicated controller to: {newControllerPath}");
        }
        else
        {
            Debug.LogError("Failed to duplicate the Animator Controller.");
            return;
        }

        // Load the new controller and the retargeted clips
        AnimatorController newController = AssetDatabase.LoadAssetAtPath<AnimatorController>(newControllerPath);
        string[] snoopyClipsGuids = AssetDatabase.FindAssets("t:AnimationClip Snoopy_", new[] { newControllerFolder });
        
        Dictionary<string, AnimationClip> snoopyClips = new Dictionary<string, AnimationClip>();
        foreach (string guid in snoopyClipsGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip != null && !snoopyClips.ContainsKey(clip.name))
            {
                snoopyClips.Add(clip.name, clip);
            }
        }

        // Replace clips in the controller
        int replacedCount = 0;
        foreach (var layer in newController.layers)
        {
            replacedCount += ReplaceClipsInStateMachine(layer.stateMachine, snoopyClips);
        }

        EditorUtility.SetDirty(newController);
        AssetDatabase.SaveAssets();
        
        Debug.Log($"=== Animator Setup Complete! ===");
        Debug.Log($"Replaced {replacedCount} animation clips with Snoopy versions.");
        Selection.activeObject = newController;
    }

    static int ReplaceClipsInStateMachine(AnimatorStateMachine stateMachine, Dictionary<string, AnimationClip> snoopyClips)
    {
        int count = 0;

        // Process States
        foreach (var state in stateMachine.states)
        {
            count += ReplaceClipInMotion(state.state.motion as Motion, snoopyClips, (newMotion) => { state.state.motion = newMotion; });
        }

        // Process Sub-State Machines
        foreach (var subMachine in stateMachine.stateMachines)
        {
            count += ReplaceClipsInStateMachine(subMachine.stateMachine, snoopyClips);
        }

        return count;
    }

    static int ReplaceClipInMotion(Motion motion, Dictionary<string, AnimationClip> snoopyClips, System.Action<Motion> onReplaced)
    {
        if (motion == null) return 0;

        if (motion is AnimationClip clip)
        {
            // The original clips might be named "WL_Walk", "WL_Run", etc.
            // Our retargeted clips are named "Snoopy_WL_Walk", "Snoopy_WL_Run", etc.
            string expectedSnoopyName = "Snoopy_" + clip.name;
            
            if (snoopyClips.TryGetValue(expectedSnoopyName, out AnimationClip snoopyClip))
            {
                onReplaced(snoopyClip);
                return 1;
            }
        }
        else if (motion is BlendTree blendTree)
        {
            int count = 0;
            var children = blendTree.children;
            for (int i = 0; i < children.Length; i++)
            {
                int index = i; // capture for closure
                count += ReplaceClipInMotion(children[i].motion, snoopyClips, (newMotion) => 
                { 
                    children[index].motion = newMotion; 
                });
            }
            blendTree.children = children; // Apply changes back
            return count;
        }

        return 0;
    }
}
