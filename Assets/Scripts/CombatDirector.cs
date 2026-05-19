using UnityEngine;

/// <summary>
/// Spawns a humanoid enemy from <c>Resources/EnemyHumanoid</c> with health, world bar, chase AI, and animation.
/// </summary>
[DefaultExecutionOrder(-50)]
public class CombatDirector : MonoBehaviour
{
    [SerializeField] string humanoidResourceName = "EnemyHumanoid";
    [SerializeField] Vector3 enemySpawnPosition = new Vector3(21f, 0f, 14f);
    [SerializeField] Vector3 enemySpawnEuler = new Vector3(0f, 180f, 0f);
    [SerializeField] RuntimeAnimatorController animatorController;
    [SerializeField] Avatar humanoidAvatar;
    [SerializeField] float enemyMaxHealth = 120f;

    void Start()
    {
        SpawnEnemy();
    }

    void SpawnEnemy()
    {
        GameObject prefab = Resources.Load<GameObject>(humanoidResourceName);
        if (prefab == null)
        {
            Debug.LogError("[CombatDirector] Could not load Resources prefab: " + humanoidResourceName, this);
            return;
        }

        GameObject enemy = Instantiate(prefab, enemySpawnPosition, Quaternion.Euler(enemySpawnEuler));
        enemy.name = "Enemy";
        enemy.tag = "Enemy";

        CharacterController cc = enemy.GetComponent<CharacterController>();
        if (cc == null)
            cc = enemy.AddComponent<CharacterController>();

        cc.height = 2f;
        cc.radius = 0.5f;
        cc.center = new Vector3(0f, 1.06f, 0f);
        cc.skinWidth = 0.08f;
        cc.stepOffset = 0.3f;

        Animator animator = enemy.GetComponent<Animator>();
        if (animator == null)
            animator = enemy.AddComponent<Animator>();

        if (animatorController != null)
            animator.runtimeAnimatorController = animatorController;
        if (humanoidAvatar != null)
            animator.avatar = humanoidAvatar;

        animator.applyRootMotion = false;

        var health = enemy.AddComponent<EnemyHealth>();
        health.ApplyRuntimeLoadout(enemyMaxHealth, destroyWhenDead: true);

        enemy.AddComponent<WorldHealthBar>();
        enemy.AddComponent<SimpleEnemyChase>();
    }
}
