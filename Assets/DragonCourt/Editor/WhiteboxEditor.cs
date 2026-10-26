using System.IO;
using DragonCourt;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class WhiteboxEditor
{
    const string EditableScene = "Assets/DragonCourt/Scenes/DragonCourt_Editable.unity";

    [MenuItem("Dragon Court/Generate Editable Whitebox")]
    public static void Generate()
    {
        if (EditorApplication.isPlaying) { Debug.LogWarning("Exit Play mode first."); return; }
        if (File.Exists(EditableScene) && !EditorUtility.DisplayDialog("Regenerate whitebox?",
            "This replaces DragonCourt_Editable.unity. Save a copy first if you edited it.", "Replace", "Cancel")) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject root = CourtBuilder.Build();
        // Generated meshes/materials must become assets to survive editor restart.
        string folder = AssetDatabase.GenerateUniqueAssetPath("Assets/DragonCourt/Generated");
        Directory.CreateDirectory(folder);
        AssetDatabase.Refresh();
        var materials = new System.Collections.Generic.HashSet<Material>();
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
            foreach (Material mat in renderer.sharedMaterials)
                if (mat != null && !AssetDatabase.Contains(mat)) materials.Add(mat);
        int index = 0;
        foreach (Material mat in materials)
            AssetDatabase.CreateAsset(mat, folder + "/Material_" + index++ + ".mat");
        foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>())
            if (!AssetDatabase.Contains(filter.sharedMesh))
                AssetDatabase.CreateAsset(filter.sharedMesh, folder + "/Ramp_" + index++ + ".asset");
        Directory.CreateDirectory("Assets/DragonCourt/Scenes");
        EditorSceneManager.SaveScene(scene, EditableScene);
        AssetDatabase.SaveAssets();
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(EditableScene, true) };
        Selection.activeGameObject = root;
        Debug.Log("Editable whitebox generated. Press Play. Layout source: Resources/WhiteboxLayout.json");
    }

    [MenuItem("Dragon Court/Validate Current Scene Floors")]
    public static void ValidateFloors()
    {
        Physics.SyncTransforms();
        Vector3[] feet = { new Vector3(0,0,4), new Vector3(0,0,20),
            new Vector3(-30,0,65), new Vector3(30,8,79), new Vector3(-30,0,102),
            new Vector3(30,0,102), new Vector3(0,-3,118), new Vector3(0,0,138), new Vector3(0,0,164) };
        int passed = 0;
        foreach (Vector3 point in feet)
        {
            if (Physics.Raycast(point + Vector3.up * .35f, Vector3.down, out RaycastHit hit,
                1f, Physics.DefaultRaycastLayers) && Mathf.Abs(hit.point.y - point.y) < .15f) passed++;
            else Debug.LogError("Missing walking surface at " + point);
        }
        Debug.Log("Floor checks: " + passed + "/" + feet.Length + ". This does not replace a complete playthrough.");
    }
}
