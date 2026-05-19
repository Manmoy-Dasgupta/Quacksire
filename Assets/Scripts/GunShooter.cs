using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Screen-center raycast shot from the main camera. Applies <see cref="GameCombatRules.DamagePerHit"/>.
/// </summary>
public class GunShooter : MonoBehaviour
{
    [SerializeField] Transform ownerRoot;
    [SerializeField] HealthSystem ownerHealth;
    [SerializeField] Camera aimCamera;
    [SerializeField] float range = 80f;
    [SerializeField] float fireCooldown = 0.25f;
    [SerializeField] LayerMask hitMask = ~0;
    [SerializeField] bool listenForFireInput = true;
    [SerializeField] bool createSimpleGunMesh = true;
    [SerializeField] Vector3 gunLocalPosition = new Vector3(0.28f, 1.15f, 0.32f);
    [SerializeField] Vector3 gunLocalScale = new Vector3(0.07f, 0.07f, 0.22f);

    float _nextFireTime;

    void Awake()
    {
        if (ownerRoot == null)
            ownerRoot = transform.root;
        if (ownerHealth == null)
            ownerHealth = ownerRoot.GetComponentInChildren<HealthSystem>();
        if (aimCamera == null)
            aimCamera = Camera.main;
        if (aimCamera == null)
            aimCamera = FindFirstObjectByType<Camera>();

        if (createSimpleGunMesh && transform.Find("SimpleGun") == null)
        {
            GameObject gun = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gun.name = "SimpleGun";
            Object.Destroy(gun.GetComponent<Collider>());
            gun.transform.SetParent(transform, false);
            gun.transform.localPosition = gunLocalPosition;
            gun.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            gun.transform.localScale = gunLocalScale;
            var r = gun.GetComponent<Renderer>();
            if (r != null)
                r.sharedMaterial.color = new Color(0.15f, 0.15f, 0.18f, 1f);
        }
    }

    void Update()
    {
        if (!listenForFireInput)
            return;

        bool pressed = Input.GetButtonDown("Fire1") || Input.GetMouseButtonDown(0);
        if (pressed)
            TryShoot();
    }

    public void TryShoot()
    {
        if (PointerOverUi())
            return;

        if (Time.time < _nextFireTime)
            return;

        if (ownerHealth != null && ownerHealth.IsDead)
            return;

        if (aimCamera == null)
            aimCamera = Camera.main;
        if (aimCamera == null)
            aimCamera = FindFirstObjectByType<Camera>();
        if (aimCamera == null)
            return;

        _nextFireTime = Time.time + fireCooldown;

        Ray ray = aimCamera.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f));
        if (!Physics.Raycast(ray, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Collide))
            return;

        if (ownerHealth != null && hit.collider.transform.IsChildOf(ownerHealth.transform))
            return;

        HealthSystem targetHealth = hit.collider.GetComponentInParent<HealthSystem>();
        if (targetHealth == null)
            return;

        targetHealth.Damage(GameCombatRules.DamagePerHit);
    }

    static bool PointerOverUi()
    {
        if (EventSystem.current == null)
            return false;

        if (Input.touchCount > 0)
            return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);

        return EventSystem.current.IsPointerOverGameObject();
    }
}
