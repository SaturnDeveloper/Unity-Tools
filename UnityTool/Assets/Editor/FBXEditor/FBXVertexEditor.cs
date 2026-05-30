using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;

public class FBXVertexEditor : EditorWindow
{
    private GameObject selectedObject;
    private Mesh workMesh;
    private Vector3[] vertices;
    private Vector2 scrollPos;
    private Vector3 globalOffset;
    private float globalScale = 1f;
    private bool showGizmos = true;
    private int selectedVertex = -1;

    // ── Event: wird gefeuert wenn sich Vertices ändern ──────
    public event Action<Vector3[]> OnVerticesChanged;

    [MenuItem("Tools/FBX Vertex Editor")]
    public static void Open()
        => GetWindow<FBXVertexEditor>("Vertex Editor");

    private void LoadFromSelection()
    {
        selectedObject = Selection.activeGameObject;
        if (selectedObject == null) { Debug.LogWarning("Kein Objekt ausgewählt"); return; }

        var mf = selectedObject.GetComponent<MeshFilter>();
        if (mf == null) { Debug.LogWarning("Kein MeshFilter gefunden"); return; }

        workMesh = Instantiate(mf.sharedMesh);
        vertices = workMesh.vertices;
        Debug.Log($"Geladen: {vertices.Length} Vertices");
    }

    private void OnGUI()
    {
        DrawToolbar();
        if (vertices == null) { EditorGUILayout.HelpBox("Objekt auswählen und 'Laden' klicken", MessageType.Info); return; }
        DrawGlobalOps();
        DrawVertexList();
        DrawFooter();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        selectedObject = (GameObject)EditorGUILayout.ObjectField(selectedObject, typeof(GameObject), true);
        if (GUILayout.Button("Laden", EditorStyles.toolbarButton, GUILayout.Width(60)))
            LoadFromSelection();
        showGizmos = GUILayout.Toggle(showGizmos, "Gizmos", EditorStyles.toolbarButton);
        EditorGUILayout.EndHorizontal();
    }

    private void DrawGlobalOps()
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Global bearbeiten", EditorStyles.boldLabel);
        globalOffset = EditorGUILayout.Vector3Field("Offset", globalOffset);
        globalScale = EditorGUILayout.Slider("Scale", globalScale, 0.01f, 10f);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Offset anwenden")) ApplyOffset();
        if (GUILayout.Button("Scale anwenden")) ApplyScale();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(4);
    }

    private void DrawVertexList()
    {
        EditorGUILayout.LabelField($"Vertices ({vertices.Length})", EditorStyles.boldLabel);
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(position.height - 200));

        for (int i = 0; i < vertices.Length; i++)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"#{i}", GUILayout.Width(40));

            // ── Change Detection pro Vertex ─────────────────
            EditorGUI.BeginChangeCheck();
            vertices[i] = EditorGUILayout.Vector3Field(GUIContent.none, vertices[i]);
            if (EditorGUI.EndChangeCheck())
                HandleVerticesChanged();

            if (GUILayout.Button("●", GUILayout.Width(22)))
                selectedVertex = (selectedVertex == i) ? -1 : i;

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawFooter()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Auf Mesh anwenden")) ApplyToMesh();
        if (GUILayout.Button("Als Asset speichern")) SaveAsset();
        EditorGUILayout.EndHorizontal();
    }

    // ── Wird bei jeder Vertex-Änderung aufgerufen ────────────
    private void HandleVerticesChanged()
    {
        // Mesh sofort live aktualisieren
        workMesh.vertices = vertices;
        workMesh.RecalculateBounds();
        workMesh.RecalculateNormals();

        var mf = selectedObject?.GetComponent<MeshFilter>();
        if (mf != null) mf.mesh = workMesh;

        // Event feuern – falls jemand von außen lauscht
        OnVerticesChanged?.Invoke(vertices);

        // Scene-View neu zeichnen
        SceneView.RepaintAll();
    }

    private void ApplyOffset()
    {
        for (int i = 0; i < vertices.Length; i++)
            vertices[i] += globalOffset;
        globalOffset = Vector3.zero;
        HandleVerticesChanged();
    }

    private void ApplyScale()
    {
        for (int i = 0; i < vertices.Length; i++)
            vertices[i] *= globalScale;
        globalScale = 1f;
        HandleVerticesChanged();
    }

    private void ApplyToMesh()
    {
        HandleVerticesChanged();
        Debug.Log("Mesh aktualisiert");
    }

    private void SaveAsset()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Mesh speichern", "EditedMesh", "asset", "Speicherpfad wählen");
        if (string.IsNullOrEmpty(path)) return;
        workMesh.vertices = vertices;
        workMesh.RecalculateBounds();
        workMesh.RecalculateNormals();
        AssetDatabase.CreateAsset(workMesh, path);
        AssetDatabase.SaveAssets();
        Debug.Log($"Gespeichert: {path}");
    }

    private void OnEnable() => SceneView.duringSceneGui += OnSceneGUI;
    private void OnDisable() => SceneView.duringSceneGui -= OnSceneGUI;

    private void OnSceneGUI(SceneView sv)
    {
        if (!showGizmos || vertices == null || selectedObject == null) return;
        var t = selectedObject.transform;

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 worldPos = t.TransformPoint(vertices[i]);
            bool isSel = i == selectedVertex;
            Handles.color = isSel ? Color.yellow : new Color(0f, 1f, 0.5f, 0.7f);
            Handles.SphereHandleCap(0, worldPos, Quaternion.identity, isSel ? 0.05f : 0.02f, EventType.Repaint);
            if (isSel) Handles.Label(worldPos, $"#{i}");
        }
    }
}