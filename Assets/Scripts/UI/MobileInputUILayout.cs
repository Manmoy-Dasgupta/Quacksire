using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Creates left (movement) and right (camera) full-screen touch zones and parents the joystick under the left zone only.
/// </summary>
[DefaultExecutionOrder(-55)]
public class MobileInputUILayout : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform movementJoystick;
    [SerializeField, Range(0.2f, 0.6f)] private float movementZoneScreenPercent = MobileTouchZones.DefaultMovementZonePercent;

    RectTransform movementZone;
    RectTransform cameraZone;

    public RectTransform MovementZone => movementZone;
    public RectTransform CameraZone => cameraZone;
    public float MovementZoneScreenPercent => movementZoneScreenPercent;

    void Awake()
    {
        ResolveCanvas();
        BuildZones();
    }

    void ResolveCanvas()
    {
        if (canvas != null)
            return;

        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = FindFirstObjectByType<Canvas>();
    }

    void BuildZones()
    {
        if (canvas == null)
            return;

        Transform existing = canvas.transform.Find("MobileInputZones");
        if (existing != null)
            Destroy(existing.gameObject);

        GameObject root = new GameObject("MobileInputZones", typeof(RectTransform));
        root.transform.SetParent(canvas.transform, false);
        root.transform.SetAsFirstSibling();

        RectTransform rootRt = root.GetComponent<RectTransform>();
        Stretch(rootRt);

        movementZone = CreateZone("MovementZone", rootRt, new Vector2(0f, 0f), new Vector2(movementZoneScreenPercent, 1f));
        cameraZone = CreateZone("CameraZone", rootRt, new Vector2(movementZoneScreenPercent, 0f), new Vector2(1f, 1f));

        Image movementBlocker = movementZone.gameObject.AddComponent<Image>();
        movementBlocker.color = new Color(0f, 0f, 0f, 0f);
        movementBlocker.raycastTarget = true;

        Image cameraBlocker = cameraZone.gameObject.AddComponent<Image>();
        cameraBlocker.color = new Color(0f, 0f, 0f, 0f);
        cameraBlocker.raycastTarget = true;

        if (movementJoystick == null)
        {
            Joystick stick = FindFirstObjectByType<PlayerMovementJoystick>();
            if (stick == null)
                stick = FindFirstObjectByType<VariableJoystick>();
            if (stick != null)
                movementJoystick = stick.GetComponent<RectTransform>();
        }

        if (movementJoystick != null)
        {
            movementJoystick.SetParent(movementZone, false);
            Stretch(movementJoystick);
        }
    }

    static RectTransform CreateZone(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        return rt;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }
}
