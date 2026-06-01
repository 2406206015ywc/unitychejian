using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class WorkshopSceneSetup
{
    private const string ScenePath = "Assets/Scenes/CodexXietong_Main.unity";

    [MenuItem("Codex/Setup Workshop Scene")]
    public static void SetupWorkshopScene()
    {
        EditorSceneManager.OpenScene(ScenePath);

        ConfigureResource("\u673a\u5e8a1", "M1", "\u673a\u5e8a1", "Idle", new Vector3(0f, 2.8f, 0f), 0.12f);
        ConfigureResource("\u673a\u5e8a2", "M2", "\u673a\u5e8a2", "Idle", new Vector3(0f, 2.8f, 0f), 0.12f);
        ConfigureResource("\u673a\u5e8a3", "M3", "\u673a\u5e8a3", "Idle", new Vector3(0f, 2.8f, 0f), 0.12f);
        ConfigureResource("\u673a\u5e8a4", "M4", "\u673a\u5e8a4", "Idle", new Vector3(0f, 2.8f, 0f), 0.12f);

        GameObject agv = ConfigureResource("AGV_01", "AGV_01", "AGV_01", "Waiting", new Vector3(0f, 1.4f, 0f), 0.1f);
        ConfigureAgvPath(agv);
        OrderVisualManager orderVisualManager = ConfigureOrderVisualManager();
        DisturbanceEventManager disturbanceEventManager = ConfigureDisturbanceEventManager();
        MatlabPlaybackController playbackController = ConfigurePlaybackController(orderVisualManager, disturbanceEventManager);
        ConfigurePlaybackHud(playbackController);
        ConfigureRuntimeCamera();
        ConfigureWorkshopEnvelope();
        ConfigureStaticPresentationText();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("Codex workshop scene setup completed.");
    }

    private static GameObject ConfigureResource(string objectName, string resourceId, string displayName, string state, Vector3 offset, float textSize)
    {
        GameObject target = GameObject.Find(objectName);
        if (target == null)
        {
            Debug.LogWarning("Workshop resource not found: " + objectName);
            return null;
        }

        FloatingStatusLabel label = target.GetComponent<FloatingStatusLabel>();
        if (label == null)
        {
            label = target.AddComponent<FloatingStatusLabel>();
        }

        label.displayName = displayName;
        label.state = state;
        label.offset = offset;
        label.tintRenderers = false;
        label.panelSize = new Vector2(150f, 54f);
        label.ClearResourceTint();
        label.RebuildUi();

        WorkshopResourceIdentity identity = target.GetComponent<WorkshopResourceIdentity>();
        if (identity == null)
        {
            identity = target.AddComponent<WorkshopResourceIdentity>();
        }

        identity.resourceId = resourceId;
        identity.displayName = displayName;
        identity.initialState = state;
        identity.Apply();

        EditorUtility.SetDirty(target);
        return target;
    }

    private static void ConfigureAgvPath(GameObject agv)
    {
        if (agv == null)
        {
            return;
        }

        ManualAgvPathMover mover = agv.GetComponent<ManualAgvPathMover>();
        if (mover == null)
        {
            mover = agv.AddComponent<ManualAgvPathMover>();
        }

        mover.waypoints = new[]
        {
            FindTransform("Node_Raw"),
            FindTransform("Node_M1"),
            FindTransform("Node_Aisle_Left"),
            FindTransform("Node_Aisle_Center"),
            FindTransform("Node_Aisle_Right"),
            FindTransform("Node_M2")
        };
        mover.speed = 1.8f;
        mover.playOnStart = false;
        mover.loop = true;
        mover.enabled = false;

        EditorUtility.SetDirty(agv);
    }

    private static OrderVisualManager ConfigureOrderVisualManager()
    {
        GameObject managerObject = GameObject.Find("Order_Visual_Manager");
        if (managerObject == null)
        {
            managerObject = new GameObject("Order_Visual_Manager");
        }

        OrderVisualManager manager = managerObject.GetComponent<OrderVisualManager>();
        if (manager == null)
        {
            manager = managerObject.AddComponent<OrderVisualManager>();
        }

        manager.playbackDirectory = "C:/Users/ywc/Desktop/codex/matlab_workshop_model/output/unity_export_v2/simevents_stateflow_finaltransport_4m1agv";
        manager.agvObjectName = "AGV_01";
        manager.partScale = new Vector3(0.45f, 0.24f, 0.45f);
        manager.groundHeight = 0.22f;
        manager.processingHeight = 0.8f;
        manager.processingForwardOffset = 0.05f;

        EditorUtility.SetDirty(managerObject);
        return manager;
    }

    private static DisturbanceEventManager ConfigureDisturbanceEventManager()
    {
        GameObject managerObject = GameObject.Find("Disturbance_Event_Manager");
        if (managerObject == null)
        {
            managerObject = new GameObject("Disturbance_Event_Manager");
        }

        DisturbanceEventManager manager = managerObject.GetComponent<DisturbanceEventManager>();
        if (manager == null)
        {
            manager = managerObject.AddComponent<DisturbanceEventManager>();
        }

        manager.playbackDirectory = "C:/Users/ywc/Desktop/codex/matlab_workshop_model/output/unity_export_v2/simevents_stateflow_finaltransport_4m1agv";
        manager.agvObjectName = "AGV_01";
        manager.markerScale = 0.012f;
        manager.zeroDurationHoldSeconds = 5f;
        manager.showAgvWorldMarkers = false;

        EditorUtility.SetDirty(managerObject);
        return manager;
    }

    private static MatlabPlaybackController ConfigurePlaybackController(OrderVisualManager orderVisualManager, DisturbanceEventManager disturbanceEventManager)
    {
        GameObject controllerObject = GameObject.Find("MATLAB_Playback_Controller");
        if (controllerObject == null)
        {
            controllerObject = new GameObject("MATLAB_Playback_Controller");
        }

        MatlabPlaybackController controller = controllerObject.GetComponent<MatlabPlaybackController>();
        if (controller == null)
        {
            controller = controllerObject.AddComponent<MatlabPlaybackController>();
        }

        controller.playbackDirectory = "C:/Users/ywc/Desktop/codex/matlab_workshop_model/output/unity_export_v2/simevents_stateflow_finaltransport_4m1agv";
        controller.agvObjectName = "AGV_01";
        controller.useUnitySceneNodes = true;
        controller.useAisleRouting = true;
        controller.showDebugOnGui = false;
        controller.playOnStart = false;
        controller.playbackSpeed = 1f;
        controller.orderVisualManager = orderVisualManager;
        controller.disturbanceEventManager = disturbanceEventManager;

        EditorUtility.SetDirty(controllerObject);
        return controller;
    }

    private static void ConfigurePlaybackHud(MatlabPlaybackController playbackController)
    {
        GameObject hudObject = GameObject.Find("Workshop_Playback_HUD");
        if (hudObject == null)
        {
            hudObject = new GameObject("Workshop_Playback_HUD");
        }

        WorkshopPlaybackHud hud = hudObject.GetComponent<WorkshopPlaybackHud>();
        if (hud == null)
        {
            hud = hudObject.AddComponent<WorkshopPlaybackHud>();
        }

        hud.controller = playbackController;
        hud.rebuildOnAwake = true;
        hud.RebuildHud();

        EditorUtility.SetDirty(hudObject);
    }

    private static void ConfigureRuntimeCamera()
    {
        Camera camera = Camera.main;
        GameObject cameraObject;
        if (camera != null)
        {
            cameraObject = camera.gameObject;
        }
        else
        {
            cameraObject = GameObject.Find("Main Camera");
            if (cameraObject == null)
            {
                cameraObject = new GameObject("Main Camera");
            }

            camera = cameraObject.GetComponent<Camera>();
            if (camera == null)
            {
                camera = cameraObject.AddComponent<Camera>();
            }
            cameraObject.tag = "MainCamera";
        }

        cameraObject.transform.position = new Vector3(0f, 8.5f, -12f);
        cameraObject.transform.rotation = Quaternion.Euler(36f, 0f, 0f);
        camera.fieldOfView = 55f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 1000f;

        RuntimeCameraController controller = cameraObject.GetComponent<RuntimeCameraController>();
        if (controller == null)
        {
            controller = cameraObject.AddComponent<RuntimeCameraController>();
        }

        controller.moveSpeed = 7f;
        controller.fastMoveMultiplier = 2.5f;
        controller.lookSensitivity = 2.2f;
        controller.scrollSpeed = 9f;
        controller.ignoreInputWhenPointerOverUi = true;

        EditorUtility.SetDirty(cameraObject);
    }

    private static void ConfigureWorkshopEnvelope()
    {
        GameObject envelope = GameObject.Find("Workshop_Building_Envelope");
        if (envelope == null)
        {
            envelope = new GameObject("Workshop_Building_Envelope");
        }

        Material wallMaterial = GetOrCreateMaterial("Assets/Materials/Workshop_Wall_Material.mat", new Color(0.62f, 0.67f, 0.68f, 1f), false);
        Material roofMaterial = GetOrCreateMaterial("Assets/Materials/Workshop_Roof_Material.mat", new Color(0.72f, 0.82f, 0.90f, 1f), false);

        float centerX = -1.0f;
        float centerZ = -1.5f;
        float width = 42.0f;
        float depth = 21.75f;
        float wallThickness = 0.22f;
        float wallHeight = 8.4f;
        float roofThickness = 0.18f;
        float roofY = wallHeight + roofThickness * 0.5f;
        float wallY = wallHeight * 0.5f;

        ConfigureEnvelopeCube("Wall_North", envelope.transform, wallMaterial, new Vector3(centerX, wallY, centerZ + depth * 0.5f), new Vector3(width, wallHeight, wallThickness));
        ConfigureEnvelopeCube("Wall_South", envelope.transform, wallMaterial, new Vector3(centerX, wallY, centerZ - depth * 0.5f), new Vector3(width, wallHeight, wallThickness));
        ConfigureEnvelopeCube("Wall_West", envelope.transform, wallMaterial, new Vector3(centerX - width * 0.5f, wallY, centerZ), new Vector3(wallThickness, wallHeight, depth));
        ConfigureEnvelopeCube("Wall_East", envelope.transform, wallMaterial, new Vector3(centerX + width * 0.5f, wallY, centerZ), new Vector3(wallThickness, wallHeight, depth));
        ConfigureEnvelopeCube("Roof", envelope.transform, roofMaterial, new Vector3(centerX, roofY, centerZ), new Vector3(width, roofThickness, depth));

        EditorUtility.SetDirty(envelope);
    }

    private static void ConfigureEnvelopeCube(string objectName, Transform parent, Material material, Vector3 position, Vector3 scale)
    {
        Transform existing = parent.Find(objectName);
        GameObject cube = existing != null ? existing.gameObject : GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = objectName;
        cube.transform.SetParent(parent, true);
        cube.transform.position = position;
        cube.transform.rotation = Quaternion.identity;
        cube.transform.localScale = scale;

        Renderer renderer = cube.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
        }

        Collider collider = cube.GetComponent<Collider>();
        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }

        EditorUtility.SetDirty(cube);
    }

    private static Material GetOrCreateMaterial(string path, Color color, bool transparent)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            string folder = System.IO.Path.GetDirectoryName(path);
            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder("Assets", "Materials");
            }

            Shader shader = Shader.Find("Standard");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.color = color;
        if (transparent)
        {
            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
        }
        else
        {
            material.SetFloat("_Mode", 0f);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            material.SetInt("_ZWrite", 1);
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = -1;
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    private static void ConfigureStaticPresentationText()
    {
        Transform[] transforms = Object.FindObjectsOfType<Transform>(true);
        foreach (Transform item in transforms)
        {
            if (item == null)
            {
                continue;
            }

            if (ShouldHideStaticTextLabel(item))
            {
                item.gameObject.SetActive(false);
                EditorUtility.SetDirty(item.gameObject);
            }
        }
    }

    private static bool ShouldHideStaticTextLabel(Transform item)
    {
        string itemName = item.name;
        string parentName = item.parent != null ? item.parent.name : "";

        if (itemName == "Raw_Label" || itemName == "Finished_Label")
        {
            return true;
        }

        if (itemName.StartsWith("Node_", System.StringComparison.Ordinal) && itemName.EndsWith("_Label", System.StringComparison.Ordinal))
        {
            return true;
        }

        if (itemName == "Machine_Label")
        {
            return true;
        }

        if (parentName == "Layout_Nodes_And_Zones" && item.GetComponent<TextMesh>() != null)
        {
            return true;
        }

        return false;
    }

    private static Transform FindTransform(string objectName)
    {
        GameObject target = GameObject.Find(objectName);
        if (target == null)
        {
            Debug.LogWarning("Path node not found: " + objectName);
            return null;
        }

        return target.transform;
    }
}
