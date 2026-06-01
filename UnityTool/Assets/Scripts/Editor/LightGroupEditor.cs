using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LightGroup))]
public class LightGroupEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var group = (LightGroup)target;

        string[] guids = AssetDatabase.FindAssets("t:LightGroupDatabase");
        if (guids.Length == 0)
        {
            EditorGUILayout.HelpBox("Keine LightGroupDatabase gefunden!", MessageType.Warning);
            return;
        }

        var db = AssetDatabase.LoadAssetAtPath<LightGroupDatabase>(
            AssetDatabase.GUIDToAssetPath(guids[0])
        );

        int index = Mathf.Max(0, db.groups.IndexOf(group.groupName));
        index = EditorGUILayout.Popup("Group", index, db.groups.ToArray());
        group.groupName = db.groups[index];

        if (GUI.changed)
            EditorUtility.SetDirty(group);
    }
}
