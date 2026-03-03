using UnityEngine;
using UnityEditor;

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

    [MenuItem("Window/Instant Light Setup")]
    public static void ShowWindow()
    {
        var window = GetWindow<lightSetup>();
        var títle = new GUIContent("Instant Light Setup");
        window.titleContent = títle;

    }

    private void OnGUI()
    {
        GUILayout.Space(20);
       

        selectedObject = (GameObject)EditorGUILayout.ObjectField(
            "GameObject:",
            selectedObject,
            typeof(GameObject),
            true  // true = Scene-Objekte erlauben
            );
       
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        EditorGUILayout.LabelField("Light Settings");
        selectedIndex = EditorGUILayout.Popup("Types:", selectedIndex, options);


        // Licht Einstellungen
        lightIntensity = EditorGUILayout.Slider("Intensity:", lightIntensity, 0f, 8f);
        lightColor = EditorGUILayout.ColorField("Color:", lightColor);
        
        GUILayout.Space(20);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        EditorGUILayout.LabelField("Light Position");

        sliderDistance = EditorGUILayout.Slider("Distance:", sliderDistance, 0f, 100f);
        horizontalAngle = EditorGUILayout.Slider("Horizontal:", horizontalAngle, 0f, 360f);
        verticalAngle = EditorGUILayout.Slider("Vertical:", verticalAngle, 0f, 90f);

        GUILayout.Space(20);

        showPreview = EditorGUILayout.Toggle("Preview", showPreview);

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Space(20);

        EditorGUILayout.LabelField("Presets", EditorStyles.boldLabel);
        presetName = EditorGUILayout.TextField("Preset Name:", presetName);

        using (new EditorGUI.DisabledScope(selectedObject == null))
        {
            if (GUILayout.Button("Save Preset", GUILayout.Height(32)))
            {

                LightRigManager.SaveRig(GetLightsRoot(selectedObject), presetName);
            }

            if (GUILayout.Button("Load Preset", GUILayout.Height(32)))
            {
                
                LightRigManager.LoadRig(GetLightsRoot(selectedObject), presetName);
            }
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.Space(10);

        if (GUILayout.Button("Add Light", GUILayout.Height(40)))
        {
            CreateLightWithAngles();
        }

        if (GUILayout.Button("Delete Lights", GUILayout.Height(40)))
        {
            DeleteLights();
        }



    }

    void CreateLightWithAngles()
    {
        if (selectedObject == null)
        {
            EditorUtility.DisplayDialog("Fehler", "Wähle ein Objekt!", "OK");
            return;
        }

        // Startposition = Objekt-Position
        Vector3 lightPos = selectedObject.transform.position;

        // Horizontal um Y-Achse drehen (0-360°)
        lightPos += Quaternion.Euler(0, horizontalAngle, 0) * selectedObject.transform.forward * sliderDistance;

        // Vertikal anheben/senken (0-90°)
        lightPos += Vector3.up * Mathf.Sin(verticalAngle * Mathf.Deg2Rad) * sliderDistance;

        // Licht erstellen
        GameObject lightGO = new GameObject(options[selectedIndex]);
        Undo.RegisterCreatedObjectUndo(lightGO, "Create Light");

        Transform lightsRoot = GetOrCreateLightsRoot(selectedObject);

        lightGO.transform.SetParent(lightsRoot, true); // parent FIRST, world stays

        Light light = lightGO.AddComponent<Light>();
        light.type = MapToLightType(selectedIndex);
        light.intensity = lightIntensity;
        light.color = lightColor;

        lightGO.transform.position = lightPos;
        lightGO.transform.LookAt(selectedObject.transform.position);

        GameObject child;
        if (!HasChildWithName(selectedObject, "Lights"))
        {
            child = new GameObject("Lights");
            child.transform.SetParent(selectedObject.transform);
        }
        else
        {
            child = selectedObject.transform.Find("Lights").gameObject;
        }


        light.transform.SetParent(child.transform);
        Undo.RegisterCreatedObjectUndo(lightGO, "Licht positioniert");
        Debug.Log($"Licht bei H:{horizontalAngle:F0}° V:{verticalAngle:F0}° Distanz:{sliderDistance}");
    }

    bool HasChildWithName(GameObject parent, string childName)
    {
        for (int i = 0; i < parent.transform.childCount; i++)
        {
            if (parent.transform.GetChild(i).name == childName)
                return true;
        }
        return false;
    }

    void DeleteLights()
    {
        if (selectedObject == null)
        {
            EditorUtility.DisplayDialog("Fehler", "Wähle ein Objekt!", "OK");
            return;
        }

        bool userClickedOK = EditorUtility.DisplayDialog("Are you sure?", "Delete the whole setup", "Yes", "Discard");
        if (userClickedOK)
        {
            Transform lightsParent = selectedObject.transform.Find("Lights");
            if (lightsParent != null)
            {
                Undo.DestroyObjectImmediate(lightsParent.gameObject);
                Debug.Log("Alle Lichter gelöscht.");
            }
            else
            {
                EditorUtility.DisplayDialog("Info", "Keine Lichter zum Löschen gefunden!", "OK");
            }
        }
        else
        {
            // User hat "Abbrechen" → nichts tun
        }


    }

    private static Transform GetOrCreateLightsRoot(GameObject parent)
    {
        var existing = parent.transform.Find("Lights");
        if (existing != null) return existing;

        var root = new GameObject("Lights");
        Undo.RegisterCreatedObjectUndo(root, "Create Lights Root");
        root.transform.SetParent(parent.transform, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        return root.transform;
    }
    private static GameObject GetLightsRoot(GameObject parent)
    {
        // Für Save/Load: wir speichern den "Lights" Root (oder erstellen ihn)
        return GetOrCreateLightsRoot(parent).gameObject;
    }

    private static LightType MapToLightType(int index)
    {
        // Dropdown: Directional, Point, Spot
        return index switch
        {
            0 => LightType.Directional,
            1 => LightType.Point,
            2 => LightType.Spot,
            _ => LightType.Point
        };
    }

}

