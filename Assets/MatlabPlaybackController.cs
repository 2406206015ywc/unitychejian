using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

public class MatlabPlaybackController : MonoBehaviour
{
    public string playbackDirectory = "C:/Users/ywc/Desktop/codex/matlab_workshop_model/output/unity_export_v2/simevents_stateflow_finaltransport_4m1agv";
    public string agvObjectName = "AGV_01";
    public bool playOnStart = false;
    public bool useUnitySceneNodes = true;
    public bool useAisleRouting = true;
    public bool showDebugOnGui = false;
    public float playbackSpeed = 1f;
    public float playbackTime = 0f;
    public OrderVisualManager orderVisualManager;
    public DisturbanceEventManager disturbanceEventManager;

    private readonly List<AgvMotionRow> agvMotions = new List<AgvMotionRow>();
    private readonly List<ResourceStateRow> resourceStates = new List<ResourceStateRow>();
    private readonly Dictionary<string, WorkshopResourceIdentity> resourcesById = new Dictionary<string, WorkshopResourceIdentity>();
    private readonly Dictionary<string, Transform> nodesById = new Dictionary<string, Transform>();

    private GameObject agvObject;
    private bool isPlaying;
    private float makespan;
    private string loadStatus = "Not loaded";

    public bool IsPlaying
    {
        get { return isPlaying; }
    }

    public float Makespan
    {
        get { return makespan; }
    }

    public string LoadStatus
    {
        get { return loadStatus; }
    }

    private void Awake()
    {
        LoadPlaybackPackage();
        ResetPlayback();
        isPlaying = playOnStart;
    }

    private void Update()
    {
        if (isPlaying)
        {
            playbackTime += Time.deltaTime * playbackSpeed;
            if (playbackTime >= makespan)
            {
                playbackTime = makespan;
                isPlaying = false;
            }
        }

        ApplyPlaybackTime(playbackTime);
    }

    public void LoadPlaybackPackage()
    {
        agvMotions.Clear();
        resourceStates.Clear();
        resourcesById.Clear();
        nodesById.Clear();

        CacheSceneObjects();

        string motionPath = Path.Combine(playbackDirectory, "agv_motion_timeline.csv");
        string resourcePath = Path.Combine(playbackDirectory, "resource_state_timeline.csv");

        if (!File.Exists(motionPath) || !File.Exists(resourcePath))
        {
            loadStatus = "Missing playback files";
            Debug.LogError("[MatlabPlaybackController] Missing playback files in " + playbackDirectory);
            return;
        }

        LoadAgvMotions(motionPath);
        LoadResourceStates(resourcePath);
        if (orderVisualManager != null)
        {
            orderVisualManager.playbackDirectory = playbackDirectory;
            orderVisualManager.agvObjectName = agvObjectName;
            orderVisualManager.LoadOrderTimeline();
        }
        if (disturbanceEventManager != null)
        {
            disturbanceEventManager.playbackDirectory = playbackDirectory;
            disturbanceEventManager.agvObjectName = agvObjectName;
            disturbanceEventManager.LoadDisturbances();
        }
        makespan = CalculateMakespan();
        loadStatus = string.Format(CultureInfo.InvariantCulture, "Loaded: {0} AGV rows, {1} resource rows", agvMotions.Count, resourceStates.Count);
        Debug.Log("[MatlabPlaybackController] " + loadStatus);
    }

    public void ResetPlayback()
    {
        playbackTime = 0f;
        isPlaying = false;
        ApplyPlaybackTime(playbackTime);
    }

    public void Play()
    {
        if (playbackTime >= makespan)
        {
            playbackTime = 0f;
        }
        isPlaying = true;
    }

    public void Pause()
    {
        isPlaying = false;
    }

    public void SetPlaybackTime(float time)
    {
        playbackTime = Mathf.Clamp(time, 0f, makespan);
        ApplyPlaybackTime(playbackTime);
    }

