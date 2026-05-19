using UnityEngine;

/// <summary>
/// Disables player control when <see cref="HealthSystem"/> reaches zero health.
/// </summary>
public class PlayerDeathHandler : MonoBehaviour
{
    [SerializeField] HealthSystem health;
    MobileJoystickPlayerMovement movement;
    GunShooter gun;
    Animator animator;

    void Awake()
    {
        if (health == null)
            health = GetComponent<HealthSystem>();
        movement = GetComponent<MobileJoystickPlayerMovement>();
        gun = GetComponent<GunShooter>();
        animator = GetComponent<Animator>();
    }

    void OnEnable()
    {
        if (health != null)
            health.OnDeath += OnPlayerDied;
    }

    void OnDisable()
    {
        if (health != null)
            health.OnDeath -= OnPlayerDied;
    }

    void OnPlayerDied()
    {
        if (movement != null)
            movement.enabled = false;
        if (gun != null)
            gun.enabled = false;
        if (animator != null)
        {
            animator.SetFloat("Speed", 0f);
            animator.enabled = false;
        }
    }
}
