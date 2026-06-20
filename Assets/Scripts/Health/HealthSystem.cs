using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Reusable health component for player and enemies. Bind a Slider from a <see cref="WorldHealthBar"/> or the Inspector.
/// </summary>
public class HealthSystem : MonoBehaviour
{
    [SerializeField] float maxHealth = 120f;
    [SerializeField] float currentHealth = 120f;
    [SerializeField] Slider healthSlider;
    [SerializeField] bool destroyOnDeath;
    [SerializeField] bool addCapsuleHitTrigger = true;
    [SerializeField] Vector3 hitCapsuleCenter = new Vector3(0f, 1.06f, 0f);

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public bool IsDead => currentHealth <= 0f;

    public event Action OnDeath;

    void Awake()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        if (addCapsuleHitTrigger)
            EnsureHitTriggerChild();
        RefreshUI();
    }

    void Start()
    {
        if (healthSlider == null && CompareTag("Player"))
            healthSlider = FindPlayerHudSlider();

        RefreshUI();
    }

    public void BindSlider(Slider slider)
    {
        healthSlider = slider;
        RefreshUI();
    }

    public void ApplyRuntimeLoadout(float maxHp, bool destroyWhenDead)
    {
        destroyOnDeath = destroyWhenDead;
        maxHealth = Mathf.Max(1f, maxHp);
        currentHealth = maxHealth;
        RefreshUI();
    }

    public void Damage(float amount)
    {
        if (IsDead || amount <= 0f)
            return;

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        RefreshUI();

        if (currentHealth <= 0f)
            HandleDeath();
    }

    public void Heal(float amount)
    {
        if (amount <= 0f)
            return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        RefreshUI();
    }

    public float GetHealth()
    {
        return currentHealth;
    }

    public void SetHealth(float amount)
    {
        currentHealth = Mathf.Clamp(amount, 0f, maxHealth);
        RefreshUI();
    }

    public void SetMaxHealth(float amount, bool refillHealth = false)
    {
        maxHealth = Mathf.Max(1f, amount);

        if (refillHealth || currentHealth > maxHealth)
            currentHealth = maxHealth;

        RefreshUI();
    }

    void HandleDeath()
    {
        OnDeath?.Invoke();
        if (destroyOnDeath)
            Destroy(gameObject);
    }

    void RefreshUI()
    {
        if (healthSlider == null)
            return;

        healthSlider.maxValue = maxHealth;
        healthSlider.value = currentHealth;
    }

    void EnsureHitTriggerChild()
    {
        if (transform.Find("DamageHitbox") != null)
            return;

        var hb = new GameObject("DamageHitbox");
        hb.transform.SetParent(transform, false);
        hb.transform.localPosition = hitCapsuleCenter;
        hb.layer = gameObject.layer;
        var cap = hb.AddComponent<CapsuleCollider>();
        cap.isTrigger = true;
        cap.radius = 0.45f;
        cap.height = 2f;
        cap.direction = 1;
    }

    void OnValidate()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        RefreshUI();
    }

    Slider FindPlayerHudSlider()
    {
        Slider[] sliders = FindObjectsByType<Slider>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Slider slider in sliders)
        {
            if (slider == null || slider.name != "HealthBar")
                continue;

            Canvas canvas = slider.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.WorldSpace)
                return slider;
        }

        return null;
    }
}
