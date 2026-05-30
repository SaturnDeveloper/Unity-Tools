using UnityEditor;
using UnityEngine;

public static class LightHotkeys
{
    // Shift + L 
    [MenuItem("Tools/Hotkeys/Spawn Light At Mouse #l")]
    private static void SpawnLightAtMouse()
    {
        SceneView sceneView = SceneView.lastActiveSceneView;

        if (sceneView == null)
        {
            Debug.LogWarning("Kein SceneView gefunden.");
            return;
        }

        // Mausposition im SceneView holen
        Vector2 mousePos = Event.current != null
            ? Event.current.mousePosition
            : sceneView.position.size / 2f;

        // GUI → Welt-Ray
        Ray ray = HandleUtility.GUIPointToWorldRay(mousePos);

        Vector3 spawnPos;

        // Wenn Raycast etwas trifft → Hit-Point
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            spawnPos = hit.point;
        }
        else
        {
            // Wenn nichts getroffen → Punkt direkt vor der Kamera
            spawnPos = ray.origin + ray.direction * 5f;
        }

        GameObject lightGO = new GameObject("Hotkey Light");
        Undo.RegisterCreatedObjectUndo(lightGO, "Create Light");

        Light light = lightGO.AddComponent<Light>();
        light.type = LightType.Point;

        lightGO.transform.position = spawnPos;

        Selection.activeGameObject = lightGO;
    }
}
