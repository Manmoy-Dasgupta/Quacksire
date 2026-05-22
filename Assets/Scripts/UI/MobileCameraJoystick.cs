using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Invisible right-side look zone for Android camera control.
/// Left 40% of the screen is reserved for movement; this script only reads the right 60%.
/// </summary>
[DefaultExecutionOrder(-45)]
public class MobileCameraJoystick : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Canvas canvas;

    [Header("Touch Zone")]
    [SerializeField, Range(0.4f, 0.8f)] private float rightTouchStartPercent = 0.4f;
    [SerializeField] private float lookRadiusPixels = 120f;
    [SerializeField, Range(0.1f, 1f)] private float deadZone = 0.08f;
    [SerializeField] private bool hideExistingLookGraphics = true;

    private int lookTouchId = int.MinValue;
    private Vector2 lastLookPosition;

    public Vector2 LookInput { get; private set; }
    public bool HasInput => LookInput.sqrMagnitude > deadZone * deadZone;

    private void Start()
    {
        ResolveCanvas();
        if (hideExistingLookGraphics)
            HideGeneratedLookGraphics();
    }

    private void Update()
    {
        ReadRightSideTouch();
    }

    private void ResolveCanvas()
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

    private void ReadRightSideTouch()
    {
        LookInput = Vector2.zero;

        if (Input.touchCount == 0)
        {
            lookTouchId = int.MinValue;
            return;
        }

        float rightBoundary = Screen.width * rightTouchStartPercent;

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);

            if (lookTouchId == int.MinValue)
            {
                if (touch.phase == TouchPhase.Began && touch.position.x >= rightBoundary && !IsTouchOverUi(touch.fingerId))
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

            Vector2 delta = touch.position - lastLookPosition;
            lastLookPosition = touch.position;
            LookInput = Vector2.ClampMagnitude(delta / Mathf.Max(1f, lookRadiusPixels), 1f);
            if (LookInput.magnitude < deadZone)
                LookInput = Vector2.zero;

            return;
        }

        lookTouchId = int.MinValue;
    }

    private static bool IsTouchOverUi(int fingerId)
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(fingerId);
    }

    private void HideGeneratedLookGraphics()
    {
        if (canvas == null)
            return;

        Transform existing = canvas.transform.Find("CameraLookJoystick");
        if (existing != null)
            existing.gameObject.SetActive(false);
    }
}
