using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds a red title-bar strip with minimize, maximize, and close buttons (top-right).
/// </summary>
[DefaultExecutionOrder(50)]
public class GameWindowTitleBar : MonoBehaviour
{
    [SerializeField] Canvas canvas;
    [SerializeField] float barHeight = 36f;
    [SerializeField] float buttonWidth = 46f;
    [SerializeField] Color barColor = new Color(0.86f, 0.12f, 0.12f, 1f);
    [SerializeField] Color iconColor = Color.white;
    [SerializeField] Color hoverColor = new Color(1f, 1f, 1f, 0.12f);
    [SerializeField] Color closeHoverColor = new Color(0.9f, 0.15f, 0.15f, 1f);

    void Start()
    {
        if (canvas == null)
            canvas = FindScreenCanvas();

        if (canvas == null)
        {
            Debug.LogWarning("[GameWindowTitleBar] No screen-space Canvas found.", this);
            return;
        }

        BuildTitleBar();
    }

    Canvas FindScreenCanvas()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (Canvas c in canvases)
        {
            if (c.renderMode != RenderMode.WorldSpace && c.transform.parent == null)
                return c;
        }

        return null;
    }

    void BuildTitleBar()
    {
        Transform existing = canvas.transform.Find("WindowTitleBar");
        if (existing != null)
            Destroy(existing.gameObject);

        GameObject root = new GameObject("WindowTitleBar", typeof(RectTransform));
        root.transform.SetParent(canvas.transform, false);
        root.transform.SetAsLastSibling();

        RectTransform bar = root.GetComponent<RectTransform>();
        bar.anchorMin = new Vector2(0f, 1f);
        bar.anchorMax = new Vector2(1f, 1f);
        bar.pivot = new Vector2(0.5f, 1f);
        bar.sizeDelta = new Vector2(0f, barHeight);
        bar.anchoredPosition = Vector2.zero;

        Image bg = root.AddComponent<Image>();
        bg.color = barColor;
        bg.raycastTarget = true;

        RectTransform buttons = new GameObject("Buttons", typeof(RectTransform)).GetComponent<RectTransform>();
        buttons.SetParent(bar, false);
        buttons.anchorMin = new Vector2(1f, 0f);
        buttons.anchorMax = new Vector2(1f, 1f);
        buttons.pivot = new Vector2(1f, 0.5f);
        buttons.sizeDelta = new Vector2(buttonWidth * 3f, 0f);
        buttons.anchoredPosition = Vector2.zero;
        buttons.offsetMin = new Vector2(-buttonWidth * 3f, 0f);
        buttons.offsetMax = Vector2.zero;

        CreateWindowButton(buttons, "Minimize", 2, DrawMinimizeIcon, GameWindowControls.Minimize);
        CreateWindowButton(buttons, "Maximize", 1, DrawMaximizeIcon, GameWindowControls.ToggleMaximize);
        CreateWindowButton(buttons, "Close", 0, DrawCloseIcon, GameWindowControls.Close, useCloseHover: true);
    }

    void CreateWindowButton(RectTransform parent, string name, int index, System.Action<RectTransform> drawIcon, UnityEngine.Events.UnityAction onClick, bool useCloseHover = false)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 0.5f);
        rt.sizeDelta = new Vector2(buttonWidth, 0f);
        rt.anchoredPosition = new Vector2(-index * buttonWidth, 0f);

        Image img = go.GetComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0f);

        Button button = go.GetComponent<Button>();
        button.targetGraphic = img;
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0f, 0f, 0f, 0f);
        colors.highlightedColor = useCloseHover ? closeHoverColor : hoverColor;
        colors.pressedColor = colors.highlightedColor;
        colors.selectedColor = colors.normalColor;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        button.onClick.AddListener(onClick);

        RectTransform iconRoot = new GameObject("Icon", typeof(RectTransform)).GetComponent<RectTransform>();
        iconRoot.SetParent(rt, false);
        iconRoot.anchorMin = Vector2.zero;
        iconRoot.anchorMax = Vector2.one;
        iconRoot.offsetMin = Vector2.zero;
        iconRoot.offsetMax = Vector2.zero;
        drawIcon(iconRoot);
    }

    void DrawMinimizeIcon(RectTransform parent)
    {
        Image line = CreateIconImage("Line", parent);
        RectTransform rt = line.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(14f, 2f);
        rt.anchoredPosition = Vector2.zero;
        line.sprite = WhiteSprite();
        line.color = iconColor;
    }

    void DrawMaximizeIcon(RectTransform parent)
    {
        Image back = CreateIconImage("Back", parent);
        RectTransform backRt = back.rectTransform;
        backRt.anchorMin = new Vector2(0.5f, 0.5f);
        backRt.anchorMax = new Vector2(0.5f, 0.5f);
        backRt.pivot = new Vector2(0.5f, 0.5f);
        backRt.sizeDelta = new Vector2(12f, 12f);
        backRt.anchoredPosition = new Vector2(-2f, 2f);
        back.sprite = OutlineSprite(12, 2);
        back.color = iconColor;
        back.type = Image.Type.Sliced;

        Image front = CreateIconImage("Front", parent);
        RectTransform frontRt = front.rectTransform;
        frontRt.anchorMin = new Vector2(0.5f, 0.5f);
        frontRt.anchorMax = new Vector2(0.5f, 0.5f);
        frontRt.pivot = new Vector2(0.5f, 0.5f);
        frontRt.sizeDelta = new Vector2(12f, 12f);
        frontRt.anchoredPosition = new Vector2(3f, -3f);
        front.sprite = OutlineSprite(12, 2);
        front.color = iconColor;
        front.type = Image.Type.Sliced;
    }

    void DrawCloseIcon(RectTransform parent)
    {
        Image a = CreateIconImage("LineA", parent);
        RectTransform aRt = a.rectTransform;
        aRt.anchorMin = new Vector2(0.5f, 0.5f);
        aRt.anchorMax = new Vector2(0.5f, 0.5f);
        aRt.pivot = new Vector2(0.5f, 0.5f);
        aRt.sizeDelta = new Vector2(14f, 2f);
        aRt.localEulerAngles = new Vector3(0f, 0f, 45f);
        a.sprite = WhiteSprite();
        a.color = iconColor;

        Image b = CreateIconImage("LineB", parent);
        RectTransform bRt = b.rectTransform;
        bRt.anchorMin = new Vector2(0.5f, 0.5f);
        bRt.anchorMax = new Vector2(0.5f, 0.5f);
        bRt.pivot = new Vector2(0.5f, 0.5f);
        bRt.sizeDelta = new Vector2(14f, 2f);
        bRt.localEulerAngles = new Vector3(0f, 0f, -45f);
        b.sprite = WhiteSprite();
        b.color = iconColor;
    }

    static Image CreateIconImage(string name, RectTransform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.raycastTarget = false;
        return image;
    }

    static Sprite _white;
    static Sprite _outline;

    static Sprite WhiteSprite()
    {
        if (_white != null)
            return _white;

        Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply(false, true);
        _white = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 100f);
        return _white;
    }

    static Sprite OutlineSprite(int size, int border)
    {
        if (_outline != null)
            return _outline;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color clear = new Color(1f, 1f, 1f, 0f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool edge = x < border || y < border || x >= size - border || y >= size - border;
                tex.SetPixel(x, y, edge ? Color.white : clear);
            }
        }

        tex.Apply(false, true);
        _outline = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        return _outline;
    }
}
