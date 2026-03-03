using UnityEngine;

[System.Serializable]
public class LightData
{
    public string name;

    // Light settings
    public LightType type;
    public float intensity;
    public Color color;
    public float range;       // "distance" -> Unity heißt das range
    public float spotAngle;   // für Spotlights relevant

    // Transform
    public Vector3 localPosition;
    public Vector3 localEulerAngles;

}
