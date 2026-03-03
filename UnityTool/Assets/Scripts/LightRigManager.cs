using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class LightRigManager
{
    private const string RIG_FOLDER = "Assets/Editor/LightRigs/";

    public static void SaveRig(GameObject rigRoot, string rigName)
    {
        if (rigRoot == null || string.IsNullOrWhiteSpace(rigName))
        {
            Debug.LogWarning("SaveRig: rigRoot oder rigName ist ungültig.");
            return;
        }

        if (!Directory.Exists(RIG_FOLDER))
            Directory.CreateDirectory(RIG_FOLDER);

        var rig = new LightsRigData { rigName = rigName };

        // Rekursiv alle Lights unterhalb des Roots
        var lights = rigRoot.GetComponentsInChildren<Light>(true);

        foreach (var light in lights)
        {
            // Optional: Root selbst ignorieren, falls dort ein Light hängt
            if (light.gameObject == rigRoot) continue;

            Transform t = light.transform;

            var lightData = new LightData
            {
                name = t.name,

                type = light.type,
                intensity = light.intensity,
                color = light.color,
                range = light.range,
                spotAngle = light.spotAngle,

                localPosition = t.localPosition,
                localEulerAngles = t.localEulerAngles
            };

            rig.lights.Add(lightData);
        }

        string path = Path.Combine(RIG_FOLDER, rigName + ".json");
        File.WriteAllText(path, JsonUtility.ToJson(rig, true));
        AssetDatabase.Refresh();

        Debug.Log($"Saved rig '{rigName}' with {rig.lights.Count} lights to: {path}");
    }

    public static void LoadRig(GameObject rigRoot, string rigName)
    {
        if (rigRoot == null || string.IsNullOrWhiteSpace(rigName))
        {
            Debug.LogWarning("LoadRig: rigRoot oder rigName ist ungültig.");
            return;
        }

        string path = Path.Combine(RIG_FOLDER, rigName + ".json");
        if (!File.Exists(path))
        {
            Debug.LogWarning($"LoadRig: Rig nicht gefunden: {path}");
            return;
        }

        var rig = JsonUtility.FromJson<LightsRigData>(File.ReadAllText(path));
        if (rig == null || rig.lights == null)
        {
            Debug.LogWarning("LoadRig: JSON konnte nicht gelesen werden oder ist leer.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();

        DeleteLights(rigRoot);

        foreach (var lightData in rig.lights)
        {
            var lightGO = new GameObject(string.IsNullOrEmpty(lightData.name) ? "Light" : lightData.name);
            Undo.RegisterCreatedObjectUndo(lightGO, $"Load Light Rig {rigName}");

            // Parent FIRST, dann locals setzen
            lightGO.transform.SetParent(rigRoot.transform, false);
            lightGO.transform.localPosition = lightData.localPosition;
            lightGO.transform.localEulerAngles = lightData.localEulerAngles;

            var light = lightGO.AddComponent<Light>();
            light.type = lightData.type;
            light.intensity = lightData.intensity;
            light.color = lightData.color;
            light.range = lightData.range;
            light.spotAngle = lightData.spotAngle;
        }

        Undo.CollapseUndoOperations(group);

        Debug.Log($"Loaded rig '{rigName}' with {rig.lights.Count} lights from: {path}");
    }

    public static string[] GetRigNames()
    {
        if (!Directory.Exists(RIG_FOLDER)) return new string[0];

        string[] files = Directory.GetFiles(RIG_FOLDER, "*.json");
        string[] names = new string[files.Length];

        for (int i = 0; i < files.Length; i++)
            names[i] = Path.GetFileNameWithoutExtension(files[i]);

        return names;
    }

    private static void DeleteLights(GameObject rigRoot)
    {
        // Rekursiv alle Lights löschen
        var lights = rigRoot.GetComponentsInChildren<Light>(true);

        // Liste, damit wir nicht während der Iteration kaputt machen
        var toDelete = new List<GameObject>(lights.Length);
        foreach (var light in lights)
        {
            if (light == null) continue;
            if (light.gameObject == rigRoot) continue; // optional
            toDelete.Add(light.gameObject);
        }

        foreach (var go in toDelete)
            Undo.DestroyObjectImmediate(go);
    }
}