using UnityEngine;
using UnityEditor;
using System.IO;

[InitializeOnLoad]
public class DumpModes {
    static DumpModes() {
        if (SessionState.GetBool("DumpModesRun", false)) return;
        SessionState.SetBool("DumpModesRun", true);
        EditorApplication.delayCall += DoDump;
    }
    
    static void DoDump() {
        try {
            GameObject snoopy = GameObject.Find("Snoopy Controller");
            if (snoopy == null) {
                File.WriteAllText("ModesLog.txt", "Snoopy Controller not found.");
                return;
            }
            
            string log = "Modes for Snoopy:\n";
            var animal = snoopy.GetComponent<MalbersAnimations.Controller.MAnimal>();
            if (animal != null) {
                foreach (var mode in animal.modes) {
                    log += $"Mode: {mode.ID.name} (Input: {mode.Input})\n";
                    if (mode.Abilities != null) {
                        foreach (var ab in mode.Abilities) {
                            log += $"  Ability: {ab.Name}\n";
                        }
                    }
                }
            } else {
                log += "MAnimal not found.\n";
            }
            
            log += "\nMInputs:\n";
            var minput = snoopy.GetComponent<MalbersAnimations.MInput>();
            if (minput != null) {
                foreach (var input in minput.inputs) {
                    log += $"Input: {input.name} - Button: {input.input} - Active: {input.active}\n";
                }
            }
            
            File.WriteAllText("ModesLog.txt", log);
            Debug.Log("ModesLog.txt written.");
        } catch (System.Exception e) {
            File.WriteAllText("ModesLog.txt", "Error: " + e.Message);
        }
    }
}
