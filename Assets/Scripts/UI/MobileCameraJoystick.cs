using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Camera look input from the right side of the screen only (separate from movement joystick).
/// </summary>
[DefaultExecutionOrder(-45)]
public class MobileCameraJoystick : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Canvas canvas;

    [Header("Touch Zone")]
    [SerializeField, Range(0.2f, 0.6f)] private float movementZoneScreenPercent = MobileTouchZones.DefaultMovementZonePercent;
    [SerializeField] private float lookRadiusPixels = 120f;
    [SerializeField, Range(0.1f, 1f)] private float deadZone = 0.08f;
    [SerializeField] private bool hideExistingLookGraphics = true;

    int lookTouchId = int.MinValue;
    Vector2 lastLookPosition;

    public Vector2 LookInput { get; private set; }
    public bool HasInput => LookInput.sqrMagnitude > deadZone * deadZone;

    void Start()
    {
        ResolveCanvas();
        SyncZoneFromLayout();
        if (hideExistingLookGraphics)
            HideGeneratedLookGraphics();
    }

    void Update()
    {
        ReadCameraZoneTouch();
    }

    void ResolveCanvas()
    {
        if (canvas != null)
            return;

        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (Canvas candidate in canvases)
        {
            if (candidate.renderMode != RenderMode.WorldSpace && candidate.transform.parent == null)
            {
                canvas = candidate;
                return;
            }
        }
    }

    void SyncZoneFromLayout()
    {
        MobileInputUILayout layout = FindFirstObjectByType<MobileInputUILayout>();
        if (layout != null)
            movementZoneScreenPercent = layout.MovementZoneScreenPercent;
    }

    void ReadCameraZoneTouch()
    {
        LookInput = Vector2.zero;

        if (Input.touchCount == 0)
        {
            lookTouchId = int.MinValue;
            return;
        }

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);

            if (lookTouchId == int.MinValue)
            {
                if (touch.phase == TouchPhase.Began
                    && MobileTouchZones.IsCameraZone(touch.position, movementZoneScreenPercent)
                    && !IsTouchOverUi(touch.fingerId))
                {
                    lookTouchId = touch.fingerId;
                    lastLookPosition = touch.position;
                }
            }

            if (touch.fingerId != lookTouchId)
                continue;

            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                lookTouchId = int.MinValue;
                return;
            }

            if (!MobileTouchZones.IsCameraZone(touch.position, movementZoneScreenPercent))
            {
                lookTouchId = int.MinValue;
                return;
            }

            Vector2 delta = touch.position - lastLookPosition;
            lastLookPosition = touch.position;
            LookInput = Vector2.ClampMagnitude(delta / Mathf.Max(1f, lookRadiusPixels), 1f);
            if (LookInput.magnitude < deadZone)
                LookInput = Vector2.zero;

            return;
        }

        lookTouchId = int.MinValue;
    }

    static bool IsTouchOverUi(int fingerId)
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(fingerId);
    }

    void HideGeneratedLookGraphics()
    {
        if (canvas == null)
            return;

        Transform existing = canvas.transform.Find("CameraLookJoystick");
        if (existing != null)
            existing.gameObject.SetActive(false);
    }
}
