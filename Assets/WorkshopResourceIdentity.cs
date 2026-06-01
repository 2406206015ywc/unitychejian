using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(FloatingStatusLabel))]
public class WorkshopResourceIdentity : MonoBehaviour
{
    public string resourceId = "";
    public string displayName = "";
    public string initialState = "Idle";

    private FloatingStatusLabel statusLabel;

    private void Awake()
    {
        Apply();
    }

    private void OnEnable()
    {
        Apply();
    }

    private void OnValidate()
    {
        Apply();
    }

    public void SetState(string state)
    {
        initialState = state;
        Apply();
    }

    public void Apply()
    {
        statusLabel = GetComponent<FloatingStatusLabel>();
        if (statusLabel == null)
        {
            return;
        }

        statusLabel.displayName = string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName;
        statusLabel.state = string.IsNullOrWhiteSpace(initialState) ? "Idle" : initialState;
    }
}
