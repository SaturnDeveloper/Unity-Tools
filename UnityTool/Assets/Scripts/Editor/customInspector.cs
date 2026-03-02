using UnityEngine;
using UnityEditor;
[CustomEditor(typeof(testScript))]
public class customInspector : Editor
{
   public override void OnInspectorGUI()
   {
        EditorGUILayout.LabelField("this is dogshit");
        DrawDefaultInspector();
        testScript myScript = (testScript)target;
        if(GUILayout.Button("press me"))
            {
                myScript.myInt++;
        }
    }
}
