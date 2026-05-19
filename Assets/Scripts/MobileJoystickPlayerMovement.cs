using UnityEngine;

/// <summary>
/// Mobile-friendly movement driven by the Joystick Pack Joystick (Fixed / Floating / Variable / Dynamic).
/// Assign references in the Inspector. Leave Character Controller and Rigidbody empty to move the transform directly.
/// </summary>
public class MobileJoystickPlayerMovement : MonoBehaviour
{
    [Header("Joystick Input")]
    [Tooltip("Drag the GameObject that has your joystick script (FixedJoystick, FloatingJoystick, VariableJoystick, or DynamicJoystick).")]
    [SerializeField] private Joystick joystick;
    [Tooltip("Optional. If Joystick above is empty, the first Joystick found under this RectTransform is used at runtime.")]
    [SerializeField] private RectTransform joystickSearchRoot;

    [Header("Player & Camera")]
    [Tooltip("Root transform that moves and rotates. Defaults to this object if empty.")]
    [SerializeField] private Transform playerTransform;
    [Tooltip("If set, uses CharacterController.Move in Update.")]
    [SerializeField] private CharacterController characterController;
    [Tooltip("If set (and no CharacterController), sets horizontal velocity in FixedUpdate.")]
    [SerializeField] private Rigidbody playerRigidbody;
    [Tooltip("Optional. If set and camera-relative movement is enabled, stick forward follows camera view.")]
    [SerializeField] private Transform cameraTransform;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private bool rotateTowardsMoveDirection = true;
    [SerializeField] private float rotationSpeedDegrees = 540f;
    [SerializeField] private bool cameraRelativeMovement = true;

    private float verticalVelocity;
    private bool loggedMissingJoystick;

    /// <summary>Normalised XY input from the joystick this frame.</summary>
    public Vector2 MoveInput { get; private set; }

    private void Reset()
    {
        playerTransform = transform;
    }

    private void Awake()
    {
        if (playerTransform == null)
            playerTransform = transform;

        if (characterController == null && playerRigidbody == null)
            characterController = playerTransform.GetComponent<CharacterController>();

        if (characterController != null)
            playerRigidbody = null;
    }

    private void Start()
    {
        ResolveJoystick();
    }

    /// <summary>Fills joystick from joystickSearchRoot when the direct reference was not set.</summary>
    private void ResolveJoystick()
    {
        if (joystick != null)
            return;

        if (joystickSearchRoot != null)
            joystick = joystickSearchRoot.GetComponentInChildren<Joystick>(true);

        if (joystick == null && !loggedMissingJoystick)
        {
            loggedMissingJoystick = true;
            Debug.LogError(
                "[MobileJoystickPlayerMovement] No Joystick assigned. " +
                "Drag the Variable Joystick GameObject onto the Joystick field in the Inspector.",
                this);
        }
    }

    private void Update()
    {
        if (joystick == null || playerTransform == null)
            return;

        float horizontal = joystick.Horizontal;
        float vertical   = joystick.Vertical;
        MoveInput        = new Vector2(horizontal, vertical);

        Vector3 moveWorld = GetWorldMoveDirection(horizontal, vertical);
        bool hasInput     = MoveInput.sqrMagnitude > 0.0001f;

        if (rotateTowardsMoveDirection && hasInput)
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

            Vector3 velocity = moveWorld * moveSpeed + Vector3.up * verticalVelocity;
            characterController.Move(velocity * Time.deltaTime);
        }
        else if (playerRigidbody == null)
        {
            playerTransform.position += moveWorld * (moveSpeed * Time.deltaTime);
        }
    }

    private void FixedUpdate()
    {
        if (joystick == null || playerTransform == null || characterController != null || playerRigidbody == null)
            return;

        Vector3 moveWorld = GetWorldMoveDirection(joystick.Horizontal, joystick.Vertical);
        Vector3 v = moveWorld * moveSpeed;
        playerRigidbody.linearVelocity = new Vector3(v.x, playerRigidbody.linearVelocity.y, v.z);
    }

    /// <summary>Converts raw joystick axes into a world-space movement direction.</summary>
    private Vector3 GetWorldMoveDirection(float horizontal, float vertical)
    {
        Vector3 forward = cameraRelativeMovement && cameraTransform != null ? cameraTransform.forward : Vector3.forward;
        Vector3 right   = cameraRelativeMovement && cameraTransform != null ? cameraTransform.right   : Vector3.right;

        forward.y = 0f;
        right.y   = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 dir = right * horizontal + forward * vertical;
        if (dir.sqrMagnitude > 1f)
            dir.Normalize();

        return dir;
    }

    private void OnValidate()
    {
        if (characterController != null && playerRigidbody != null)
            playerRigidbody = null;
    }
}
