using UnityEditor;
using UnityEngine;

public class lightSetup : EditorWindow
{
    GameObject selectedObject;

    int selectedIndex = 0;
    string[] options = { "Directional Light", "Point Light", "Spot Light" };
    float lightIntensity = 1f;
    Color lightColor = Color.white;

    float horizontalAngle = 0f;
    float verticalAngle = 0f;
    float sliderDistance = 5f;

    bool showPreview = false;
    private string presetName = "MyRig";

    private GameObject previewLightGO;

    // --- Light Group System ---
    private LightGroupDatabase groupDB;
    private string[] groupOptions = { "None" };
    private int selectedGroupIndex = 0;

    [MenuItem("Window/Instant Light Setup")]
    public static void ShowWindow()
    {
        var window = GetWindow<lightSetup>();
        window.titleContent = new GUIContent("Instant Light Setup");
    }

    private void OnEnable()
    {
        LoadGroupDatabase();
    }

    private void LoadGroupDatabase()
    {
        string[] guids = AssetDatabase.FindAssets("t:LightGroupDatabase");

        if (guids.Length == 0)
        {
            Debug.LogWarning("Keine LightGroupDatabase gefunden!");
            groupDB = null;
            RefreshGroupOptions();
            return;
        }

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        groupDB = AssetDatabase.LoadAssetAtPath<LightGroupDatabase>(path);

        RefreshGroupOptions();
    }

    private void RefreshGroupOptions()
    {
        if (groupDB == null)
        {
            groupOptions = new string[] { "None" };
            selectedGroupIndex = 0;
            return;
        }

        if (groupDB.groups == null)
            groupDB.groups = new System.Collections.Generic.List<string>();

        groupOptions = new string[groupDB.groups.Count + 1];
        groupOptions[0] = "None";

        for (int i = 0; i < groupDB.groups.Count; i++)
            groupOptions[i + 1] = groupDB.groups[i];

        selectedGroupIndex = Mathf.Clamp(selectedGroupIndex, 0, groupOptions.Length - 1);
    }

