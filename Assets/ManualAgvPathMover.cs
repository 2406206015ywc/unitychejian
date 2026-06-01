using UnityEngine;

[DisallowMultipleComponent]
public class ManualAgvPathMover : MonoBehaviour
{
    public Transform[] waypoints;
    public float speed = 1.8f;
    public bool playOnStart = true;
    public bool loop = true;
    public float reachDistance = 0.05f;
    public string movingState = "Moving";
    public string waitingState = "Waiting";

    private int currentTargetIndex = 1;
    private WorkshopResourceIdentity resourceIdentity;

    private void Awake()
    {
        resourceIdentity = GetComponent<WorkshopResourceIdentity>();
        SnapToFirstWaypoint();
    }

    private void Start()
    {
        SetAgvState(playOnStart ? movingState : waitingState);
    }

    private void Update()
    {
        if (!playOnStart || waypoints == null || waypoints.Length < 2)
        {
            SetAgvState(waitingState);
            return;
        }

        Transform target = waypoints[currentTargetIndex];
        if (target == null)
        {
            AdvanceTarget();
            return;
        }

        Vector3 nextPosition = Vector3.MoveTowards(transform.position, target.position, speed * Time.deltaTime);
        Vector3 moveVector = nextPosition - transform.position;
        transform.position = nextPosition;

        if (moveVector.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(moveVector.normalized, Vector3.up);
        }

        SetAgvState(movingState);

        if (Vector3.Distance(transform.position, target.position) <= reachDistance)
        {
            AdvanceTarget();
        }
    }

    public void ResetPath()
    {
        currentTargetIndex = 1;
        SnapToFirstWaypoint();
        SetAgvState(waitingState);
    }

    private void SnapToFirstWaypoint()
    {
        if (waypoints != null && waypoints.Length > 0 && waypoints[0] != null)
        {
            transform.position = waypoints[0].position;
        }
    }

    private void AdvanceTarget()
    {
        currentTargetIndex++;
        if (currentTargetIndex < waypoints.Length)
        {
            return;
        }

        if (loop)
        {
            currentTargetIndex = 0;
        }
        else
        {
            currentTargetIndex = Mathf.Max(waypoints.Length - 1, 0);
            playOnStart = false;
            SetAgvState(waitingState);
        }
    }

    private void SetAgvState(string state)
    {
        if (resourceIdentity != null)
        {
            resourceIdentity.SetState(state);
            return;
        }

        FloatingStatusLabel label = GetComponent<FloatingStatusLabel>();
        if (label != null)
        {
            label.SetState(state);
        }
    }
}
