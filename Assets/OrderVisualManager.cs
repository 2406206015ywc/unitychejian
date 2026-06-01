using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

public class OrderVisualManager : MonoBehaviour
{
    public string playbackDirectory = "C:/Users/ywc/Desktop/codex/matlab_workshop_model/output/unity_export_v2/simevents_stateflow_finaltransport_4m1agv";
    public string agvObjectName = "AGV_01";
    public Vector3 partScale = new Vector3(0.45f, 0.24f, 0.45f);
    public Vector3 agvCarryOffset = new Vector3(0f, 0.65f, 0f);
    public Vector3 rawQueueOffset = new Vector3(0.55f, 0f, 0f);
    public Vector3 finishedQueueOffset = new Vector3(0.55f, 0f, 0f);
    public Vector3 waitingQueueOffset = new Vector3(0.48f, 0f, 0f);
    public float groundHeight = 0.22f;
    public float processingHeight = 0.8f;
    public float processingForwardOffset = 0.05f;

    private readonly List<OrderStateRow> orderStates = new List<OrderStateRow>();
    private readonly Dictionary<string, OrderVisual> visualsByOrderId = new Dictionary<string, OrderVisual>();
    private readonly Dictionary<string, Transform> nodesById = new Dictionary<string, Transform>();
    private readonly Dictionary<string, Transform> machinesById = new Dictionary<string, Transform>();
    private readonly Dictionary<string, string> partIdByOrderId = new Dictionary<string, string>();
    private readonly List<ProcessingOrderInfo> currentProcessingOrders = new List<ProcessingOrderInfo>();

    private Transform agvTransform;
    private Transform visualRoot;
    private Material partMaterial;
    private bool hasLoaded;

    public int ActiveOrderCount { get; private set; }
    public int TransportingOrderCount { get; private set; }
    public int ProcessingOrderCount { get; private set; }
    public int FinishedOrderCount { get; private set; }

    public List<ProcessingOrderInfo> CurrentProcessingOrders
    {
        get { return currentProcessingOrders; }
    }

    public void LoadOrderTimeline()
    {
        orderStates.Clear();
        partIdByOrderId.Clear();
        visualsByOrderId.Clear();
        CacheSceneObjects();

        string orderPath = Path.Combine(playbackDirectory, "order_state_timeline.csv");
        if (!File.Exists(orderPath))
        {
            Debug.LogError("[OrderVisualManager] Missing order timeline: " + orderPath);
            return;
        }

        foreach (Dictionary<string, string> row in ReadCsv(orderPath))
        {
            OrderStateRow stateRow = new OrderStateRow();
            stateRow.orderId = Get(row, "order_id");
            stateRow.partId = Get(row, "part_id");
            stateRow.state = Get(row, "state");
            stateRow.location = Get(row, "location");
            stateRow.startTime = GetFloat(row, "start_time");
            stateRow.endTime = GetFloat(row, "end_time");

            if (string.IsNullOrWhiteSpace(stateRow.orderId))
            {
                continue;
            }

            orderStates.Add(stateRow);
            if (!partIdByOrderId.ContainsKey(stateRow.orderId))
            {
                partIdByOrderId[stateRow.orderId] = stateRow.partId;
            }
        }

        orderStates.Sort((a, b) =>
        {
            int orderCompare = string.CompareOrdinal(a.orderId, b.orderId);
            return orderCompare != 0 ? orderCompare : a.startTime.CompareTo(b.startTime);
        });
        AssignOperationSteps();

        RemoveStaleVisuals();
        EnsureVisuals();
        hasLoaded = true;
        Debug.Log("[OrderVisualManager] Loaded " + orderStates.Count + " order state rows, " + visualsByOrderId.Count + " order visuals.");
    }

