using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds and updates the player HP/stamina HUD under the scene Canvas.
/// </summary>
public class GenshinHudController : MonoBehaviour
{
    [SerializeField] Canvas canvas;
    [SerializeField] Camera mainCamera;
    [SerializeField] Transform player;
    [SerializeField] HealthSystem playerHealth;
    [SerializeField] MobileJoystickPlayerMovement playerMovement;
    [SerializeField] string playerTag = "Player";

    [SerializeField] Color hpStartColor = new Color(0.32f, 0.95f, 0.83f, 1f);
    [SerializeField] Color hpEndColor = new Color(0.16f, 0.7f, 0.95f, 1f);
    [SerializeField] Color hpTrailColor = new Color(1f, 0.84f, 0.34f, 1f);
    [SerializeField] Color panelColor = new Color(0.04f, 0.055f, 0.065f, 0.58f);
    [SerializeField] float hpLerpSpeed = 9f;
    [SerializeField] float damageTrailSpeed = 1.8f;
    [SerializeField] bool showStamina = true;
    [SerializeField] float maxStamina = 100f;
    [SerializeField] float sprintDrainPerSecond = 24f;
    [SerializeField] float staminaRecoverPerSecond = 18f;

    [SerializeField] RectTransform hudRoot;
    [SerializeField] Image hpFill;
    [SerializeField] Image hpTrail;
    [SerializeField] Text hpText;
    [SerializeField] Image staminaFill;
    [SerializeField] CanvasGroup hudGroup;

    float displayedHpFraction = 1f;
    float trailHpFraction = 1f;
    float stamina;

    void Awake()
    {
        ResolveReferences();
        if (hudRoot == null)
            BuildHud();
        stamina = maxStamina;
    }

    void Update()
    {
        if (playerHealth == null)
            ResolveReferences();
        if (playerHealth == null)
            return;

        float targetFraction = playerHealth.MaxHealth > 0f
            ? playerHealth.CurrentHealth / playerHealth.MaxHealth
            : 0f;

        displayedHpFraction = Mathf.Lerp(displayedHpFraction, targetFraction, Time.deltaTime * hpLerpSpeed);
        if (targetFraction < trailHpFraction)
            trailHpFraction = Mathf.MoveTowards(trailHpFraction, targetFraction, Time.deltaTime * damageTrailSpeed);
        else
            trailHpFraction = targetFraction;

        if (hpFill != null)
        {
            hpFill.fillAmount = displayedHpFraction;
            hpFill.color = Color.Lerp(hpEndColor, hpStartColor, displayedHpFraction);
        }

        if (hpTrail != null)
        {
            hpTrail.fillAmount = trailHpFraction;
            hpTrail.color = hpTrailColor;
        }

        if (hpText != null)
            hpText.text = $"{Mathf.CeilToInt(playerHealth.CurrentHealth)}/{Mathf.CeilToInt(playerHealth.MaxHealth)}";

        UpdateStamina();
    }

    void UpdateStamina()
    {
        if (!showStamina || staminaFill == null)
            return;

        bool sprinting = playerMovement != null && playerMovement.IsSprinting;
        if (sprinting)
            stamina = Mathf.Max(0f, stamina - sprintDrainPerSecond * Time.deltaTime);
        else
            stamina = Mathf.Min(maxStamina, stamina + staminaRecoverPerSecond * Time.deltaTime);

        staminaFill.fillAmount = maxStamina > 0f ? stamina / maxStamina : 0f;
    }

    void ResolveReferences()
    {
        if (canvas == null)
            canvas = FindFirstObjectByType<Canvas>();

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (player == null)
        {
            GameObject playerGo = GameObject.FindGameObjectWithTag(playerTag);
            if (playerGo != null)
                player = playerGo.transform;
        }

        if (playerHealth == null && player != null)
            playerHealth = player.GetComponent<HealthSystem>();

        if (playerMovement == null && player != null)
            playerMovement = player.GetComponent<MobileJoystickPlayerMovement>();
    }

    void BuildHud()
    {
        if (canvas == null)
            return;

        Transform existing = canvas.transform.Find("GenshinHudRoot");
        if (existing != null)
            Destroy(existing.gameObject);

        GameObject root = new GameObject("GenshinHudRoot", typeof(RectTransform), typeof(CanvasGroup));
        root.transform.SetParent(canvas.transform, false);
        hudRoot = root.GetComponent<RectTransform>();
        hudRoot.anchorMin = Vector2.zero;
        hudRoot.anchorMax = Vector2.one;
        hudRoot.offsetMin = Vector2.zero;
        hudRoot.offsetMax = Vector2.zero;

        hudGroup = root.GetComponent<CanvasGroup>();
        hudGroup.alpha = 1f;

        GameObject panel = new GameObject("HpPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(hudRoot, false);
        RectTransform panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0f);
        panelRt.anchorMax = new Vector2(0.5f, 0f);
        panelRt.pivot = new Vector2(0.5f, 0f);
        panelRt.sizeDelta = new Vector2(420f, 72f);
        panelRt.anchoredPosition = new Vector2(0f, 48f);
        panel.GetComponent<Image>().color = panelColor;

        hpTrail = CreateBarFill(panel.transform, "HpTrail", hpTrailColor, 0);
        hpFill = CreateBarFill(panel.transform, "HpFill", hpStartColor, 1);

        GameObject textGo = new GameObject("HpText", typeof(RectTransform), typeof(Text));
        textGo.transform.SetParent(panel.transform, false);
        hpText = textGo.GetComponent<Text>();
        hpText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hpText.fontSize = 22;
        hpText.alignment = TextAnchor.MiddleCenter;
        hpText.color = Color.white;
        RectTransform textRt = hpText.rectTransform;
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        if (showStamina)
        {
            GameObject staminaGo = new GameObject("StaminaBar", typeof(RectTransform), typeof(Image));
            staminaGo.transform.SetParent(panel.transform, false);
            RectTransform staminaRt = staminaGo.GetComponent<RectTransform>();
            staminaRt.anchorMin = new Vector2(0.05f, 0.08f);
            staminaRt.anchorMax = new Vector2(0.95f, 0.18f);
            staminaRt.offsetMin = Vector2.zero;
            staminaRt.offsetMax = Vector2.zero;
            staminaGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.35f);
            staminaFill = CreateBarFill(staminaGo.transform, "StaminaFill", new Color(0.95f, 0.85f, 0.35f, 1f), 1);
        }
    }

    static Image CreateBarFill(Transform parent, string name, Color color, int siblingIndex)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.transform.SetSiblingIndex(siblingIndex);
        Image image = go.GetComponent<Image>();
        image.color = color;
        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Horizontal;
        image.fillOrigin = (int)Image.OriginHorizontal.Left;
        image.fillAmount = 1f;
        RectTransform rt = image.rectTransform;
        rt.anchorMin = new Vector2(0.05f, 0.28f);
        rt.anchorMax = new Vector2(0.95f, 0.82f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return image;
    }
}
