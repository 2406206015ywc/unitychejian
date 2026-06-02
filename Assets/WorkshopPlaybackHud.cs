using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WorkshopPlaybackHud : MonoBehaviour
{
    public MatlabPlaybackController controller;
    public bool rebuildOnAwake = true;
    public Sprite playbackBackgroundSprite;
    public Texture2D playbackBorderTexture;

    private static readonly Color BarColor = new Color(0.045f, 0.052f, 0.06f, 0.94f);
    private static readonly Color SectionColor = new Color(0.04f, 0.052f, 0.058f, 0.82f);
    private static readonly Color ButtonColor = new Color(0.08f, 0.37f, 0.82f, 1f);
    private static readonly Color SecondaryButtonColor = new Color(0.16f, 0.18f, 0.2f, 1f);
    private static readonly Color WarningColor = new Color(1f, 0.58f, 0.18f, 1f);
    private static readonly Color TransportColor = new Color(0.36f, 0.72f, 1f, 1f);
    private static readonly Color ProcessingColor = new Color(1f, 0.78f, 0.2f, 1f);
    private static readonly Color FinishedColor = new Color(0.28f, 0.82f, 0.42f, 1f);
    private static readonly Color MutedTextColor = new Color(0.72f, 0.78f, 0.82f, 1f);
    private const string HudBackgroundRelativePath = "\u6750\u8d28/\u80cc\u666f.png";
    private const string HudBorderRelativePath = "\u6750\u8d28/\u8fb9\u6846.jpg";

    private Canvas canvas;
    private Font uiFont;
    private Text statusText;
    private Text timeText;
    private Text percentText;
    private Text speedText;
    private Text playPauseText;
    private Text disturbanceText;
    private Text disturbanceCountText;
    private Text transportingText;
    private Text processingText;
    private Text finishedText;
    private readonly Text[] processingRows = new Text[2];
    private Slider timeSlider;
    private Slider speedSlider;

    public void RebuildHud()
    {
        ClearChildren(transform);
        EnsureController();
        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        ConfigureCanvas();

        GameObject bar = CreatePanel("Playback_Bottom_Bar", transform, BarColor);
        ApplyPlaybackBackground(bar.GetComponent<Image>());
        RectTransform barRect = bar.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0f, 0f);
        barRect.anchorMax = new Vector2(1f, 0f);
        barRect.pivot = new Vector2(0.5f, 0f);
        barRect.anchoredPosition = new Vector2(0f, 12f);
        barRect.sizeDelta = new Vector2(-32f, 112f);

        HorizontalLayoutGroup layout = AddHorizontalLayout(bar, 8f, new RectOffset(14, 14, 12, 12));
        layout.childForceExpandWidth = false;

        BuildCompactStatusPanel(bar.transform);
        BuildCompactControlPanel(bar.transform);
        BuildCompactOrderPanel(bar.transform);
        AddPlaybackBorder(bar.transform);

        Refresh();
    }

    private void Awake()
    {
        if (rebuildOnAwake)
        {
            RebuildHud();
        }
    }

    private void Start()
    {
        EnsureController();
        if (controller != null)
        {
            controller.LoadPlaybackPackage();
            controller.SetPlaybackTime(controller.playbackTime);
        }
    }

    private void Update()
    {
        Refresh();
    }

    private void ConfigureCanvas()
    {
        canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
        }
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

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

    private void BuildCompactStatusPanel(Transform parent)
    {
        GameObject panel = CreatePanel("Hud_Status_Panel", parent, SectionColor);
        LayoutElement panelLayout = panel.AddComponent<LayoutElement>();
        panelLayout.preferredWidth = 284f;
        panelLayout.flexibleHeight = 1f;

        VerticalLayoutGroup vertical = AddVerticalLayout(panel, 5f, new RectOffset(12, 12, 9, 9));
        vertical.childForceExpandHeight = false;

        Text title = CreateText("Title", panel.transform, "\u67d4\u6027\u8f66\u95f4\u4eff\u771f", 17, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
        title.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;

        statusText = CreateText("StatusText", panel.transform, "\u72b6\u6001: \u5df2\u6682\u505c", 14, FontStyle.Bold, TextAnchor.MiddleLeft, MutedTextColor);
        statusText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

        GameObject bottomRow = new GameObject("StatusBottomRow");
        bottomRow.transform.SetParent(panel.transform, false);
        bottomRow.AddComponent<LayoutElement>().preferredHeight = 30f;
        HorizontalLayoutGroup bottomLayout = AddHorizontalLayout(bottomRow, 8f, new RectOffset(0, 0, 0, 0));
        bottomLayout.childForceExpandWidth = false;

        GameObject badge = CreatePanel("DisturbanceBadge", bottomRow.transform, WarningColor);
        LayoutElement badgeLayout = badge.AddComponent<LayoutElement>();
        badgeLayout.preferredWidth = 32f;
        badgeLayout.preferredHeight = 30f;
        disturbanceCountText = CreateText("Text", badge.transform, "0", 15, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        StretchToParent(disturbanceCountText.rectTransform, Vector2.zero);

        disturbanceText = CreateText("DisturbanceText", bottomRow.transform, "\u5f53\u524d\u6270\u52a8: \u65e0", 13, FontStyle.Normal, TextAnchor.MiddleLeft, Color.white);
        disturbanceText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
    }

    private void BuildCompactControlPanel(Transform parent)
    {
        GameObject panel = CreatePanel("Hud_Control_Panel", parent, SectionColor);
        LayoutElement panelLayout = panel.AddComponent<LayoutElement>();
        panelLayout.flexibleWidth = 1f;
        panelLayout.flexibleHeight = 1f;
        panelLayout.minWidth = 520f;

        VerticalLayoutGroup vertical = AddVerticalLayout(panel, 5f, new RectOffset(10, 10, 8, 8));
        vertical.childForceExpandHeight = false;

        GameObject topRow = new GameObject("ControlTopRow");
        topRow.transform.SetParent(panel.transform, false);
        topRow.AddComponent<LayoutElement>().preferredHeight = 32f;
        HorizontalLayoutGroup topLayout = AddHorizontalLayout(topRow, 8f, new RectOffset(0, 0, 0, 0));
        topLayout.childForceExpandWidth = false;

        Button playButton = CreateButton("PlayPauseButton", topRow.transform, "\u64ad\u653e", ButtonColor, 14);
        playButton.gameObject.AddComponent<LayoutElement>().preferredWidth = 72f;
        playPauseText = playButton.GetComponentInChildren<Text>();
        playButton.onClick.AddListener(TogglePlayPause);

        Button resetButton = CreateButton("ResetButton", topRow.transform, "\u91cd\u7f6e", SecondaryButtonColor, 14);
        resetButton.gameObject.AddComponent<LayoutElement>().preferredWidth = 64f;
        resetButton.onClick.AddListener(ResetPlayback);

        timeText = CreateText("TimeText", topRow.transform, "0.0 / 0.0", 13, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
        timeText.gameObject.AddComponent<LayoutElement>().preferredWidth = 112f;

        percentText = CreateText("PercentText", topRow.transform, "0%", 14, FontStyle.Bold, TextAnchor.MiddleRight, ButtonColor);
        LayoutElement percentLayout = percentText.gameObject.AddComponent<LayoutElement>();
        percentLayout.flexibleWidth = 1f;
        percentLayout.minWidth = 50f;

        GameObject timeRow = new GameObject("TimeSliderRow");
        timeRow.transform.SetParent(panel.transform, false);
        timeRow.AddComponent<LayoutElement>().preferredHeight = 24f;
        HorizontalLayoutGroup timeLayout = AddHorizontalLayout(timeRow, 8f, new RectOffset(0, 0, 0, 0));
        timeLayout.childForceExpandWidth = false;

        timeSlider = CreateSlider("TimeSlider", timeRow.transform, 0f, 1f, 0f, 8f, ButtonColor);
        timeSlider.onValueChanged.AddListener(OnTimeSliderChanged);

        GameObject bottomRow = new GameObject("ControlBottomRow");
        bottomRow.transform.SetParent(panel.transform, false);
        bottomRow.AddComponent<LayoutElement>().preferredHeight = 22f;
        HorizontalLayoutGroup bottomLayout = AddHorizontalLayout(bottomRow, 8f, new RectOffset(0, 0, 0, 0));
        bottomLayout.childForceExpandWidth = false;

        Text speedLabel = CreateText("SpeedLabel", bottomRow.transform, "\u901f\u5ea6", 13, FontStyle.Normal, TextAnchor.MiddleLeft, MutedTextColor);
        speedLabel.gameObject.AddComponent<LayoutElement>().preferredWidth = 34f;

        speedSlider = CreateSlider("SpeedSlider", bottomRow.transform, 0.1f, 10f, 1f, 5f, new Color(0.42f, 0.66f, 0.88f, 1f));
        LayoutElement speedSliderLayout = speedSlider.gameObject.GetComponent<LayoutElement>();
        speedSliderLayout.preferredWidth = 122f;
        speedSliderLayout.flexibleWidth = 0f;
        speedSlider.onValueChanged.AddListener(OnSpeedSliderChanged);

        speedText = CreateText("SpeedText", bottomRow.transform, "1.0x", 13, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
        speedText.gameObject.AddComponent<LayoutElement>().preferredWidth = 48f;

        Text processingTitle = CreateText("ProcessingTitle", bottomRow.transform, "\u5f53\u524d\u52a0\u5de5", 13, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
        processingTitle.gameObject.AddComponent<LayoutElement>().preferredWidth = 68f;

        processingRows[0] = CreateText("ProcessingRow_0", bottomRow.transform, "", 13, FontStyle.Normal, TextAnchor.MiddleLeft, MutedTextColor);
        processingRows[0].gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        processingRows[1] = CreateText("ProcessingRow_1", bottomRow.transform, "", 13, FontStyle.Normal, TextAnchor.MiddleLeft, MutedTextColor);
        processingRows[1].gameObject.AddComponent<LayoutElement>().preferredWidth = 0f;
    }

    private void BuildCompactOrderPanel(Transform parent)
    {
        GameObject panel = CreatePanel("Hud_Order_Panel", parent, SectionColor);
        LayoutElement panelLayout = panel.AddComponent<LayoutElement>();
        panelLayout.preferredWidth = 278f;
        panelLayout.flexibleHeight = 1f;

        VerticalLayoutGroup vertical = AddVerticalLayout(panel, 6f, new RectOffset(12, 12, 9, 9));
        vertical.childForceExpandHeight = false;

        Text title = CreateText("OrderTitle", panel.transform, "\u8ba2\u5355\u603b\u89c8", 15, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
        title.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;

        GameObject metricsRow = new GameObject("OrderMetricsRow");
        metricsRow.transform.SetParent(panel.transform, false);
        metricsRow.AddComponent<LayoutElement>().preferredHeight = 58f;
        HorizontalLayoutGroup metricsLayout = AddHorizontalLayout(metricsRow, 8f, new RectOffset(0, 0, 0, 0));
        metricsLayout.childForceExpandWidth = true;

        transportingText = CreateMetricText("TransportingMetric", metricsRow.transform, "\u8fd0\u8f93\u4e2d", TransportColor);
        processingText = CreateMetricText("ProcessingMetric", metricsRow.transform, "\u52a0\u5de5\u4e2d", ProcessingColor);
        finishedText = CreateMetricText("FinishedMetric", metricsRow.transform, "\u5df2\u5b8c\u6210", FinishedColor);
    }

    private void BuildStatusRow(Transform parent)
    {
        GameObject row = CreateSection("Hud_Status_Row", parent, 24f);
        HorizontalLayoutGroup group = AddHorizontalLayout(row, 12f, new RectOffset(12, 12, 5, 5));
        group.childForceExpandWidth = false;

        Text title = CreateText("Title", row.transform, "\u67d4\u6027\u8f66\u95f4\u4eff\u771f", 20, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
        title.gameObject.AddComponent<LayoutElement>().preferredWidth = 190f;

        statusText = CreateText("StatusText", row.transform, "\u72b6\u6001: \u5df2\u6682\u505c", 17, FontStyle.Bold, TextAnchor.MiddleLeft, MutedTextColor);
        statusText.gameObject.AddComponent<LayoutElement>().preferredWidth = 140f;

        timeText = CreateText("TimeText", row.transform, "0.0 / 0.0", 18, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
        timeText.gameObject.AddComponent<LayoutElement>().preferredWidth = 150f;

        percentText = CreateText("PercentText", row.transform, "0%", 18, FontStyle.Bold, TextAnchor.MiddleLeft, ButtonColor);
        percentText.gameObject.AddComponent<LayoutElement>().preferredWidth = 58f;

        Text spacer = CreateText("StatusSpacer", row.transform, "", 1, FontStyle.Normal, TextAnchor.MiddleLeft, Color.white);
        spacer.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        transportingText = CreateText("TransportingMetric", row.transform, "\u8fd0\u8f93 0", 17, FontStyle.Bold, TextAnchor.MiddleRight, TransportColor);
        transportingText.gameObject.AddComponent<LayoutElement>().preferredWidth = 82f;
        processingText = CreateText("ProcessingMetric", row.transform, "\u52a0\u5de5 0", 17, FontStyle.Bold, TextAnchor.MiddleRight, ProcessingColor);
        processingText.gameObject.AddComponent<LayoutElement>().preferredWidth = 82f;
        finishedText = CreateText("FinishedMetric", row.transform, "\u5b8c\u6210 0", 17, FontStyle.Bold, TextAnchor.MiddleRight, FinishedColor);
        finishedText.gameObject.AddComponent<LayoutElement>().preferredWidth = 82f;
    }

    private void BuildControlRow(Transform parent)
    {
        GameObject row = CreateSection("Hud_Control_Row", parent, 42f);
        HorizontalLayoutGroup group = AddHorizontalLayout(row, 10f, new RectOffset(12, 12, 8, 8));
        group.childForceExpandWidth = false;

        Button playButton = CreateButton("PlayPauseButton", row.transform, "\u64ad\u653e", ButtonColor);
        playButton.gameObject.AddComponent<LayoutElement>().preferredWidth = 84f;
        playPauseText = playButton.GetComponentInChildren<Text>();
        playButton.onClick.AddListener(TogglePlayPause);

        Button resetButton = CreateButton("ResetButton", row.transform, "\u91cd\u7f6e", SecondaryButtonColor);
        resetButton.gameObject.AddComponent<LayoutElement>().preferredWidth = 74f;
        resetButton.onClick.AddListener(ResetPlayback);

        timeSlider = CreateSlider("TimeSlider", row.transform, 0f, 1f, 0f, 12f, ButtonColor);
        timeSlider.onValueChanged.AddListener(OnTimeSliderChanged);

        Text speedLabel = CreateText("SpeedLabel", row.transform, "\u901f\u5ea6", 17, FontStyle.Normal, TextAnchor.MiddleLeft, MutedTextColor);
        speedLabel.gameObject.AddComponent<LayoutElement>().preferredWidth = 44f;

        speedSlider = CreateSlider("SpeedSlider", row.transform, 0.1f, 10f, 1f, 8f, new Color(0.42f, 0.66f, 0.88f, 1f));
        LayoutElement speedSliderLayout = speedSlider.gameObject.GetComponent<LayoutElement>();
        speedSliderLayout.preferredWidth = 160f;
        speedSliderLayout.flexibleWidth = 0f;
        speedSlider.onValueChanged.AddListener(OnSpeedSliderChanged);

        speedText = CreateText("SpeedText", row.transform, "1.0x", 17, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
        speedText.gameObject.AddComponent<LayoutElement>().preferredWidth = 58f;
    }

    private void BuildProductionRow(Transform parent)
    {
        GameObject row = CreateSection("Hud_Production_Row", parent, 32f);
        HorizontalLayoutGroup group = AddHorizontalLayout(row, 10f, new RectOffset(12, 12, 7, 7));
        group.childForceExpandWidth = false;

        Text title = CreateText("ProcessingTitle", row.transform, "\u5f53\u524d\u52a0\u5de5", 17, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
        title.gameObject.AddComponent<LayoutElement>().preferredWidth = 96f;

        for (int i = 0; i < processingRows.Length; i++)
        {
            Text processingRow = CreateText("ProcessingRow_" + i.ToString(CultureInfo.InvariantCulture), row.transform, "", 16, FontStyle.Normal, TextAnchor.MiddleLeft, MutedTextColor);
            LayoutElement rowLayout = processingRow.gameObject.AddComponent<LayoutElement>();
            rowLayout.flexibleWidth = 1f;
            rowLayout.minWidth = 260f;
            processingRows[i] = processingRow;
        }
    }

    private void BuildDisturbanceRow(Transform parent)
    {
        GameObject row = CreateSection("Hud_Disturbance_Row", parent, 22f);
        HorizontalLayoutGroup group = AddHorizontalLayout(row, 8f, new RectOffset(12, 12, 5, 5));
        group.childForceExpandWidth = false;

        GameObject badge = CreatePanel("DisturbanceBadge", row.transform, WarningColor);
        LayoutElement badgeLayout = badge.AddComponent<LayoutElement>();
        badgeLayout.preferredWidth = 30f;
        badgeLayout.preferredHeight = 24f;
        disturbanceCountText = CreateText("Text", badge.transform, "0", 15, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        StretchToParent(disturbanceCountText.rectTransform, Vector2.zero);

        disturbanceText = CreateText("DisturbanceText", row.transform, "\u5f53\u524d\u6270\u52a8: \u65e0", 16, FontStyle.Normal, TextAnchor.MiddleLeft, Color.white);
        disturbanceText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
    }

    private void EnsureController()
    {
        if (controller == null)
        {
            controller = FindObjectOfType<MatlabPlaybackController>(true);
        }
    }

    private void Refresh()
    {
        EnsureController();
        if (controller == null || timeSlider == null || speedSlider == null)
        {
            return;
        }

        float makespan = Mathf.Max(0.0001f, controller.Makespan);
        float clampedTime = Mathf.Clamp(controller.playbackTime, 0f, makespan);
        float progress = Mathf.Clamp01(clampedTime / makespan);

        timeSlider.minValue = 0f;
        timeSlider.maxValue = makespan;
        timeSlider.SetValueWithoutNotify(clampedTime);
        speedSlider.SetValueWithoutNotify(controller.playbackSpeed);

        if (playPauseText != null)
        {
            playPauseText.text = controller.IsPlaying ? "\u6682\u505c" : "\u64ad\u653e";
        }
        if (statusText != null)
        {
            statusText.text = controller.IsPlaying ? "\u72b6\u6001: \u64ad\u653e\u4e2d" : "\u72b6\u6001: \u5df2\u6682\u505c";
            statusText.color = controller.IsPlaying ? TransportColor : MutedTextColor;
        }
        if (timeText != null)
        {
            timeText.text = string.Format(CultureInfo.InvariantCulture, "\u65f6\u95f4 {0:0.0} / {1:0.0}", controller.playbackTime, controller.Makespan);
        }
        if (percentText != null)
        {
            percentText.text = Mathf.RoundToInt(progress * 100f).ToString(CultureInfo.InvariantCulture) + "%";
        }
        if (speedText != null)
        {
            speedText.text = controller.playbackSpeed.ToString("0.0", CultureInfo.InvariantCulture) + "x";
        }
        if (disturbanceText != null)
        {
            disturbanceText.text = "\u5f53\u524d\u6270\u52a8: " + controller.GetCurrentDisturbanceSummary();
        }
        if (disturbanceCountText != null)
        {
            disturbanceCountText.text = controller.GetActiveDisturbanceCount().ToString(CultureInfo.InvariantCulture);
        }

        if (controller.orderVisualManager != null)
        {
            OrderVisualManager orders = controller.orderVisualManager;
            if (transportingText != null)
            {
                transportingText.text = orders.TransportingOrderCount.ToString(CultureInfo.InvariantCulture) + "\n\u8fd0\u8f93\u4e2d";
            }
            if (processingText != null)
            {
                processingText.text = orders.ProcessingOrderCount.ToString(CultureInfo.InvariantCulture) + "\n\u52a0\u5de5\u4e2d";
            }
            if (finishedText != null)
            {
                finishedText.text = orders.FinishedOrderCount.ToString(CultureInfo.InvariantCulture) + "\n\u5df2\u5b8c\u6210";
            }
            RefreshProcessingRows(orders.CurrentProcessingOrders);
        }
    }

    private void RefreshProcessingRows(List<OrderVisualManager.ProcessingOrderInfo> processingOrders)
    {
        EnsureProcessingRowReferences();
        if (processingRows[0] == null)
        {
            return;
        }

        if (processingOrders.Count == 0)
        {
            processingRows[0].text = "\u5f53\u524d\u65e0\u52a0\u5de5\u8ba2\u5355";
            processingRows[0].color = MutedTextColor;
            for (int i = 1; i < processingRows.Length; i++)
            {
                processingRows[i].text = "";
            }
            return;
        }

        OrderVisualManager.ProcessingOrderInfo item = processingOrders[0];
        string summary = string.Format(
            CultureInfo.InvariantCulture,
            "{0} \u7b2c{1}\u9053 {2} \u5269\u4f59 {3:0.0}s",
            item.orderId,
            Mathf.Max(1, item.operationStep),
            FormatMachineName(item.machineId),
            item.remainingTime);
        if (processingOrders.Count > 1)
        {
            summary += string.Format(CultureInfo.InvariantCulture, "  +{0}\u5355", processingOrders.Count - 1);
        }

        processingRows[0].text = summary;
        processingRows[0].color = Color.white;
        for (int i = 1; i < processingRows.Length; i++)
        {
            processingRows[i].text = "";
        }
    }

    private void EnsureProcessingRowReferences()
    {
        for (int i = 0; i < processingRows.Length; i++)
        {
            if (processingRows[i] != null)
            {
                continue;
            }

            GameObject rowObject = GameObject.Find("ProcessingRow_" + i.ToString(CultureInfo.InvariantCulture));
            if (rowObject != null)
            {
                processingRows[i] = rowObject.GetComponent<Text>();
            }
        }
    }

    private void TogglePlayPause()
    {
        EnsureController();
        if (controller == null)
        {
            return;
        }

        if (controller.IsPlaying)
        {
            controller.Pause();
        }
        else
        {
            controller.Play();
        }
        Refresh();
    }

    private void ResetPlayback()
    {
        EnsureController();
        if (controller == null)
        {
            return;
        }

        controller.ResetPlayback();
        Refresh();
    }

    private void OnTimeSliderChanged(float value)
    {
        EnsureController();
        if (controller != null)
        {
            controller.SetPlaybackTime(value);
        }
    }

    private void OnSpeedSliderChanged(float value)
    {
        EnsureController();
        if (controller != null)
        {
            controller.SetPlaybackSpeed(value);
        }
    }

    private GameObject CreateSection(string objectName, Transform parent, float height)
    {
        GameObject section = CreatePanel(objectName, parent, SectionColor);
        LayoutElement layout = section.AddComponent<LayoutElement>();
        layout.preferredHeight = height;
        layout.flexibleWidth = 1f;
        return section;
    }

    private GameObject CreatePanel(string objectName, Transform parent, Color color)
    {
        GameObject panel = new GameObject(objectName);
        panel.transform.SetParent(parent, false);
        Image image = panel.AddComponent<Image>();
        image.color = color;
        return panel;
    }

    private void ApplyPlaybackBackground(Image target)
    {
        if (target == null)
        {
            return;
        }

        Sprite sprite = playbackBackgroundSprite != null ? playbackBackgroundSprite : LoadSpriteFromAssets(HudBackgroundRelativePath, Vector4.zero);
        if (sprite == null)
        {
            target.color = BarColor;
            return;
        }

        target.sprite = sprite;
        target.type = Image.Type.Simple;
        target.preserveAspect = false;
        target.color = Color.white;
    }

    private void AddPlaybackBorder(Transform parent)
    {
        Texture2D source = playbackBorderTexture != null ? playbackBorderTexture : LoadTextureFromAssets(HudBorderRelativePath);
        Sprite sprite = CreateTransparentBorderSprite(source);
        if (sprite == null)
        {
            return;
        }

        GameObject border = new GameObject("Playback_Outer_Border");
        border.transform.SetParent(parent, false);
        LayoutElement layout = border.AddComponent<LayoutElement>();
        layout.ignoreLayout = true;

        Image borderImage = border.AddComponent<Image>();
        borderImage.sprite = sprite;
        borderImage.type = Image.Type.Sliced;
        borderImage.color = Color.white;
        borderImage.raycastTarget = false;

        RectTransform rect = border.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static Sprite LoadSpriteFromAssets(string relativePath, Vector4 border)
    {
        Texture2D texture = LoadTextureFromAssets(relativePath);
        if (texture == null)
        {
            return null;
        }

        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect, border);
    }

    private static Texture2D LoadTextureFromAssets(string relativePath)
    {
        string path = Path.Combine(Application.dataPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path))
        {
            return null;
        }

        byte[] bytes = File.ReadAllBytes(path);
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!texture.LoadImage(bytes))
        {
            Object.Destroy(texture);
            return null;
        }

        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        return texture;
    }

    private static Sprite CreateTransparentBorderSprite(Texture2D source)
    {
        if (source == null)
        {
            return null;
        }

        Color32[] sourcePixels = source.GetPixels32();
        int minX = source.width;
        int minY = source.height;
        int maxX = -1;
        int maxY = -1;

        for (int y = 0; y < source.height; y++)
        {
            for (int x = 0; x < source.width; x++)
            {
                int index = y * source.width + x;
                Color32 pixel = sourcePixels[index];
                bool transparent = IsNearlyWhite(pixel);
                pixel.a = transparent ? (byte)0 : (byte)255;
                sourcePixels[index] = pixel;

                if (!transparent)
                {
                    minX = Mathf.Min(minX, x);
                    minY = Mathf.Min(minY, y);
                    maxX = Mathf.Max(maxX, x);
                    maxY = Mathf.Max(maxY, y);
                }
            }
        }

        if (maxX < minX || maxY < minY)
        {
            return null;
        }

        int width = maxX - minX + 1;
        int height = maxY - minY + 1;
        Color32[] croppedPixels = new Color32[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                croppedPixels[y * width + x] = sourcePixels[(y + minY) * source.width + x + minX];
            }
        }

        Texture2D cropped = new Texture2D(width, height, TextureFormat.RGBA32, false);
        cropped.SetPixels32(croppedPixels);
        cropped.Apply();
        cropped.wrapMode = TextureWrapMode.Clamp;
        cropped.filterMode = FilterMode.Bilinear;

        Vector4 border = DetectSpriteBorder(croppedPixels, width, height);
        return Sprite.Create(cropped, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect, border);
    }

    private static bool IsNearlyWhite(Color32 pixel)
    {
        return pixel.r > 230 && pixel.g > 230 && pixel.b > 230;
    }

    private static Vector4 DetectSpriteBorder(Color32[] pixels, int width, int height)
    {
        int centerY = height / 2;
        int centerX = width / 2;
        int left = FindFirstTransparentX(pixels, width, centerY, 1, width / 2, 1);
        int rightTransparent = FindFirstTransparentX(pixels, width, centerY, width - 2, width / 2, -1);
        int bottom = FindFirstTransparentY(pixels, width, centerX, 1, height / 2, 1);
        int topTransparent = FindFirstTransparentY(pixels, width, centerX, height - 2, height / 2, -1);

        if (left < 1)
        {
            left = Mathf.RoundToInt(width * 0.12f);
        }
        int right = rightTransparent > 0 ? width - rightTransparent - 1 : Mathf.RoundToInt(width * 0.12f);
        if (bottom < 1)
        {
            bottom = Mathf.RoundToInt(height * 0.12f);
        }
        int top = topTransparent > 0 ? height - topTransparent - 1 : Mathf.RoundToInt(height * 0.12f);

        left = Mathf.Clamp(left, 1, width / 2);
        right = Mathf.Clamp(right, 1, width / 2);
        bottom = Mathf.Clamp(bottom, 1, height / 2);
        top = Mathf.Clamp(top, 1, height / 2);
        return new Vector4(left, bottom, right, top);
    }

    private static int FindFirstTransparentX(Color32[] pixels, int width, int y, int start, int end, int step)
    {
        for (int x = start; step > 0 ? x < end : x > end; x += step)
        {
            if (pixels[y * width + x].a == 0)
            {
                return x;
            }
        }
        return -1;
    }

    private static int FindFirstTransparentY(Color32[] pixels, int width, int x, int start, int end, int step)
    {
        for (int y = start; step > 0 ? y < end : y > end; y += step)
        {
            if (pixels[y * width + x].a == 0)
            {
                return y;
            }
        }
        return -1;
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
        layout.childForceExpandHeight = false;
        return layout;
    }

    private Text CreateText(string objectName, Transform parent, string textValue, int fontSize, FontStyle style, TextAnchor alignment, Color color)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);
        Text text = textObject.AddComponent<Text>();
        text.font = uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = textValue;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }

    private Text CreateMetricText(string objectName, Transform parent, string label, Color color)
    {
        GameObject metricPanel = CreatePanel(objectName + "Panel", parent, new Color(0.065f, 0.075f, 0.088f, 0.96f));
        LayoutElement metricLayout = metricPanel.AddComponent<LayoutElement>();
        metricLayout.flexibleWidth = 1f;
        metricLayout.preferredHeight = 58f;

        Text text = CreateText(objectName, metricPanel.transform, "0\n" + label, 16, FontStyle.Bold, TextAnchor.MiddleCenter, color);
        text.lineSpacing = 0.92f;
        StretchToParent(text.rectTransform, new Vector2(4f, 3f));
        return text;
    }

    private Button CreateButton(string objectName, Transform parent, string label, Color baseColor, int fontSize = 18)
    {
        GameObject buttonObject = CreatePanel(objectName, parent, baseColor);
        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = baseColor;
        colors.highlightedColor = Color.Lerp(baseColor, Color.white, 0.18f);
        colors.pressedColor = Color.Lerp(baseColor, Color.black, 0.25f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        Text text = CreateText("Text", buttonObject.transform, label, fontSize, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        StretchToParent(text.rectTransform, Vector2.zero);
        return button;
    }

    private Slider CreateSlider(string objectName, Transform parent, float min, float max, float value, float trackHeight, Color fillColor)
    {
        GameObject sliderObject = new GameObject(objectName);
        sliderObject.transform.SetParent(parent, false);
        Slider slider = sliderObject.AddComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = value;

        LayoutElement layout = sliderObject.AddComponent<LayoutElement>();
        layout.preferredHeight = Mathf.Max(30f, trackHeight + 16f);
        layout.flexibleWidth = 1f;

        RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderRect.sizeDelta = new Vector2(240f, Mathf.Max(30f, trackHeight + 16f));

        GameObject background = CreatePanel("Background", sliderObject.transform, new Color(0.18f, 0.21f, 0.235f, 1f));
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0f, 0.5f);
        backgroundRect.anchorMax = new Vector2(1f, 0.5f);
        backgroundRect.sizeDelta = new Vector2(0f, trackHeight);
        backgroundRect.anchoredPosition = Vector2.zero;

        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObject.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0.5f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.5f);
        fillAreaRect.offsetMin = new Vector2(0f, -trackHeight * 0.5f);
        fillAreaRect.offsetMax = new Vector2(0f, trackHeight * 0.5f);

        GameObject fill = CreatePanel("Fill", fillArea.transform, fillColor);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(sliderObject.transform, false);
        RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = Vector2.zero;
        handleAreaRect.offsetMax = Vector2.zero;

        GameObject handle = CreatePanel("Handle", handleArea.transform, new Color(0.95f, 0.97f, 1f, 1f));
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(18f, Mathf.Max(24f, trackHeight + 10f));

        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handle.GetComponent<Image>();
        return slider;
    }

    private static string FormatMachineName(string machineId)
    {
        switch (machineId)
        {
            case "M1":
                return "\u673a\u5e8a1";
            case "M2":
                return "\u673a\u5e8a2";
            case "M3":
                return "\u673a\u5e8a3";
            case "M4":
                return "\u673a\u5e8a4";
            default:
                return string.IsNullOrWhiteSpace(machineId) ? "\u672a\u77e5" : machineId;
        }
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
