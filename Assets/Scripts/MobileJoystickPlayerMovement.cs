using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Mobile and PC movement for a third-person camera setup.
/// Joystick input is used when present; WASD/arrow keys are used as a fallback or alongside it.
/// </summary>
public class MobileJoystickPlayerMovement : MonoBehaviour
{
    [Header("Joystick Input")]
    [Tooltip("Drag the GameObject that has your joystick script (FixedJoystick, FloatingJoystick, VariableJoystick, or DynamicJoystick).")]
    [SerializeField] private Joystick joystick;
    [Tooltip("Optional. If Joystick above is empty, the first Joystick found under this RectTransform is used at runtime.")]
    [SerializeField] private RectTransform joystickSearchRoot;
    [SerializeField] private bool hideJoystickVisuals = true;
    [SerializeField] private bool useInvisibleLeftTouchZone = true;
    [SerializeField, Range(0.2f, 0.6f)] private float leftTouchScreenPercent = 0.4f;
    [SerializeField] private float invisibleJoystickRadiusPixels = 135f;
    [SerializeField, Range(0f, 0.4f)] private float invisibleJoystickDeadZone = 0.08f;

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
    private int movementTouchId = int.MinValue;
    private Vector2 movementTouchStart;

    /// <summary>Normalised XY input from the joystick this frame.</summary>
    public Vector2 MoveInput { get; private set; }
    public bool IsSprinting { get; private set; }
    public bool IsMoving => MoveInput.sqrMagnitude > 0.0001f;
    public float CurrentMoveSpeed => IsSprinting ? sprintSpeed : moveSpeed;
    public float AnimationSpeed => IsMoving ? Mathf.Clamp01(MoveInput.magnitude) * (IsSprinting ? 1f : 0.72f) : 0f;

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
        {
            if (hideJoystickVisuals)
                HideJoystickGraphics();
            return;
        }

        if (joystickSearchRoot != null)
            joystick = joystickSearchRoot.GetComponentInChildren<Joystick>(true);

        if (joystick != null && hideJoystickVisuals)
            HideJoystickGraphics();

        if (joystick == null && !loggedMissingJoystick)
        {
            loggedMissingJoystick = true;
            Debug.Log(
                "[MobileJoystickPlayerMovement] No joystick assigned. Keyboard input will remain active for PC testing.",
                this);
        }
    }

    private void Update()
    {
        if (playerTransform == null)
            return;

        ResolveJoystick();

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        if (TryReadInvisibleLeftTouch(out Vector2 touchInput))
        {
            horizontal = touchInput.x;
            vertical = touchInput.y;
        }
        else if (joystick != null)
        {
            Vector2 stickInput = new Vector2(joystick.Horizontal, joystick.Vertical);
            if (stickInput.sqrMagnitude > new Vector2(horizontal, vertical).sqrMagnitude)
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

    private void FixedUpdate()
    {
        if (playerTransform == null || characterController != null || playerRigidbody == null)
            return;

        Vector3 moveWorld = GetWorldMoveDirection(MoveInput.x, MoveInput.y);
        Vector3 v = moveWorld * CurrentMoveSpeed;
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

    private bool TryReadInvisibleLeftTouch(out Vector2 input)
    {
        input = Vector2.zero;
        if (!useInvisibleLeftTouchZone || Input.touchCount == 0)
        {
            movementTouchId = int.MinValue;
            return false;
        }

        float leftBoundary = Screen.width * leftTouchScreenPercent;

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (movementTouchId == int.MinValue)
            {
                if (touch.phase == TouchPhase.Began && touch.position.x <= leftBoundary && !IsTouchOverUi(touch.fingerId))
                {
                    movementTouchId = touch.fingerId;
                    movementTouchStart = touch.position;
                }
            }

            if (touch.fingerId != movementTouchId)
                continue;

            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                movementTouchId = int.MinValue;
                return false;
            }

            Vector2 delta = touch.position - movementTouchStart;
            input = Vector2.ClampMagnitude(delta / Mathf.Max(1f, invisibleJoystickRadiusPixels), 1f);
            if (input.magnitude < invisibleJoystickDeadZone)
                input = Vector2.zero;

            return true;
        }

        movementTouchId = int.MinValue;
        return false;
    }

    private static bool IsTouchOverUi(int fingerId)
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(fingerId);
    }

    private void HideJoystickGraphics()
    {
        if (joystick == null)
            return;

        Graphic[] graphics = joystick.GetComponentsInChildren<Graphic>(true);
        foreach (Graphic graphic in graphics)
        {
            Color color = graphic.color;
            color.a = 0f;
            graphic.color = color;
        }
    }

    private void OnValidate()
    {
        if (characterController != null && playerRigidbody != null)
            playerRigidbody = null;

        moveSpeed = Mathf.Max(0.1f, moveSpeed);
        sprintSpeed = Mathf.Max(moveSpeed, sprintSpeed);
        invisibleJoystickRadiusPixels = Mathf.Max(20f, invisibleJoystickRadiusPixels);
    }
}
