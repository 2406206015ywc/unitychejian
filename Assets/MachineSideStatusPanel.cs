using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class MachineSideStatusPanel : MonoBehaviour
{
    [Header("Identity")]
    public string resourceId = "";
    public string displayName = "";

    [Header("Layout")]
    public Vector3 worldOffset = new Vector3(2.2f, 2.4f, 0f);
    public Vector2 panelSize = new Vector2(230f, 170f);
    public float panelScale = 0.012f;
    public bool faceCamera = true;

    [Header("Display")]
    public bool showMachineName = true;
    public bool showAxisCoordinates = true;
    public bool showProcessState = true;
    public bool showFaultState = true;

    [Header("Axis Simulation")]
    public bool simulateAxis = true;
    public float axisAmplitudeX = 120f;
    public float axisAmplitudeY = 60f;
    public float axisAmplitudeZ = 35f;

    [Header("Style")]
    public Color panelColor = new Color(0.045f, 0.052f, 0.06f, 0.9f);
    public Sprite backgroundSprite;
    public Color borderColor = new Color(0.1f, 0.85f, 0.9f, 0.75f);
    public Sprite borderSprite;
    public Vector2 borderInset = Vector2.zero;
    public Font customFont;
    public int titleFontSize = 22;
    public int axisFontSize = 18;
    public int stateFontSize = 18;
    public Color titleTextColor = Color.white;
    public Color axisTextColor = Color.white;
    public Color stateTextColor = Color.white;
    public Color idleProcessColor = new Color(0.55f, 0.55f, 0.55f, 1f);
    public Color processingProcessColor = new Color(0.1f, 0.85f, 0.25f, 1f);
    public Color normalFaultColor = new Color(0.1f, 0.85f, 0.25f, 1f);
    public Color faultColor = new Color(0.95f, 0.12f, 0.08f, 1f);
    public Color toolWearColor = new Color(1f, 0.72f, 0.08f, 1f);
    public Vector2 statusBlockSize = new Vector2(16f, 16f);

    [Header("Data")]
    public string playbackDirectory = "C:/Users/ywc/Desktop/codex/matlab_workshop_model/output/unity_export_v2/simevents_stateflow_finaltransport_4m1agv";

    private const string RootName = "Machine_Side_Status_Panel";

    private WorkshopResourceIdentity identity;
    private MatlabPlaybackController playbackController;
    private RectTransform rootRect;
    private Text titleText;
    private Text xText;
    private Text yText;
    private Text zText;
    private Text processText;
    private Text faultText;
    private Image backgroundImage;
    private Image borderImage;
    private Image processBlock;
    private Image faultBlock;
    private Font uiFont;
    private readonly List<DisturbanceWindow> disturbances = new List<DisturbanceWindow>();
    private bool disturbancesLoaded;

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        Initialize();
    }

    private void LateUpdate()
    {
        Initialize();
        if (rootRect == null)
        {
            return;
        }

        rootRect.position = transform.position + worldOffset;
        FaceCamera();
        RefreshText();
    }

    public void RebuildPanel()
    {
        Transform existing = transform.Find(RootName);
        if (existing != null)
        {
            DestroyImmediateSafe(existing.gameObject);
        }

        rootRect = null;
        titleText = null;
        xText = null;
        yText = null;
        zText = null;
        processText = null;
        faultText = null;
        processBlock = null;
        faultBlock = null;
        EnsurePanel();
        RefreshText();
    }

    private void Initialize()
    {
        if (identity == null)
        {
            identity = GetComponent<WorkshopResourceIdentity>();
        }
        if (playbackController == null)
        {
            playbackController = FindObjectOfType<MatlabPlaybackController>(true);
        }
        if (string.IsNullOrWhiteSpace(resourceId) && identity != null)
        {
            resourceId = identity.resourceId;
        }
        if (string.IsNullOrWhiteSpace(displayName) && identity != null)
        {
            displayName = string.IsNullOrWhiteSpace(identity.displayName) ? gameObject.name : identity.displayName;
        }
        if (playbackController != null)
        {
            playbackDirectory = playbackController.playbackDirectory;
        }

        EnsurePanel();
        EnsureDisturbancesLoaded();
    }

    private void EnsurePanel()
    {
        if (uiFont == null)
        {
            uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        if (rootRect == null)
        {
            Transform existing = transform.Find(RootName);
            GameObject rootObject = existing != null ? existing.gameObject : new GameObject(RootName);
            rootObject.transform.SetParent(transform, false);
            rootRect = EnsureComponent<RectTransform>(rootObject);

            Canvas canvas = EnsureComponent<Canvas>(rootObject);
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 60;
            EnsureComponent<CanvasScaler>(rootObject).dynamicPixelsPerUnit = 20f;
            EnsureComponent<GraphicRaycaster>(rootObject);
        }

        rootRect.sizeDelta = panelSize;
        rootRect.localScale = Vector3.one * panelScale;

        backgroundImage = EnsureComponent<Image>(rootRect.gameObject);
        ApplyImageStyle(backgroundImage, panelColor, backgroundSprite);
        EnsureBorder();

        titleText = EnsureText("Title", displayName, titleFontSize, FontStyle.Bold, TextAnchor.MiddleLeft, titleTextColor);
        SetRect(titleText.rectTransform, new Vector2(12f, -12f), new Vector2(-12f, -42f));
        titleText.gameObject.SetActive(showMachineName);

        xText = EnsureText("Axis_X", "X: 0.000", axisFontSize, FontStyle.Bold, TextAnchor.MiddleLeft, axisTextColor);
        SetRect(xText.rectTransform, new Vector2(12f, -44f), new Vector2(-12f, -68f));
        xText.gameObject.SetActive(showAxisCoordinates);

        yText = EnsureText("Axis_Y", "Y: 0.000", axisFontSize, FontStyle.Bold, TextAnchor.MiddleLeft, axisTextColor);
        SetRect(yText.rectTransform, new Vector2(12f, -68f), new Vector2(-12f, -92f));
        yText.gameObject.SetActive(showAxisCoordinates);

        zText = EnsureText("Axis_Z", "Z: 0.000", axisFontSize, FontStyle.Bold, TextAnchor.MiddleLeft, axisTextColor);
        SetRect(zText.rectTransform, new Vector2(12f, -92f), new Vector2(-12f, -116f));
        zText.gameObject.SetActive(showAxisCoordinates);

        processBlock = EnsureColorBlock("Process_Block", new Vector2(13f, -130f));
        processBlock.gameObject.SetActive(showProcessState);
        processText = EnsureText("Process_State", "\u52a0\u5de5\u72b6\u6001: \u7a7a\u95f2", stateFontSize, FontStyle.Normal, TextAnchor.MiddleLeft, stateTextColor);
        SetRect(processText.rectTransform, new Vector2(36f, -121f), new Vector2(-12f, -145f));
        processText.gameObject.SetActive(showProcessState);

        faultBlock = EnsureColorBlock("Fault_Block", new Vector2(13f, -154f));
        faultBlock.gameObject.SetActive(showFaultState);
        faultText = EnsureText("Fault_State", "\u6545\u969c\u72b6\u6001: \u6b63\u5e38", stateFontSize, FontStyle.Normal, TextAnchor.MiddleLeft, stateTextColor);
        SetRect(faultText.rectTransform, new Vector2(36f, -145f), new Vector2(-12f, -169f));
        faultText.gameObject.SetActive(showFaultState);
    }

    private void RefreshText()
    {
        string currentState = identity != null ? identity.initialState : "Idle";
        bool processing = currentState == "Processing";
        float time = playbackController != null ? playbackController.playbackTime : Time.time;
        Vector3 axis = CalculateAxis(time, processing);
        FaultInfo fault = ResolveFault(time);

        titleText.text = string.IsNullOrWhiteSpace(displayName) ? resourceId : displayName;
        xText.text = string.Format(CultureInfo.InvariantCulture, "X: {0,7:0.000}", axis.x);
        yText.text = string.Format(CultureInfo.InvariantCulture, "Y: {0,7:0.000}", axis.y);
        zText.text = string.Format(CultureInfo.InvariantCulture, "Z: {0,7:0.000}", axis.z);
        processText.text = "\u52a0\u5de5\u72b6\u6001: " + (processing ? "\u52a0\u5de5\u4e2d" : "\u7a7a\u95f2");
        faultText.text = "\u6545\u969c\u72b6\u6001: " + fault.label;
        processBlock.color = processing ? processingProcessColor : idleProcessColor;
        faultBlock.color = fault.color;
    }

    private Vector3 CalculateAxis(float time, bool processing)
    {
        if (!simulateAxis || !processing)
        {
            return Vector3.zero;
        }

        float phase = Mathf.Abs(resourceId.GetHashCode() % 100) * 0.01f;
        return new Vector3(
            Mathf.Sin(time * 1.6f + phase) * axisAmplitudeX,
            Mathf.Cos(time * 1.1f + phase) * axisAmplitudeY,
            Mathf.Sin(time * 2.0f + phase) * axisAmplitudeZ);
    }

    private FaultInfo ResolveFault(float time)
    {
        foreach (DisturbanceWindow item in disturbances)
        {
            if (item.targetId != resourceId)
            {
                continue;
            }
            if (time >= item.startTime && time <= item.endTime)
            {
                if (item.type.Contains("tool") || item.effect.Contains("tool"))
                {
                    return new FaultInfo("\u5200\u5177\u78e8\u635f", toolWearColor);
                }
                return new FaultInfo("\u6545\u969c", faultColor);
            }
        }

        return new FaultInfo("\u6b63\u5e38", normalFaultColor);
    }

    private void EnsureDisturbancesLoaded()
    {
        if (disturbancesLoaded)
        {
            return;
        }

        disturbancesLoaded = true;
        disturbances.Clear();
        string path = Path.Combine(playbackDirectory, "disturbance_markers.csv");
        if (!File.Exists(path))
        {
            return;
        }

        foreach (Dictionary<string, string> row in ReadCsv(path))
        {
            DisturbanceWindow item = new DisturbanceWindow();
            item.targetId = Get(row, "target_id");
            item.type = Get(row, "disturbance_type");
            item.effect = Get(row, "effect");
            item.startTime = GetFloat(row, "start_time");
            item.endTime = GetFloat(row, "end_time");
            if (item.endTime <= item.startTime)
            {
                item.endTime = item.startTime + 5f;
            }
            disturbances.Add(item);
        }
    }

    private Text EnsureText(string objectName, string textValue, int fontSize, FontStyle style, TextAnchor alignment, Color color)
    {
        Transform existing = rootRect.Find(objectName);
        GameObject textObject = existing != null ? existing.gameObject : new GameObject(objectName);
        textObject.transform.SetParent(rootRect, false);
        Text text = EnsureComponent<Text>(textObject);
        text.font = customFont != null ? customFont : uiFont;
        text.text = textValue;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }

    private Image EnsureColorBlock(string objectName, Vector2 anchoredPosition)
    {
        Transform existing = rootRect.Find(objectName);
        GameObject blockObject = existing != null ? existing.gameObject : new GameObject(objectName);
        blockObject.transform.SetParent(rootRect, false);
        Image image = EnsureComponent<Image>(blockObject);
        RectTransform rect = image.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = statusBlockSize;
        return image;
    }

    private void EnsureBorder()
    {
        Transform existing = rootRect.Find("Panel_Border");
        GameObject borderObject = existing != null ? existing.gameObject : new GameObject("Panel_Border");
        borderObject.transform.SetParent(rootRect, false);
        borderImage = EnsureComponent<Image>(borderObject);
        borderImage.raycastTarget = false;
        ApplyImageStyle(borderImage, borderColor, borderSprite);

        RectTransform rect = borderImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = borderInset;
        rect.offsetMax = -borderInset;
        rect.SetAsLastSibling();

        borderObject.SetActive(borderSprite != null);
    }

    private static void ApplyImageStyle(Image image, Color color, Sprite sprite)
    {
        image.color = color;
        image.sprite = sprite;
        image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.preserveAspect = false;
        image.raycastTarget = false;
    }

    private static void SetRect(RectTransform rect, Vector2 topLeft, Vector2 bottomRight)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(topLeft.x, bottomRight.y);
        rect.offsetMax = new Vector2(bottomRight.x, topLeft.y);
    }

    private void FaceCamera()
    {
        if (!faceCamera)
        {
            return;
        }

        Camera targetCamera = Camera.main;
        if (targetCamera == null)
        {
            return;
        }

        Vector3 direction = rootRect.position - targetCamera.transform.position;
        if (direction.sqrMagnitude > 0.0001f)
        {
            rootRect.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
    }

    private static T EnsureComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        if (component == null)
        {
            component = target.AddComponent<T>();
        }
        return component;
    }

    private static IEnumerable<Dictionary<string, string>> ReadCsv(string path)
    {
        string[] lines = File.ReadAllLines(path);
        if (lines.Length < 2)
        {
            yield break;
        }

        string[] headers = SplitCsvLine(lines[0]);
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
            {
                continue;
            }

            string[] values = SplitCsvLine(lines[i]);
            Dictionary<string, string> row = new Dictionary<string, string>();
            for (int h = 0; h < headers.Length && h < values.Length; h++)
            {
                row[headers[h]] = values[h];
            }
            yield return row;
        }
    }

    private static string[] SplitCsvLine(string line)
    {
        List<string> values = new List<string>();
        bool inQuotes = false;
        string current = "";
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                values.Add(current);
                current = "";
            }
            else
            {
                current += c;
            }
        }
        values.Add(current);
        return values.ToArray();
    }

    private static string Get(Dictionary<string, string> row, string key)
    {
        string value;
        return row.TryGetValue(key, out value) ? value.Trim() : "";
    }

    private static float GetFloat(Dictionary<string, string> row, string key)
    {
        float value;
        return float.TryParse(Get(row, key), NumberStyles.Float, CultureInfo.InvariantCulture, out value) ? value : 0f;
    }

    private static void DestroyImmediateSafe(Object target)
    {
        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
    }

    private class DisturbanceWindow
    {
        public string targetId;
        public string type;
        public string effect;
        public float startTime;
        public float endTime;
    }

    private struct FaultInfo
    {
        public readonly string label;
        public readonly Color color;

        public FaultInfo(string label, Color color)
        {
            this.label = label;
            this.color = color;
        }
    }
}
