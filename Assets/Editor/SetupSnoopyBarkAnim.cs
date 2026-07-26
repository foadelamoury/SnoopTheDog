using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using MalbersAnimations.Controller;

[InitializeOnLoad]
public class SetupSnoopyBarkAnim {
    static SetupSnoopyBarkAnim() {
        if (SessionState.GetBool("SetupSnoopyBarkAnimRun", false)) return;
        SessionState.SetBool("SetupSnoopyBarkAnimRun", true);
        EditorApplication.delayCall += SetupBark;
    }

    [MenuItem("Tools/Setup Snoopy Bark Animation")]
    public static void SetupBark() {
        GameObject snoopy = GameObject.Find("Snoopy Controller");
        if (snoopy == null) {
            Debug.LogError("Snoopy Controller not found in scene.");
            return;
        }

        // 1. Setup MAnimal Mode
        MAnimal animal = snoopy.GetComponent<MAnimal>();
        int barkAbilityIndex = 2; // Default typical bark ID
        if (animal != null) {
            Mode actionMode = null;
            foreach (var m in animal.modes) {
                if (m.ID != null && m.ID.name.Contains("Action")) {
                    actionMode = m;
                    break;
                }
            }

            if (actionMode != null) {
                actionMode.Input = "Attack"; // Link left mouse to Action mode
                foreach (var ab in actionMode.Abilities) {
                    if (ab.Name != null && ab.Name.ToLower().Contains("bark")) {
                        barkAbilityIndex = ab.Index;
                        ab.active.Value = true;
                        break;
                    }
                }
                Debug.Log($"Action mode configured for Bark. Ability Index: {barkAbilityIndex}");
                EditorUtility.SetDirty(animal);
            } else {
                Debug.LogWarning("Action mode not found on MAnimal.");
            }
        }

        // 2. Setup Animator
        Animator anim = snoopy.GetComponent<Animator>();
        if (anim != null && anim.runtimeAnimatorController != null) {
            var controller = anim.runtimeAnimatorController as AnimatorController;
            if (controller != null) {
                // Find Bark Animation
                var barkClipPath = "Assets/Animations/Snoopy_Retargeted/Snoopy_WL_Bark1.anim";
                AnimationClip barkClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(barkClipPath);
                
                if (barkClip == null) {
                    Debug.LogError("Bark animation clip not found at " + barkClipPath);
                    return;
                }

                // Add state to base layer
                AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;
                
                // Check if Bark state already exists
                AnimatorState barkState = null;
                foreach (var childState in rootStateMachine.states) {
                    if (childState.state.name == "Bark") {
                        barkState = childState.state;
                        break;
                    }
                }

                if (barkState == null) {
                    barkState = rootStateMachine.AddState("Bark");
                    barkState.motion = barkClip;
                    Debug.Log("Created Bark Animator State.");
                } else {
                    barkState.motion = barkClip;
                    Debug.Log("Updated existing Bark Animator State.");
                }

                // Make transition from AnyState
                bool hasTransition = false;
                foreach (var t in rootStateMachine.anyStateTransitions) {
                    if (t.destinationState == barkState) {
                        hasTransition = true;
                        break;
                    }
                }

                if (!hasTransition) {
                    var transition = rootStateMachine.AddAnyStateTransition(barkState);
                    transition.AddCondition(AnimatorConditionMode.If, 0, "ModeOn");
                    transition.AddCondition(AnimatorConditionMode.Equals, 4, "Mode");
                    transition.AddCondition(AnimatorConditionMode.Equals, barkAbilityIndex, "ModeStatus");
                    transition.duration = 0.1f;
                    transition.canTransitionToSelf = false;
                    
                    // Exit transition
                    var exitTransition = barkState.AddExitTransition();
                    exitTransition.hasExitTime = true;
                    exitTransition.exitTime = 0.8f;
                    
                    Debug.Log("Created Bark Transitions.");
                }
                
                EditorUtility.SetDirty(controller);
                AssetDatabase.SaveAssets();
            }
        }
        
        Debug.Log("Snoopy Bark Animation Setup Complete!");
    }
}
