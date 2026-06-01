using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.Linq;

public class LightDebuggerWindow : EditorWindow
{
    private const int FpsSampleCount = 150;
    private readonly List<float> fpsHistory = new List<float>(FpsSampleCount);

    private Vector2 mainScroll;
    private Vector2 heatmapScroll;
    private Vector2 lightListScroll;

    private bool showLightPopup;

    private Light[] cachedLights;

    [MenuItem("Window/Light Debugger")]
    public static void ShowWindow()
    {
        GetWindow<LightDebuggerWindow>("Light Debugger");
    }

    private void OnGUI()
    {
        GUI.depth = 0; // ensures normal UI draws first

        cachedLights = Resources.FindObjectsOfTypeAll<Light>();

        mainScroll = EditorGUILayout.BeginScrollView(mainScroll);

        // TOP AREA – 2 COLUMNS
        EditorGUILayout.BeginHorizontal();

        // Left: Light Stats
        EditorGUILayout.BeginVertical("box", GUILayout.Width(position.width * 0.48f));
        DrawLightStats(cachedLights);
        EditorGUILayout.EndVertical();

        // Right: Light Quality Analyzer
        EditorGUILayout.BeginVertical("box", GUILayout.Width(position.width * 0.48f));
        DrawLightQualityAnalyzer(cachedLights);
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);

        // BOTTOM AREA – FPS Graph + Heatmap
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.BeginVertical("box", GUILayout.Width(position.width * 0.55f), GUILayout.Height(position.height * 0.4f));
        DrawPerformanceAndFpsGraph();
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical("box", GUILayout.Width(position.width * 0.4f), GUILayout.Height(position.height * 0.4f));
        DrawExpensiveLightsHeatmap(cachedLights);
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndScrollView();

        // Draw popup LAST so it overlays everything
        if (showLightPopup)
        {
            GUI.depth = -100; // popup always on top
            DrawLightPopup(cachedLights);
        }

        Repaint();
    }

    // ---------------------------------------------------------
    //  LIGHT STATS
    // ---------------------------------------------------------
    private void DrawLightStats(Light[] lights)
    {
        GUILayout.Label("Light Statistics", EditorStyles.boldLabel);

        int enabled = lights.Count(l => l.enabled && l.gameObject.activeInHierarchy);
        int disabled = lights.Length - enabled;

        int directional = lights.Count(l => l.type == LightType.Directional);
        int point = lights.Count(l => l.type == LightType.Point);
        int spot = lights.Count(l => l.type == LightType.Spot);
        int area = lights.Count(l => l.type == LightType.Rectangle);

        EditorGUILayout.LabelField("Total Lights", lights.Length.ToString());
        EditorGUILayout.LabelField("Enabled", enabled.ToString());
        EditorGUILayout.LabelField("Disabled", disabled.ToString());

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Directional", directional.ToString());
        EditorGUILayout.LabelField("Point", point.ToString());
        EditorGUILayout.LabelField("Spot", spot.ToString());
        EditorGUILayout.LabelField("Area", area.ToString());

        EditorGUILayout.Space();

        if (GUILayout.Button("Show Lights List"))
        {
            showLightPopup = !showLightPopup;
        }
    }

    // ---------------------------------------------------------
    //  LIGHT QUALITY ANALYZER
    // ---------------------------------------------------------
    private void DrawLightQualityAnalyzer(Light[] lights)
    {
        GUILayout.Label("Light Quality Analyzer", EditorStyles.boldLabel);

        int realtime = lights.Count(l => l.lightmapBakeType == LightmapBakeType.Realtime);
        int mixed = lights.Count(l => l.lightmapBakeType == LightmapBakeType.Mixed);
        int baked = lights.Count(l => l.lightmapBakeType == LightmapBakeType.Baked);

        int shadowCasters = lights.Count(l => l.shadows != LightShadows.None);

        // FIX: Shadow resolution fallback
        int highShadowRes = lights.Count(l => GetShadowRes(l) == LightShadowResolution.VeryHigh);
        int mediumShadowRes = lights.Count(l => GetShadowRes(l) == LightShadowResolution.High);
        int lowShadowRes = lights.Count(l => GetShadowRes(l) == LightShadowResolution.Medium);

        // FIX: Expensive lights based on cost
        int expensiveLights = lights.Count(l => CalculateLightCost(l) > 40f);

        EditorGUILayout.LabelField("Realtime Lights", realtime.ToString());
        EditorGUILayout.LabelField("Mixed Lights", mixed.ToString());
        EditorGUILayout.LabelField("Baked Lights", baked.ToString());

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Shadow Casters", shadowCasters.ToString());
        EditorGUILayout.LabelField("Shadow Res High", highShadowRes.ToString());
        EditorGUILayout.LabelField("Shadow Res Medium", mediumShadowRes.ToString());
        EditorGUILayout.LabelField("Shadow Res Low", lowShadowRes.ToString());

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Expensive Lights", expensiveLights.ToString());

        EditorGUILayout.Space(6);

        float score = CalculateLightQualityScore(lights, realtime, shadowCasters, highShadowRes, expensiveLights);
        DrawQualityScoreBar(score);

        DrawLightWarnings(score, realtime, shadowCasters, expensiveLights);
    }

    private LightShadowResolution GetShadowRes(Light l)
{
    if (l.shadowResolution == LightShadowResolution.FromQualitySettings)
        return ConvertShadowRes(QualitySettings.shadowResolution);

    return l.shadowResolution;
}

