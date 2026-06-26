using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

[InitializeOnLoad]
public static class WorkshopDashboardScrollSetup
{
    private const string AutoRunFlagRelativePath = "Library/RunWorkshopDashboardScrollSetup.flag";

    static WorkshopDashboardScrollSetup()
    {
        string flagPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, AutoRunFlagRelativePath);
        if (!File.Exists(flagPath))
        {
            return;
        }

        File.Delete(flagPath);
        EditorApplication.delayCall += UpgradeDashboardScrollAreas;
    }

    [MenuItem("Workshop/Upgrade Dashboard Scroll Areas")]
    public static void UpgradeDashboardScrollAreas()
    {
        GameObject hud = GameObject.Find("Workshop_Dashboard_HUD");
        if (hud == null)
        {
            Debug.LogError("[WorkshopDashboardScrollSetup] Workshop_Dashboard_HUD not found.");
            return;
        }

        SetModuleTitle(hud.transform, "Module_Current_Orders", "\u8ba2\u5355\u6982\u89c8");
        WrapTextInVerticalScrollView(hud.transform, "Module_Current_Orders", "Current_Orders", "Order_Overview_ScrollView");
        WrapTextInVerticalScrollView(hud.transform, "Module_Device_Status", "Device_Status", "Device_Status_ScrollView");
        DisableDecorationRaycasts(hud);

        EditorUtility.SetDirty(hud);
        EditorSceneManager.MarkSceneDirty(hud.scene);
        EditorSceneManager.SaveScene(hud.scene);
        Debug.Log("[WorkshopDashboardScrollSetup] Dashboard scroll areas upgraded and scene saved.");
    }

    private static void SetModuleTitle(Transform root, string moduleName, string title)
    {
        Transform module = FindDeepChild(root, moduleName);
        if (module == null)
        {
            return;
        }

        Transform titleTransform = module.Find("Title");
        Text titleText = titleTransform != null ? titleTransform.GetComponent<Text>() : null;
        if (titleText != null)
        {
            titleText.text = title;
            EditorUtility.SetDirty(titleText);
        }
    }

    private static void WrapTextInVerticalScrollView(Transform root, string moduleName, string textName, string scrollName)
    {
        Transform module = FindDeepChild(root, moduleName);
        if (module == null)
        {
            Debug.LogWarning("[WorkshopDashboardScrollSetup] Module not found: " + moduleName);
            return;
        }

        Transform textTransform = FindDeepChild(module, textName);
        Text text = textTransform != null ? textTransform.GetComponent<Text>() : null;
        if (text == null)
        {
            Debug.LogWarning("[WorkshopDashboardScrollSetup] Text not found: " + textName);
            return;
        }

        ScrollRect existingScroll = text.GetComponentInParent<ScrollRect>();
        if (existingScroll != null && existingScroll.transform.name == scrollName)
        {
            ConfigureScrollRect(existingScroll, text);
            return;
        }

        Transform oldParent = text.transform.parent;
        int siblingIndex = text.transform.GetSiblingIndex();
        LayoutElement oldLayout = text.GetComponent<LayoutElement>();
        if (oldLayout != null)
        {
            Object.DestroyImmediate(oldLayout);
        }

        GameObject scrollObject = new GameObject(scrollName, typeof(RectTransform), typeof(ScrollRect), typeof(LayoutElement));
        scrollObject.transform.SetParent(oldParent, false);
        scrollObject.transform.SetSiblingIndex(siblingIndex);
        LayoutElement scrollLayout = scrollObject.GetComponent<LayoutElement>();
        scrollLayout.flexibleHeight = 1f;
        scrollLayout.minHeight = 0f;

        RectTransform scrollRectTransform = scrollObject.GetComponent<RectTransform>();
        Stretch(scrollRectTransform, Vector2.zero);

        GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        viewportObject.transform.SetParent(scrollObject.transform, false);
        RectTransform viewport = viewportObject.GetComponent<RectTransform>();
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = Vector2.zero;
        viewport.offsetMax = new Vector2(-14f, 0f);
        Image viewportImage = viewportObject.GetComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0f);
        viewportImage.raycastTarget = true;

        GameObject contentObject = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentObject.transform.SetParent(viewportObject.transform, false);
        RectTransform content = contentObject.GetComponent<RectTransform>();
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.offsetMin = Vector2.zero;
        content.offsetMax = Vector2.zero;
        VerticalLayoutGroup contentLayout = contentObject.GetComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(0, 0, 0, 0);
        contentLayout.spacing = 0f;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;
        ContentSizeFitter contentFitter = contentObject.GetComponent<ContentSizeFitter>();
        contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        text.transform.SetParent(contentObject.transform, false);
        ConfigureText(text);

        Scrollbar verticalScrollbar = CreateVerticalScrollbar(scrollObject.transform);

        ScrollRect scrollRect = scrollObject.GetComponent<ScrollRect>();
        scrollRect.content = content;
        scrollRect.viewport = viewport;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.verticalScrollbar = verticalScrollbar;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        scrollRect.horizontalScrollbar = null;
        scrollRect.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.inertia = true;
        scrollRect.scrollSensitivity = 20f;

        EditorUtility.SetDirty(scrollObject);
        EditorUtility.SetDirty(text);
    }

    private static void ConfigureScrollRect(ScrollRect scrollRect, Text text)
    {
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.horizontalScrollbar = null;
        scrollRect.scrollSensitivity = 20f;
        ConfigureText(text);
    }

    private static void ConfigureText(Text text)
    {
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;

        RectTransform rect = text.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = Vector2.zero;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static Scrollbar CreateVerticalScrollbar(Transform parent)
    {
        GameObject scrollbarObject = new GameObject("Scrollbar Vertical", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        scrollbarObject.transform.SetParent(parent, false);
        RectTransform scrollbarRect = scrollbarObject.GetComponent<RectTransform>();
        scrollbarRect.anchorMin = new Vector2(1f, 0f);
        scrollbarRect.anchorMax = new Vector2(1f, 1f);
        scrollbarRect.pivot = new Vector2(1f, 0.5f);
        scrollbarRect.sizeDelta = new Vector2(12f, 0f);
        scrollbarRect.anchoredPosition = Vector2.zero;
        Image scrollbarImage = scrollbarObject.GetComponent<Image>();
        scrollbarImage.color = new Color(1f, 1f, 1f, 0.12f);
        scrollbarImage.raycastTarget = true;

        GameObject slidingArea = new GameObject("Sliding Area", typeof(RectTransform));
        slidingArea.transform.SetParent(scrollbarObject.transform, false);
        Stretch(slidingArea.GetComponent<RectTransform>(), Vector2.zero);

        GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(slidingArea.transform, false);
        Stretch(handle.GetComponent<RectTransform>(), Vector2.zero);
        Image handleImage = handle.GetComponent<Image>();
        handleImage.color = new Color(0.25f, 0.85f, 0.95f, 0.85f);
        handleImage.raycastTarget = true;

        Scrollbar scrollbar = scrollbarObject.GetComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.targetGraphic = handleImage;
        scrollbar.handleRect = handle.GetComponent<RectTransform>();
        scrollbar.value = 1f;
        return scrollbar;
    }

    private static void DisableDecorationRaycasts(GameObject hud)
    {
        Image[] images = hud.GetComponentsInChildren<Image>(true);
        foreach (Image image in images)
        {
            bool isButtonGraphic = image.GetComponentInParent<Button>() != null;
            bool isScrollbarGraphic = image.GetComponentInParent<Scrollbar>() != null;
            bool isViewport = image.transform.name == "Viewport";
            image.raycastTarget = isButtonGraphic || isScrollbarGraphic || isViewport;
            EditorUtility.SetDirty(image);
        }
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

    private static void Stretch(RectTransform rect, Vector2 padding)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = padding;
        rect.offsetMax = -padding;
    }
}
