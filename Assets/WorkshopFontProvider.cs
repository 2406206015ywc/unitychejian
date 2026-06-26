using UnityEngine;
using UnityEngine.UI;

public static class WorkshopFontProvider
{
    private const string ChineseFontResourcePath = "Fonts/MicrosoftYaHei-Regular";
    private static Font cachedFont;

    public static Font GetFont()
    {
        if (cachedFont == null)
        {
            cachedFont = Resources.Load<Font>(ChineseFontResourcePath);
        }

        return cachedFont != null ? cachedFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    public static void Apply(Text text)
    {
        if (text != null)
        {
            text.font = GetFont();
        }
    }

    public static void ApplyToChildren(Transform root)
    {
        if (root == null)
        {
            return;
        }

        Text[] texts = root.GetComponentsInChildren<Text>(true);
        foreach (Text text in texts)
        {
            Apply(text);
        }
    }
}
