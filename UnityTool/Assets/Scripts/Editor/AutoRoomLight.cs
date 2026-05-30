using UnityEditor;
using UnityEngine;

public class AutoLightGrid : EditorWindow
{
    GameObject roomObject;

    // Grid
    int gridX = 3;
    int gridY = 1;
    int gridZ = 3;
    float gridSpacing = 3f;
    float heightOffset = 0f;

    // Light settings
    float intensity = 1f;
    float range = 8f;
    Color lightColor = Color.white;

    // Preview
    bool showPreview = false;
    GameObject previewRoot;

    bool showGridPreview = false;
    GameObject gridPreviewRoot;

    [MenuItem("Window/Auto Light Grid")]
    public static void ShowWindow()
    {
        GetWindow<AutoLightGrid>("Auto Light Grid");
    }

    private void OnGUI()
    {
        roomObject = (GameObject)EditorGUILayout.ObjectField("Room Object:", roomObject, typeof(GameObject), true);

        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("Light Grid", EditorStyles.boldLabel);

        gridX = EditorGUILayout.IntSlider("Grid X", gridX, 1, 20);
        gridY = EditorGUILayout.IntSlider("Grid Y", gridY, 1, 10);
        gridZ = EditorGUILayout.IntSlider("Grid Z", gridZ, 1, 20);
        gridSpacing = EditorGUILayout.Slider("Spacing", gridSpacing, 0.5f, 10f);

        heightOffset = EditorGUILayout.FloatField("Height Offset", heightOffset);

        bool newGridPreview = EditorGUILayout.Toggle("Grid Preview", showGridPreview);
        if (newGridPreview != showGridPreview)
        {
            showGridPreview = newGridPreview;
            if (showGridPreview) CreateGridPreview();
            else DestroyGridPreview();
        }

        if (showGridPreview)
            UpdateGridPreview();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Light Settings", EditorStyles.boldLabel);

        intensity = EditorGUILayout.Slider("Intensity", intensity, 0f, 8f);
        range = EditorGUILayout.Slider("Range", range, 1f, 20f);
        lightColor = EditorGUILayout.ColorField("Color", lightColor);

        EditorGUILayout.Space(10);

        bool newPreview = EditorGUILayout.Toggle("Light Preview", showPreview);
        if (newPreview != showPreview)
        {
            showPreview = newPreview;
            if (showPreview) CreatePreview();
            else DestroyPreview();
        }

        if (showPreview)
            UpdatePreview();

        EditorGUILayout.Space(20);

        using (new EditorGUI.DisabledScope(roomObject == null))
        {
            if (GUILayout.Button("Generate Light Grid", GUILayout.Height(40)))
                GenerateGrid();

            if (GUILayout.Button("Delete Lights", GUILayout.Height(30)))
                DeleteLights();
        }
    }

    // ---------------- GRID PREVIEW ----------------

    void CreateGridPreview()
    {
        DestroyGridPreview();
        gridPreviewRoot = new GameObject("__GridPreview__");
        gridPreviewRoot.hideFlags = HideFlags.HideAndDontSave;
    }

    void UpdateGridPreview()
    {
        if (gridPreviewRoot == null || roomObject == null)
            return;

        foreach (Transform child in gridPreviewRoot.transform)
            DestroyImmediate(child.gameObject);

        Bounds b = GetBounds(roomObject);

        Vector3 center = b.center;

        // X + Z zentriert
        Vector3 totalSizeXZ = new Vector3(
            (gridX - 1) * gridSpacing,
            0,
            (gridZ - 1) * gridSpacing
        );

        Vector3 start = center - new Vector3(totalSizeXZ.x * 0.5f, 0, totalSizeXZ.z * 0.5f);

        // Y startet in der Mitte und geht nur nach oben
        float yStart = center.y + heightOffset;

        for (int x = 0; x < gridX; x++)
        for (int y = 0; y < gridY; y++)
        for (int z = 0; z < gridZ; z++)
        {
            Vector3 pos = new Vector3(
                start.x + x * gridSpacing,
                yStart + y * gridSpacing,
                start.z + z * gridSpacing
            );

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.hideFlags = HideFlags.HideAndDontSave;
            go.transform.SetParent(gridPreviewRoot.transform);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * 0.2f;

            DestroyImmediate(go.GetComponent<Collider>());
        }

        SceneView.RepaintAll();
    }

