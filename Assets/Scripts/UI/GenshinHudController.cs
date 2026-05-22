using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds and updates the adventure HUD: circular minimap, player HP/stamina, action buttons, and enemy markers.
/// The visuals are generated at runtime so the existing scene can be upgraded without hand-authoring fragile UI objects.
/// </summary>
[DefaultExecutionOrder(-40)]
public class GenshinHudController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform player;
    [SerializeField] private HealthSystem playerHealth;
    [SerializeField] private MobileJoystickPlayerMovement playerMovement;
    [SerializeField] private string playerTag = "Player";

    [Header("Health")]
    [SerializeField] private Color hpStartColor = new Color(0.32f, 0.95f, 0.83f, 1f);
    [SerializeField] private Color hpEndColor = new Color(0.16f, 0.70f, 0.95f, 1f);
    [SerializeField] private Color hpTrailColor = new Color(1f, 0.84f, 0.34f, 1f);
    [SerializeField] private Color panelColor = new Color(0.04f, 0.055f, 0.065f, 0.58f);
    [SerializeField] private Sprite characterIconSprite;
    [SerializeField] private float hpLerpSpeed = 9f;
    [SerializeField] private float damageTrailSpeed = 1.8f;

    [Header("Stamina")]
    [SerializeField] private bool showStamina = true;
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float sprintDrainPerSecond = 24f;
    [SerializeField] private float staminaRecoverPerSecond = 18f;

    [Header("Minimap")]
    [SerializeField] private float minimapSize = 154f;
    [SerializeField] private float minimapWorldRadius = 42f;
    [SerializeField] private float minimapCameraHeight = 72f;
    [SerializeField] private LayerMask minimapCullingMask = ~0;
    [SerializeField] private Color minimapBorderColor = new Color(0.92f, 0.78f, 0.48f, 0.95f);
    [SerializeField] private Color enemyMarkerColor = new Color(1f, 0.26f, 0.2f, 1f);
    [SerializeField] private Color objectiveMarkerColor = new Color(0.28f, 0.95f, 0.84f, 1f);

    private RectTransform hudRoot;
    private Image hpFill;
    private Image hpTrail;
    private Text hpText;
    private Image staminaFill;
    private Camera minimapCamera;
    private RenderTexture minimapTexture;
    private RectTransform minimapRoot;
    private RectTransform markerRoot;
    private RectTransform playerArrow;
    private CanvasGroup hudGroup;
    private readonly List<MarkerBinding> markers = new List<MarkerBinding>();
    private float displayedHpFraction = 1f;
    private float trailHpFraction = 1f;
    private float stamina;
    private float markerRefreshTimer;
    private bool minimapExpanded;

    private sealed class MarkerBinding
    {
        public Transform Target;
        public RectTransform Icon;
        public bool Objective;
    }

    private void Start()
    {
        ResolveReferences();
        ConfigureCanvas();
        BuildHud();
        RefreshMarkers();
    }

    private void LateUpdate()
    {
        if (player == null)
            ResolveReferences();

        UpdateHealth();
        UpdateStamina();
        UpdateMinimap();

        if (hudGroup != null)
            hudGroup.alpha = Mathf.MoveTowards(hudGroup.alpha, 1f, Time.deltaTime * 3.5f);

        markerRefreshTimer -= Time.deltaTime;
        if (markerRefreshTimer <= 0f)
        {
            markerRefreshTimer = 0.7f;
            RefreshMarkers();
        }
    }

    private void OnDestroy()
    {
        if (minimapTexture != null)
            minimapTexture.Release();
    }

    private void ResolveReferences()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObject != null)
                player = playerObject.transform;
        }

        if (player != null)
        {
            if (playerHealth == null)
                playerHealth = player.GetComponent<HealthSystem>();
            if (playerMovement == null)
                playerMovement = player.GetComponent<MobileJoystickPlayerMovement>();
        }

        if (mainCamera == null)
            mainCamera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();

        if (canvas == null)
        {
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (Canvas candidate in canvases)
            {
                if (candidate.renderMode != RenderMode.WorldSpace && candidate.transform.parent == null)
                {
                    canvas = candidate;
                    break;
                }
            }
        }

        stamina = maxStamina;
    }

    private void ConfigureCanvas()
    {
        if (canvas == null)
            return;

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
    }

    private void BuildHud()
    {
        if (canvas == null)
            return;

        Transform oldRoot = canvas.transform.Find("GenshinHUDRoot");
        if (oldRoot != null)
            Destroy(oldRoot.gameObject);

        GameObject root = new GameObject("GenshinHUDRoot", typeof(RectTransform), typeof(CanvasGroup));
        root.transform.SetParent(canvas.transform, false);
        hudRoot = root.GetComponent<RectTransform>();
        hudGroup = root.GetComponent<CanvasGroup>();
        hudGroup.alpha = 0f;
        hudRoot.anchorMin = Vector2.zero;
        hudRoot.anchorMax = Vector2.one;
        hudRoot.offsetMin = Vector2.zero;
        hudRoot.offsetMax = Vector2.zero;

        BuildMinimap();
        BuildHealthPanel();
        BuildTopRightIcons();
        BuildAbilityButtons();
        BuildPartyStatus();
    }

    private void BuildHealthPanel()
    {
        RectTransform panel = CreatePanel("CharacterStatus", hudRoot, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(430f, 74f), new Vector2(0f, 30f), panelColor);

        Image glow = CreateImage("SoftGlow", panel, new Color(0.22f, 0.82f, 0.86f, 0.16f), RoundedSprite(64, 64, 24, Color.white));
        Stretch(glow.rectTransform, new Vector2(-12f, -10f), new Vector2(12f, 10f));

        Image portrait = CreateImage("CharacterIcon", panel, new Color(0.13f, 0.21f, 0.28f, 0.88f), CircleSprite(96, Color.white));
        if (characterIconSprite != null)
            portrait.sprite = characterIconSprite;
        portrait.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        portrait.rectTransform.anchorMax = new Vector2(0f, 0.5f);
        portrait.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        portrait.rectTransform.sizeDelta = new Vector2(52f, 52f);
        portrait.rectTransform.anchoredPosition = new Vector2(34f, 0f);

        Text level = CreateText("LevelText", panel, "Lv. 70", 16, TextAnchor.MiddleLeft, new Color(1f, 0.92f, 0.74f, 0.94f));
        level.rectTransform.anchorMin = new Vector2(0f, 0f);
        level.rectTransform.anchorMax = new Vector2(0f, 0f);
        level.rectTransform.pivot = new Vector2(0f, 0f);
        level.rectTransform.sizeDelta = new Vector2(80f, 22f);
        level.rectTransform.anchoredPosition = new Vector2(74f, 8f);

        RectTransform hpTrack = CreatePanel("HPTrack", panel, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-110f, 20f), new Vector2(44f, 8f), new Color(0.02f, 0.03f, 0.035f, 0.82f));
        hpTrack.offsetMin = new Vector2(84f, -6f);
        hpTrack.offsetMax = new Vector2(-26f, 14f);

        hpTrail = CreateImage("DamageTrail", hpTrack, hpTrailColor, GradientSprite(256, 10, hpTrailColor, new Color(1f, 0.56f, 0.2f, 1f)));
        Stretch(hpTrail.rectTransform, new Vector2(3f, 3f), new Vector2(-3f, -3f));
        hpTrail.type = Image.Type.Filled;
        hpTrail.fillMethod = Image.FillMethod.Horizontal;
        hpTrail.fillOrigin = 0;

        hpFill = CreateImage("HPFill", hpTrack, Color.white, GradientSprite(256, 10, hpStartColor, hpEndColor));
        Stretch(hpFill.rectTransform, new Vector2(3f, 3f), new Vector2(-3f, -3f));
        hpFill.type = Image.Type.Filled;
        hpFill.fillMethod = Image.FillMethod.Horizontal;
        hpFill.fillOrigin = 0;

        hpText = CreateText("HPText", panel, "120 / 120", 15, TextAnchor.MiddleRight, new Color(1f, 1f, 1f, 0.92f));
        hpText.rectTransform.anchorMin = new Vector2(1f, 0.5f);
        hpText.rectTransform.anchorMax = new Vector2(1f, 0.5f);
        hpText.rectTransform.pivot = new Vector2(1f, 0.5f);
        hpText.rectTransform.sizeDelta = new Vector2(130f, 24f);
        hpText.rectTransform.anchoredPosition = new Vector2(-30f, 18f);

        if (!showStamina)
            return;

        RectTransform staminaTrack = CreatePanel("StaminaTrack", panel, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(-128f, 8f), new Vector2(54f, 8f), new Color(0.02f, 0.03f, 0.035f, 0.72f));
        staminaTrack.offsetMin = new Vector2(104f, 12f);
        staminaTrack.offsetMax = new Vector2(-42f, 20f);

        staminaFill = CreateImage("StaminaFill", staminaTrack, new Color(0.95f, 0.83f, 0.42f, 1f), GradientSprite(256, 6, new Color(1f, 0.91f, 0.52f, 1f), new Color(0.38f, 0.91f, 0.82f, 1f)));
        Stretch(staminaFill.rectTransform, new Vector2(2f, 2f), new Vector2(-2f, -2f));
        staminaFill.type = Image.Type.Filled;
        staminaFill.fillMethod = Image.FillMethod.Horizontal;
        staminaFill.fillOrigin = 0;
    }

    private void BuildMinimap()
    {
        RectTransform root = CreatePanel("MiniMap", hudRoot, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(minimapSize, minimapSize), new Vector2(28f, -24f), new Color(0.02f, 0.03f, 0.04f, 0.4f));
        minimapRoot = root;

        Button mapButton = root.gameObject.AddComponent<Button>();
        mapButton.transition = Selectable.Transition.None;
        mapButton.targetGraphic = root.GetComponent<Image>();
        mapButton.onClick.AddListener(ToggleMinimapExpanded);

        Image border = CreateImage("GoldBorder", root, minimapBorderColor, CircleSprite(256, Color.white));
        Stretch(border.rectTransform, Vector2.zero, Vector2.zero);
        border.raycastTarget = false;

        GameObject maskGo = new GameObject("MapMask", typeof(RectTransform), typeof(Image), typeof(Mask));
        maskGo.transform.SetParent(root, false);
        RectTransform maskRect = maskGo.GetComponent<RectTransform>();
        Stretch(maskRect, new Vector2(7f, 7f), new Vector2(-7f, -7f));
        Image maskImage = maskGo.GetComponent<Image>();
        maskImage.sprite = CircleSprite(256, Color.white);
        maskImage.color = Color.white;
        maskImage.raycastTarget = false;
        maskGo.GetComponent<Mask>().showMaskGraphic = false;

        GameObject rawGo = new GameObject("MapRender", typeof(RectTransform), typeof(RawImage));
        rawGo.transform.SetParent(maskGo.transform, false);
        RectTransform rawRect = rawGo.GetComponent<RectTransform>();
        Stretch(rawRect, Vector2.zero, Vector2.zero);
        RawImage raw = rawGo.GetComponent<RawImage>();
        raw.raycastTarget = false;

        markerRoot = new GameObject("Markers", typeof(RectTransform)).GetComponent<RectTransform>();
        markerRoot.SetParent(root, false);
        markerRoot.anchorMin = new Vector2(0.5f, 0.5f);
        markerRoot.anchorMax = new Vector2(0.5f, 0.5f);
        markerRoot.pivot = new Vector2(0.5f, 0.5f);
        markerRoot.sizeDelta = new Vector2(minimapSize - 18f, minimapSize - 18f);
        markerRoot.anchoredPosition = Vector2.zero;

        playerArrow = CreateImage("PlayerArrow", root, new Color(0.93f, 0.95f, 1f, 1f), TriangleSprite(64, Color.white)).rectTransform;
        playerArrow.GetComponent<Image>().raycastTarget = false;
        playerArrow.anchorMin = new Vector2(0.5f, 0.5f);
        playerArrow.anchorMax = new Vector2(0.5f, 0.5f);
        playerArrow.pivot = new Vector2(0.5f, 0.5f);
        playerArrow.sizeDelta = new Vector2(18f, 24f);
        playerArrow.anchoredPosition = Vector2.zero;

        CreateCompassLabel(root, "N", new Vector2(0f, 58f));
        CreateCompassLabel(root, "E", new Vector2(58f, 0f));
        CreateCompassLabel(root, "S", new Vector2(0f, -58f));
        CreateCompassLabel(root, "W", new Vector2(-58f, 0f));

        minimapTexture = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32);
        minimapTexture.name = "GenshinMiniMapTexture_Runtime";
        minimapTexture.Create();
        raw.texture = minimapTexture;

        GameObject cameraGo = new GameObject("GenshinMiniMapCamera");
        cameraGo.transform.SetParent(transform, false);
        minimapCamera = cameraGo.AddComponent<Camera>();
        minimapCamera.orthographic = true;
        minimapCamera.orthographicSize = minimapWorldRadius;
        minimapCamera.clearFlags = CameraClearFlags.SolidColor;
        minimapCamera.backgroundColor = new Color(0.12f, 0.18f, 0.17f, 0f);
        minimapCamera.cullingMask = minimapCullingMask & ~(1 << 5);
        minimapCamera.nearClipPlane = 0.3f;
        minimapCamera.farClipPlane = 180f;
        minimapCamera.allowHDR = false;
        minimapCamera.allowMSAA = false;
        minimapCamera.targetTexture = minimapTexture;
        minimapCamera.depth = -20f;
    }

    private void ToggleMinimapExpanded()
    {
        SetMinimapExpanded(!minimapExpanded);
    }

    private void SetMinimapExpanded(bool expanded)
    {
        if (minimapRoot == null)
            return;

        minimapExpanded = expanded;
        float size = expanded ? Mathf.Max(320f, minimapSize * 2.65f) : minimapSize;

        minimapRoot.SetAsLastSibling();
        minimapRoot.anchorMin = expanded ? new Vector2(0.5f, 0.5f) : new Vector2(0f, 1f);
        minimapRoot.anchorMax = minimapRoot.anchorMin;
        minimapRoot.pivot = expanded ? new Vector2(0.5f, 0.5f) : new Vector2(0f, 1f);
        minimapRoot.sizeDelta = new Vector2(size, size);
        minimapRoot.anchoredPosition = expanded ? Vector2.zero : new Vector2(28f, -24f);

        if (markerRoot != null)
            markerRoot.sizeDelta = new Vector2(size - 18f, size - 18f);

        if (minimapCamera != null)
            minimapCamera.orthographicSize = expanded ? minimapWorldRadius * 1.45f : minimapWorldRadius;
    }

    private void BuildTopRightIcons()
    {
        string[] labels = { "Map", "Bag", "Book", "Star", "Wish", "Mail" };
        for (int i = 0; i < labels.Length; i++)
        {
            RectTransform button = CreateIconButton("TopIcon_" + labels[i], hudRoot, labels[i].Substring(0, 1), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-42f - i * 54f, -28f), 38f);
            button.gameObject.AddComponent<Button>();
        }
    }

    private void BuildAbilityButtons()
    {
        CreateIconButton("Ability_Attack", hudRoot, "ATK", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-68f, 82f), 68f).gameObject.AddComponent<Button>();
        CreateIconButton("Ability_E", hudRoot, "E", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-152f, 70f), 52f).gameObject.AddComponent<Button>();
        CreateIconButton("Ability_Q", hudRoot, "Q", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-92f, 158f), 54f).gameObject.AddComponent<Button>();
        CreateIconButton("Ability_Dash", hudRoot, "Z", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-42f, 172f), 42f).gameObject.AddComponent<Button>();
    }

    private void BuildPartyStatus()
    {
        RectTransform list = CreatePanel("PartyStatus", hudRoot, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(176f, 170f), new Vector2(-22f, 40f), new Color(0f, 0f, 0f, 0f));
        string[] names = { "Player", "Yanfei", "Faruzan", "Bennett" };
        for (int i = 0; i < names.Length; i++)
        {
            Text label = CreateText("Party_" + names[i], list, names[i] + "   " + (i + 1), 16, TextAnchor.MiddleRight, new Color(1f, 1f, 1f, i == 0 ? 0.98f : 0.72f));
            label.rectTransform.anchorMin = new Vector2(0f, 1f);
            label.rectTransform.anchorMax = new Vector2(1f, 1f);
            label.rectTransform.pivot = new Vector2(1f, 1f);
            label.rectTransform.sizeDelta = new Vector2(0f, 32f);
            label.rectTransform.anchoredPosition = new Vector2(0f, -i * 38f);
        }
    }

    private void UpdateHealth()
    {
        if (playerHealth == null || hpFill == null)
            return;

        float target = Mathf.Clamp01(playerHealth.CurrentHealth / Mathf.Max(1f, playerHealth.MaxHealth));
        displayedHpFraction = Mathf.Lerp(displayedHpFraction, target, Time.deltaTime * hpLerpSpeed);

        if (target > trailHpFraction)
            trailHpFraction = target;
        else
            trailHpFraction = Mathf.MoveTowards(trailHpFraction, target, Time.deltaTime * damageTrailSpeed);

        hpFill.fillAmount = displayedHpFraction;
        hpTrail.fillAmount = trailHpFraction;

        if (hpText != null)
            hpText.text = Mathf.CeilToInt(playerHealth.CurrentHealth) + " / " + Mathf.CeilToInt(playerHealth.MaxHealth);
    }

    private void UpdateStamina()
    {
        if (!showStamina || staminaFill == null)
            return;

        bool draining = playerMovement != null && playerMovement.IsSprinting && playerMovement.IsMoving;
        stamina += (draining ? -sprintDrainPerSecond : staminaRecoverPerSecond) * Time.deltaTime;
        stamina = Mathf.Clamp(stamina, 0f, maxStamina);
        staminaFill.fillAmount = stamina / Mathf.Max(1f, maxStamina);
    }

    private void UpdateMinimap()
    {
        if (player == null || minimapCamera == null)
            return;

        minimapCamera.transform.position = player.position + Vector3.up * minimapCameraHeight;
        minimapCamera.transform.rotation = Quaternion.Euler(90f, player.eulerAngles.y, 0f);

        if (playerArrow != null)
            playerArrow.localEulerAngles = Vector3.zero;

        UpdateMarkerPositions();
    }

    private void RefreshMarkers()
    {
        if (markerRoot == null)
            return;

        for (int i = markers.Count - 1; i >= 0; i--)
        {
            if (markers[i].Target == null)
            {
                Destroy(markers[i].Icon.gameObject);
                markers.RemoveAt(i);
            }
        }

        AddTaggedMarkers("Enemy", false);
        AddTaggedMarkers("Objective", true);
        AddTaggedMarkers("Quest", true);
        AddTaggedMarkers("Waypoint", true);
    }

    private void AddTaggedMarkers(string tag, bool objective)
    {
        GameObject[] objects;
        try
        {
            objects = GameObject.FindGameObjectsWithTag(tag);
        }
        catch (UnityException)
        {
            return;
        }

        foreach (GameObject obj in objects)
        {
            if (obj == null || obj.transform == player || HasMarker(obj.transform))
                continue;

            Image icon = CreateImage(tag + "Marker", markerRoot, objective ? objectiveMarkerColor : enemyMarkerColor, objective ? DiamondSprite(32, Color.white) : CircleSprite(32, Color.white));
            icon.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            icon.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            icon.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            icon.rectTransform.sizeDelta = objective ? new Vector2(14f, 14f) : new Vector2(10f, 10f);
            markers.Add(new MarkerBinding { Target = obj.transform, Icon = icon.rectTransform, Objective = objective });
        }
    }

    private bool HasMarker(Transform target)
    {
        foreach (MarkerBinding marker in markers)
        {
            if (marker.Target == target)
                return true;
        }

        return false;
    }

    private void UpdateMarkerPositions()
    {
        if (player == null || markerRoot == null)
            return;

        float uiRadius = markerRoot.sizeDelta.x * 0.48f;
        Quaternion rotation = Quaternion.Euler(0f, -player.eulerAngles.y, 0f);

        foreach (MarkerBinding marker in markers)
        {
            if (marker.Target == null || marker.Icon == null)
                continue;

            Vector3 delta = marker.Target.position - player.position;
            Vector3 rotated = rotation * new Vector3(delta.x, 0f, delta.z);
            Vector2 mapPos = new Vector2(rotated.x, rotated.z) / minimapWorldRadius * uiRadius;
            if (mapPos.magnitude > uiRadius)
                mapPos = mapPos.normalized * uiRadius;

            marker.Icon.anchoredPosition = mapPos;
        }
    }

    private RectTransform CreateIconButton(string name, Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, float size)
    {
        Image button = CreateImage(name, parent, new Color(0.045f, 0.07f, 0.085f, 0.72f), CircleSprite(96, Color.white));
        RectTransform rt = button.rectTransform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(size, size);
        rt.anchoredPosition = position;

        Image ring = CreateImage("Ring", rt, new Color(0.92f, 0.78f, 0.48f, 0.8f), CircleSprite(96, Color.white));
        Stretch(ring.rectTransform, new Vector2(-2f, -2f), new Vector2(2f, 2f));
        ring.transform.SetAsFirstSibling();

        Text text = CreateText("Label", rt, label, label.Length > 1 ? 13 : 18, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.92f));
        Stretch(text.rectTransform, Vector2.zero, Vector2.zero);
        return rt;
    }

    private void CreateCompassLabel(Transform parent, string label, Vector2 pos)
    {
        Text text = CreateText("Compass_" + label, parent, label, 13, TextAnchor.MiddleCenter, new Color(1f, 0.92f, 0.74f, 0.82f));
        text.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        text.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        text.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        text.rectTransform.sizeDelta = new Vector2(20f, 20f);
        text.rectTransform.anchoredPosition = pos;
    }

    private static RectTransform CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size, Vector2 position, Color color)
    {
        Image image = CreateImage(name, parent, color, RoundedSprite(64, 64, 18, Color.white));
        RectTransform rt = image.rectTransform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.sizeDelta = size;
        rt.anchoredPosition = position;
        return rt;
    }

    private static Image CreateImage(string name, Transform parent, Color color, Sprite sprite)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        return image;
    }

    private static Text CreateText(string name, Transform parent, string content, int size, TextAnchor alignment, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        Text text = go.GetComponent<Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    private static void Stretch(RectTransform rt, Vector2 offsetMin, Vector2 offsetMax)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }

    private static Sprite RoundedSprite(int width, int height, int radius, Color color)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color clear = new Color(1f, 1f, 1f, 0f);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float dx = Mathf.Max(radius - x, 0, x - (width - radius - 1));
                float dy = Mathf.Max(radius - y, 0, y - (height - radius - 1));
                bool inside = dx * dx + dy * dy <= radius * radius;
                texture.SetPixel(x, y, inside ? color : clear);
            }
        }
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
    }

    private static Sprite CircleSprite(int size, Color color)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color clear = new Color(1f, 1f, 1f, 0f);
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                texture.SetPixel(x, y, distance <= radius ? color : clear);
            }
        }
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Sprite TriangleSprite(int size, Color color)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color clear = new Color(1f, 1f, 1f, 0f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float halfWidth = Mathf.Lerp(size * 0.08f, size * 0.42f, y / (float)(size - 1));
                bool inside = Mathf.Abs(x - size * 0.5f) <= halfWidth && y > size * 0.08f;
                texture.SetPixel(x, y, inside ? color : clear);
            }
        }
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Sprite DiamondSprite(int size, Color color)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color clear = new Color(1f, 1f, 1f, 0f);
        float center = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool inside = Mathf.Abs(x - center) + Mathf.Abs(y - center) <= center;
                texture.SetPixel(x, y, inside ? color : clear);
            }
        }
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Sprite GradientSprite(int width, int height, Color left, Color right)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color color = Color.Lerp(left, right, x / (float)(width - 1));
                texture.SetPixel(x, y, color);
            }
        }
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
    }
}
