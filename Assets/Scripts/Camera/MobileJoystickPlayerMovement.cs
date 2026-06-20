using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Player movement from the left joystick (or keyboard). Camera look is handled elsewhere.
/// </summary>
public class MobileJoystickPlayerMovement : MonoBehaviour
{
    [Header("Joystick")]
    [FormerlySerializedAs("joystick")]
    [SerializeField] Joystick movementJoystick;
    [FormerlySerializedAs("joystickSearchRoot")]
    [SerializeField] RectTransform movementJoystickSearchRoot;

    [Header("Player & Camera")]
    [SerializeField] Transform playerTransform;
    [SerializeField] CharacterController characterController;
    [SerializeField] Rigidbody playerRigidbody;
    [SerializeField] Transform cameraTransform;

    [Header("Movement")]
    [SerializeField] float moveSpeed = 3.5f;
    [SerializeField] float sprintSpeed = 5.4f;
    [SerializeField] float gravity = -20f;
    [SerializeField] bool rotateTowardsMoveDirection = true;
    [SerializeField] float rotationSpeedDegrees = 540f;
    [SerializeField] bool cameraRelativeMovement = true;
    [SerializeField] KeyCode sprintKey = KeyCode.LeftShift;
    [SerializeField, Range(0.1f, 1f)] float sprintInputThreshold = 0.85f;

    float verticalVelocity;

    public Vector2 MoveInput { get; private set; }
    public bool IsSprinting { get; private set; }
    public bool IsMoving => MoveInput.sqrMagnitude > 0.0001f;
    public float CurrentMoveSpeed => IsSprinting ? sprintSpeed : moveSpeed;
    public float AnimationSpeed => IsMoving ? Mathf.Clamp01(MoveInput.magnitude) * (IsSprinting ? 1f : 0.72f) : 0f;

    void Awake()
    {
        if (playerTransform == null)
            playerTransform = transform;

        if (characterController == null && playerRigidbody == null)
            characterController = playerTransform.GetComponent<CharacterController>();

        if (characterController != null)
            playerRigidbody = null;
    }

    void Start() => ResolveMovementJoystick();

    void ResolveMovementJoystick()
    {
        if (movementJoystick != null)
            return;

        if (movementJoystickSearchRoot != null)
            movementJoystick = movementJoystickSearchRoot.GetComponentInChildren<Joystick>(true);

        if (movementJoystick == null)
        {
            MobileInputUILayout layout = FindFirstObjectByType<MobileInputUILayout>();
            if (layout != null && layout.MovementZone != null)
                movementJoystick = layout.MovementZone.GetComponentInChildren<Joystick>(true);
        }

        if (movementJoystick == null)
            movementJoystick = FindFirstObjectByType<PlayerMovementJoystick>();
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

    void OnValidate()
    {
        if (characterController != null && playerRigidbody != null)
            playerRigidbody = null;

        moveSpeed = Mathf.Max(0.1f, moveSpeed);
        sprintSpeed = Mathf.Max(moveSpeed, sprintSpeed);
    }
}
