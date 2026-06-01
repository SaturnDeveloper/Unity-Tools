using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Lighting/LightGroupDatabase")]
public class LightGroupDatabase : ScriptableObject
{
    public List<string> groups = new List<string>();
}
