using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>
/// Exports Unity .anim clips as JSON files that can be imported into Blender
/// using the companion blender_import_anim.py script.
/// </summary>
public class AnimToJsonExporter : EditorWindow
{
    private GameObject sourceObject;
    private List<AnimationClip> clips = new List<AnimationClip>();
    private string exportPath = "Assets/Animation Exporting/JSON";
    private Vector2 scrollPos;
    private int sampleRate = 30;

    [MenuItem("Tools/Export Anims to JSON (for Blender)")]
    public static void ShowWindow()
    {
        var window = GetWindow<AnimToJsonExporter>("Anim → JSON");
        window.minSize = new Vector2(400, 300);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Export .anim → JSON (for Blender)", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "Exports animation clips as JSON files.\n" +
            "Use the companion 'blender_import_anim.py' script in Blender\n" +
            "to apply them to your model's armature.",
            MessageType.Info);

        EditorGUILayout.Space();

        sourceObject = (GameObject)EditorGUILayout.ObjectField(
            "Source Model (Scene)", sourceObject, typeof(GameObject), true);

        sampleRate = EditorGUILayout.IntSlider("Sample Rate (FPS)", sampleRate, 15, 60);

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        exportPath = EditorGUILayout.TextField("Export Path", exportPath);
        if (GUILayout.Button("...", GUILayout.Width(30)))
        {
            string path = EditorUtility.OpenFolderPanel("Select Export Folder", "Assets", "");
            if (!string.IsNullOrEmpty(path) && path.StartsWith(Application.dataPath))
                exportPath = "Assets" + path.Substring(Application.dataPath.Length);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        if (GUILayout.Button("Add Selected Clips"))
        {
            foreach (var obj in Selection.objects)
                if (obj is AnimationClip clip && !clips.Contains(clip))
                    clips.Add(clip);
        }

        Rect dropArea = GUILayoutUtility.GetRect(0, 40, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, "Drop .anim clips here", EditorStyles.helpBox);
        HandleDragAndDrop(dropArea);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.MaxHeight(150));
        for (int i = clips.Count - 1; i >= 0; i--)
        {
            EditorGUILayout.BeginHorizontal();
            clips[i] = (AnimationClip)EditorGUILayout.ObjectField(clips[i], typeof(AnimationClip), false);
            if (GUILayout.Button("X", GUILayout.Width(25))) clips.RemoveAt(i);
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear All")) clips.Clear();

        GUI.enabled = sourceObject != null && clips.Count > 0;
        GUI.backgroundColor = new Color(0.2f, 0.8f, 1f);
        if (GUILayout.Button("Export All to JSON", GUILayout.Height(30)))
            ExportAll();
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();
    }

    private void HandleDragAndDrop(Rect dropArea)
    {
        Event evt = Event.current;
        if (!dropArea.Contains(evt.mousePosition)) return;
        if (evt.type == EventType.DragUpdated) { DragAndDrop.visualMode = DragAndDropVisualMode.Copy; evt.Use(); }
        else if (evt.type == EventType.DragPerform)
        {
            DragAndDrop.AcceptDrag();
            foreach (var obj in DragAndDrop.objectReferences)
                if (obj is AnimationClip clip && !clips.Contains(clip)) clips.Add(clip);
            evt.Use();
        }
    }

