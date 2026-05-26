using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// Player movement only — reads the left-side movement joystick (and keyboard on PC).
/// Camera rotation is handled separately by <see cref="MobileCameraJoystick"/> / <see cref="GenshinThirdPersonCamera"/>.
/// </summary>
public class MobileJoystickPlayerMovement : MonoBehaviour
{
    [Header("Joystick Input (movement only)")]
    [Tooltip("Left-zone joystick used for walking / sprinting.")]
    [FormerlySerializedAs("joystick")]
    [SerializeField] private Joystick movementJoystick;
    [Tooltip("If Movement Joystick is empty, searches this RectTransform (e.g. MovementZone).")]
    [FormerlySerializedAs("joystickSearchRoot")]
    [SerializeField] private RectTransform movementJoystickSearchRoot;
    [SerializeField] private bool hideJoystickVisuals = true;

    [Header("Player & Camera")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Rigidbody playerRigidbody;
    [Tooltip("Used for camera-relative movement direction.")]
    [SerializeField] private Transform cameraTransform;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float sprintSpeed = 5.4f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private bool rotateTowardsMoveDirection = true;
    [SerializeField] private float rotationSpeedDegrees = 540f;
    [SerializeField] private bool cameraRelativeMovement = true;
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
    [SerializeField, Range(0.1f, 1f)] private float sprintInputThreshold = 0.85f;

    private float verticalVelocity;
    private bool loggedMissingJoystick;

    public Vector2 MoveInput { get; private set; }
    public bool IsSprinting { get; private set; }
    public bool IsMoving => MoveInput.sqrMagnitude > 0.0001f;
    public float CurrentMoveSpeed => IsSprinting ? sprintSpeed : moveSpeed;
    public float AnimationSpeed => IsMoving ? Mathf.Clamp01(MoveInput.magnitude) * (IsSprinting ? 1f : 0.72f) : 0f;

    void Reset()
    {
        playerTransform = transform;
    }

    void Awake()
    {
        if (playerTransform == null)
            playerTransform = transform;

        if (characterController == null && playerRigidbody == null)
            characterController = playerTransform.GetComponent<CharacterController>();

        if (characterController != null)
            playerRigidbody = null;
    }

    void Start()
    {
        ResolveMovementJoystick();
    }

    void ResolveMovementJoystick()
    {
        if (movementJoystick == null && movementJoystickSearchRoot != null)
            movementJoystick = movementJoystickSearchRoot.GetComponentInChildren<Joystick>(true);

        if (movementJoystick == null)
        {
            MobileInputUILayout layout = FindFirstObjectByType<MobileInputUILayout>();
            if (layout != null && layout.MovementZone != null)
                movementJoystick = layout.MovementZone.GetComponentInChildren<Joystick>(true);
        }

        if (movementJoystick == null)
            movementJoystick = FindFirstObjectByType<PlayerMovementJoystick>();

        if (movementJoystick != null && hideJoystickVisuals)
            HideJoystickGraphics();

        if (movementJoystick == null && !loggedMissingJoystick)
        {
            loggedMissingJoystick = true;
            Debug.LogWarning(
                "[MobileJoystickPlayerMovement] No movement joystick found. Keyboard input still works for PC testing.",
                this);
        }
    }

    void Update()
    {
        if (playerTransform == null)
            return;

        if (movementJoystick == null)
            ResolveMovementJoystick();

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        if (movementJoystick != null)
        {
            Vector2 stickInput = new Vector2(movementJoystick.Horizontal, movementJoystick.Vertical);
            if (stickInput.sqrMagnitude > 0.0001f)
            {
                horizontal = stickInput.x;
                vertical = stickInput.y;
            }
        }

        MoveInput = Vector2.ClampMagnitude(new Vector2(horizontal, vertical), 1f);
        IsSprinting = IsMoving && (Input.GetKey(sprintKey) || MoveInput.magnitude >= sprintInputThreshold);

        Vector3 moveWorld = GetWorldMoveDirection(MoveInput.x, MoveInput.y);

        if (rotateTowardsMoveDirection && IsMoving)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveWorld, Vector3.up);
            playerTransform.rotation = Quaternion.RotateTowards(
                playerTransform.rotation,
                targetRot,
                rotationSpeedDegrees * Time.deltaTime);
        }

        if (characterController != null)
        {
            if (characterController.isGrounded)
                verticalVelocity = -2f;
            else
                verticalVelocity += gravity * Time.deltaTime;

            Vector3 velocity = moveWorld * CurrentMoveSpeed + Vector3.up * verticalVelocity;
            characterController.Move(velocity * Time.deltaTime);
        }
        else if (playerRigidbody == null)
        {
            playerTransform.position += moveWorld * (CurrentMoveSpeed * Time.deltaTime);
        }
    }

    void FixedUpdate()
    {
        if (playerTransform == null || characterController != null || playerRigidbody == null)
            return;

        Vector3 moveWorld = GetWorldMoveDirection(MoveInput.x, MoveInput.y);
        Vector3 v = moveWorld * CurrentMoveSpeed;
        playerRigidbody.linearVelocity = new Vector3(v.x, playerRigidbody.linearVelocity.y, v.z);
    }

    Vector3 GetWorldMoveDirection(float horizontal, float vertical)
    {
        if (cameraTransform == null)
        {
            Camera cam = Camera.main;
            if (cam != null)
                cameraTransform = cam.transform;
        }

        Vector3 forward = cameraRelativeMovement && cameraTransform != null ? cameraTransform.forward : Vector3.forward;
        Vector3 right = cameraRelativeMovement && cameraTransform != null ? cameraTransform.right : Vector3.right;

        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 dir = right * horizontal + forward * vertical;
        if (dir.sqrMagnitude > 1f)
            dir.Normalize();

        return dir;
    }

    void HideJoystickGraphics()
    {
        if (movementJoystick == null)
            return;

        foreach (Graphic graphic in movementJoystick.GetComponentsInChildren<Graphic>(true))
        {
            Color color = graphic.color;
            color.a = 0f;
            graphic.color = color;
        }
    }

    void OnValidate()
    {
        if (characterController != null && playerRigidbody != null)
            playerRigidbody = null;

        moveSpeed = Mathf.Max(0.1f, moveSpeed);
        sprintSpeed = Mathf.Max(moveSpeed, sprintSpeed);
    }
}
