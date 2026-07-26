using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Bakes animation clips by sampling every frame from an actual skinned mesh,
/// producing clean clips with explicit position + rotation + scale curves on every bone.
/// This completely eliminates NaN issues when exporting to FBX.
/// </summary>
public class AnimationClipBaker : EditorWindow
{
    private GameObject sourceObject;
    private List<AnimationClip> clips = new List<AnimationClip>();
    private string exportPath = "Assets/Animation Exporting/Baked";
    private Vector2 scrollPos;
    private int sampleRate = 30;
    private bool bakeRootMotion = true;

    [MenuItem("Tools/Animation Clip Baker (FBX Fix)")]
    public static void ShowWindow()
    {
        var window = GetWindow<AnimationClipBaker>("Animation Baker");
        window.minSize = new Vector2(420, 350);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Animation Clip Baker", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "This tool BAKES animations by sampling every frame on the actual skeleton.\n" +
            "It writes explicit position, rotation, and scale curves for ALL bones,\n" +
            "completely eliminating NaN/missing curve issues in FBX export.\n\n" +
            "1. Assign the model GameObject (must be in the scene with an Animator)\n" +
            "2. Add the animation clips you want to bake\n" +
            "3. Click 'Bake & Save'",
            MessageType.Info);

        EditorGUILayout.Space();

        // Source object (must be in scene)
        sourceObject = (GameObject)EditorGUILayout.ObjectField(
            "Source Model (Scene)", sourceObject, typeof(GameObject), true);

        if (sourceObject != null && sourceObject.GetComponent<Animator>() == null)
        {
            EditorGUILayout.HelpBox("Selected object has no Animator component!", MessageType.Error);
        }

        EditorGUILayout.Space();

        sampleRate = EditorGUILayout.IntSlider("Sample Rate (FPS)", sampleRate, 15, 60);
        bakeRootMotion = EditorGUILayout.Toggle("Bake Root Motion", bakeRootMotion);

        EditorGUILayout.Space();

        // Export path
        EditorGUILayout.BeginHorizontal();
        exportPath = EditorGUILayout.TextField("Export Path", exportPath);
        if (GUILayout.Button("...", GUILayout.Width(30)))
        {
            string path = EditorUtility.OpenFolderPanel("Select Export Folder", "Assets", "");
            if (!string.IsNullOrEmpty(path))
            {
                if (path.StartsWith(Application.dataPath))
                    exportPath = "Assets" + path.Substring(Application.dataPath.Length);
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Add clips
        EditorGUILayout.LabelField("Animation Clips to Bake:");

        if (GUILayout.Button("Add Selected Clips from Project"))
        {
            foreach (var obj in Selection.objects)
            {
                if (obj is AnimationClip clip && !clips.Contains(clip))
                    clips.Add(clip);
            }
        }

        // Drag & drop
        Rect dropArea = GUILayoutUtility.GetRect(0, 40, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, "Drop .anim clips here", EditorStyles.helpBox);
        HandleDragAndDrop(dropArea);

        EditorGUILayout.Space();

        // Clip list
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.MaxHeight(200));
        for (int i = clips.Count - 1; i >= 0; i--)
        {
            EditorGUILayout.BeginHorizontal();
            clips[i] = (AnimationClip)EditorGUILayout.ObjectField(clips[i], typeof(AnimationClip), false);
            if (GUILayout.Button("X", GUILayout.Width(25)))
                clips.RemoveAt(i);
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        // Buttons
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear All"))
            clips.Clear();

        GUI.enabled = sourceObject != null && clips.Count > 0;
        GUI.backgroundColor = new Color(0.3f, 0.9f, 0.3f);
        if (GUILayout.Button("Bake & Save Clips", GUILayout.Height(30)))
            BakeAllClips();
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();
    }

    private void HandleDragAndDrop(Rect dropArea)
    {
        Event evt = Event.current;
        if (!dropArea.Contains(evt.mousePosition)) return;

        switch (evt.type)
        {
            case EventType.DragUpdated:
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                evt.Use();
                break;
            case EventType.DragPerform:
                DragAndDrop.AcceptDrag();
                foreach (var obj in DragAndDrop.objectReferences)
                {
                    if (obj is AnimationClip clip && !clips.Contains(clip))
                        clips.Add(clip);
                }
                evt.Use();
                break;
        }
    }

    private void BakeAllClips()
    {
        if (sourceObject == null || clips.Count == 0) return;

        Animator animator = sourceObject.GetComponent<Animator>();
        if (animator == null)
        {
            EditorUtility.DisplayDialog("Error", "Source object needs an Animator component.", "OK");
            return;
        }

        // Ensure export directory
        EnsureFolderExists(exportPath);

        // Gather all bone transforms
        Transform root = sourceObject.transform;
        Transform[] allBones = root.GetComponentsInChildren<Transform>();

        int bakedCount = 0;

        foreach (var sourceClip in clips)
        {
            if (sourceClip == null) continue;

            EditorUtility.DisplayProgressBar("Baking Animations",
                $"Baking {sourceClip.name}...", (float)bakedCount / clips.Count);

            AnimationClip bakedClip = BakeClip(sourceClip, root, allBones);

            if (bakedClip != null)
            {
                string savePath = $"{exportPath}/{sourceClip.name}_baked.anim";
                AssetDatabase.CreateAsset(bakedClip, savePath);
                bakedCount++;
                Debug.Log($"[Baker] Saved baked clip: {savePath}");
            }
        }

        EditorUtility.ClearProgressBar();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Baking Complete!",
            $"Baked {bakedCount} animation clips.\n" +
            $"Saved to: {exportPath}\n\n" +
            "These clips have explicit curves on ALL bones.\n" +
            "You can now export them to FBX without NaN warnings.",
            "OK");
    }

