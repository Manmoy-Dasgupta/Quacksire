using UnityEngine;

/// <summary>
/// Controls character animations based on movement input from MobileJoystickPlayerMovement.
/// Works with HumanMale_Character_FREE prefab and other humanoid characters.
/// </summary>
[RequireComponent(typeof(Animator))]
public class PlayerAnimationController : MonoBehaviour
{
    [Header("Movement Reference")]
    [Tooltip("Reference to the MobileJoystickPlayerMovement script to get movement input.")]
    [SerializeField] private MobileJoystickPlayerMovement movementScript;

    [Header("Animation Settings")]
    [Tooltip("Smooth transition blend time between animations.")]
    [SerializeField] private float transitionDuration = 0.2f;

    [Header("Animation Parameters")]
    [Tooltip("Name of the float parameter for movement speed (default: Speed).")]
    [SerializeField] private string speedParameter = "Speed";

    private Animator animator;
    private float currentSpeed;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        
        // Auto-find movement script in parent objects
        movementScript = GetComponentInParent<MobileJoystickPlayerMovement>();
        
        if (movementScript == null)
        {
            // Try to find in the scene if not in parent
            movementScript = FindFirstObjectByType<MobileJoystickPlayerMovement>();
        }
    }

    private void Update()
    {
        if (movementScript == null || animator == null)
            return;

        float moveMagnitude = movementScript.AnimationSpeed;
        
        // Smooth the speed value for better animation transitions.
        float safeDuration = Mathf.Max(0.01f, transitionDuration);
        currentSpeed = Mathf.Lerp(currentSpeed, moveMagnitude, Time.deltaTime / safeDuration);
        
        // Drive a single Speed float so the state machine can flow
        // Idle -> Walk -> Run -> Walk -> Idle cleanly.
        animator.SetFloat(speedParameter, currentSpeed);
    }

    private void OnValidate()
    {
        transitionDuration = Mathf.Max(0.01f, transitionDuration);
    }
}
