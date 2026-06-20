using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Enemy-only world-space health bar with a slim action-RPG style fill and delayed damage trail.
/// </summary>
[DefaultExecutionOrder(-100)]
public class WorldHealthBar : MonoBehaviour
{
    [SerializeField] HealthSystem healthSystem;
    [SerializeField] Vector3 localOffset = new Vector3(0f, 2.35f, 0f);
    [SerializeField] Vector2 barSize = new Vector2(155f, 12f);
    [SerializeField] float worldScale = 0.0065f;
    [SerializeField] float visibleDistance = 28f;
    [SerializeField] float damageTrailSpeed = 1.7f;
    [SerializeField] Color fillColor = new Color(0.94f, 0.18f, 0.15f, 1f);
    [SerializeField] Color trailColor = new Color(1f, 0.78f, 0.28f, 1f);
    [SerializeField] Color frameColor = new Color(0.05f, 0.045f, 0.04f, 0.88f);
    [SerializeField] Color backgroundColor = new Color(0.12f, 0.105f, 0.09f, 0.82f);

    Transform _billboardRoot;
    Camera _cam;
    Slider _slider;
    Image _trailFill;
    float _trailFraction = 1f;

    void Awake()
    {
        if (CompareTag("Player"))
        {
            enabled = false;
            return;
        }

        if (healthSystem == null)
            healthSystem = GetComponent<HealthSystem>();

        _cam = Camera.main;
        if (_cam == null)
            _cam = FindFirstObjectByType<Camera>();

        Slider slider = BuildBar();
        if (healthSystem != null)
        {
            healthSystem.BindSlider(slider);
            _trailFraction = HealthFraction();
        }
    }

    void LateUpdate()
    {
        if (_cam == null)
            _cam = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();

        if (_billboardRoot == null || _cam == null)
            return;

        _billboardRoot.position = transform.TransformPoint(localOffset);
        Vector3 toCam = _cam.transform.position - _billboardRoot.position;
        float sqrDistance = toCam.sqrMagnitude;
        bool shouldShow = sqrDistance <= visibleDistance * visibleDistance && healthSystem != null && !healthSystem.IsDead;
        _billboardRoot.gameObject.SetActive(shouldShow);
        if (!shouldShow)
            return;

        if (toCam.sqrMagnitude > 0.0001f)
            _billboardRoot.rotation = Quaternion.LookRotation(-toCam, Vector3.up);

        UpdateTrail();
    }

    Slider BuildBar()
    {
        var root = new GameObject("EnemyHealthBarDisplay");
        root.transform.SetParent(transform, false);
        root.layer = gameObject.layer;
        _billboardRoot = root.transform;

        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = barSize;
        root.transform.localScale = Vector3.one * worldScale;

        var frameGo = CreateImage("Frame", root.transform, frameColor);
        StretchFull(frameGo.GetComponent<RectTransform>(), 0f);

        var bgGo = CreateImage("Background", root.transform, backgroundColor);
        StretchFull(bgGo.GetComponent<RectTransform>(), 2f);

        var trailGo = CreateImage("DamageTrail", root.transform, trailColor);
        RectTransform trailRt = trailGo.GetComponent<RectTransform>();
        StretchFull(trailRt, 3f);
        _trailFill = trailGo.GetComponent<Image>();
        _trailFill.type = Image.Type.Filled;
        _trailFill.fillMethod = Image.FillMethod.Horizontal;
        _trailFill.fillOrigin = 0;
        _trailFill.fillAmount = 1f;

        var sliderGo = new GameObject("HealthSlider");
        sliderGo.transform.SetParent(root.transform, false);
        var sliderRt = sliderGo.AddComponent<RectTransform>();
        StretchFull(sliderRt, 3f);
        var slider = sliderGo.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.wholeNumbers = false;
        slider.interactable = false;

        var fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderGo.transform, false);
        var fillAreaRt = fillArea.AddComponent<RectTransform>();
        StretchFull(fillAreaRt, 0f);

        var fillGo = CreateImage("Fill", fillArea.transform, fillColor);
        var fillRt = fillGo.GetComponent<RectTransform>();
        StretchFull(fillRt, 0f);
        var fillImg = fillGo.GetComponent<Image>();
        fillImg.color = fillColor;
        fillImg.type = Image.Type.Simple;

        slider.fillRect = fillRt;
        slider.targetGraphic = fillImg;
        slider.transition = Selectable.Transition.None;
        _slider = slider;

        return slider;
    }

    void UpdateTrail()
    {
        if (_trailFill == null || healthSystem == null)
            return;

        float target = HealthFraction();
        if (target > _trailFraction)
            _trailFraction = target;
        else
            _trailFraction = Mathf.MoveTowards(_trailFraction, target, damageTrailSpeed * Time.deltaTime);

        _trailFill.fillAmount = _trailFraction;

        if (_slider != null)
        {
            _slider.maxValue = healthSystem.MaxHealth;
            _slider.value = healthSystem.CurrentHealth;
        }
    }

    float HealthFraction()
    {
        if (healthSystem == null)
            return 0f;

        return Mathf.Clamp01(healthSystem.CurrentHealth / Mathf.Max(1f, healthSystem.MaxHealth));
    }

    static GameObject CreateImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var image = go.AddComponent<Image>();
        image.sprite = WhiteSprite();
        image.color = color;
        return go;
    }

    static void StretchFull(RectTransform r, float inset)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = new Vector2(inset, inset);
        r.offsetMax = new Vector2(-inset, -inset);
    }

    static Sprite _white;

    static Sprite WhiteSprite()
    {
        if (_white != null)
            return _white;

        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply(false, true);
        _white = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 100f);
        return _white;
    }
}
