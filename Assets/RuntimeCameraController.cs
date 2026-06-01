using UnityEngine;
using UnityEngine.EventSystems;

public class RuntimeCameraController : MonoBehaviour
{
    public float moveSpeed = 7f;
    public float fastMoveMultiplier = 2.5f;
    public float lookSensitivity = 2.2f;
    public float scrollSpeed = 9f;
    public float minPitch = -10f;
    public float maxPitch = 82f;
    public bool ignoreInputWhenPointerOverUi = true;

    private float yaw;
    private float pitch;

    private void Awake()
    {
        Vector3 euler = transform.eulerAngles;
        yaw = euler.y;
        pitch = NormalizeAngle(euler.x);
    }

    private void Update()
    {
        if (ignoreInputWhenPointerOverUi && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        HandleLook();
        HandleMove();
        HandleScroll();
    }

    private void HandleLook()
    {
        if (!Input.GetMouseButton(1))
        {
            return;
        }

        yaw += Input.GetAxis("Mouse X") * lookSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * lookSensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void HandleMove()
    {
        Vector3 input = Vector3.zero;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            input += Vector3.forward;
        }
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            input += Vector3.back;
        }
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            input += Vector3.left;
        }
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            input += Vector3.right;
        }
        if (Input.GetKey(KeyCode.E))
        {
            input += Vector3.up;
        }
        if (Input.GetKey(KeyCode.Q))
        {
            input += Vector3.down;
        }

        if (input.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        float speed = moveSpeed * (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? fastMoveMultiplier : 1f);
        Vector3 movement = transform.TransformDirection(input.normalized) * speed * Time.deltaTime;
        transform.position += movement;
    }

    private void HandleScroll()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) <= 0.0001f)
        {
            return;
        }

        transform.position += transform.forward * scroll * scrollSpeed;
    }

    private static float NormalizeAngle(float angle)
    {
        while (angle > 180f)
        {
            angle -= 360f;
        }
        while (angle < -180f)
        {
            angle += 360f;
        }
        return angle;
    }
}