    private void ExportAll()
    {
        // Ensure folder
        string fullPath = Path.Combine(Application.dataPath, exportPath.Replace("Assets/", "").Replace("Assets\\", ""));
        if (!Directory.Exists(fullPath)) Directory.CreateDirectory(fullPath);

        Transform root = sourceObject.transform;
        Transform[] bones = root.GetComponentsInChildren<Transform>();

        // Save original state
        var originals = new Dictionary<Transform, (Vector3 pos, Quaternion rot, Vector3 scale)>();
        foreach (var b in bones) originals[b] = (b.localPosition, b.localRotation, b.localScale);

        int exported = 0;
        foreach (var clip in clips)
        {
            if (clip == null) continue;

            EditorUtility.DisplayProgressBar("Exporting", $"Exporting {clip.name}...", (float)exported / clips.Count);

            string json = ExportClipToJson(clip, root, bones);
            string filePath = Path.Combine(fullPath, clip.name + ".json");
            File.WriteAllText(filePath, json);
            exported++;

            Debug.Log($"[JSON Export] Saved: {filePath}");
        }

        // Restore
        foreach (var kvp in originals)
        {
            if (kvp.Key != null)
            {
                kvp.Key.localPosition = kvp.Value.pos;
                kvp.Key.localRotation = kvp.Value.rot;
                kvp.Key.localScale = kvp.Value.scale;
            }
        }

        EditorUtility.ClearProgressBar();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Export Complete!",
            $"Exported {exported} clips as JSON.\n" +
            $"Location: {fullPath}\n\n" +
            "Use 'blender_import_anim.py' in Blender to import them.",
            "OK");
    }

    private string ExportClipToJson(AnimationClip clip, Transform root, Transform[] bones)
    {
        float length = clip.length;
        int frameCount = Mathf.CeilToInt(length * sampleRate) + 1;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine($"  \"name\": \"{clip.name}\",");
        sb.AppendLine($"  \"fps\": {sampleRate},");
        sb.AppendLine($"  \"length\": {length:F6},");
        sb.AppendLine($"  \"frameCount\": {frameCount},");
        sb.AppendLine($"  \"loop\": {(clip.isLooping ? "true" : "false")},");
        sb.AppendLine("  \"bones\": [");

        // Collect bone names (skip root transform itself)
        List<Transform> boneList = new List<Transform>();
        foreach (var b in bones)
        {
            if (b == root) continue;
            boneList.Add(b);
        }

        for (int boneIdx = 0; boneIdx < boneList.Count; boneIdx++)
        {
            Transform bone = boneList[boneIdx];
            string boneName = bone.name;

            sb.AppendLine("    {");
            sb.AppendLine($"      \"name\": \"{boneName}\",");
            sb.AppendLine("      \"frames\": [");

            for (int frame = 0; frame < frameCount; frame++)
            {
                float time = Mathf.Min((float)frame / sampleRate, length);
                clip.SampleAnimation(root.gameObject, time);

                Vector3 pos = bone.localPosition;
                Quaternion rot = bone.localRotation;
                Vector3 scale = bone.localScale;

                // Sanitize
                if (float.IsNaN(pos.x)) pos.x = 0;
                if (float.IsNaN(pos.y)) pos.y = 0;
                if (float.IsNaN(pos.z)) pos.z = 0;
                if (float.IsNaN(rot.x) || float.IsNaN(rot.y) || float.IsNaN(rot.z) || float.IsNaN(rot.w))
                    rot = Quaternion.identity;
                if (float.IsNaN(scale.x)) scale.x = 1;
                if (float.IsNaN(scale.y)) scale.y = 1;
                if (float.IsNaN(scale.z)) scale.z = 1;

                sb.Append($"        {{\"t\": {time:F6}, ");
                sb.Append($"\"p\": [{pos.x:F6}, {pos.y:F6}, {pos.z:F6}], ");
                sb.Append($"\"r\": [{rot.x:F6}, {rot.y:F6}, {rot.z:F6}, {rot.w:F6}], ");
                sb.Append($"\"s\": [{scale.x:F6}, {scale.y:F6}, {scale.z:F6}]}}");

                if (frame < frameCount - 1) sb.AppendLine(",");
                else sb.AppendLine();
            }

            sb.AppendLine("      ]");
            sb.Append("    }");
            if (boneIdx < boneList.Count - 1) sb.AppendLine(",");
            else sb.AppendLine();
        }

        sb.AppendLine("  ]");
        sb.AppendLine("}");

        return sb.ToString();
    }
}