    public void ApplyPlaybackTime(float time)
    {
        if (!hasLoaded)
        {
            LoadOrderTimeline();
        }

        ActiveOrderCount = 0;
        TransportingOrderCount = 0;
        ProcessingOrderCount = 0;
        FinishedOrderCount = 0;
        currentProcessingOrders.Clear();

        Dictionary<string, int> rawSlots = new Dictionary<string, int>();
        Dictionary<string, int> waitingSlots = new Dictionary<string, int>();
        Dictionary<string, int> processingSlots = new Dictionary<string, int>();
        Dictionary<string, int> finishedSlots = new Dictionary<string, int>();
        int agvSlot = 0;

        foreach (KeyValuePair<string, OrderVisual> pair in visualsByOrderId)
        {
            OrderStateRow active = FindActiveState(pair.Key, time);
            if (active == null)
            {
                pair.Value.SetTarget(pair.Value.transform.position, pair.Value.transform.rotation, false, true);
                continue;
            }

            ActiveOrderCount++;
            int slot;
            Vector3 position;
            Quaternion rotation;
            bool visible = true;
            bool immediate = !Application.isPlaying || Mathf.Approximately(time, 0f);

            switch (active.state)
            {
                case "Released":
                    slot = TakeSlot(rawSlots, "Raw");
                    position = GetNodeQueuePosition("Raw", slot, rawQueueOffset);
                    rotation = Quaternion.identity;
                    break;
                case "Transporting":
                    TransportingOrderCount++;
                    slot = agvSlot++;
                    position = GetAgvCarryPosition(slot);
                    rotation = agvTransform != null ? agvTransform.rotation : Quaternion.identity;
                    break;
                case "Processing":
                    ProcessingOrderCount++;
                    slot = TakeSlot(processingSlots, active.location);
                    position = GetProcessingPosition(active.location, slot);
                    rotation = GetMachineRotation(active.location);
                    currentProcessingOrders.Add(new ProcessingOrderInfo(active.orderId, active.partId, active.location, active.operationStep, Mathf.Max(0f, active.endTime - time), active.startTime, active.endTime));
                    break;
                case "Finished":
                    FinishedOrderCount++;
                    slot = TakeSlot(finishedSlots, "Finished");
                    position = GetNodeQueuePosition("Finished", slot, finishedQueueOffset);
                    rotation = Quaternion.identity;
                    break;
                case "WaitingTransport":
                default:
                    slot = TakeSlot(waitingSlots, active.location);
                    position = GetNodeQueuePosition(active.location, slot, waitingQueueOffset);
                    rotation = Quaternion.identity;
                    break;
            }

            pair.Value.SetTarget(position, rotation, visible, immediate);
        }

        currentProcessingOrders.Sort((a, b) => string.CompareOrdinal(a.machineId, b.machineId));
    }

    private void AssignOperationSteps()
    {
        Dictionary<string, int> stepByOrderId = new Dictionary<string, int>();
        foreach (OrderStateRow row in orderStates)
        {
            if (row.state != "Processing")
            {
                continue;
            }

            int nextStep;
            if (!stepByOrderId.TryGetValue(row.orderId, out nextStep))
            {
                nextStep = 1;
            }

            row.operationStep = nextStep;
            stepByOrderId[row.orderId] = nextStep + 1;
        }
    }

    private void Awake()
    {
        CacheSceneObjects();
    }

    private void CacheSceneObjects()
    {
        GameObject rootObject = GameObject.Find("Order_Visuals");
        if (rootObject == null)
        {
            rootObject = new GameObject("Order_Visuals");
        }
        visualRoot = rootObject.transform;

        GameObject agvObject = GameObject.Find(agvObjectName);
        agvTransform = agvObject != null ? agvObject.transform : null;

        nodesById.Clear();
        Transform[] transforms = FindObjectsOfType<Transform>(true);
        foreach (Transform item in transforms)
        {
            if (item != null && item.name.StartsWith("Node_", StringComparison.Ordinal))
            {
                nodesById[item.name.Substring("Node_".Length)] = item;
            }
        }

        machinesById.Clear();
        WorkshopResourceIdentity[] identities = FindObjectsOfType<WorkshopResourceIdentity>(true);
        foreach (WorkshopResourceIdentity identity in identities)
        {
            if (identity == null || identity.transform == null)
            {
                continue;
            }

            if (identity.resourceId == "M1" || identity.resourceId == "M2" || identity.resourceId == "M3" || identity.resourceId == "M4")
            {
                machinesById[identity.resourceId] = identity.transform;
            }
        }

        EnsurePartMaterial();
    }

    private void EnsurePartMaterial()
    {
        if (partMaterial != null)
        {
            return;
        }

        Shader shader = Shader.Find("Standard");
        partMaterial = new Material(shader != null ? shader : Shader.Find("Diffuse"));
        partMaterial.name = "Order_Part_Neutral_Material";
        partMaterial.color = new Color(0.72f, 0.76f, 0.78f, 1f);
    }

    private void EnsureVisuals()
    {
        foreach (KeyValuePair<string, string> pair in partIdByOrderId)
        {
            if (visualsByOrderId.ContainsKey(pair.Key))
            {
                continue;
            }

            Transform existing = visualRoot != null ? visualRoot.Find("Order_" + pair.Key) : null;
            OrderVisual visual;
            if (existing != null)
            {
                visual = existing.GetComponent<OrderVisual>();
                if (visual == null)
                {
                    visual = existing.gameObject.AddComponent<OrderVisual>();
                }
            }
            else
            {
                GameObject visualObject = new GameObject("Order_" + pair.Key);
                if (visualRoot != null)
                {
                    visualObject.transform.SetParent(visualRoot, false);
                }
                visual = visualObject.AddComponent<OrderVisual>();
            }

            visual.Initialize(pair.Key, pair.Value, partMaterial, partScale);
            visualsByOrderId[pair.Key] = visual;
        }
    }

