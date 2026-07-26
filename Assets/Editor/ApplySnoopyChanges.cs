using UnityEngine;
using UnityEditor;
using System.IO;

[InitializeOnLoad]
public class ApplySnoopyChanges {
    static ApplySnoopyChanges() {
        if (SessionState.GetBool("ApplySnoopyChangesRun", false)) return;
        SessionState.SetBool("ApplySnoopyChangesRun", true);
        EditorApplication.delayCall += DoApply;
    }

    static void DoApply() {
        GameObject snoopy = GameObject.Find("Snoopy Controller");
        if (snoopy == null) {
            Debug.Log("Snoopy Controller not found in scene for automatic changes.");
            return;
        }

        // 1. Disable Aim
        var aim = snoopy.GetComponent("Aim") as MonoBehaviour;
        if (aim != null && aim.enabled) {
            aim.enabled = false;
            Debug.Log("Automatically disabled Aim component.");
            EditorUtility.SetDirty(aim);
        }

        // 2. Disable Weapon Manager (Attack)
        var weaponManager = snoopy.GetComponent("MWeaponManager") as MonoBehaviour;
        if (weaponManager != null && weaponManager.enabled) {
            weaponManager.enabled = false;
            Debug.Log("Automatically disabled Weapon Manager component.");
            EditorUtility.SetDirty(weaponManager);
        }

        var mAttack = snoopy.GetComponent("MAttack") as MonoBehaviour;
        if (mAttack != null && mAttack.enabled) {
            mAttack.enabled = false;
            Debug.Log("Automatically disabled MAttack component.");
            EditorUtility.SetDirty(mAttack);
        }

        // 3. Assign Left Click (Attack) to Bark
        var animal = snoopy.GetComponent<MalbersAnimations.Controller.MAnimal>();
        if (animal != null) {
            foreach (var mode in animal.modes) {
                // If it's the Bark mode or Action mode that has a Bark ability
                if (mode.ID != null && mode.ID.name.ToLower().Contains("bark")) {
                    mode.Input = "Attack";
                    Debug.Log($"Set Mode '{mode.ID.name}' to listen to 'Attack' input (Left Click).");
                    EditorUtility.SetDirty(animal);
                }
                else if (mode.ID != null && mode.ID.name.ToLower().Contains("action")) {
                    // Often Bark is an ability in the Action mode.
                    // We can just set the Action mode to Attack, but maybe we shouldn't overwrite if it's already bound to something else.
                    if (mode.Input != "Attack" && mode.Input != "Action") {
                        // mode.Input = "Attack";
                    }
                }
            }
        }
        
        // Also update MInput if Bark is an explicit input
        var minput = snoopy.GetComponent<MalbersAnimations.MInput>();
        if (minput != null) {
            foreach (var input in minput.inputs) {
                if (input.name.ToLower().Contains("bark")) {
                    input.input = "Attack";
                    input.active = true;
                    Debug.Log("Set MInput Bark to use 'Attack' button.");
                    EditorUtility.SetDirty(minput);
                }
            }
        }

        Debug.Log("Successfully applied Snoop the Dog input changes.");
    }
}
