using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Rotates animation clips by a specified offset on the root bone.
/// Bakes the rotation into the animation data so it works everywhere (Unity, Blender, etc.)
/// </summary>
public class AnimationRotationFixer : EditorWindow
{
    private GameObject sourceObject;
    private List<AnimationClip> clips = new List<AnimationClip>();
    private string exportPath = "Assets/Animation Exporting/Rotated";
    private Vector2 scrollPos;
    private int sampleRate = 30;
    private float rotationOffsetY = -90f;
    private string rootBoneName = "";

    [MenuItem("Tools/Animation Rotation Fixer")]
    public static void ShowWindow()
    {
        var window = GetWindow<AnimationRotationFixer>("Rotation Fixer");
        window.minSize = new Vector2(420, 350);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Animation Rotation Fixer", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "Bakes a rotation offset into animation clips by re-sampling them\n" +
            "with the model rotated. The resulting clips will have the correct\n" +
            "orientation when exported to FBX or JSON for Blender.",
            MessageType.Info);

        EditorGUILayout.Space();

        sourceObject = (GameObject)EditorGUILayout.ObjectField(
            "Source Model (Scene)", sourceObject, typeof(GameObject), true);

        if (sourceObject != null)
        {
            // Auto-detect root bone
            Animator animator = sourceObject.GetComponent<Animator>();
            if (animator == null)
                animator = sourceObject.GetComponentInChildren<Animator>();
            
            if (animator != null && string.IsNullOrEmpty(rootBoneName))
            {
                // Try to find the first child bone
                Transform firstChild = sourceObject.transform.childCount > 0 ? 
                    sourceObject.transform.GetChild(0) : null;
                if (firstChild != null)
                    rootBoneName = firstChild.name;
            }
        }

        EditorGUILayout.Space();

        rotationOffsetY = EditorGUILayout.FloatField("Y Rotation Offset (degrees)", rotationOffsetY);

        EditorGUILayout.HelpBox(
            "Common values:\n" +
            "  -90° = Rotate left 90°\n" +
            "   90° = Rotate right 90°\n" +
            "  180° = Rotate to face opposite direction\n" +
            "Try different values until the model faces the correct direction.",
            MessageType.None);

