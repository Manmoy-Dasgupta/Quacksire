using UnityEngine;

/// <summary>
/// Mobile input handler for attack actions. Attach to a UI Canvas with buttons.
/// Works with PlayerCombatAnimator to trigger attack animations.
/// </summary>
public class MobileAttackInput : MonoBehaviour
{
    [Header("Combat Reference")]
    [Tooltip("Reference to the PlayerCombatAnimator component.")]
    [SerializeField] private PlayerCombatAnimator combatAnimator;

    [Header("UI References")]
    [Tooltip("Assign a UI Button to trigger basic attack.")]
    [SerializeField] private UnityEngine.UI.Button attackButton;
    [Tooltip("Assign a UI Button to trigger special attack.")]
    [SerializeField] private UnityEngine.UI.Button specialAttackButton;

    private void Start()
    {
        // Auto-find combat animator if not assigned
        if (combatAnimator == null)
            combatAnimator = FindFirstObjectByType<PlayerCombatAnimator>();

        // Setup button listeners
        if (attackButton != null)
            attackButton.onClick.AddListener(OnAttackPressed);

        if (specialAttackButton != null)
            specialAttackButton.onClick.AddListener(OnSpecialAttackPressed);
    }

    private void OnAttackPressed()
    {
        if (combatAnimator != null)
            combatAnimator.Attack();
    }

    private void OnSpecialAttackPressed()
    {
        if (combatAnimator != null)
            combatAnimator.SpecialAttack();
    }

    private void OnDestroy()
    {
        // Clean up button listeners
        if (attackButton != null)
            attackButton.onClick.RemoveListener(OnAttackPressed);

        if (specialAttackButton != null)
            specialAttackButton.onClick.RemoveListener(OnSpecialAttackPressed);
    }
}