using UnityEngine;
using UnityEngine.UI;

public class OrderVisual : MonoBehaviour
{
    public string orderId;
    public string partId;
    public float moveSmoothing = 18f;
    public bool smoothMotion = true;
    public bool showFloatingId = true;
    public Vector3 labelLocalOffset = new Vector3(0f, 0.42f, 0f);

    private Transform body;
    private Canvas labelCanvas;
    private Text labelText;
    private Vector3 targetPosition;
    private Quaternion targetRotation = Quaternion.identity;

    public void Initialize(string newOrderId, string newPartId, Material sharedMaterial, Vector3 bodyScale)
    {
        orderId = newOrderId;
        partId = newPartId;
        name = "Order_" + orderId;

        if (body == null)
        {
            Transform existing = transform.Find("PartBody");
            if (existing != null)
            {
                body = existing;
            }
            else
            {
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = "PartBody";
                cube.transform.SetParent(transform, false);
                body = cube.transform;

                Collider cubeCollider = cube.GetComponent<Collider>();
                if (cubeCollider != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(cubeCollider);
                    }
                    else
                    {
                        DestroyImmediate(cubeCollider);
                    }
                }
            }
        }

        body.localPosition = Vector3.zero;
        body.localRotation = Quaternion.identity;
        body.localScale = bodyScale;

        Renderer renderer = body.GetComponent<Renderer>();
        if (renderer != null && sharedMaterial != null)
        {
            renderer.sharedMaterial = sharedMaterial;
        }

        EnsureFloatingLabel();
        if (labelText != null)
        {
            labelText.text = string.IsNullOrWhiteSpace(orderId) ? "Order" : orderId;
        }
        if (labelCanvas != null)
        {
            labelCanvas.gameObject.SetActive(showFloatingId);
        }
    }

    public void SetTarget(Vector3 position, Quaternion rotation, bool visible, bool immediate)
    {
        targetPosition = position;
        targetRotation = rotation;
        if (gameObject.activeSelf != visible)
        {
            gameObject.SetActive(visible);
        }

        if (immediate || !smoothMotion)
        {
            transform.position = targetPosition;
            transform.rotation = targetRotation;
        }
    }

    private void Awake()
    {
        targetPosition = transform.position;
        targetRotation = transform.rotation;
    }

    private void Update()
    {
        if (!smoothMotion)
        {
            return;
        }

        float t = 1f - Mathf.Exp(-moveSmoothing * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, targetPosition, t);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, t);
        FaceLabelToCamera();
    }

    private void LateUpdate()
    {
        FaceLabelToCamera();
    }

    private void EnsureFloatingLabel()
    {
        if (labelCanvas != null)
        {
            return;
        }

        Transform existing = transform.Find("FloatingOrderId");
        if (existing != null)
        {
            labelCanvas = existing.GetComponent<Canvas>();
            labelText = existing.GetComponentInChildren<Text>(true);
            return;
        }

        GameObject canvasObject = new GameObject("FloatingOrderId");
        canvasObject.transform.SetParent(transform, false);
        canvasObject.transform.localPosition = labelLocalOffset;
        canvasObject.transform.localRotation = Quaternion.identity;
        canvasObject.transform.localScale = Vector3.one * 0.012f;

        labelCanvas = canvasObject.AddComponent<Canvas>();
        labelCanvas.renderMode = RenderMode.WorldSpace;
        labelCanvas.sortingOrder = 30;

        RectTransform canvasRect = labelCanvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(96f, 36f);

        GameObject background = new GameObject("Background");
        background.transform.SetParent(canvasObject.transform, false);
        Image backgroundImage = background.AddComponent<Image>();
        backgroundImage.color = new Color(0.06f, 0.07f, 0.08f, 0.88f);
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        GameObject textObject = new GameObject("Text");
        textObject.transform.SetParent(canvasObject.transform, false);
        labelText = textObject.AddComponent<Text>();
        labelText.font = WorkshopFontProvider.GetFont();
        labelText.text = orderId;
        labelText.fontSize = 20;
        labelText.fontStyle = FontStyle.Bold;
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.color = Color.white;
        labelText.horizontalOverflow = HorizontalWrapMode.Overflow;
        labelText.verticalOverflow = VerticalWrapMode.Truncate;

        RectTransform textRect = labelText.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(4f, 1f);
        textRect.offsetMax = new Vector2(-4f, -1f);
    }

    private void FaceLabelToCamera()
    {
        if (labelCanvas == null || !labelCanvas.gameObject.activeInHierarchy)
        {
            return;
        }

        Camera targetCamera = Camera.main;
        if (targetCamera == null)
        {
            return;
        }

        labelCanvas.transform.localPosition = labelLocalOffset;
        labelCanvas.transform.rotation = Quaternion.LookRotation(labelCanvas.transform.position - targetCamera.transform.position, Vector3.up);
    }
}
