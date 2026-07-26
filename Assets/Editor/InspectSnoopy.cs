using UnityEngine;
using UnityEditor;
using System.Text;
using MalbersAnimations;

public class InspectSnoopy {
    [MenuItem("Tools/Inspect Snoopy")]
    public static void Inspect() {
        GameObject snoopy = GameObject.Find("Snoopy Controller");
        if (snoopy == null) {
            Debug.Log("Snoopy not found");
            return;
        }
        var minput = snoopy.GetComponent<MalbersAnimations.MInput>();
        if (minput != null) {
            Debug.Log("Found MInput");
            foreach (var input in minput.inputs) {
                Debug.Log($"Input: {input.name} - Button: {input.input} - Active: {input.active}");
            }
        }
        
        var jiggles = snoopy.GetComponentsInChildren<Transform>(true);
        foreach (var t in jiggles) {
            var jiggle = t.GetComponent("SimpleJiggle"); // Assuming SimpleJiggle is the script name
            if (jiggle != null) {
                Debug.Log($"Found SimpleJiggle on {t.name}");
            }
        }
    }
}
