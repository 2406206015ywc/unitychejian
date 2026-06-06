using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class DisturbanceEventManager : MonoBehaviour
{
    public string playbackDirectory = "C:/Users/ywc/Desktop/codex/matlab_workshop_model/output/unity_export_v2/simevents_stateflow_finaltransport_4m1agv";
    public string agvObjectName = "AGV_01";
    public Vector3 machineMarkerOffset = new Vector3(0f, 3.35f, 0f);
    public Vector3 agvMarkerOffset = new Vector3(0f, 1.55f, 0f);
    public Vector3 globalMarkerPosition = new Vector3(0f, 4.2f, -1.8f);
    public float markerScale = 0.012f;
    public float zeroDurationHoldSeconds = 5f;
    public bool showAgvWorldMarkers = false;

    private readonly List<DisturbanceRow> disturbances = new List<DisturbanceRow>();
    private readonly Dictionary<string, Transform> targetsById = new Dictionary<string, Transform>();
    private readonly Dictionary<string, MarkerView> markersById = new Dictionary<string, MarkerView>();

    private Transform markerRoot;
    private bool hasLoaded;

    public int ActiveEventCount { get; private set; }
    public string CurrentEventSummary { get; private set; } = "";

    public void LoadDisturbances()
    {
        disturbances.Clear();
        CacheSceneObjects();

        string path = Path.Combine(playbackDirectory, "disturbance_markers.csv");
        if (!File.Exists(path))
        {
            Debug.LogError("[DisturbanceEventManager] Missing disturbance markers: " + path);
            return;
        }

        foreach (Dictionary<string, string> row in ReadCsv(path))
        {
            DisturbanceRow disturbance = new DisturbanceRow();
            disturbance.markerId = Get(row, "marker_id");
            disturbance.disturbanceType = Get(row, "disturbance_type");
            disturbance.targetId = Get(row, "target_id");
            disturbance.orderId = Get(row, "order_id");
            disturbance.partId = Get(row, "part_id");
            disturbance.startTime = GetFloat(row, "start_time");
            disturbance.endTime = GetFloat(row, "end_time");
            disturbance.effect = Get(row, "effect");

            if (!string.IsNullOrWhiteSpace(disturbance.markerId))
            {
                disturbances.Add(disturbance);
            }
        }

        disturbances.Sort((a, b) => a.startTime.CompareTo(b.startTime));
        EnsureMarkerPool();
        hasLoaded = true;
        Debug.Log("[DisturbanceEventManager] Loaded " + disturbances.Count + " disturbance markers.");
    }

    public void ApplyPlaybackTime(float time)
    {
        if (!hasLoaded)
        {
            LoadDisturbances();
        }

        ActiveEventCount = 0;
        CurrentEventSummary = "";
        int slot = 0;

        foreach (DisturbanceRow disturbance in disturbances)
        {
            MarkerView marker;
            if (!markersById.TryGetValue(disturbance.markerId, out marker))
            {
                continue;
            }

            bool active = IsActive(disturbance, time);
            if (!active)
            {
                marker.root.SetActive(false);
                continue;
            }

            ActiveEventCount++;
            if (string.IsNullOrEmpty(CurrentEventSummary))
            {
                CurrentEventSummary = BuildCompactSummary(disturbance);
            }

            bool showWorldMarker = ShouldShowWorldMarker(disturbance);
            marker.root.SetActive(showWorldMarker);
            if (showWorldMarker)
            {
                Vector3 position = ResolveMarkerPosition(disturbance, slot);
                marker.root.transform.position = position;
                Camera camera = Camera.main;
                if (camera != null)
                {
                    marker.root.transform.rotation = camera.transform.rotation;
                }

                marker.colorBlock.color = GetTypeColor(disturbance.disturbanceType);
                marker.text.text = BuildDisplayText(disturbance);
                slot++;
            }
        }

        if (ActiveEventCount > 1)
        {
            CurrentEventSummary += " +" + (ActiveEventCount - 1).ToString(CultureInfo.InvariantCulture);
        }
    }

    public string GetCurrentDisturbanceForTarget(string targetId, float time)
    {
        if (!hasLoaded)
        {
            LoadDisturbances();
        }

        if (string.IsNullOrWhiteSpace(targetId))
        {
            return "";
        }

        foreach (DisturbanceRow disturbance in disturbances)
        {
            if (disturbance.targetId == targetId && IsActive(disturbance, time))
            {
                return TranslateType(disturbance.disturbanceType);
            }
        }

        return "";
    }

    private void Awake()
    {
        CacheSceneObjects();
    }

    private void CacheSceneObjects()
    {
        GameObject rootObject = GameObject.Find("Disturbance_Event_Markers");
        if (rootObject == null)
        {
            rootObject = new GameObject("Disturbance_Event_Markers");
        }
        markerRoot = rootObject.transform;

        targetsById.Clear();
        GameObject agv = GameObject.Find(agvObjectName);
        if (agv != null)
        {
            targetsById[agvObjectName] = agv.transform;
        }

        WorkshopResourceIdentity[] identities = FindObjectsOfType<WorkshopResourceIdentity>(true);
        foreach (WorkshopResourceIdentity identity in identities)
        {
            if (identity != null && !string.IsNullOrWhiteSpace(identity.resourceId))
            {
                targetsById[identity.resourceId] = identity.transform;
            }
        }
    }

    private void EnsureMarkerPool()
    {
        foreach (DisturbanceRow disturbance in disturbances)
        {
            if (markersById.ContainsKey(disturbance.markerId))
            {
                continue;
            }

            Transform existing = markerRoot != null ? markerRoot.Find("Marker_" + disturbance.markerId) : null;
            MarkerView marker = existing != null ? ReadMarker(existing.gameObject) : CreateMarker(disturbance.markerId);
            markersById[disturbance.markerId] = marker;
            marker.root.SetActive(false);
        }
    }

    private MarkerView CreateMarker(string markerId)
    {
        GameObject root = new GameObject("Marker_" + markerId);
        if (markerRoot != null)
        {
            root.transform.SetParent(markerRoot, false);
        }

        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 20f;
        root.AddComponent<GraphicRaycaster>();

        RectTransform canvasRect = root.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(230f, 56f);
        root.transform.localScale = Vector3.one * markerScale;

        GameObject panelObject = new GameObject("Panel");
        panelObject.transform.SetParent(root.transform, false);
        Image panel = panelObject.AddComponent<Image>();
        panel.color = new Color(0.06f, 0.07f, 0.08f, 0.88f);
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        GameObject colorObject = new GameObject("ColorBlock");
        colorObject.transform.SetParent(panelObject.transform, false);
        Image colorBlock = colorObject.AddComponent<Image>();
        RectTransform colorRect = colorObject.GetComponent<RectTransform>();
        colorRect.anchorMin = new Vector2(0f, 0.5f);
        colorRect.anchorMax = new Vector2(0f, 0.5f);
        colorRect.pivot = new Vector2(0f, 0.5f);
        colorRect.anchoredPosition = new Vector2(14f, 0f);
        colorRect.sizeDelta = new Vector2(18f, 18f);

        GameObject textObject = new GameObject("Text");
        textObject.transform.SetParent(panelObject.transform, false);
        Text text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 20;
        text.alignment = TextAnchor.MiddleLeft;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.offsetMin = new Vector2(42f, 0f);
        textRect.offsetMax = new Vector2(-10f, 0f);

        MarkerView marker = new MarkerView();
        marker.root = root;
        marker.colorBlock = colorBlock;
        marker.text = text;
        return marker;
    }

    private MarkerView ReadMarker(GameObject root)
    {
        MarkerView marker = new MarkerView();
        marker.root = root;
        marker.colorBlock = root.transform.Find("Panel/ColorBlock").GetComponent<Image>();
        marker.text = root.transform.Find("Panel/Text").GetComponent<Text>();
        return marker;
    }

    private bool IsActive(DisturbanceRow disturbance, float time)
    {
        if (disturbance.endTime > disturbance.startTime)
        {
            return time >= disturbance.startTime && time < disturbance.endTime;
        }

        return time >= disturbance.startTime && time < disturbance.startTime + zeroDurationHoldSeconds;
    }

    private bool ShouldShowWorldMarker(DisturbanceRow disturbance)
    {
        if (!showAgvWorldMarkers && disturbance.targetId == agvObjectName)
        {
            return false;
        }

        return true;
    }

    private Vector3 ResolveMarkerPosition(DisturbanceRow disturbance, int slot)
    {
        Transform target;
        Vector3 stackedOffset = Vector3.up * slot * 0.46f;
        if (!string.IsNullOrWhiteSpace(disturbance.targetId) && targetsById.TryGetValue(disturbance.targetId, out target) && target != null)
        {
            if (disturbance.targetId == agvObjectName)
            {
                return target.position + agvMarkerOffset + stackedOffset;
            }
            return target.position + machineMarkerOffset + stackedOffset;
        }

        return globalMarkerPosition + stackedOffset;
    }

    private string BuildDisplayText(DisturbanceRow disturbance)
    {
        string text = TranslateType(disturbance.disturbanceType);
        if (!string.IsNullOrWhiteSpace(disturbance.targetId))
        {
            text += " " + disturbance.targetId;
        }
        if (!string.IsNullOrWhiteSpace(disturbance.orderId))
        {
            text += "  \u8ba2\u5355" + disturbance.orderId;
        }
        return text;
    }

    private string BuildCompactSummary(DisturbanceRow disturbance)
    {
        string summary = TranslateType(disturbance.disturbanceType);
        if (!string.IsNullOrWhiteSpace(disturbance.targetId))
        {
            summary += " " + disturbance.targetId;
        }
        return summary;
    }

    public static string TranslateType(string disturbanceType)
    {
        switch (disturbanceType)
        {
            case "agv_delay":
                return "AGV\u5ef6\u8fdf";
            case "tool_wear":
                return "\u5200\u5177\u78e8\u635f";
            case "machine_down":
                return "\u673a\u5e8a\u505c\u673a";
            case "machine_recover":
                return "\u673a\u5e8a\u6062\u590d";
            default:
                return string.IsNullOrWhiteSpace(disturbanceType) ? "\u6270\u52a8\u4e8b\u4ef6" : disturbanceType;
        }
    }

    private static Color GetTypeColor(string disturbanceType)
    {
        switch (disturbanceType)
        {
            case "agv_delay":
                return new Color(1f, 0.55f, 0.16f, 1f);
            case "tool_wear":
                return new Color(1f, 0.83f, 0.18f, 1f);
            case "machine_down":
                return new Color(0.95f, 0.16f, 0.12f, 1f);
            case "machine_recover":
                return new Color(0.18f, 0.75f, 0.32f, 1f);
            default:
                return new Color(0.25f, 0.55f, 1f, 1f);
        }
    }

    private static IEnumerable<Dictionary<string, string>> ReadCsv(string path)
    {
        string[] lines = File.ReadAllLines(path);
        if (lines.Length < 2)
        {
            yield break;
        }

        List<string> headers = SplitCsvLine(lines[0]);
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
            {
                continue;
            }

            List<string> values = SplitCsvLine(lines[i]);
            Dictionary<string, string> row = new Dictionary<string, string>();
            for (int j = 0; j < headers.Count; j++)
            {
                row[headers[j]] = j < values.Count ? values[j] : "";
            }
            yield return row;
        }
    }

    private static List<string> SplitCsvLine(string line)
    {
        List<string> result = new List<string>();
        bool inQuotes = false;
        string current = "";

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current += '"';
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current);
                current = "";
            }
            else
            {
                current += c;
            }
        }

        result.Add(current);
        return result;
    }

    private static string Get(Dictionary<string, string> row, string key)
    {
        string value;
        return row.TryGetValue(key, out value) ? value : "";
    }

    private static float GetFloat(Dictionary<string, string> row, string key)
    {
        string value = Get(row, key);
        float result;
        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
        {
            return result;
        }
        return 0f;
    }

    private class DisturbanceRow
    {
        public string markerId;
        public string disturbanceType;
        public string targetId;
        public string orderId;
        public string partId;
        public float startTime;
        public float endTime;
        public string effect;
    }

    private class MarkerView
    {
        public GameObject root;
        public Image colorBlock;
        public Text text;
    }
}
