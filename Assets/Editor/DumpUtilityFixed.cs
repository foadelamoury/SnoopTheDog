using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class DumpAnimationWindowUtility {
    [InitializeOnLoadMethod]
    static void Dump() {
        var asmUnityEditor = Assembly.LoadFrom(UnityEditorInternal.InternalEditorUtility.GetEditorAssemblyPath());
        var type = asmUnityEditor.GetType("UnityEditorInternal.AnimationWindowUtility");
        if (type == null) {
            File.WriteAllText("dump_log.txt", "AnimationWindowUtility type not found.");
            return;
        }
        string outText = "";
        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)) {
            if (method.Name.Contains("Create")) {
                string p = "";
                foreach(var param in method.GetParameters()) p += param.ParameterType.Name + ",";
                outText += method.Name + "(" + p + ")\n";
            }
        }
        File.WriteAllText("dump_log.txt", outText);
    }
}