    public void SetPlaybackSpeed(float speed)
    {
        playbackSpeed = Mathf.Clamp(speed, 0.1f, 10f);
    }

    public string GetCurrentDisturbanceSummary()
    {
        if (disturbanceEventManager == null || string.IsNullOrEmpty(disturbanceEventManager.CurrentEventSummary))
        {
            return "\u65e0";
        }

        return disturbanceEventManager.CurrentEventSummary;
    }

    public int GetActiveDisturbanceCount()
    {
        return disturbanceEventManager != null ? disturbanceEventManager.ActiveEventCount : 0;
    }

    public string GetCurrentResourceState(string resourceId)
    {
        if (string.IsNullOrWhiteSpace(resourceId))
        {
            return "";
        }

        string state = FindResourceState(resourceId, playbackTime);
        if (!string.IsNullOrWhiteSpace(state))
        {
            return state;
        }

        WorkshopResourceIdentity identity;
        if (resourcesById.TryGetValue(resourceId, out identity) && identity != null)
        {
            return identity.initialState;
        }

        return "";
    }

    public bool TryGetCurrentAgvTask(out AgvTaskInfo task)
    {
        AgvMotionRow active = FindActiveAgvMotion(playbackTime);
        if (active == null)
        {
            task = null;
            return false;
        }

        string partId = orderVisualManager != null ? orderVisualManager.GetPartId(active.orderId) : "";
        float progress = Mathf.Clamp01((playbackTime - active.startTime) / Mathf.Max(0.0001f, active.duration));
        task = new AgvTaskInfo(active.taskId, active.agvId, active.orderId, partId, active.fromNode, active.toNode, active.startTime, active.endTime, progress);
        return true;
    }

    public AgvTaskInfo GetLastOrCurrentAgvTask()
    {
        AgvMotionRow selected = FindActiveAgvMotion(playbackTime);
        if (selected == null)
        {
            foreach (AgvMotionRow row in agvMotions)
            {
                if (row.agvId != agvObjectName)
                {
                    continue;
                }

                if (playbackTime >= row.endTime)
                {
                    selected = row;
                }
                else
                {
                    break;
                }
            }
        }

        if (selected == null)
        {
            return null;
        }

        string partId = orderVisualManager != null ? orderVisualManager.GetPartId(selected.orderId) : "";
        float progress = playbackTime >= selected.endTime ? 1f : Mathf.Clamp01((playbackTime - selected.startTime) / Mathf.Max(0.0001f, selected.duration));
        return new AgvTaskInfo(selected.taskId, selected.agvId, selected.orderId, partId, selected.fromNode, selected.toNode, selected.startTime, selected.endTime, progress);
    }

    public void ApplyPlaybackTime(float time)
    {
        ApplyAgvMotion(time);
        ApplyResourceStates(time);
        if (orderVisualManager != null)
        {
            orderVisualManager.ApplyPlaybackTime(time);
        }
        if (disturbanceEventManager != null)
        {
            disturbanceEventManager.ApplyPlaybackTime(time);
        }
    }

    private void CacheSceneObjects()
    {
        agvObject = GameObject.Find(agvObjectName);
        if (agvObject != null)
        {
            ManualAgvPathMover manualMover = agvObject.GetComponent<ManualAgvPathMover>();
            if (manualMover != null)
            {
                manualMover.enabled = false;
            }
        }

        if (orderVisualManager == null)
        {
            orderVisualManager = FindObjectOfType<OrderVisualManager>(true);
        }
        if (disturbanceEventManager == null)
        {
            disturbanceEventManager = FindObjectOfType<DisturbanceEventManager>(true);
        }

        WorkshopResourceIdentity[] identities = FindObjectsOfType<WorkshopResourceIdentity>(true);
        foreach (WorkshopResourceIdentity identity in identities)
        {
            if (identity != null && !string.IsNullOrWhiteSpace(identity.resourceId))
            {
                resourcesById[identity.resourceId] = identity;
            }
        }

        Transform[] transforms = FindObjectsOfType<Transform>(true);
        foreach (Transform item in transforms)
        {
            if (item == null || !item.name.StartsWith("Node_", StringComparison.Ordinal))
            {
                continue;
            }

            string nodeId = item.name.Substring("Node_".Length);
            nodesById[nodeId] = item;
        }
    }

