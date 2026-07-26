using UnityEngine;
using UnityEditor;
using System.IO;

[InitializeOnLoad]
public class SnoopyModifier {
    static SnoopyModifier() {
        if (SessionState.GetBool("SnoopyModifierRun", false)) return;
        SessionState.SetBool("SnoopyModifierRun", true);
        
        EditorApplication.delayCall += DoModify;
    }
    
    static void DoModify() {
        try {
            string output = "Starting Snoopy modification...\n";
            GameObject snoopy = GameObject.Find("Snoopy Controller");
            if (snoopy == null) {
                File.WriteAllText("SnoopyLog.txt", "Snoopy Controller not found in the current scene.");
                return;
            }
            
            output += "Found Snoopy Controller.\n";
            
            // Look for MInput
            var minput = snoopy.GetComponent<MalbersAnimations.MInput>();
            if (minput != null) {
                output += "Found MInput.\n";
                foreach (var input in minput.inputs) {
                    output += $"Input: {input.name} - Button: {input.input} - Active: {input.active}\n";
                }
            } else {
                output += "MInput not found.\n";
            }
            
            // Look for Aim
            var aim = snoopy.GetComponent("Aim");
            if (aim != null) {
                output += "Found Aim.\n";
            } else {
                output += "Aim not found.\n";
            }
            
            // Look for Weapon Manager
            var weaponManager = snoopy.GetComponent("MWeaponManager");
            if (weaponManager != null) {
                output += "Found Weapon Manager.\n";
            } else {
                output += "Weapon Manager not found.\n";
            }
            
            // Look for Jiggle
            var jiggles = snoopy.GetComponentsInChildren<Transform>(true);
            foreach (var t in jiggles) {
                var jiggle = t.GetComponent("SimpleJiggle");
                if (jiggle != null) {
                    output += $"Found SimpleJiggle on {t.name}\n";
                }
            }
            
            File.WriteAllText("SnoopyLog.txt", output);
            Debug.Log("SnoopyLog.txt written successfully.");
        } catch (System.Exception e) {
            File.WriteAllText("SnoopyLog.txt", "Error: " + e.Message + "\n" + e.StackTrace);
        }
    }
}