    private void OnGUI()
    {
        GUILayout.Space(20);

        selectedObject = (GameObject)EditorGUILayout.ObjectField(
            "GameObject:",
            selectedObject,
            typeof(GameObject),
            true
        );

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        EditorGUILayout.LabelField("Light Settings");
        selectedIndex = EditorGUILayout.Popup("Types:", selectedIndex, options);

        lightIntensity = EditorGUILayout.Slider("Intensity:", lightIntensity, 0f, 8f);
        lightColor = EditorGUILayout.ColorField("Color:", lightColor);

        GUILayout.Space(20);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        EditorGUILayout.LabelField("Light Position");

        sliderDistance = EditorGUILayout.Slider("Distance:", sliderDistance, 0f, 100f);
        horizontalAngle = EditorGUILayout.Slider("Horizontal:", horizontalAngle, 0f, 360f);
        verticalAngle = EditorGUILayout.Slider("Vertical:", verticalAngle, 0f, 90f);

        GUILayout.Space(20);

        // --- Light Group Dropdown ---
        EditorGUILayout.LabelField("Light Group", EditorStyles.boldLabel);
        selectedGroupIndex = EditorGUILayout.Popup("Group:", selectedGroupIndex, groupOptions);

        GUILayout.Space(20);

        if (showPreview = EditorGUILayout.Toggle("Preview", showPreview))
            UpdatePreviewLight();
        else
            DestroyPreviewLight();

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Space(20);

        EditorGUILayout.LabelField("Presets", EditorStyles.boldLabel);
        presetName = EditorGUILayout.TextField("Preset Name:", presetName);

        using (new EditorGUI.DisabledScope(selectedObject == null))
        {
            if (GUILayout.Button("Save Preset", GUILayout.Height(32)))
                LightRigManager.SaveRig(GetLightsRoot(selectedObject), presetName);

            if (GUILayout.Button("Load Preset", GUILayout.Height(32)))
                LightRigManager.LoadRig(GetLightsRoot(selectedObject), presetName);
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.Space(10);

        if (GUILayout.Button("Add Light", GUILayout.Height(40)))
            CreateLightWithAngles();

        if (GUILayout.Button("Delete Lights", GUILayout.Height(40)))
            DeleteLights();
    }

    void CreateLightWithAngles()
    {
        if (selectedObject == null)
        {
            EditorUtility.DisplayDialog("Fehler", "Wähle ein Objekt!", "OK");
            return;
        }

        Vector3 lightPos = selectedObject.transform.position;

        lightPos += Quaternion.Euler(0, horizontalAngle, 0) * selectedObject.transform.forward * sliderDistance;
        lightPos += Vector3.up * Mathf.Sin(verticalAngle * Mathf.Deg2Rad) * sliderDistance;

        GameObject lightGO = new GameObject(options[selectedIndex]);
        Undo.RegisterCreatedObjectUndo(lightGO, "Create Light");

        Transform lightsRoot = GetOrCreateLightsRoot(selectedObject);
        lightGO.transform.SetParent(lightsRoot, true);

        Light light = lightGO.AddComponent<Light>();
        light.type = MapToLightType(selectedIndex);
        light.intensity = lightIntensity;
        light.color = lightColor;

        lightGO.transform.position = lightPos;
        lightGO.transform.LookAt(selectedObject.transform.position);

        // --- Assign Light Group ---
        var group = lightGO.AddComponent<LightGroup>();

        if (groupOptions != null && selectedGroupIndex < groupOptions.Length)
            group.groupName = groupOptions[selectedGroupIndex];
        else
            group.groupName = "None";

        Debug.Log($"Licht erstellt in Gruppe: {group.groupName}");
    }

    void DeleteLights()
    {
        if (selectedObject == null)
        {
            EditorUtility.DisplayDialog("Fehler", "Wähle ein Objekt!", "OK");
            return;
        }

        bool ok = EditorUtility.DisplayDialog("Are you sure?", "Delete the whole setup", "Yes", "Discard");
        if (!ok) return;

        Transform lightsParent = selectedObject.transform.Find("Lights");
        if (lightsParent != null)
        {
            Undo.DestroyObjectImmediate(lightsParent.gameObject);
            Debug.Log("Alle Lichter gelöscht.");
        }
        else
        {
            EditorUtility.DisplayDialog("Info", "Keine Lichter gefunden!", "OK");
        }
    }

    private static Transform GetOrCreateLightsRoot(GameObject parent)
    {
        var existing = parent.transform.Find("Lights");
        if (existing != null) return existing;

        var root = new GameObject("Lights");
        Undo.RegisterCreatedObjectUndo(root, "Create Lights Root");
        root.transform.SetParent(parent.transform, false);
        return root.transform;
    }

    private static GameObject GetLightsRoot(GameObject parent)
    {
        return GetOrCreateLightsRoot(parent).gameObject;
    }

    private static LightType MapToLightType(int index)
    {
        return index switch
        {
            0 => LightType.Directional,
            1 => LightType.Point,
            2 => LightType.Spot,
            _ => LightType.Point
        };
    }

    private void UpdatePreviewLight()
    {
        if (!showPreview || selectedObject == null)
        {
            DestroyPreviewLight();
            return;
        }

        if (previewLightGO == null)
        {
            previewLightGO = new GameObject("__LightPreview__");
            previewLightGO.hideFlags = HideFlags.HideAndDontSave;
            previewLightGO.AddComponent<Light>();
        }

        Vector3 targetPos = selectedObject.transform.position;
        Vector3 lightPos = targetPos;

        lightPos += Quaternion.Euler(0, horizontalAngle, 0) * selectedObject.transform.forward * sliderDistance;
        lightPos += Vector3.up * Mathf.Sin(verticalAngle * Mathf.Deg2Rad) * sliderDistance;

        previewLightGO.transform.position = lightPos;
        previewLightGO.transform.LookAt(targetPos);

        var l = previewLightGO.GetComponent<Light>();
        l.type = MapToLightType(selectedIndex);
        l.intensity = lightIntensity;
        l.color = lightColor;

        SceneView.RepaintAll();
    }

    private void DestroyPreviewLight()
    {
        if (previewLightGO != null)
        {
            DestroyImmediate(previewLightGO);
            previewLightGO = null;
            SceneView.RepaintAll();
        }
    }
}
