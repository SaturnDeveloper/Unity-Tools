using UnityEditor;
using UnityEngine;

public static class LightHotkeys
{
    // Ctrl + Shift + L
    [MenuItem("Tools/Hotkeys/Spawn Light At Mouse #l")]
    private static void SpawnLightAtMouse()
    {
        SceneView sceneView = SceneView.lastActiveSceneView;

        if (sceneView == null)
        {
            Debug.LogWarning("Kein SceneView gefunden.");
            return;
        }

        Event e = Event.current;

        // Mausposition im SceneView
        Vector2 mousePos = Event.current != null
            ? Event.current.mousePosition
            : sceneView.position.size / 2f;

        mousePos.y = sceneView.camera.pixelHeight - mousePos.y;

        Ray ray = sceneView.camera.ScreenPointToRay(mousePos);

        Vector3 spawnPos;

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            spawnPos = hit.point;
        }
        else
        {
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