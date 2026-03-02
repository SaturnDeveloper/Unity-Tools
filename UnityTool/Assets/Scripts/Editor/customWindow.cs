using UnityEngine;
using UnityEditor;

public class customWindow : EditorWindow
{
    float sliderValue = 50f;


    [MenuItem("Window/Test Window")]
    public static void ShowWindow()
    {
        var window = GetWindow<customWindow>();
        var títle = new GUIContent("Test Window");
        window.titleContent = títle;

    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("this is a custom window");
        EditorGUILayout.CurveField(new AnimationCurve());
        EditorGUILayout.ColorField(Color.red);
        EditorGUILayout.BoundsField(new Bounds(Vector3.zero, Vector3.one));
        sliderValue = EditorGUILayout.Slider("Wert:", sliderValue, 0f, 100f);
    }
}