        sampleRate = EditorGUILayout.IntSlider("Sample Rate (FPS)", sampleRate, 15, 60);
        rootBoneName = EditorGUILayout.TextField("Root Bone Name (auto-detected)", rootBoneName);

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

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.MaxHeight(120));
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
        GUI.backgroundColor = new Color(1f, 0.7f, 0.2f);
        if (GUILayout.Button("Fix Rotation & Save", GUILayout.Height(30)))
            FixRotationAll();
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

    private void FixRotationAll()
    {
        if (sourceObject == null || clips.Count == 0) return;

        EnsureFolderExists(exportPath);

        Transform root = sourceObject.transform;
        Transform[] allBones = root.GetComponentsInChildren<Transform>();

        // Save original state
        var originals = new Dictionary<Transform, (Vector3 pos, Quaternion rot, Vector3 scale)>();
        foreach (var b in allBones)
            originals[b] = (b.localPosition, b.localRotation, b.localScale);

        // Store the original root rotation
        Quaternion originalRootRotation = root.localRotation;

        int fixedCount = 0;

        foreach (var clip in clips)
        {
            if (clip == null) continue;

            EditorUtility.DisplayProgressBar("Fixing Rotation",
                $"Processing {clip.name}...", (float)fixedCount / clips.Count);

            AnimationClip fixedClip = BakeWithRotation(clip, root, allBones, originalRootRotation);

            if (fixedClip != null)
            {
                string savePath = $"{exportPath}/{clip.name}_rotated.anim";
                AssetDatabase.CreateAsset(fixedClip, savePath);
                fixedCount++;
                Debug.Log($"[Rotation Fix] Saved: {savePath}");
            }

            // Restore all transforms
            foreach (var kvp in originals)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.localPosition = kvp.Value.pos;
                    kvp.Key.localRotation = kvp.Value.rot;
                    kvp.Key.localScale = kvp.Value.scale;
                }
            }
        }

        EditorUtility.ClearProgressBar();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Done!",
            $"Fixed rotation on {fixedCount} clips.\n" +
            $"Rotation offset: {rotationOffsetY}° on Y axis.\n" +
            $"Saved to: {exportPath}\n\n" +
            "These clips can be exported to FBX/JSON for Blender.",
            "OK");
    }

    private AnimationClip BakeWithRotation(AnimationClip sourceClip, Transform root, Transform[] allBones, Quaternion originalRootRotation)
    {
        AnimationClip newClip = new AnimationClip();
        newClip.name = sourceClip.name + "_rotated";
        newClip.frameRate = sampleRate;
        newClip.legacy = sourceClip.legacy;

        float clipLength = sourceClip.length;
        int frameCount = Mathf.CeilToInt(clipLength * sampleRate) + 1;

        Quaternion rotationOffset = Quaternion.Euler(0f, rotationOffsetY, 0f);

        // Build curves per bone
        Dictionary<string, BoneCurves> boneCurves = new Dictionary<string, BoneCurves>();
        foreach (var bone in allBones)
        {
            if (bone == root) continue;
            string path = GetRelativePath(root, bone);
            boneCurves[path] = new BoneCurves();
        }

        // Sample each frame
        for (int frame = 0; frame < frameCount; frame++)
        {
            float time = Mathf.Min((float)frame / sampleRate, clipLength);

            // Reset root rotation before sampling
            root.localRotation = originalRootRotation;

            // Sample the animation
            sourceClip.SampleAnimation(root.gameObject, time);

            // Now apply the rotation offset to the root bone
            // This rotates the entire skeleton
            root.localRotation = rotationOffset * root.localRotation;

            // Record all bone transforms (in local space relative to parent)
            foreach (var bone in allBones)
            {
                if (bone == root) continue;

                string path = GetRelativePath(root, bone);
                if (!boneCurves.ContainsKey(path)) continue;

                var curves = boneCurves[path];

                Vector3 pos = bone.localPosition;
                Quaternion rot = bone.localRotation;
                Vector3 scale = bone.localScale;

                // Sanitize
                pos = Sanitize(pos, 0f);
                scale = Sanitize(scale, 1f);
                if (float.IsNaN(rot.x) || float.IsNaN(rot.y) || float.IsNaN(rot.z) || float.IsNaN(rot.w))
                    rot = Quaternion.identity;

                // For the first bone (direct child of root), we need to apply the rotation offset
                if (bone.parent == root)
                {
                    // The rotation offset needs to be applied to the first bone's transform
                    // since we rotated the root
                    rot = bone.localRotation;

                    // Also rotate position for root-level bones
                    pos = rotationOffset * pos;
                }

                curves.posX.Add(new Keyframe(time, pos.x));
                curves.posY.Add(new Keyframe(time, pos.y));
                curves.posZ.Add(new Keyframe(time, pos.z));
                curves.rotX.Add(new Keyframe(time, rot.x));
                curves.rotY.Add(new Keyframe(time, rot.y));
                curves.rotZ.Add(new Keyframe(time, rot.z));
                curves.rotW.Add(new Keyframe(time, rot.w));
                curves.scaleX.Add(new Keyframe(time, scale.x == 0 ? 1e-5f : scale.x));
                curves.scaleY.Add(new Keyframe(time, scale.y == 0 ? 1e-5f : scale.y));
                curves.scaleZ.Add(new Keyframe(time, scale.z == 0 ? 1e-5f : scale.z));
            }
        }

        // Apply curves
        foreach (var kvp in boneCurves)
        {
            string path = kvp.Key;
            var c = kvp.Value;

            newClip.SetCurve(path, typeof(Transform), "localPosition.x", new AnimationCurve(c.posX.ToArray()));
            newClip.SetCurve(path, typeof(Transform), "localPosition.y", new AnimationCurve(c.posY.ToArray()));
            newClip.SetCurve(path, typeof(Transform), "localPosition.z", new AnimationCurve(c.posZ.ToArray()));
            newClip.SetCurve(path, typeof(Transform), "localRotation.x", new AnimationCurve(c.rotX.ToArray()));
            newClip.SetCurve(path, typeof(Transform), "localRotation.y", new AnimationCurve(c.rotY.ToArray()));
            newClip.SetCurve(path, typeof(Transform), "localRotation.z", new AnimationCurve(c.rotZ.ToArray()));
            newClip.SetCurve(path, typeof(Transform), "localRotation.w", new AnimationCurve(c.rotW.ToArray()));
            newClip.SetCurve(path, typeof(Transform), "localScale.x", new AnimationCurve(c.scaleX.ToArray()));
            newClip.SetCurve(path, typeof(Transform), "localScale.y", new AnimationCurve(c.scaleY.ToArray()));
            newClip.SetCurve(path, typeof(Transform), "localScale.z", new AnimationCurve(c.scaleZ.ToArray()));
        }

        // Copy clip settings
        AnimationClipSettings srcSettings = AnimationUtility.GetAnimationClipSettings(sourceClip);
        AnimationClipSettings newSettings = AnimationUtility.GetAnimationClipSettings(newClip);
        newSettings.loopTime = srcSettings.loopTime;
        newSettings.loopBlend = srcSettings.loopBlend;
        AnimationUtility.SetAnimationClipSettings(newClip, newSettings);

        // Copy events
        AnimationEvent[] events = AnimationUtility.GetAnimationEvents(sourceClip);
        if (events != null && events.Length > 0)
            AnimationUtility.SetAnimationEvents(newClip, events);

        return newClip;
    }

    private Vector3 Sanitize(Vector3 v, float defaultVal)
    {
        return new Vector3(
            float.IsNaN(v.x) || float.IsInfinity(v.x) ? defaultVal : v.x,
            float.IsNaN(v.y) || float.IsInfinity(v.y) ? defaultVal : v.y,
            float.IsNaN(v.z) || float.IsInfinity(v.z) ? defaultVal : v.z);
    }

    private string GetRelativePath(Transform root, Transform target)
    {
        if (target == root) return "";
        List<string> parts = new List<string>();
        Transform current = target;
        while (current != root && current != null)
        {
            parts.Add(current.name);
            current = current.parent;
        }
        parts.Reverse();
        return string.Join("/", parts);
    }

    private void EnsureFolderExists(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath)) return;
        string[] folders = folderPath.Split('/');
        string currentPath = folders[0];
        for (int i = 1; i < folders.Length; i++)
        {
            string newPath = currentPath + "/" + folders[i];
            if (!AssetDatabase.IsValidFolder(newPath))
                AssetDatabase.CreateFolder(currentPath, folders[i]);
            currentPath = newPath;
        }
    }

    private class BoneCurves
    {
        public List<Keyframe> posX = new List<Keyframe>(), posY = new List<Keyframe>(), posZ = new List<Keyframe>();
        public List<Keyframe> rotX = new List<Keyframe>(), rotY = new List<Keyframe>(), rotZ = new List<Keyframe>(), rotW = new List<Keyframe>();
        public List<Keyframe> scaleX = new List<Keyframe>(), scaleY = new List<Keyframe>(), scaleZ = new List<Keyframe>();
    }
}
