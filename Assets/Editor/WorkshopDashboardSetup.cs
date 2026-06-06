using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class WorkshopDashboardSetup
{
    [MenuItem("Workshop/Create Editable Dashboard HUD")]
    public static void CreateEditableDashboardHud()
    {
        RemoveOldPlaybackHud();

        GameObject hudObject = GameObject.Find("Workshop_Dashboard_HUD");
        if (hudObject == null)
        {
            hudObject = new GameObject("Workshop_Dashboard_HUD");
        }

        WorkshopDashboardHud hud = hudObject.GetComponent<WorkshopDashboardHud>();
        if (hud == null)
        {
            hud = hudObject.AddComponent<WorkshopDashboardHud>();
        }

        hud.controller = Object.FindObjectOfType<MatlabPlaybackController>(true);
        hud.createDefaultLayoutIfMissing = false;
        hud.RebuildEditableLayout();

        EditorUtility.SetDirty(hudObject);
        EditorSceneManager.MarkSceneDirty(hudObject.scene);
        EditorSceneManager.SaveScene(hudObject.scene);
        Debug.Log("[WorkshopDashboardSetup] Editable dashboard HUD created and scene saved.");
    }

    public static void CreateEditableDashboardHudBatch()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/CodexXietong_Main.unity");
        CreateEditableDashboardHud();
    }

    private static void RemoveOldPlaybackHud()
    {
        GameObject oldHud = GameObject.Find("Workshop_Playback_HUD");
        if (oldHud != null)
        {
            Object.DestroyImmediate(oldHud);
        }
    }
}