    private void LoadAgvMotions(string path)
    {
        foreach (Dictionary<string, string> row in ReadCsv(path))
        {
            AgvMotionRow motion = new AgvMotionRow();
            motion.taskId = Get(row, "task_id");
            motion.agvId = Get(row, "agv_id");
            motion.orderId = Get(row, "order_id");
            motion.fromNode = Get(row, "from_node");
            motion.toNode = Get(row, "to_node");
            motion.startTime = GetFloat(row, "start_time");
            motion.endTime = GetFloat(row, "end_time");
            motion.duration = Mathf.Max(0.0001f, GetFloat(row, "duration"));
            motion.fromPosition = new Vector3(GetFloat(row, "from_x"), GetFloat(row, "from_y"), GetFloat(row, "from_z"));
            motion.toPosition = new Vector3(GetFloat(row, "to_x"), GetFloat(row, "to_y"), GetFloat(row, "to_z"));
            agvMotions.Add(motion);
        }

        agvMotions.Sort((a, b) => a.startTime.CompareTo(b.startTime));
    }

    private void LoadResourceStates(string path)
    {
        foreach (Dictionary<string, string> row in ReadCsv(path))
        {
            ResourceStateRow stateRow = new ResourceStateRow();
            stateRow.resourceId = Get(row, "resource_id");
            stateRow.state = Get(row, "state");
            stateRow.startTime = GetFloat(row, "start_time");
            stateRow.endTime = GetFloat(row, "end_time");
            resourceStates.Add(stateRow);
        }

        resourceStates.Sort((a, b) => a.startTime.CompareTo(b.startTime));
    }

    private void ApplyAgvMotion(float time)
    {
        if (agvObject == null || agvMotions.Count == 0)
        {
            return;
        }

        AgvMotionRow active = null;
        AgvMotionRow lastCompleted = null;
        AgvMotionRow firstFuture = null;

        foreach (AgvMotionRow row in agvMotions)
        {
            if (row.agvId != agvObjectName)
            {
                continue;
            }

            if (time >= row.startTime && time <= row.endTime)
            {
                active = row;
                break;
            }

            if (time > row.endTime)
            {
                lastCompleted = row;
            }
            else if (time < row.startTime && firstFuture == null)
            {
                firstFuture = row;
            }
        }

        if (active != null)
        {
            float alpha = Mathf.Clamp01((time - active.startTime) / Mathf.Max(0.0001f, active.duration));
            Vector3 nextPosition = EvaluateMotionPosition(active, alpha);
            Vector3 moveVector = nextPosition - agvObject.transform.position;
            agvObject.transform.position = nextPosition;
            if (moveVector.sqrMagnitude > 0.0001f)
            {
                agvObject.transform.rotation = Quaternion.LookRotation(moveVector.normalized, Vector3.up);
            }
        }
        else if (lastCompleted != null)
        {
            agvObject.transform.position = ResolveNodePosition(lastCompleted.toNode, lastCompleted.toPosition);
        }
        else if (firstFuture != null)
        {
            agvObject.transform.position = ResolveNodePosition(firstFuture.fromNode, firstFuture.fromPosition);
        }
    }

    private AgvMotionRow FindActiveAgvMotion(float time)
    {
        foreach (AgvMotionRow row in agvMotions)
        {
            if (row.agvId != agvObjectName)
            {
                continue;
            }

            if (time >= row.startTime && time <= row.endTime)
            {
                return row;
            }
        }

        return null;
    }

    private void ApplyResourceStates(float time)
    {
        foreach (KeyValuePair<string, WorkshopResourceIdentity> pair in resourcesById)
        {
            string state = FindResourceState(pair.Key, time);
            if (!string.IsNullOrWhiteSpace(state))
            {
                pair.Value.SetState(state);
            }
        }
    }