    void DestroyGridPreview()
    {
        if (gridPreviewRoot != null)
        {
            DestroyImmediate(gridPreviewRoot);
            gridPreviewRoot = null;
            SceneView.RepaintAll();
        }
    }

    // ---------------- LIGHT PREVIEW ----------------

    void CreatePreview()
    {
        DestroyPreview();
        previewRoot = new GameObject("__LightPreview__");
        previewRoot.hideFlags = HideFlags.HideAndDontSave;
    }

    void UpdatePreview()
    {
        if (previewRoot == null || roomObject == null)
            return;

        foreach (Transform child in previewRoot.transform)
            DestroyImmediate(child.gameObject);

        Bounds b = GetBounds(roomObject);

        Vector3 center = b.center;

        Vector3 totalSizeXZ = new Vector3(
            (gridX - 1) * gridSpacing,
            0,
            (gridZ - 1) * gridSpacing
        );

        Vector3 start = center - new Vector3(totalSizeXZ.x * 0.5f, 0, totalSizeXZ.z * 0.5f);

        float yStart = center.y + heightOffset;

        for (int x = 0; x < gridX; x++)
        for (int y = 0; y < gridY; y++)
        for (int z = 0; z < gridZ; z++)
        {
            Vector3 pos = new Vector3(
                start.x + x * gridSpacing,
                yStart + y * gridSpacing,
                start.z + z * gridSpacing
            );

            GameObject go = new GameObject($"PreviewLight_{x}_{y}_{z}");
            go.hideFlags = HideFlags.HideAndDontSave;
            go.transform.SetParent(previewRoot.transform);
            go.transform.position = pos;

            Light l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.intensity = intensity;
            l.range = range;
            l.color = lightColor;
        }

        SceneView.RepaintAll();
    }

    void DestroyPreview()
    {
        if (previewRoot != null)
        {
            DestroyImmediate(previewRoot);
            previewRoot = null;
            SceneView.RepaintAll();
        }
    }

    // ---------------- GENERATION ----------------

    void GenerateGrid()
    {
        if (roomObject == null) return;

        Transform root = GetOrCreateLightsRoot(roomObject);
        Bounds b = GetBounds(roomObject);

        Vector3 center = b.center;

        Vector3 totalSizeXZ = new Vector3(
            (gridX - 1) * gridSpacing,
            0,
            (gridZ - 1) * gridSpacing
        );

        Vector3 start = center - new Vector3(totalSizeXZ.x * 0.5f, 0, totalSizeXZ.z * 0.5f);

        float yStart = center.y + heightOffset;

        for (int x = 0; x < gridX; x++)
        for (int y = 0; y < gridY; y++)
        for (int z = 0; z < gridZ; z++)
        {
            Vector3 pos = new Vector3(
                start.x + x * gridSpacing,
                yStart + y * gridSpacing,
                start.z + z * gridSpacing
            );

            GameObject go = new GameObject($"Light_{x}_{y}_{z}");
            Undo.RegisterCreatedObjectUndo(go, "Create Light");
            go.transform.SetParent(root);
            go.transform.position = pos;

            Light l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.intensity = intensity;
            l.range = range;
            l.color = lightColor;
        }
    }

    void DeleteLights()
    {
        Transform root = roomObject.transform.Find("Lights");
        if (root != null)
            Undo.DestroyObjectImmediate(root.gameObject);
    }

    // ---------------- HELPERS ----------------

    Bounds GetBounds(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        Bounds b = new Bounds(obj.transform.position, Vector3.zero);
        foreach (var r in renderers)
            b.Encapsulate(r.bounds);
        return b;
    }

    Transform GetOrCreateLightsRoot(GameObject parent)
    {
        Transform t = parent.transform.Find("Lights");
        if (t != null) return t;

        GameObject go = new GameObject("Lights");
        go.transform.SetParent(parent.transform);
        go.transform.localPosition = Vector3.zero;
        return go.transform;
    }
}
