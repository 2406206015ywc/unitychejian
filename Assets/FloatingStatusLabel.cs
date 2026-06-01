using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class FloatingStatusLabel : MonoBehaviour
{
    public string displayName = "\u8bbe\u5907";
    public string state = "Idle";
    public Vector3 offset = new Vector3(0f, 2.8f, 0f);
    public Vector2 panelSize = new Vector2(150f, 54f);
    public bool tintRenderers = false;
    public bool showLabel = true;

    private const string RootName = "Floating_Status_Label";
    private const string PanelName = "Status_Panel";
    private const string ColorBlockName = "Status_Color_Block";
    private const string TextName = "Status_Text";

    private RectTransform rootRect;
    private Image panelImage;
    private Image colorBlockImage;
    private Text stateText;
    private string lastDisplayName;
    private string lastState;
    private Vector2 lastPanelSize;

    private void Awake()
    {
        Refresh();
    }

    private void OnEnable()
    {
        Refresh();
    }

    private void OnValidate()
    {
        lastDisplayName = null;
        lastState = null;
    }

    private void LateUpdate()
    {
        if (!showLabel)
        {
            if (rootRect != null && rootRect.gameObject.activeSelf)
            {
                rootRect.gameObject.SetActive(false);
            }
            return;
        }

        EnsureUi();
        if (!rootRect.gameObject.activeSelf)
        {
            rootRect.gameObject.SetActive(true);
        }
        rootRect.position = transform.position + offset;
        FaceCamera();

        if (lastDisplayName != displayName || lastState != state || lastPanelSize != panelSize)
        {
            ApplyState();
        }
    }

    public void SetState(string newState)
    {
        state = string.IsNullOrWhiteSpace(newState) ? "Idle" : newState.Trim();
        ApplyState();
    }

    public void SetDisplayName(string newDisplayName)
    {
        displayName = string.IsNullOrWhiteSpace(newDisplayName) ? gameObject.name : newDisplayName.Trim();
        ApplyState();
    }

    public void ClearResourceTint()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer targetRenderer in renderers)
        {
            if (targetRenderer == null || (rootRect != null && targetRenderer.transform.IsChildOf(rootRect)))
            {
                continue;
            }

            targetRenderer.SetPropertyBlock(null);
        }
    }

    public void RebuildUi()
    {
        Transform existing = transform.Find(RootName);
        if (existing != null)
        {
            DestroyImmediateSafe(existing.gameObject);
        }

        rootRect = null;
        panelImage = null;
        colorBlockImage = null;
        stateText = null;
        EnsureUi();
        ApplyState();
    }

    public static string StateText(string value)
    {
        switch (value)
        {
            case "Idle":
                return "\u7a7a\u95f2";
            case "Processing":
                return "\u52a0\u5de5\u4e2d";
            case "Down":
                return "\u505c\u673a";
            case "Recovering":
                return "\u6062\u590d\u4e2d";
            case "Moving":
                return "\u79fb\u52a8\u4e2d";
            case "Delayed":
                return "\u5ef6\u8fdf";
            case "Waiting":
                return "\u7b49\u5f85";
            default:
                return string.IsNullOrWhiteSpace(value) ? "\u672a\u77e5" : value;
        }
    }

    public static Color StateColor(string value)
    {
        switch (value)
        {
            case "Processing":
                return new Color(0.1f, 0.85f, 0.25f, 1f);
            case "Down":
                return new Color(0.95f, 0.1f, 0.08f, 1f);
            case "Recovering":
                return new Color(1f, 0.82f, 0.08f, 1f);
            case "Moving":
                return new Color(0.1f, 0.45f, 1f, 1f);
            case "Delayed":
                return new Color(1f, 0.48f, 0.05f, 1f);
            case "Waiting":
                return new Color(0.65f, 0.25f, 0.9f, 1f);
            default:
                return new Color(0.55f, 0.55f, 0.55f, 1f);
        }
    }

    private void Refresh()
    {
        EnsureUi();
        ClearResourceTint();
        ApplyState();
    }

    private void EnsureUi()
    {
        if (rootRect == null)
        {
            Transform existing = transform.Find(RootName);
            GameObject rootObject = existing != null ? existing.gameObject : new GameObject(RootName);
            rootObject.transform.SetParent(transform, false);
            rootRect = EnsureComponent<RectTransform>(rootObject);

            Canvas canvas = EnsureComponent<Canvas>(rootObject);
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 50;

            EnsureComponent<CanvasScaler>(rootObject);
            EnsureComponent<GraphicRaycaster>(rootObject);
        }

        rootRect.sizeDelta = panelSize;
        rootRect.localScale = Vector3.one * 0.01f;

        RectTransform panelRect = EnsureChildRect(PanelName);
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        panelImage = EnsureComponent<Image>(panelRect.gameObject);

        RectTransform blockRect = EnsureChildRect(ColorBlockName);
        blockRect.anchorMin = new Vector2(0f, 0.5f);
        blockRect.anchorMax = new Vector2(0f, 0.5f);
        blockRect.pivot = new Vector2(0.5f, 0.5f);
        blockRect.sizeDelta = new Vector2(20f, 20f);
        blockRect.anchoredPosition = new Vector2(22f, 0f);
        colorBlockImage = EnsureComponent<Image>(blockRect.gameObject);

        RectTransform textRect = EnsureChildRect(TextName);
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.offsetMin = new Vector2(42f, 0f);
        textRect.offsetMax = new Vector2(-8f, 0f);
        stateText = EnsureComponent<Text>(textRect.gameObject);
        stateText.alignment = TextAnchor.MiddleLeft;
        stateText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        stateText.fontSize = 20;
        stateText.color = Color.white;
        stateText.raycastTarget = false;
    }

    private RectTransform EnsureChildRect(string childName)
    {
        Transform existing = rootRect.Find(childName);
        GameObject childObject = existing != null ? existing.gameObject : new GameObject(childName);
        childObject.transform.SetParent(rootRect, false);
        return EnsureComponent<RectTransform>(childObject);
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

    private void ApplyState()
    {
        EnsureUi();
        panelImage.color = new Color(0.04f, 0.05f, 0.06f, 0.88f);
        colorBlockImage.color = StateColor(state);
        stateText.text = displayName + "  " + StateText(state);

        lastDisplayName = displayName;
        lastState = state;
        lastPanelSize = panelSize;
    }

    private void FaceCamera()
    {
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
}
