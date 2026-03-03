using UnityEngine;
using UnityEditor;

public class lightSetup : EditorWindow
{
    float horizontalAngle = 0f;    
    float verticalAngle = 0f;
    float sliderDistance = 5f;

    int selectedIndex = 0;
    string[] options = { "Directional Light", "Point Light", "Spot Light" };
    GameObject selectedObject;

    float lightIntensity = 1f;
    Color lightColor = Color.white;




    [MenuItem("Window/Light Instant Setup")]
    public static void ShowWindow()
    {
        var window = GetWindow<lightSetup>();
        var títle = new GUIContent("Light Instant Setup");
        window.titleContent = títle;

    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("this is a custom window");
        selectedIndex = EditorGUILayout.Popup("Auswahl:", selectedIndex, options);

        selectedObject = (GameObject)EditorGUILayout.ObjectField(
          "GameObject:",
          selectedObject,
          typeof(GameObject),
          true  // true = Scene-Objekte erlauben
      );


        // Licht Einstellungen
        lightIntensity = EditorGUILayout.Slider("Intensität:", lightIntensity, 0f, 8f);
        lightColor = EditorGUILayout.ColorField("Farbe:", lightColor);

        GUILayout.Space(20);

        sliderDistance = EditorGUILayout.Slider("Distanz:", sliderDistance, 0f, 100f);
        horizontalAngle = EditorGUILayout.Slider("Horizontal:", horizontalAngle, 0f, 360f);
        verticalAngle = EditorGUILayout.Slider("Vertikal:", verticalAngle, 0f, 90f);

        if (GUILayout.Button("Licht erstellen", GUILayout.Height(40)))
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
        Light light = lightGO.AddComponent<Light>();
        light.type = (LightType)selectedIndex;
        light.intensity = lightIntensity;
        light.color = lightColor;

        lightGO.transform.position = lightPos;
        lightGO.transform.LookAt(selectedObject.transform.position);  // Zum Objekt schauen

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



}