    private string FindResourceState(string resourceId, float time)
    {
        string fallback = null;
        foreach (ResourceStateRow row in resourceStates)
        {
            if (row.resourceId != resourceId)
            {
                continue;
            }

            if (time >= row.startTime && time <= row.endTime)
            {
                return row.state;
            }

            if (time > row.endTime)
            {
                fallback = row.state;
            }
        }

        return fallback;
    }

    private Vector3 ResolveNodePosition(string nodeId, Vector3 csvPosition)
    {
        if (useUnitySceneNodes && nodesById.TryGetValue(nodeId, out Transform node))
        {
            return node.position;
        }

        return csvPosition;
    }

    private Vector3 EvaluateMotionPosition(AgvMotionRow motion, float alpha)
    {
        List<Vector3> route = BuildRoute(motion);
        if (route.Count == 0)
        {
            return Vector3.zero;
        }
        if (route.Count == 1)
        {
            return route[0];
        }

        float totalDistance = 0f;
        for (int i = 0; i < route.Count - 1; i++)
        {
            totalDistance += Vector3.Distance(route[i], route[i + 1]);
        }

        if (totalDistance <= 0.0001f)
        {
            return route[route.Count - 1];
        }

        float targetDistance = Mathf.Clamp01(alpha) * totalDistance;
        float walked = 0f;
        for (int i = 0; i < route.Count - 1; i++)
        {
            float segmentDistance = Vector3.Distance(route[i], route[i + 1]);
            if (walked + segmentDistance >= targetDistance)
            {
                float segmentAlpha = (targetDistance - walked) / Mathf.Max(0.0001f, segmentDistance);
                return Vector3.Lerp(route[i], route[i + 1], segmentAlpha);
            }
            walked += segmentDistance;
        }

        return route[route.Count - 1];
    }

    private List<Vector3> BuildRoute(AgvMotionRow motion)
    {
        List<string> nodeIds = BuildRouteNodeIds(motion.fromNode, motion.toNode);
        List<Vector3> route = new List<Vector3>();
        for (int i = 0; i < nodeIds.Count; i++)
        {
            string nodeId = nodeIds[i];
            Vector3 fallback = nodeId == motion.fromNode ? motion.fromPosition : motion.toPosition;
            route.Add(ResolveNodePosition(nodeId, fallback));
        }

        return route;
    }

    private List<string> BuildRouteNodeIds(string fromNode, string toNode)
    {
        List<string> route = new List<string>();
        AddRouteNode(route, fromNode);

        if (useAisleRouting && fromNode != toNode)
        {
            string fromAisle = GetAisleForNode(fromNode);
            string toAisle = GetAisleForNode(toNode);

            if (!string.IsNullOrWhiteSpace(fromAisle))
            {
                AddRouteNode(route, fromAisle);
            }

            if (!string.IsNullOrWhiteSpace(fromAisle) && !string.IsNullOrWhiteSpace(toAisle) && fromAisle != toAisle)
            {
                AddRouteNode(route, "Aisle_Center");
            }

            if (!string.IsNullOrWhiteSpace(toAisle))
            {
                AddRouteNode(route, toAisle);
            }
        }

        AddRouteNode(route, toNode);
        return route;
    }

    private static string GetAisleForNode(string nodeId)
    {
        switch (nodeId)
        {
            case "Raw":
            case "M1":
            case "M3":
                return "Aisle_Left";
            case "M2":
            case "M4":
            case "Finished":
                return "Aisle_Right";
            default:
                return "";
        }
    }

    private static void AddRouteNode(List<string> route, string nodeId)
    {
        if (string.IsNullOrWhiteSpace(nodeId))
        {
            return;
        }

        if (route.Count == 0 || route[route.Count - 1] != nodeId)
        {
            route.Add(nodeId);
        }
    }

