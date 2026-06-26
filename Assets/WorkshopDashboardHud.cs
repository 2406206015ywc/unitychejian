using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WorkshopDashboardHud : MonoBehaviour
{
    public MatlabPlaybackController controller;
    public bool createDefaultLayoutIfMissing = false;

    [Header("Playback")]
    public Text overallProgressText;
    public Text orderSummaryText;
    public Text currentOrdersText;
    public Text deviceStatusText;
    public Text agvStatusText;

    [Header("Device Selector")]
    public Button machine1Button;
    public Button machine2Button;
    public Button machine3Button;
    public Button machine4Button;
    public Button agvButton;
    public Color selectedButtonColor = new Color(0.08f, 0.37f, 0.82f, 1f);
    public Color normalButtonColor = new Color(0.10f, 0.12f, 0.14f, 0.96f);

    [Header("Right Detail Panel")]
    public Text detailTitleText;
    public Text deviceInfoText;
    public Text taskInfoText;

    [Header("Default Layout Style")]
    public Color panelColor = new Color(0.045f, 0.052f, 0.06f, 0.92f);
    public Color subPanelColor = new Color(0.08f, 0.095f, 0.11f, 0.94f);
    public Color textColor = Color.white;
    public Color mutedTextColor = new Color(0.72f, 0.78f, 0.82f, 1f);
    public Color transportColor = new Color(0.36f, 0.72f, 1f, 1f);
    public Color processingColor = new Color(1f, 0.78f, 0.2f, 1f);
    public Color finishedColor = new Color(0.28f, 0.82f, 0.42f, 1f);

    private const string AgvId = "AGV_01";
    private string selectedResourceId = "M1";
    private bool controlsWired;
    private Font uiFont;

    private void Awake()
    {
        EnsureController();
        ConfigureCanvas();
        if (createDefaultLayoutIfMissing && transform.Find("Dashboard_Root") == null)
        {
            RebuildEditableLayout();
        }
        BindSceneReferences();
        WireControls();
    }

    private void Start()
    {
        EnsureController();
        if (controller != null)
        {
            controller.SetPlaybackTime(controller.playbackTime);
        }
        Refresh();
    }

    private void Update()
    {
        Refresh();
    }

    [ContextMenu("Rebuild Editable Dashboard Layout")]
    public void RebuildEditableLayout()
    {
        uiFont = WorkshopFontProvider.GetFont();
        ClearChildren(transform);
        ConfigureCanvas();

        GameObject root = CreatePanel("Dashboard_Root", transform, Color.clear);
        StretchToParent(root.GetComponent<RectTransform>(), Vector2.zero);

        BuildDeviceSelector(root.transform);
        BuildRightDetailPanel(root.transform);
        BuildBottomDashboard(root.transform);
        BindSceneReferences();
        WireControls();
        Refresh();
    }

    private void BuildDeviceSelector(Transform parent)
    {
        GameObject panel = CreatePanel("Device_Select_Panel", parent, panelColor);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = new Vector2(12f, 0f);
        rect.sizeDelta = new Vector2(104f, 360f);

        VerticalLayoutGroup layout = AddVerticalLayout(panel, 8f, new RectOffset(8, 8, 8, 8));
        layout.childForceExpandHeight = false;

        machine1Button = CreateDeviceButton("Button_M1", panel.transform, "\u673a\u5e8a1", "M1");
        machine2Button = CreateDeviceButton("Button_M2", panel.transform, "\u673a\u5e8a2", "M2");
        machine3Button = CreateDeviceButton("Button_M3", panel.transform, "\u673a\u5e8a3", "M3");
        machine4Button = CreateDeviceButton("Button_M4", panel.transform, "\u673a\u5e8a4", "M4");
        agvButton = CreateDeviceButton("Button_AGV", panel.transform, "AGV", AgvId);
    }

    private void BuildRightDetailPanel(Transform parent)
    {
        GameObject panel = CreatePanel("Device_Detail_Panel", parent, panelColor);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.anchoredPosition = new Vector2(-12f, 0f);
        rect.sizeDelta = new Vector2(320f, 380f);

        VerticalLayoutGroup layout = AddVerticalLayout(panel, 10f, new RectOffset(14, 14, 14, 14));
        layout.childForceExpandHeight = false;

        detailTitleText = CreateText("Detail_Title", panel.transform, "\u673a\u5e8a1", 19, FontStyle.Bold, TextAnchor.MiddleLeft, textColor);
        detailTitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;

        Text infoTitle = CreateText("Info_Header", panel.transform, "\u673a\u5e8a\u4fe1\u606f", 16, FontStyle.Bold, TextAnchor.MiddleLeft, textColor);
        infoTitle.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;

        deviceInfoText = CreateText("Device_Info", panel.transform, "", 15, FontStyle.Normal, TextAnchor.UpperLeft, mutedTextColor);
        deviceInfoText.gameObject.AddComponent<LayoutElement>().preferredHeight = 108f;

        Text taskTitle = CreateText("Task_Header", panel.transform, "\u5f53\u524d\u52a0\u5de5\u4efb\u52a1", 16, FontStyle.Bold, TextAnchor.MiddleLeft, textColor);
        taskTitle.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;

        taskInfoText = CreateText("Task_Info", panel.transform, "", 15, FontStyle.Normal, TextAnchor.UpperLeft, mutedTextColor);
        taskInfoText.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
    }

    private void BuildBottomDashboard(Transform parent)
    {
        GameObject panel = CreatePanel("Production_Dashboard_Bottom", parent, panelColor);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 12f);
        rect.offsetMin = new Vector2(124f, 12f);
        rect.offsetMax = new Vector2(-340f, 144f);

        HorizontalLayoutGroup layout = AddHorizontalLayout(panel, 8f, new RectOffset(8, 8, 8, 8));
        layout.childForceExpandWidth = true;

        GameObject overall = CreateDashboardModule("Module_Overall", panel.transform, 190f);
        overallProgressText = CreateText("Overall_Progress", overall.transform, "\u603b\u4f53\u8fdb\u5ea6 0%", 16, FontStyle.Bold, TextAnchor.MiddleLeft, textColor);
        overallProgressText.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;
        orderSummaryText = CreateText("Order_Summary", overall.transform, "", 14, FontStyle.Normal, TextAnchor.UpperLeft, mutedTextColor);
        orderSummaryText.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;

        GameObject orders = CreateDashboardModule("Module_Current_Orders", panel.transform, 270f);
        CreateModuleTitle(orders.transform, "\u8ba2\u5355\u6982\u89c8");
        currentOrdersText = CreateText("Current_Orders", orders.transform, "", 14, FontStyle.Normal, TextAnchor.UpperLeft, mutedTextColor);
        currentOrdersText.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;

        GameObject devices = CreateDashboardModule("Module_Device_Status", panel.transform, 220f);
        CreateModuleTitle(devices.transform, "\u8bbe\u5907\u72b6\u6001");
        deviceStatusText = CreateText("Device_Status", devices.transform, "", 14, FontStyle.Normal, TextAnchor.UpperLeft, mutedTextColor);
        deviceStatusText.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;

        GameObject agv = CreateDashboardModule("Module_AGV", panel.transform, 210f);
        CreateModuleTitle(agv.transform, "AGV_01");
        agvStatusText = CreateText("AGV_Status", agv.transform, "", 14, FontStyle.Normal, TextAnchor.UpperLeft, mutedTextColor);
        agvStatusText.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
    }

    private GameObject CreateDashboardModule(string name, Transform parent, float preferredWidth)
    {
        GameObject module = CreatePanel(name, parent, subPanelColor);
        LayoutElement layout = module.AddComponent<LayoutElement>();
        layout.preferredWidth = preferredWidth;
        layout.flexibleWidth = 1f;
        layout.flexibleHeight = 1f;
        AddVerticalLayout(module, 4f, new RectOffset(10, 10, 8, 8));
        return module;
    }

    private void CreateModuleTitle(Transform parent, string title)
    {
        Text text = CreateText("Title", parent, title, 15, FontStyle.Bold, TextAnchor.MiddleLeft, textColor);
        text.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;
    }

    private Button CreateDeviceButton(string name, Transform parent, string label, string resourceId)
    {
        Button button = CreateButton(name, parent, label, normalButtonColor);
        button.gameObject.AddComponent<LayoutElement>().preferredHeight = 58f;
        button.onClick.AddListener(() => SelectResource(resourceId));
        return button;
    }

    private void BindSceneReferences()
    {
        WorkshopFontProvider.ApplyToChildren(transform);
        if (overallProgressText == null) overallProgressText = FindComponent<Text>("Overall_Progress");
        if (orderSummaryText == null) orderSummaryText = FindComponent<Text>("Order_Summary");
        if (currentOrdersText == null) currentOrdersText = FindComponent<Text>("Current_Orders");
        if (deviceStatusText == null) deviceStatusText = FindComponent<Text>("Device_Status");
        if (agvStatusText == null) agvStatusText = FindComponent<Text>("AGV_Status");
        if (machine1Button == null) machine1Button = FindComponent<Button>("Button_M1");
        if (machine2Button == null) machine2Button = FindComponent<Button>("Button_M2");
        if (machine3Button == null) machine3Button = FindComponent<Button>("Button_M3");
        if (machine4Button == null) machine4Button = FindComponent<Button>("Button_M4");
        if (agvButton == null) agvButton = FindComponent<Button>("Button_AGV");
        if (detailTitleText == null) detailTitleText = FindComponent<Text>("Detail_Title");
        if (deviceInfoText == null) deviceInfoText = FindComponent<Text>("Device_Info");
        if (taskInfoText == null) taskInfoText = FindComponent<Text>("Task_Info");
    }

    private void WireControls()
    {
        if (controlsWired)
        {
            return;
        }

        if (machine1Button != null) machine1Button.onClick.AddListener(() => SelectResource("M1"));
        if (machine2Button != null) machine2Button.onClick.AddListener(() => SelectResource("M2"));
        if (machine3Button != null) machine3Button.onClick.AddListener(() => SelectResource("M3"));
        if (machine4Button != null) machine4Button.onClick.AddListener(() => SelectResource("M4"));
        if (agvButton != null) agvButton.onClick.AddListener(() => SelectResource(AgvId));
        controlsWired = true;
    }

    private void SelectResource(string resourceId)
    {
        selectedResourceId = resourceId;
        Refresh();
    }

    private void Refresh()
    {
        EnsureController();
        if (controller == null)
        {
            return;
        }

        float makespan = Mathf.Max(0.0001f, controller.Makespan);
        float progress = Mathf.Clamp01(controller.playbackTime / makespan);

        if (overallProgressText != null)
        {
            overallProgressText.text = string.Format(CultureInfo.InvariantCulture, "\u603b\u4f53\u8fdb\u5ea6 {0}%", Mathf.RoundToInt(progress * 100f));
        }
        if (orderSummaryText != null)
        {
            int total = controller.orderVisualManager != null ? controller.orderVisualManager.ActiveOrderCount : 0;
            int finished = controller.orderVisualManager != null ? controller.orderVisualManager.FinishedOrderCount : 0;
            int processing = controller.orderVisualManager != null ? controller.orderVisualManager.ProcessingOrderCount : 0;
            int transporting = controller.orderVisualManager != null ? controller.orderVisualManager.TransportingOrderCount : 0;
            orderSummaryText.text = string.Format(CultureInfo.InvariantCulture, "{0}\u5355 / \u5b8c\u6210{1} / \u52a0\u5de5{2} / \u8fd0\u8f93{3}", total, finished, processing, transporting);
        }

        RefreshCurrentOrders();
        RefreshDeviceOverview();
        RefreshAgvOverview();
        RefreshDetailPanel();
        RefreshButtonSelectionMarkers();
    }

    private void RefreshCurrentOrders()
    {
        if (currentOrdersText == null || controller.orderVisualManager == null)
        {
            return;
        }

        List<OrderVisualManager.OrderTaskInfo> tasks = controller.orderVisualManager.GetCurrentOrderTasks(controller.playbackTime);
        if (tasks.Count == 0)
        {
            currentOrdersText.text = "\u5f53\u524d\u65e0\u8ba2\u5355";
            return;
        }

        List<string> lines = new List<string>();
        for (int i = 0; i < tasks.Count; i++)
        {
            lines.Add(FormatOrderTaskLine(tasks[i]));
        }
        currentOrdersText.text = string.Join("\n", lines.ToArray());
    }

    private void RefreshDeviceOverview()
    {
        if (deviceStatusText == null)
        {
            return;
        }

        string[] ids = { "M1", "M2", "M3", "M4" };
        List<string> lines = new List<string>();
        foreach (string id in ids)
        {
            string health = GetHealthStatus(id);
            OrderVisualManager.OrderTaskInfo task;
            if (health != "\u6b63\u5e38")
            {
                lines.Add(FormatMachineName(id) + " " + health);
            }
            else if (controller.orderVisualManager != null && controller.orderVisualManager.TryGetProcessingTaskForMachine(id, out task))
            {
                lines.Add(FormatMachineName(id) + " \u52a0\u5de5 " + FormatEmpty(task.orderId));
            }
            else
            {
                lines.Add(FormatMachineName(id) + " \u7a7a\u95f2");
            }
        }
        deviceStatusText.text = string.Join("\n", lines.ToArray());
    }

    private void RefreshAgvOverview()
    {
        if (agvStatusText == null)
        {
            return;
        }

        MatlabPlaybackController.AgvTaskInfo task;
        string health = GetHealthStatus(AgvId);
        if (controller.TryGetCurrentAgvTask(out task))
        {
            agvStatusText.text = string.Format(CultureInfo.InvariantCulture, "\u8fd0\u8f93\u4e2d {0} -> {1} {2}%", FormatNode(task.fromNode), FormatNode(task.toNode), Mathf.RoundToInt(task.progress * 100f));
        }
        else
        {
            agvStatusText.text = "\u7a7a\u95f2";
        }

        if (health != "\u6b63\u5e38")
        {
            agvStatusText.text += "\n" + health;
        }
    }

    private void RefreshDetailPanel()
    {
        if (selectedResourceId == AgvId)
        {
            RefreshAgvDetail();
        }
        else
        {
            RefreshMachineDetail(selectedResourceId);
        }
    }

    private void RefreshMachineDetail(string machineId)
    {
        if (detailTitleText != null) detailTitleText.text = FormatMachineName(machineId);
        if (deviceInfoText != null)
        {
            deviceInfoText.text = string.Format(
                CultureInfo.InvariantCulture,
                "\u673a\u5e8a\u7c7b\u578b: {0}\n\n\u5de5\u4f5c\u72b6\u6001: {1}\n\n\u5065\u5eb7\u72b6\u6001: {2}",
                GetMachineType(machineId),
                GetMachineWorkStatus(machineId),
                GetHealthStatus(machineId));
        }

        if (taskInfoText == null)
        {
            return;
        }

        OrderVisualManager.OrderTaskInfo task;
        if (controller.orderVisualManager != null && controller.orderVisualManager.TryGetProcessingTaskForMachine(machineId, out task))
        {
            taskInfoText.text = string.Format(
                CultureInfo.InvariantCulture,
                "\u8ba2\u5355\u7f16\u53f7: {0}\n\n\u5de5\u4ef6\u7f16\u53f7: {1}\n\n\u5f53\u524d\u5de5\u5e8f: {2}/{3}",
                FormatEmpty(task.orderId),
                FormatEmpty(task.partId),
                Mathf.Max(1, task.operationStep),
                Mathf.Max(1, task.operationCount));
        }
        else
        {
            taskInfoText.text = "\u8ba2\u5355\u7f16\u53f7: \u65e0\n\n\u5de5\u4ef6\u7f16\u53f7: \u65e0\n\n\u5f53\u524d\u5de5\u5e8f: \u65e0";
        }
    }

    private void RefreshAgvDetail()
    {
        if (detailTitleText != null) detailTitleText.text = "AGV_01";
        MatlabPlaybackController.AgvTaskInfo task;
        bool hasTask = controller.TryGetCurrentAgvTask(out task);
        if (deviceInfoText != null)
        {
            deviceInfoText.text = string.Format(
                CultureInfo.InvariantCulture,
                "\u5de5\u4f5c\u72b6\u6001: {0}\n\n\u5065\u5eb7\u72b6\u6001: {1}",
                hasTask ? "\u8fd0\u8f93\u4e2d" : "\u7a7a\u95f2",
                GetHealthStatus(AgvId));
        }

        if (taskInfoText == null)
        {
            return;
        }

        if (hasTask)
        {
            taskInfoText.text = string.Format(
                CultureInfo.InvariantCulture,
                "\u8ba2\u5355\u7f16\u53f7: {0}\n\n\u5de5\u4ef6\u7f16\u53f7: {1}\n\n\u8fd0\u8f93\u8def\u5f84: {2} -> {3}",
                FormatEmpty(task.orderId),
                FormatEmpty(task.partId),
                FormatNode(task.fromNode),
                FormatNode(task.toNode));
        }
        else
        {
            taskInfoText.text = "\u8ba2\u5355\u7f16\u53f7: \u65e0\n\n\u5de5\u4ef6\u7f16\u53f7: \u65e0\n\n\u8fd0\u8f93\u8def\u5f84: \u65e0";
        }
    }

    private string FormatOrderTaskLine(OrderVisualManager.OrderTaskInfo task)
    {
        if (task.state == "Processing")
        {
            float percent = Mathf.Clamp01((controller.playbackTime - task.startTime) / Mathf.Max(0.0001f, task.endTime - task.startTime));
            return string.Format(CultureInfo.InvariantCulture, "{0} \u5de5\u5e8f{1}/{2} {3} {4}%", FormatEmpty(task.orderId), Mathf.Max(1, task.operationStep), Mathf.Max(1, task.operationCount), FormatMachineName(task.location), Mathf.RoundToInt(percent * 100f));
        }
        if (task.state == "Transporting")
        {
            MatlabPlaybackController.AgvTaskInfo agvTask;
            if (controller.TryGetCurrentAgvTask(out agvTask) && agvTask.orderId == task.orderId)
            {
                return string.Format(CultureInfo.InvariantCulture, "{0} \u8fd0\u8f93 {1}->{2}", FormatEmpty(task.orderId), FormatNode(agvTask.fromNode), FormatNode(agvTask.toNode));
            }
            return FormatEmpty(task.orderId) + " \u8fd0\u8f93\u4e2d";
        }
        if (task.state == "WaitingTransport")
        {
            return FormatEmpty(task.orderId) + " \u7b49\u5f85AGV";
        }
        if (task.state == "WaitingProcessing")
        {
            return FormatEmpty(task.orderId) + " \u7b49\u5f85\u52a0\u5de5";
        }
        if (task.state == "Finished")
        {
            return FormatEmpty(task.orderId) + " \u5df2\u5b8c\u6210";
        }
        if (task.state == "Canceled")
        {
            return FormatEmpty(task.orderId) + " \u5df2\u53d6\u6d88";
        }
        if (task.state == "Unreleased")
        {
            return FormatEmpty(task.orderId) + " \u672a\u91ca\u653e";
        }
        if (task.state == "Released")
        {
            return FormatEmpty(task.orderId) + " \u539f\u6599\u533a\u7b49\u5f85";
        }

        return FormatEmpty(task.orderId) + " " + task.state;
    }

    private string GetMachineWorkStatus(string machineId)
    {
        OrderVisualManager.OrderTaskInfo task;
        if (controller.orderVisualManager != null && controller.orderVisualManager.TryGetProcessingTaskForMachine(machineId, out task))
        {
            return "\u52a0\u5de5\u4e2d";
        }

        string state = controller.GetCurrentResourceState(machineId);
        return state == "Busy" || state == "Processing" ? "\u52a0\u5de5\u4e2d" : "\u7a7a\u95f2";
    }

    private string GetHealthStatus(string resourceId)
    {
        if (controller.disturbanceEventManager == null)
        {
            return "\u6b63\u5e38";
        }

        string disturbance = controller.disturbanceEventManager.GetCurrentDisturbanceForTarget(resourceId, controller.playbackTime);
        return string.IsNullOrWhiteSpace(disturbance) || disturbance == "\u673a\u5e8a\u6062\u590d" ? "\u6b63\u5e38" : disturbance;
    }

    private void RefreshButtonSelectionMarkers()
    {
        SetButtonSelectionMarker(machine1Button, selectedResourceId == "M1");
        SetButtonSelectionMarker(machine2Button, selectedResourceId == "M2");
        SetButtonSelectionMarker(machine3Button, selectedResourceId == "M3");
        SetButtonSelectionMarker(machine4Button, selectedResourceId == "M4");
        SetButtonSelectionMarker(agvButton, selectedResourceId == AgvId);
    }

    private void SetButtonSelectionMarker(Button button, bool selected)
    {
        if (button == null)
        {
            return;
        }

        Transform marker = button.transform.Find("Selected_Marker");
        if (marker == null)
        {
            marker = button.transform.Find("SelectedMarker");
        }
        if (marker == null)
        {
            marker = button.transform.Find("Selection_Marker");
        }
        if (marker == null)
        {
            marker = button.transform.Find("SelectionMarker");
        }

        if (marker != null)
        {
            marker.gameObject.SetActive(selected);
        }
    }

    private void EnsureController()
    {
        if (controller == null)
        {
            controller = FindObjectOfType<MatlabPlaybackController>(true);
        }
    }

    private void ConfigureCanvas()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
        }
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 110;

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = gameObject.AddComponent<CanvasScaler>();
        }
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        if (GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }

        EnsureEventSystem();
    }

    private T FindComponent<T>(string objectName) where T : Component
    {
        Transform child = FindDeepChild(transform, objectName);
        return child != null ? child.GetComponent<T>() : null;
    }

    private static Transform FindDeepChild(Transform parent, string objectName)
    {
        if (parent == null)
        {
            return null;
        }
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == objectName)
            {
                return child;
            }

            Transform nested = FindDeepChild(child, objectName);
            if (nested != null)
            {
                return nested;
            }
        }
        return null;
    }

    private GameObject CreatePanel(string objectName, Transform parent, Color color)
    {
        GameObject panel = new GameObject(objectName);
        panel.transform.SetParent(parent, false);
        Image image = panel.AddComponent<Image>();
        image.color = color;
        return panel;
    }

    private Button CreateButton(string objectName, Transform parent, string label, Color color)
    {
        GameObject buttonObject = CreatePanel(objectName, parent, color);
        Button button = buttonObject.AddComponent<Button>();
        Text text = CreateText("Text", buttonObject.transform, label, 15, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        StretchToParent(text.rectTransform, Vector2.zero);
        return button;
    }

    private Text CreateText(string objectName, Transform parent, string value, int fontSize, FontStyle style, TextAnchor alignment, Color color)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);
        Text text = textObject.AddComponent<Text>();
        text.font = uiFont != null ? uiFont : WorkshopFontProvider.GetFont();
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }

    private HorizontalLayoutGroup AddHorizontalLayout(GameObject target, float spacing, RectOffset padding)
    {
        HorizontalLayoutGroup layout = target.AddComponent<HorizontalLayoutGroup>();
        layout.padding = padding;
        layout.spacing = spacing;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;
        return layout;
    }

    private VerticalLayoutGroup AddVerticalLayout(GameObject target, float spacing, RectOffset padding)
    {
        VerticalLayoutGroup layout = target.AddComponent<VerticalLayoutGroup>();
        layout.padding = padding;
        layout.spacing = spacing;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;
        return layout;
    }

    private static void StretchToParent(RectTransform rect, Vector2 padding)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = padding;
        rect.offsetMax = -padding;
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
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

    private static string GetMachineType(string machineId)
    {
        return machineId == "M1" || machineId == "M3" ? "\u8f66\u5e8a" : "\u94e3\u5e8a";
    }

    private static string FormatMachineName(string machineId)
    {
        switch (machineId)
        {
            case "M1": return "\u673a\u5e8a1";
            case "M2": return "\u673a\u5e8a2";
            case "M3": return "\u673a\u5e8a3";
            case "M4": return "\u673a\u5e8a4";
            default: return string.IsNullOrWhiteSpace(machineId) ? "\u672a\u77e5" : machineId;
        }
    }

    private static string FormatNode(string nodeId)
    {
        switch (nodeId)
        {
            case "Raw": return "\u539f\u6599\u533a";
            case "Finished": return "\u6210\u54c1\u533a";
            case "M1": return "\u673a\u5e8a1";
            case "M2": return "\u673a\u5e8a2";
            case "M3": return "\u673a\u5e8a3";
            case "M4": return "\u673a\u5e8a4";
            default: return string.IsNullOrWhiteSpace(nodeId) ? "\u65e0" : nodeId;
        }
    }

    private static string FormatEmpty(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "\u65e0" : value;
    }

    private static void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>(true) != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }
}
