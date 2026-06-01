using UnityEditor;
using UnityEngine;

public class LightGroupWindow : EditorWindow
{
    private LightGroupDatabase db;
    private string newGroupName = "";

    [MenuItem("Tools/Light Groups")]
    public static void Open()
    {
        GetWindow<LightGroupWindow>("Light Groups");
    }

    private void OnEnable()
    {
        // Auto-load database
        string[] guids = AssetDatabase.FindAssets("t:LightGroupDatabase");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            db = AssetDatabase.LoadAssetAtPath<LightGroupDatabase>(path);
        }
    }

    private void OnGUI()
    {
        if (db == null)
        {
            EditorGUILayout.HelpBox("Keine LightGroupDatabase gefunden!", MessageType.Warning);
            return;
        }

        GUILayout.Label("Light Groups", EditorStyles.boldLabel);

        // Gruppen anzeigen
        for (int i = 0; i < db.groups.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();

            db.groups[i] = EditorGUILayout.TextField(db.groups[i]);

            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                EditorGUILayout.EndHorizontal();   // WICHTIG!
                db.groups.RemoveAt(i);
                EditorUtility.SetDirty(db);
                break; // statt return
            }

            EditorGUILayout.EndHorizontal();
        }

            GUILayout.Space(10);

        // Neue Gruppe hinzufügen
        EditorGUILayout.BeginHorizontal();
        newGroupName = EditorGUILayout.TextField(newGroupName);

        if (GUILayout.Button("Add") && !string.IsNullOrEmpty(newGroupName))
        {
            if (!db.groups.Contains(newGroupName))
            {
                db.groups.Add(newGroupName);
                EditorUtility.SetDirty(db);
            }
            newGroupName = "";
        }

        EditorGUILayout.EndHorizontal();
    }
}