    private float CalculateMakespan()
    {
        float maxTime = 0f;
        foreach (AgvMotionRow row in agvMotions)
        {
            maxTime = Mathf.Max(maxTime, row.endTime);
        }
        foreach (ResourceStateRow row in resourceStates)
        {
            maxTime = Mathf.Max(maxTime, row.endTime);
        }
        return Mathf.Max(0.0001f, maxTime);
    }

    private void OnGUI()
    {
        if (!showDebugOnGui)
        {
            return;
        }

        const int width = 360;
        GUILayout.BeginArea(new Rect(16, 16, width, 245), GUI.skin.box);
        GUILayout.Label("\u004d\u0041\u0054\u004c\u0041\u0042 \u64ad\u653e\u63a7\u5236");
        GUILayout.Label(loadStatus);
        GUILayout.Label(string.Format(CultureInfo.InvariantCulture, "\u65f6\u95f4: {0:0.0} / {1:0.0}", playbackTime, makespan));
        if (disturbanceEventManager != null)
        {
            string summary = string.IsNullOrEmpty(disturbanceEventManager.CurrentEventSummary) ? "\u65e0" : disturbanceEventManager.CurrentEventSummary;
            GUILayout.Label(string.Format(CultureInfo.InvariantCulture, "\u5f53\u524d\u6270\u52a8: {0} ({1})", summary, disturbanceEventManager.ActiveEventCount));
        }
        if (orderVisualManager != null)
        {
            GUILayout.Label(string.Format(CultureInfo.InvariantCulture, "\u8ba2\u5355: \u8fd0\u8f93 {0} / \u52a0\u5de5 {1} / \u5b8c\u6210 {2}", orderVisualManager.TransportingOrderCount, orderVisualManager.ProcessingOrderCount, orderVisualManager.FinishedOrderCount));
        }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button(isPlaying ? "\u6682\u505c" : "\u64ad\u653e"))
        {
            if (isPlaying)
            {
                Pause();
            }
            else
            {
                Play();
            }
        }
        if (GUILayout.Button("\u91cd\u7f6e"))
        {
            ResetPlayback();
        }
        GUILayout.EndHorizontal();

        playbackTime = GUILayout.HorizontalSlider(playbackTime, 0f, makespan);
        GUILayout.BeginHorizontal();
        GUILayout.Label("\u901f\u5ea6", GUILayout.Width(44));
        playbackSpeed = GUILayout.HorizontalSlider(playbackSpeed, 0.1f, 10f);
        GUILayout.Label(playbackSpeed.ToString("0.0", CultureInfo.InvariantCulture) + "x", GUILayout.Width(48));
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
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
        return row.TryGetValue(key, out string value) ? value : "";
    }

    private static float GetFloat(Dictionary<string, string> row, string key)
    {
        string value = Get(row, key);
        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float result))
        {
            return result;
        }
        return 0f;
    }

    private class AgvMotionRow
    {
        public string taskId;
        public string agvId;
        public string orderId;
        public string fromNode;
        public string toNode;
        public float startTime;
        public float endTime;
        public float duration;
        public Vector3 fromPosition;
        public Vector3 toPosition;
    }

    private class ResourceStateRow
    {
        public string resourceId;
        public string state;
        public float startTime;
        public float endTime;
    }

    public class AgvTaskInfo
    {
        public readonly string taskId;
        public readonly string agvId;
        public readonly string orderId;
        public readonly string partId;
        public readonly string fromNode;
        public readonly string toNode;
        public readonly float startTime;
        public readonly float endTime;
        public readonly float progress;

        public AgvTaskInfo(string taskId, string agvId, string orderId, string partId, string fromNode, string toNode, float startTime, float endTime, float progress)
        {
            this.taskId = taskId;
            this.agvId = agvId;
            this.orderId = orderId;
            this.partId = partId;
            this.fromNode = fromNode;
            this.toNode = toNode;
            this.startTime = startTime;
            this.endTime = endTime;
            this.progress = progress;
        }
    }
}
