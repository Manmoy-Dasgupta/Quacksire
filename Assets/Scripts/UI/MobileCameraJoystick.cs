using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Camera look input from the right side of the screen only.
/// </summary>
[DefaultExecutionOrder(-45)]
public class MobileCameraJoystick : MonoBehaviour
{
    [SerializeField, Range(0.2f, 0.6f)] float movementZoneScreenPercent = MobileTouchZones.DefaultMovementZonePercent;
    [SerializeField] float lookRadiusPixels = 120f;
    [SerializeField, Range(0.1f, 1f)] float deadZone = 0.08f;

    int lookTouchId = int.MinValue;
    Vector2 lastLookPosition;

    public Vector2 LookInput { get; private set; }

    void Start()
    {
        MobileInputUILayout layout = FindFirstObjectByType<MobileInputUILayout>();
        if (layout != null)
            movementZoneScreenPercent = layout.MovementZoneScreenPercent;
    }

    void Update() => ReadCameraZoneTouch();

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
            if (LookInput.sqrMagnitude < deadZone * deadZone)
                LookInput = Vector2.zero;

            return;
        }

        lookTouchId = int.MinValue;
    }

    static bool IsTouchOverUi(int fingerId) =>
        EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(fingerId);
}
