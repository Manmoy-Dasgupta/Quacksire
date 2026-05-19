using UnityEngine;

/// <summary>
/// Handles combat animations (attack, special attacks) for the player character.
/// Can be used alongside PlayerAnimationController for movement animations.
/// </summary>
public class PlayerCombatAnimator : MonoBehaviour
{
    [Header("Combat Animation Settings")]
    [Tooltip("Name of the trigger parameter for basic attack (default: Attack).")]
    [SerializeField] private string attackTrigger = "Attack";
    [Tooltip("Name of the trigger parameter for special attack (default: SpecialAttack).")]
    [SerializeField] private string specialAttackTrigger = "SpecialAttack";
    [Tooltip("Name of the bool parameter for isAttacking (default: IsAttacking).")]
    [SerializeField] private string isAttackingBool = "IsAttacking";

    [Header("Attack Settings")]
    [Tooltip("Cooldown between attacks in seconds.")]
    [SerializeField] private float attackCooldown = 0.5f;
    [Tooltip("Can attacks be interrupted by movement?")]
    [SerializeField] private bool interruptible = true;

    private Animator animator;
    private float lastAttackTime;
    private bool isAttacking;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    /// <summary>
    /// Trigger basic attack animation.
    /// </summary>
    public void Attack()
    {
        if (animator == null || !CanAttack())
            return;

        animator.SetTrigger(attackTrigger);
        animator.SetBool(isAttackingBool, true);
        isAttacking = true;
        lastAttackTime = Time.time;

        // Reset attacking state after animation duration (approximate)
        Invoke(nameof(ResetAttackState), attackCooldown);
    }

    /// <summary>
    /// Trigger special attack animation.
    /// </summary>
    public void SpecialAttack()
    {
        if (animator == null || !CanAttack())
            return;

        animator.SetTrigger(specialAttackTrigger);
        animator.SetBool(isAttackingBool, true);
        isAttacking = true;
        lastAttackTime = Time.time;

        Invoke(nameof(ResetAttackState), attackCooldown * 2);
    }

    private bool CanAttack()
    {
        // Check cooldown
        if (Time.time - lastAttackTime < attackCooldown)
            return false;

        // Check if currently attacking and not interruptible
        if (isAttacking && !interruptible)
            return false;

        return true;
    }

    private void ResetAttackState()
    {
        if (animator != null)
            animator.SetBool(isAttackingBool, false);
        isAttacking = false;
    }

    /// <summary>
    /// Check if player is currently performing an attack animation.
    /// </summary>
    public bool IsAttacking => isAttacking;
}