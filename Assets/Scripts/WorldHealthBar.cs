using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds a small world-space slider above the character and binds it to <see cref="HealthSystem"/>.
/// Fill is green for Player, red for Enemy (by tag).
/// </summary>
[DefaultExecutionOrder(-100)]
public class WorldHealthBar : MonoBehaviour
{
    [SerializeField] HealthSystem healthSystem;
    [SerializeField] Vector3 localOffset = new Vector3(0f, 2.15f, 0f);
    [SerializeField] Vector2 barSize = new Vector2(180f, 22f);
    [SerializeField] float worldScale = 0.006f;
    [SerializeField] Color playerFill = new Color(0.2f, 0.85f, 0.25f, 1f);
    [SerializeField] Color enemyFill = new Color(0.9f, 0.22f, 0.2f, 1f);
    [SerializeField] Color backgroundColor = new Color(0f, 0f, 0f, 0.65f);

    Transform _billboardRoot;
    Camera _cam;

    void Awake()
    {
        if (healthSystem == null)
            healthSystem = GetComponent<HealthSystem>();

        _cam = Camera.main;
        if (_cam == null)
            _cam = FindFirstObjectByType<Camera>();
        Color fill = CompareTag("Enemy") ? enemyFill : playerFill;
        Slider slider = BuildBar(fill);
        if (healthSystem != null)
            healthSystem.BindSlider(slider);
    }

    void LateUpdate()
    {
        if (_cam == null)
            _cam = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();

        if (_billboardRoot == null || _cam == null)
            return;

        _billboardRoot.position = transform.TransformPoint(localOffset);
        Vector3 toCam = _billboardRoot.position - _cam.transform.position;
        if (toCam.sqrMagnitude > 0.0001f)
            _billboardRoot.rotation = Quaternion.LookRotation(toCam);
    }

    Slider BuildBar(Color fillColor)
    {
        var root = new GameObject("WorldHealthBarDisplay");
        root.transform.SetParent(transform, false);
        root.layer = gameObject.layer;
        _billboardRoot = root.transform;

        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = barSize;
        root.transform.localScale = Vector3.one * worldScale;

        root.AddComponent<CanvasScaler>();
        root.AddComponent<GraphicRaycaster>();

        var bgGo = new GameObject("Background");
        bgGo.transform.SetParent(root.transform, false);
        var bgRt = bgGo.AddComponent<RectTransform>();
        StretchFull(bgRt);
        var bgImg = bgGo.AddComponent<Image>();
        bgImg.sprite = WhiteSprite();
        bgImg.color = backgroundColor;

        var sliderGo = new GameObject("HealthSlider");
        sliderGo.transform.SetParent(root.transform, false);
        var sliderRt = sliderGo.AddComponent<RectTransform>();
        StretchFull(sliderRt);
        var slider = sliderGo.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.wholeNumbers = false;
        slider.interactable = false;

        var fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderGo.transform, false);
        var fillAreaRt = fillArea.AddComponent<RectTransform>();
        StretchFull(fillAreaRt);

        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(fillArea.transform, false);
        var fillRt = fillGo.AddComponent<RectTransform>();
        StretchFull(fillRt);
        var fillImg = fillGo.AddComponent<Image>();
        fillImg.sprite = WhiteSprite();
        fillImg.color = fillColor;
        fillImg.type = Image.Type.Simple;

        slider.fillRect = fillRt;
        slider.targetGraphic = fillImg;
        slider.transition = Selectable.Transition.None;

        return slider;
    }

    static void StretchFull(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = new Vector2(3f, 3f);
        r.offsetMax = new Vector2(-3f, -3f);
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