private LightShadowResolution ConvertShadowRes(ShadowResolution res)
{
    switch (res)
    {
        case ShadowResolution.Low:
            return LightShadowResolution.Low;
        case ShadowResolution.Medium:
            return LightShadowResolution.Medium;
        case ShadowResolution.High:
            return LightShadowResolution.High;
        case ShadowResolution.VeryHigh:
            return LightShadowResolution.VeryHigh;
        default:
            return LightShadowResolution.Medium;
    }
}
    private float CalculateLightQualityScore(Light[] lights, int realtime, int shadowCasters, int highShadowRes, int expensiveLights)
    {
        if (lights.Length == 0) return 100f;

        float score = 100f;

        score -= Mathf.Clamp01(realtime / 8f) * 30f;
        score -= Mathf.Clamp01(shadowCasters / 12f) * 30f;
        score -= Mathf.Clamp01(highShadowRes / 4f) * 20f;
        score -= Mathf.Clamp01(expensiveLights / 6f) * 20f;

        return Mathf.Clamp(score, 0f, 100f);
    }

    private void DrawQualityScoreBar(float score)
    {
        GUILayout.Label("Light Quality Score: " + score.ToString("F0") + " / 100");

        Rect rect = GUILayoutUtility.GetRect(100, 18);
        EditorGUI.DrawRect(rect, Color.grey * 0.6f);

        float t = score / 100f;
        Color col = Color.Lerp(Color.red, Color.green, t);
        Rect fill = new Rect(rect.x, rect.y, rect.width * t, rect.height);
        EditorGUI.DrawRect(fill, col);
    }

    private void DrawLightWarnings(float score, int realtime, int shadowCasters, int expensiveLights)
    {
        if (score > 75f)
            EditorGUILayout.HelpBox("Lighting ist in einem sehr guten Zustand.", MessageType.Info);
        else if (score > 45f)
            EditorGUILayout.HelpBox("Lighting ist okay, aber es gibt Optimierungspotenzial.", MessageType.Warning);
        else
            EditorGUILayout.HelpBox("Lighting ist teuer – du solltest optimieren.", MessageType.Error);

        if (realtime > 4)
            EditorGUILayout.HelpBox("Viele Realtime-Lichter – reduziere auf 2–4.", MessageType.Warning);

        if (shadowCasters > 8)
            EditorGUILayout.HelpBox("Viele Shadow-Caster – prüfe, ob alle Schatten brauchen.", MessageType.Warning);

        if (expensiveLights > 3)
            EditorGUILayout.HelpBox("Mehrere teure Lichter mit hoher Range/Intensity.", MessageType.Warning);
    }

    // ---------------------------------------------------------
    //  PERFORMANCE + FPS GRAPH
    // ---------------------------------------------------------
    private void DrawPerformanceAndFpsGraph()
    {
        GUILayout.Label("Performance & FPS", EditorStyles.boldLabel);

        float fps = 1f / Mathf.Max(Time.smoothDeltaTime, 0.0001f);
        fpsHistory.Add(fps);
        if (fpsHistory.Count > FpsSampleCount)
            fpsHistory.RemoveAt(0);

        EditorGUILayout.LabelField("FPS", fps.ToString("F1"));
        EditorGUILayout.LabelField("Frame Time (ms)", (Time.smoothDeltaTime * 1000f).ToString("F2"));

        EditorGUILayout.Space(4);

        int drawCalls = UnityEditor.UnityStats.drawCalls;
        int batches = UnityEditor.UnityStats.batches;
        int tris = UnityEditor.UnityStats.triangles;

        EditorGUILayout.LabelField("Draw Calls", drawCalls.ToString());
        EditorGUILayout.LabelField("Batches", batches.ToString());
        EditorGUILayout.LabelField("Triangles", tris.ToString());

        EditorGUILayout.Space(6);

        GUILayout.Label("FPS Graph", EditorStyles.miniBoldLabel);
        Rect graphRect = GUILayoutUtility.GetRect(10, 80, GUILayout.ExpandWidth(true));
        DrawFpsGraph(graphRect);
    }

    private void DrawFpsGraph(Rect rect)
    {
        EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f));

        if (fpsHistory.Count < 2)
            return;

        float maxFps = Mathf.Max(30f, fpsHistory.Max());
        float minFps = 0f;

        Handles.BeginGUI();
        Handles.color = Color.Lerp(Color.red, Color.green, Mathf.Clamp01(fpsHistory.Last() / 60f));

        for (int i = 1; i < fpsHistory.Count; i++)
        {
            float x0 = Mathf.Lerp(rect.x, rect.xMax, (i - 1) / (float)(FpsSampleCount - 1));
            float x1 = Mathf.Lerp(rect.x, rect.xMax, i / (float)(FpsSampleCount - 1));

            float y0 = Mathf.Lerp(rect.yMax, rect.y, Mathf.InverseLerp(minFps, maxFps, fpsHistory[i - 1]));
            float y1 = Mathf.Lerp(rect.yMax, rect.y, Mathf.InverseLerp(minFps, maxFps, fpsHistory[i]));

            Handles.DrawLine(new Vector3(x0, y0), new Vector3(x1, y1));
        }

        Handles.EndGUI();
    }

    // ---------------------------------------------------------
    //  HEATMAP
    // ---------------------------------------------------------
    private void DrawExpensiveLightsHeatmap(Light[] lights)
    {
        GUILayout.Label("Expensive Lights Heatmap", EditorStyles.boldLabel);

        var ranked = lights
            .Select(l => new { light = l, cost = CalculateLightCost(l) })
            .Where(x => x.cost > 0f)
            .OrderByDescending(x => x.cost)
            .ToList();

        if (ranked.Count == 0)
        {
            EditorGUILayout.LabelField("Keine teuren Lichter gefunden.");
            return;
        }

        heatmapScroll = EditorGUILayout.BeginScrollView(heatmapScroll);

        foreach (var entry in ranked)
        {
            float t = Mathf.Clamp01(entry.cost / 100f);
            Color col = Color.Lerp(Color.green, Color.red, t);

            Rect rowRect = EditorGUILayout.BeginHorizontal();
            EditorGUI.DrawRect(new Rect(rowRect.x, rowRect.y, 6, rowRect.height), col);

            GUILayout.Space(8);
            EditorGUILayout.LabelField(entry.light.name, GUILayout.Width(140));
            EditorGUILayout.LabelField(entry.light.type.ToString(), GUILayout.Width(70));
            EditorGUILayout.LabelField("Cost: " + entry.cost.ToString("F0"), GUILayout.Width(80));

            if (GUILayout.Button("Select", GUILayout.Width(60)))
            {
                Selection.activeObject = entry.light.gameObject;
                EditorGUIUtility.PingObject(entry.light.gameObject);
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }

    private float CalculateLightCost(Light l)
    {
        float cost = 0f;

        if (l.shadows != LightShadows.None) cost += 30f;
        if (l.shadows == LightShadows.Soft) cost += 10f;

        if (l.type == LightType.Spot) cost += 15f;
        if (l.type == LightType.Point) cost += 10f;
        if (l.type == LightType.Directional) cost += 5f;

        cost += Mathf.Clamp01((l.range - 10f) / 30f) * 20f;
        cost += Mathf.Clamp01((l.intensity - 1f) / 3f) * 15f;

        if (GetShadowRes(l) == LightShadowResolution.VeryHigh) cost += 20f;
        if (GetShadowRes(l) == LightShadowResolution.High) cost += 10f;

        return cost;
    }

    // ---------------------------------------------------------
    //  POPUP WINDOW
    // ---------------------------------------------------------
    private void DrawLightPopup(Light[] lights)
    {
        Rect r = new Rect(20, 40, position.width - 40, position.height - 80);

        // Background box
        GUI.Box(r, GUIContent.none);

        // Window
        GUI.Window(123456, r, id => PopupWindow(id, lights), "Lights");
    }

    private void PopupWindow(int id, Light[] lights)
    {
        GUILayout.Label("All Lights", EditorStyles.boldLabel);

        lightListScroll = GUILayout.BeginScrollView(lightListScroll);

        foreach (var l in lights)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(l.name, GUILayout.Width(160));
            EditorGUILayout.LabelField(l.type.ToString(), GUILayout.Width(70));
            EditorGUILayout.LabelField(l.enabled ? "On" : "Off", GUILayout.Width(40));

            if (GUILayout.Button("Select", GUILayout.Width(60)))
            {
                Selection.activeObject = l.gameObject;
                EditorGUIUtility.PingObject(l.gameObject);
            }

            EditorGUILayout.EndHorizontal();
        }

        GUILayout.EndScrollView();

        if (GUILayout.Button("Close"))
        {
            showLightPopup = false;
        }

        GUI.DragWindow();
    }
}
