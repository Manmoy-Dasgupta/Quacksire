using UnityEngine;

/// <summary>
/// Smooth third-person orbit camera inspired by anime exploration games.
/// It drives the existing main Camera directly and disables Cinemachine behaviours at runtime to avoid double control.
/// </summary>
[DefaultExecutionOrder(200)]
[RequireComponent(typeof(Camera))]
public class GenshinThirdPersonCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private string targetTag = "Player";
    [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1.45f, 0f);

    [Header("Orbit")]
    [SerializeField] private float distance = 5.2f;
    [SerializeField] private float sprintDistanceBonus = 0.7f;
    [SerializeField] private float minDistance = 2.6f;
    [SerializeField] private float maxDistance = 7.4f;
    [SerializeField] private float yaw = 0f;
    [SerializeField] private float pitch = 17f;
    [SerializeField] private float minPitch = -8f;
    [SerializeField] private float maxPitch = 45f;
    [SerializeField] private float mouseSensitivityX = 145f;
    [SerializeField] private float mouseSensitivityY = 95f;
    [SerializeField] private float zoomSensitivity = 1.25f;

    [Header("Smoothing")]
    [SerializeField] private float positionSmoothTime = 0.08f;
    [SerializeField] private float rotationLerpSpeed = 16f;
    [SerializeField] private float distanceLerpSpeed = 8f;
    [SerializeField] private float normalFov = 60f;
    [SerializeField] private float sprintFov = 66f;
    [SerializeField] private float fovLerpSpeed = 5f;

    [Header("Collision")]
    [SerializeField] private LayerMask collisionMask = ~0;
    [SerializeField] private float collisionRadius = 0.28f;
    [SerializeField] private float collisionPadding = 0.18f;

    [Header("Input")]
    [SerializeField] private MobileCameraJoystick cameraJoystick;
    [SerializeField] private float joystickSensitivityX = 118f;
    [SerializeField] private float joystickSensitivityY = 78f;
    [SerializeField] private bool lockCursorOnStart;
    [SerializeField] private bool rotateWithoutMouseButton = true;
    [SerializeField] private bool disableCinemachineComponents = true;

    private Camera cam;
    private MobileJoystickPlayerMovement movement;
    private Vector3 positionVelocity;
    private float zoomDistance;
    private float currentDistance;

    public Transform Target => target;
    public float Yaw => yaw;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        ResolveTarget();
        zoomDistance = distance;
        currentDistance = distance;

        if (disableCinemachineComponents)
            DisableCinemachineBehaviours();

        if (cameraJoystick == null || !cameraJoystick.isActiveAndEnabled)
            cameraJoystick = FindFirstObjectByType<MobileCameraJoystick>();

        if (lockCursorOnStart)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void LateUpdate()
    {
        if (target == null)
            ResolveTarget();
        if (target == null)
            return;

        HandleOrbitInput();
        UpdateCameraPose();
    }

    private void ResolveTarget()
    {
        if (target == null)
        {
            GameObject targetObject = GameObject.FindGameObjectWithTag(targetTag);
            if (targetObject != null)
                target = targetObject.transform;
        }

        if (target != null && movement == null)
            movement = target.GetComponent<MobileJoystickPlayerMovement>();
    }

    private void HandleOrbitInput()
    {
        if (cameraJoystick == null || !cameraJoystick.isActiveAndEnabled)
            cameraJoystick = FindFirstObjectByType<MobileCameraJoystick>();

        bool joystickHasInput = cameraJoystick != null && cameraJoystick.HasInput;
        if (joystickHasInput)
        {
            Vector2 look = cameraJoystick.LookInput;
            yaw += look.x * joystickSensitivityX * Time.deltaTime;
            pitch -= look.y * joystickSensitivityY * Time.deltaTime;
        }

        bool canRotate = rotateWithoutMouseButton || Input.GetMouseButton(1) || Cursor.lockState == CursorLockMode.Locked;

        if (canRotate)
        {
            yaw += Input.GetAxis("Mouse X") * mouseSensitivityX * Time.deltaTime;
            pitch -= Input.GetAxis("Mouse Y") * mouseSensitivityY * Time.deltaTime;
        }

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.001f)
            zoomDistance = Mathf.Clamp(zoomDistance - scroll * zoomSensitivity, minDistance, maxDistance);
    }

    private void UpdateCameraPose()
    {
        bool sprinting = movement != null && movement.IsSprinting;
        float desiredDistance = Mathf.Clamp(zoomDistance + (sprinting ? sprintDistanceBonus : 0f), minDistance, maxDistance);
        currentDistance = Mathf.Lerp(currentDistance, desiredDistance, Time.deltaTime * distanceLerpSpeed);

        Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 pivot = target.position + targetOffset;
        Vector3 direction = orbitRotation * Vector3.back;
        float resolvedDistance = ResolveCollisionDistance(pivot, direction, currentDistance);
        Vector3 desiredPosition = pivot + direction * resolvedDistance;

        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref positionVelocity, positionSmoothTime);

        Quaternion lookRotation = Quaternion.LookRotation(pivot - transform.position, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationLerpSpeed);

        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, sprinting ? sprintFov : normalFov, Time.deltaTime * fovLerpSpeed);
    }

    private float ResolveCollisionDistance(Vector3 pivot, Vector3 direction, float desiredDistance)
    {
        if (Physics.SphereCast(pivot, collisionRadius, direction, out RaycastHit hit, desiredDistance, collisionMask, QueryTriggerInteraction.Ignore))
            return Mathf.Max(minDistance, hit.distance - collisionPadding);

        return desiredDistance;
    }

    private void DisableCinemachineBehaviours()
    {
        MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour == null)
                continue;

            string fullName = behaviour.GetType().FullName;
            if (!string.IsNullOrEmpty(fullName) && fullName.Contains("Unity.Cinemachine"))
                behaviour.enabled = false;
        }
    }

    private void OnValidate()
    {
        minDistance = Mathf.Max(0.5f, minDistance);
        maxDistance = Mathf.Max(minDistance, maxDistance);
        distance = Mathf.Clamp(distance, minDistance, maxDistance);
        zoomDistance = Mathf.Clamp(zoomDistance <= 0f ? distance : zoomDistance, minDistance, maxDistance);
        positionSmoothTime = Mathf.Max(0.01f, positionSmoothTime);
        collisionRadius = Mathf.Max(0.05f, collisionRadius);
        joystickSensitivityX = Mathf.Max(1f, joystickSensitivityX);
        joystickSensitivityY = Mathf.Max(1f, joystickSensitivityY);
    }
}
