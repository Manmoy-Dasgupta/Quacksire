using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Quacksire main menu: spawns local prefabs, cinematic camera intro, atmosphere lerp, and Start → Game Play.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    const string DefaultGameScene = "Game Play";

    [Header("Scene")]
    [SerializeField] string gameSceneName = DefaultGameScene;

    [Header("Prefabs (Quacksire project only)")]
    [SerializeField] GameObject characterPrefab;
    [SerializeField] GameObject bonfirePrefab;
    [SerializeField] GameObject tentPrefab;
    [SerializeField] GameObject treePrefab;
    [SerializeField] TerrainData terrainData;

    [Header("Character")]
    [SerializeField] RuntimeAnimatorController characterAnimator;
    [SerializeField] Avatar characterAvatar;
    [SerializeField] Vector3 characterPosition = new Vector3(13.65f, 0f, 14.25f);
    [SerializeField] float characterYaw = 200f;

    [Header("Props")]
    [SerializeField] Vector3 bonfirePosition = new Vector3(13.65f, 0f, 14.25f);
    [SerializeField] Vector3 tentPosition = new Vector3(16.2f, 0f, 12.8f);
    [SerializeField] float tentYaw = 35f;
    [SerializeField] int treeCount = 6;
    [SerializeField] float treeScatterRadius = 9f;

    [Header("Cameras")]
    [SerializeField] CinemachineCamera cameraIntro;
    [SerializeField] CinemachineCamera cameraHero;
    [SerializeField] float cameraSwitchDelay = 2.2f;
    [SerializeField] Transform lookTarget;

    [Header("Sun & fog")]
    [SerializeField] Light sunLight;
    [SerializeField] float atmosphereDuration = 7f;
    [SerializeField] Vector3 sunStartEuler = new Vector3(18f, -35f, 0f);
    [SerializeField] Vector3 sunEndEuler = new Vector3(42f, 25f, 0f);
    [SerializeField] float sunStartKelvin = 3200f;
    [SerializeField] float sunEndKelvin = 5200f;
    [SerializeField] Vector2 fogStartDistance = new Vector2(120f, 8f);
    [SerializeField] Vector2 fogEndDistance = new Vector2(140f, 45f);

    [Header("UI")]
    [SerializeField] string titleText = "QUACKSIRE";
    [SerializeField] string startButtonLabel = "START";

    Transform _environmentRoot;
    Camera _mainCamera;

    void Awake()
    {
        BuildWorld();
        BuildCameras();
        BuildMenuUi();
    }

    void Start()
    {
        StartCoroutine(IntroCameraRoutine());
        StartCoroutine(AtmosphereRoutine());
    }

    void BuildWorld()
    {
        _environmentRoot = new GameObject("MainMenuEnvironment").transform;

        if (terrainData != null)
        {
            Terrain existing = FindFirstObjectByType<Terrain>();
            if (existing == null)
            {
                GameObject terrainGo = Terrain.CreateTerrainGameObject(terrainData);
                terrainGo.name = "Ground";
                terrainGo.transform.SetParent(_environmentRoot, false);
            }
        }

        if (bonfirePrefab != null)
            Instantiate(bonfirePrefab, bonfirePosition, Quaternion.identity, _environmentRoot);

        if (tentPrefab != null)
            Instantiate(tentPrefab, tentPosition, Quaternion.Euler(0f, tentYaw, 0f), _environmentRoot);

        if (treePrefab != null)
        {
            for (int i = 0; i < treeCount; i++)
            {
                float angle = i * (360f / treeCount) * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * treeScatterRadius;
                Vector3 pos = bonfirePosition + offset;
                Instantiate(treePrefab, pos, Quaternion.Euler(0f, i * 57f, 0f), _environmentRoot);
            }
        }

        if (characterPrefab != null)
        {
            GameObject character = Instantiate(characterPrefab, characterPosition, Quaternion.Euler(0f, characterYaw, 0f), _environmentRoot);
            character.name = "MenuCharacter";

            Animator animator = character.GetComponent<Animator>();
            if (animator == null)
                animator = character.AddComponent<Animator>();

            if (characterAnimator != null)
                animator.runtimeAnimatorController = characterAnimator;
            if (characterAvatar != null)
                animator.avatar = characterAvatar;

            animator.applyRootMotion = false;
            animator.SetFloat("Speed", 0f);
        }

        if (lookTarget == null)
        {
            var targetGo = new GameObject("MenuLookTarget");
            targetGo.transform.SetParent(_environmentRoot, false);
            targetGo.transform.position = bonfirePosition + new Vector3(0f, 1.2f, 0f);
            lookTarget = targetGo.transform;
        }
    }

    void BuildCameras()
    {
        _mainCamera = Camera.main;
        if (_mainCamera == null)
        {
            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            _mainCamera = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
        }

        if (_mainCamera.GetComponent<CinemachineBrain>() == null)
            _mainCamera.gameObject.AddComponent<CinemachineBrain>();

        if (cameraIntro == null)
            cameraIntro = CreateMenuCamera("CM_MenuIntro", new Vector3(6f, 5.5f, 24f), new Vector3(12f, 18f, 0f));
        if (cameraHero == null)
            cameraHero = CreateMenuCamera("CM_MenuHero", new Vector3(17.5f, 2.8f, 19f), new Vector3(8f, -30f, 0f));

        AimCamera(cameraIntro);
        AimCamera(cameraHero);
        SetCameraPriority(cameraIntro, 20);
        SetCameraPriority(cameraHero, 10);
    }

    CinemachineCamera CreateMenuCamera(string name, Vector3 position, Vector3 euler)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.position = position;
        go.transform.rotation = Quaternion.Euler(euler);

        CinemachineCamera cm = go.AddComponent<CinemachineCamera>();
        var lookAt = go.AddComponent<CinemachineHardLookAt>();
        lookAt.LookAtOffset = new Vector3(0f, 1.1f, 0f);
        return cm;
    }

    void AimCamera(CinemachineCamera cm)
    {
        if (cm == null || lookTarget == null)
            return;

        cm.Target.TrackingTarget = lookTarget;
        cm.Target.CustomLookAtTarget = true;
        cm.Target.LookAtTarget = lookTarget;
    }

    static void SetCameraPriority(CinemachineCamera cm, int priority)
    {
        if (cm == null)
            return;

        cm.Priority = new PrioritySettings { Enabled = true, Value = priority };
    }

    void BuildMenuUi()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGo = new GameObject("MenuCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
        }

        if (canvas.GetComponent<GameWindowTitleBar>() == null)
            canvas.gameObject.AddComponent<GameWindowTitleBar>();

        Transform oldUi = canvas.transform.Find("MainMenuUI");
        if (oldUi != null)
            Destroy(oldUi.gameObject);

        GameObject uiRoot = new GameObject("MainMenuUI", typeof(RectTransform));
        uiRoot.transform.SetParent(canvas.transform, false);
        RectTransform uiRt = uiRoot.GetComponent<RectTransform>();
        uiRt.anchorMin = Vector2.zero;
        uiRt.anchorMax = Vector2.one;
        uiRt.offsetMin = Vector2.zero;
        uiRt.offsetMax = Vector2.zero;

        Text title = CreateUiText("Title", uiRoot.transform, titleText, 72, TextAnchor.UpperCenter, new Color(1f, 0.95f, 0.82f, 1f));
        RectTransform titleRt = title.rectTransform;
        titleRt.anchorMin = new Vector2(0.5f, 1f);
        titleRt.anchorMax = new Vector2(0.5f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.sizeDelta = new Vector2(900f, 100f);
        titleRt.anchoredPosition = new Vector2(0f, -80f);

        Button startButton = CreateStartButton(uiRoot.transform);
        RectTransform btnRt = startButton.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.5f, 0f);
        btnRt.anchorMax = new Vector2(0.5f, 0f);
        btnRt.pivot = new Vector2(0.5f, 0f);
        btnRt.sizeDelta = new Vector2(280f, 72f);
        btnRt.anchoredPosition = new Vector2(0f, 80f);
        startButton.onClick.AddListener(StartGame);

        Text hint = CreateUiText("Hint", uiRoot.transform, "Survive. Explore. Quack on.", 22, TextAnchor.LowerCenter, new Color(1f, 1f, 1f, 0.55f));
        RectTransform hintRt = hint.rectTransform;
        hintRt.anchorMin = new Vector2(0.5f, 0f);
        hintRt.anchorMax = new Vector2(0.5f, 0f);
        hintRt.pivot = new Vector2(0.5f, 0f);
        hintRt.sizeDelta = new Vector2(600f, 40f);
        hintRt.anchoredPosition = new Vector2(0f, 28f);
    }

    Button CreateStartButton(Transform parent)
    {
        GameObject go = new GameObject("StartButton", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        Image bg = go.GetComponent<Image>();
        bg.sprite = UiSprite(256, 64, new Color(0.08f, 0.12f, 0.14f, 0.88f), 12);
        bg.type = Image.Type.Sliced;

        Button button = go.GetComponent<Button>();
        button.targetGraphic = bg;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        button.colors = colors;

        Text label = CreateUiText("Label", go.transform, startButtonLabel, 32, TextAnchor.MiddleCenter, new Color(0.95f, 0.88f, 0.55f, 1f));
        Stretch(label.rectTransform, Vector2.zero, Vector2.zero);
        return button;
    }

    IEnumerator IntroCameraRoutine()
    {
        if (cameraIntro == null || cameraHero == null)
            yield break;

        SetCameraPriority(cameraIntro, 20);
        SetCameraPriority(cameraHero, 10);
        yield return new WaitForSeconds(cameraSwitchDelay);
        SetCameraPriority(cameraHero, 20);
        SetCameraPriority(cameraIntro, 10);
    }

    IEnumerator AtmosphereRoutine()
    {
        if (sunLight == null)
            sunLight = FindFirstObjectByType<Light>();

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.45f, 0.55f, 0.62f, 1f);

        float duration = Mathf.Max(0.1f, atmosphereDuration);
        float time = 0f;
        while (time < duration)
        {
            float t = time / duration;
            if (sunLight != null)
            {
                sunLight.transform.rotation = Quaternion.Euler(Vector3.Lerp(sunStartEuler, sunEndEuler, t));
                sunLight.colorTemperature = Mathf.Lerp(sunStartKelvin, sunEndKelvin, t);
            }

            RenderSettings.fogStartDistance = Mathf.Lerp(fogStartDistance.x, fogStartDistance.y, t);
            RenderSettings.fogEndDistance = Mathf.Lerp(fogEndDistance.x, fogEndDistance.y, t);
            time += Time.deltaTime;
            yield return null;
        }
    }

    public void StartGame()
    {
        string scene = string.IsNullOrWhiteSpace(gameSceneName) ? DefaultGameScene : gameSceneName;
        SceneManager.LoadScene(scene);
    }

    static Text CreateUiText(string name, Transform parent, string content, int size, TextAnchor anchor, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        Text text = go.GetComponent<Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.alignment = anchor;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    static void Stretch(RectTransform rt, Vector2 min, Vector2 max)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = min;
        rt.offsetMax = max;
    }

    static Sprite _uiSprite;

    static Sprite UiSprite(int width, int height, Color fill, int radius)
    {
        if (_uiSprite != null)
            return _uiSprite;

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color clear = new Color(0f, 0f, 0f, 0f);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float dx = Mathf.Max(radius - x, 0, x - (width - radius - 1));
                float dy = Mathf.Max(radius - y, 0, y - (height - radius - 1));
                tex.SetPixel(x, y, dx * dx + dy * dy <= radius * radius ? fill : clear);
            }
        }

        tex.Apply(false, true);
        _uiSprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        return _uiSprite;
    }
}