    private void RemoveStaleVisuals()
    {
        if (visualRoot == null)
        {
            return;
        }

        for (int i = visualRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = visualRoot.GetChild(i);
            if (child == null || !child.name.StartsWith("Order_", StringComparison.Ordinal))
            {
                continue;
            }

            string orderId = child.name.Substring("Order_".Length);
            if (partIdByOrderId.ContainsKey(orderId))
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }

    private OrderStateRow FindActiveState(string orderId, float time)
    {
        OrderStateRow fallback = null;
        foreach (OrderStateRow row in orderStates)
        {
            if (row.orderId != orderId)
            {
                continue;
            }

            bool hasDuration = row.endTime > row.startTime;
            if (hasDuration && time >= row.startTime && time < row.endTime)
            {
                return row;
            }

            if (!hasDuration && Mathf.Approximately(time, row.startTime))
            {
                fallback = row;
            }
            else if (time >= row.endTime)
            {
                fallback = row;
            }
        }

        return fallback;
    }

    private int TakeSlot(Dictionary<string, int> slots, string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            key = "Unknown";
        }

        int slot;
        if (!slots.TryGetValue(key, out slot))
        {
            slot = 0;
        }
        slots[key] = slot + 1;
        return slot;
    }

    private Vector3 GetNodeQueuePosition(string nodeId, int slot, Vector3 slotOffset)
    {
        Vector3 basePosition = ResolveNodePosition(nodeId);
        Vector3 offset = GetGridOffset(slot, slotOffset);
        return new Vector3(basePosition.x + offset.x, groundHeight + offset.y, basePosition.z + offset.z);
    }

    private Vector3 GetAgvCarryPosition(int slot)
    {
        Vector3 basePosition = agvTransform != null ? agvTransform.position : ResolveNodePosition("Raw");
        Vector3 sideOffset = agvTransform != null ? agvTransform.right * ((slot % 3) - 1) * 0.38f : Vector3.right * ((slot % 3) - 1) * 0.38f;
        Vector3 backOffset = agvTransform != null ? -agvTransform.forward * (slot / 3) * 0.38f : Vector3.back * (slot / 3) * 0.38f;
        return basePosition + agvCarryOffset + sideOffset + backOffset;
    }

    private Vector3 GetProcessingPosition(string machineId, int slot)
    {
        Transform machine;
        if (!machinesById.TryGetValue(machineId, out machine) || machine == null)
        {
            return GetNodeQueuePosition(machineId, slot, waitingQueueOffset);
        }

        Vector3 lateralOffset = machine.right * ((slot % 3) - 1) * 0.32f;
        Vector3 depthOffset = -machine.forward * (slot / 3) * 0.24f;
        return machine.position + machine.forward * processingForwardOffset + lateralOffset + depthOffset + Vector3.up * processingHeight;
    }

    private Quaternion GetMachineRotation(string machineId)
    {
        Transform machine;
        if (machinesById.TryGetValue(machineId, out machine) && machine != null)
        {
            return machine.rotation;
        }

        return Quaternion.identity;
    }

    private Vector3 ResolveNodePosition(string nodeId)
    {
        Transform node;
        if (!string.IsNullOrWhiteSpace(nodeId) && nodesById.TryGetValue(nodeId, out node) && node != null)
        {
            return node.position;
        }

        return transform.position;
    }

    private static Vector3 GetGridOffset(int slot, Vector3 slotOffset)
    {
        int col = slot % 5;
        int row = slot / 5;
        return slotOffset * col + Vector3.back * row * 0.52f;
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

    private class OrderStateRow
    {
        public string orderId;
        public string partId;
        public string state;
        public string location;
        public float startTime;
        public float endTime;
        public int operationStep;
    }

    public class ProcessingOrderInfo
    {
        public readonly string orderId;
        public readonly string partId;
        public readonly string machineId;
        public readonly int operationStep;
        public readonly float remainingTime;
        public readonly float startTime;
        public readonly float endTime;

        public ProcessingOrderInfo(string orderId, string partId, string machineId, int operationStep, float remainingTime, float startTime, float endTime)
        {
            this.orderId = orderId;
            this.partId = partId;
            this.machineId = machineId;
            this.operationStep = operationStep;
            this.remainingTime = remainingTime;
            this.startTime = startTime;
            this.endTime = endTime;
        }
    }
}