    private AnimationClip BakeClip(AnimationClip sourceClip, Transform root, Transform[] allBones)
    {
        AnimationClip bakedClip = new AnimationClip();
        bakedClip.name = sourceClip.name + "_baked";
        bakedClip.frameRate = sampleRate;
        bakedClip.legacy = sourceClip.legacy;

        float clipLength = sourceClip.length;
        int frameCount = Mathf.CeilToInt(clipLength * sampleRate) + 1;

        // Store original transforms so we can restore them
        Dictionary<Transform, TransformData> originalTransforms = new Dictionary<Transform, TransformData>();
        foreach (var bone in allBones)
        {
            originalTransforms[bone] = new TransformData
            {
                localPosition = bone.localPosition,
                localRotation = bone.localRotation,
                localScale = bone.localScale
            };
        }

        // Dictionary to hold curves per bone
        Dictionary<string, BoneCurves> boneCurves = new Dictionary<string, BoneCurves>();

        // Initialize curve storage for each bone
        foreach (var bone in allBones)
        {
            if (bone == root && !bakeRootMotion) continue;

            string path = GetRelativePath(root, bone);
            boneCurves[path] = new BoneCurves();
        }

        // Sample every frame
        for (int frame = 0; frame < frameCount; frame++)
        {
            float time = Mathf.Min((float)frame / sampleRate, clipLength);

            // Sample the animation at this time
            sourceClip.SampleAnimation(root.gameObject, time);

            // Record all bone transforms
            foreach (var bone in allBones)
            {
                if (bone == root && !bakeRootMotion) continue;

                string path = GetRelativePath(root, bone);
                if (!boneCurves.ContainsKey(path)) continue;

                var curves = boneCurves[path];

                Vector3 pos = bone.localPosition;
                Quaternion rot = bone.localRotation;
                Vector3 scale = bone.localScale;

                // Sanitize values - replace NaN/Infinity with 0
                pos = SanitizeVector(pos);
                scale = SanitizeVector(scale, 1f);

                if (float.IsNaN(rot.x) || float.IsNaN(rot.y) || float.IsNaN(rot.z) || float.IsNaN(rot.w))
                    rot = Quaternion.identity;

                curves.posX.Add(new Keyframe(time, pos.x));
                curves.posY.Add(new Keyframe(time, pos.y));
                curves.posZ.Add(new Keyframe(time, pos.z));

                curves.rotX.Add(new Keyframe(time, rot.x));
                curves.rotY.Add(new Keyframe(time, rot.y));
                curves.rotZ.Add(new Keyframe(time, rot.z));
                curves.rotW.Add(new Keyframe(time, rot.w));

                // Fix zero scale for FBX SDK compatibility
                curves.scaleX.Add(new Keyframe(time, scale.x == 0 ? 1e-5f : scale.x));
                curves.scaleY.Add(new Keyframe(time, scale.y == 0 ? 1e-5f : scale.y));
                curves.scaleZ.Add(new Keyframe(time, scale.z == 0 ? 1e-5f : scale.z));
            }
        }

        // Restore original transforms
        foreach (var kvp in originalTransforms)
        {
            if (kvp.Key != null)
            {
                kvp.Key.localPosition = kvp.Value.localPosition;
                kvp.Key.localRotation = kvp.Value.localRotation;
                kvp.Key.localScale = kvp.Value.localScale;
            }
        }

        // Apply curves to the baked clip
        foreach (var kvp in boneCurves)
        {
            string path = kvp.Key;
            var curves = kvp.Value;

            // Position
            bakedClip.SetCurve(path, typeof(Transform), "localPosition.x", new AnimationCurve(curves.posX.ToArray()));
            bakedClip.SetCurve(path, typeof(Transform), "localPosition.y", new AnimationCurve(curves.posY.ToArray()));
            bakedClip.SetCurve(path, typeof(Transform), "localPosition.z", new AnimationCurve(curves.posZ.ToArray()));

            // Rotation (quaternion)
            bakedClip.SetCurve(path, typeof(Transform), "localRotation.x", new AnimationCurve(curves.rotX.ToArray()));
            bakedClip.SetCurve(path, typeof(Transform), "localRotation.y", new AnimationCurve(curves.rotY.ToArray()));
            bakedClip.SetCurve(path, typeof(Transform), "localRotation.z", new AnimationCurve(curves.rotZ.ToArray()));
            bakedClip.SetCurve(path, typeof(Transform), "localRotation.w", new AnimationCurve(curves.rotW.ToArray()));

            // Scale
            bakedClip.SetCurve(path, typeof(Transform), "localScale.x", new AnimationCurve(curves.scaleX.ToArray()));
            bakedClip.SetCurve(path, typeof(Transform), "localScale.y", new AnimationCurve(curves.scaleY.ToArray()));
            bakedClip.SetCurve(path, typeof(Transform), "localScale.z", new AnimationCurve(curves.scaleZ.ToArray()));
        }

        // Copy animation events
        AnimationEvent[] events = AnimationUtility.GetAnimationEvents(sourceClip);
        if (events != null && events.Length > 0)
            AnimationUtility.SetAnimationEvents(bakedClip, events);

        // Ensure clip settings match
        AnimationClipSettings sourceSettings = AnimationUtility.GetAnimationClipSettings(sourceClip);
        AnimationClipSettings bakedSettings = AnimationUtility.GetAnimationClipSettings(bakedClip);
        bakedSettings.loopTime = sourceSettings.loopTime;
        bakedSettings.loopBlend = sourceSettings.loopBlend;
        AnimationUtility.SetAnimationClipSettings(bakedClip, bakedSettings);

        return bakedClip;
    }

    private Vector3 SanitizeVector(Vector3 v, float defaultVal = 0f)
    {
        return new Vector3(
            float.IsNaN(v.x) || float.IsInfinity(v.x) ? defaultVal : v.x,
            float.IsNaN(v.y) || float.IsInfinity(v.y) ? defaultVal : v.y,
            float.IsNaN(v.z) || float.IsInfinity(v.z) ? defaultVal : v.z
        );
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

    private struct TransformData
    {
        public Vector3 localPosition;
        public Quaternion localRotation;
        public Vector3 localScale;
    }

    private class BoneCurves
    {
        public List<Keyframe> posX = new List<Keyframe>();
        public List<Keyframe> posY = new List<Keyframe>();
        public List<Keyframe> posZ = new List<Keyframe>();
        public List<Keyframe> rotX = new List<Keyframe>();
        public List<Keyframe> rotY = new List<Keyframe>();
        public List<Keyframe> rotZ = new List<Keyframe>();
        public List<Keyframe> rotW = new List<Keyframe>();
        public List<Keyframe> scaleX = new List<Keyframe>();
        public List<Keyframe> scaleY = new List<Keyframe>();
        public List<Keyframe> scaleZ = new List<Keyframe>();
    }
}
