using UnityEngine;

public class AdvancedCameraController : MonoBehaviour
{
    [Header("References")]
    private Transform player;
    private Transform cameraHolder;
    
    [Header("Camera Position")]
    public float distance = 4f;
    public float minDistance = 1f;
    public float maxDistance = 8f;
    public float height = 1.5f;
    public float minHeight = 0.5f;
    public float maxHeight = 3f;
    
    [Header("Rotation")]
    private float currentX = 0f;
    private float currentY = 20f;
    private float targetX = 0f;
    private float targetY = 20f;
    
    [Header("Rotation Limits")]
    public float maxYAngle = 80f;
    public float minYAngle = -30f;
    
    [Header("Sensitivity Settings")]
    public float sensitivityX = 1f;
    public float sensitivityY = 1f;
    public float aimSensitivity = 0.5f;
    
    [Header("Camera Smoothing")]
    public float smoothing = 0.1f;
    public float smoothingWhileAiming = 0.05f;
    private float currentSmoothing;
    
    [Header("Touch Input")]
    public float touchDeadzone = 10f;
    public float maxTouchDistance = 300f;
    
    [Header("Acceleration")]
    public bool useAcceleration = true;
    public float accelerationMultiplier = 1.5f;
    
    [Header("Look Acceleration")]
    public AnimationCurve accelerationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    private Vector2 touchStartPos;
    private Vector2 currentTouchPos;
    private bool isTouching = false;
    private bool isAiming = false;
    private float touchDistance = 0f;
    
    // For debug
    public bool showDebugInfo = true;
    private Vector2 lastSwipeDelta;

    void Start()
    {
        player = FindObjectOfType<PlayerController>().transform;
        cameraHolder = transform;
        
        if (player == null)
        {
            Debug.LogError("Player not found!");
            return;
        }
        
        // Load saved sensitivity settings
        LoadSensitivitySettings();
    }

    void Update()
    {
        HandleTouchInput();
        UpdateCameraRotation();
    }

    void LateUpdate()
    {
        if (player != null)
        {
            UpdateCameraPosition();
        }
    }

    #region Touch Input Handling

    void HandleTouchInput()
    {
        // Handle touch input using old Input system
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            
            if (touch.phase == TouchPhase.Began)
            {
                touchStartPos = touch.position;
                currentTouchPos = touch.position;
                isTouching = true;
            }
            else if (touch.phase == TouchPhase.Moved)
            {
                currentTouchPos = touch.position;
                Vector2 touchDelta = currentTouchPos - touchStartPos;
                touchDistance = touchDelta.magnitude;
                
                if (touchDistance > touchDeadzone)
                {
                    ProcessTouchDelta(touchDelta);
                }
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                isTouching = false;
                lastSwipeDelta = Vector2.zero;
            }
        }
    }

    void ProcessTouchDelta(Vector2 touchDelta)
    {
        // Normalize touch delta
        Vector2 normalizedDelta = touchDelta / Screen.width;
        
        // Apply deadzone
        normalizedDelta -= (touchDelta.normalized * touchDeadzone / Screen.width);
        
        // Calculate acceleration
        float accelerationFactor = 1f;
        if (useAcceleration)
        {
            float distanceRatio = Mathf.Clamp01(touchDistance / maxTouchDistance);
            accelerationFactor = accelerationCurve.Evaluate(distanceRatio);
            accelerationFactor = Mathf.Lerp(1f, accelerationMultiplier, accelerationFactor);
        }
        
        // Apply current sensitivity
        float currentSensX = sensitivityX * (isAiming ? aimSensitivity : 1f);
        float currentSensY = sensitivityY * (isAiming ? aimSensitivity : 1f);
        
        // Update target rotation
        targetX += normalizedDelta.x * currentSensX * 360f * accelerationFactor;
        targetY -= normalizedDelta.y * currentSensY * 360f * accelerationFactor;
        
        // Store last swipe for debug
        lastSwipeDelta = normalizedDelta;
        
        // Clamp Y rotation
        targetY = Mathf.Clamp(targetY, minYAngle, maxYAngle);
    }

    #endregion

    #region Camera Rotation

    void UpdateCameraRotation()
    {
        // Smooth rotation
        currentSmoothing = isAiming ? smoothingWhileAiming : smoothing;
        
        currentX = Mathf.Lerp(currentX, targetX, currentSmoothing);
        currentY = Mathf.Lerp(currentY, targetY, currentSmoothing);
    }

    #endregion

    #region Camera Position

    void UpdateCameraPosition()
    {
        // Calculate offset based on rotation
        Vector3 offset = Vector3.zero;
        offset.x = Mathf.Sin(Mathf.Deg2Rad * currentX) * Mathf.Cos(Mathf.Deg2Rad * currentY) * distance;
        offset.z = -Mathf.Cos(Mathf.Deg2Rad * currentX) * Mathf.Cos(Mathf.Deg2Rad * currentY) * distance;
        offset.y = Mathf.Sin(Mathf.Deg2Rad * currentY) * distance + height;

        // Set camera position
        cameraHolder.position = player.position + offset;
        
        // Look at player
        Vector3 lookTarget = player.position + Vector3.up * height * 0.5f;
        cameraHolder.LookAt(lookTarget);
    }

    #endregion

    #region Public Methods

    public void SetSensitivity(float x, float y)
    {
        sensitivityX = Mathf.Clamp01(x);
        sensitivityY = Mathf.Clamp01(y);
    }

    public void SetAimSensitivity(float value)
    {
        aimSensitivity = Mathf.Clamp01(value);
    }

    public void SetSmoothing(float value)
    {
        smoothing = Mathf.Clamp(value, 0.01f, 0.5f);
    }

    public void SetDeadzone(float value)
    {
        touchDeadzone = Mathf.Clamp(value, 0f, 50f);
    }

    public void SetAiming(bool aiming)
    {
        isAiming = aiming;
    }

    public float GetSensitivityX() => sensitivityX;
    public float GetSensitivityY() => sensitivityY;
    public float GetAimSensitivity() => aimSensitivity;
    public float GetSmoothing() => smoothing;
    public float GetDeadzone() => touchDeadzone;

    #endregion

    #region Settings Save/Load

    public void SaveSensitivitySettings()
    {
        PlayerPrefs.SetFloat("CameraSensitivityX", sensitivityX);
        PlayerPrefs.SetFloat("CameraSensitivityY", sensitivityY);
        PlayerPrefs.SetFloat("CameraAimSensitivity", aimSensitivity);
        PlayerPrefs.SetFloat("CameraSmoothing", smoothing);
        PlayerPrefs.SetFloat("CameraDeadzone", touchDeadzone);
        PlayerPrefs.SetFloat("CameraDistance", distance);
        PlayerPrefs.Save();
    }

    public void LoadSensitivitySettings()
    {
        sensitivityX = PlayerPrefs.GetFloat("CameraSensitivityX", 1f);
        sensitivityY = PlayerPrefs.GetFloat("CameraSensitivityY", 1f);
        aimSensitivity = PlayerPrefs.GetFloat("CameraAimSensitivity", 0.5f);
        smoothing = PlayerPrefs.GetFloat("CameraSmoothing", 0.1f);
        touchDeadzone = PlayerPrefs.GetFloat("CameraDeadzone", 10f);
        distance = PlayerPrefs.GetFloat("CameraDistance", 4f);
    }

    public void ResetToDefaults()
    {
        sensitivityX = 1f;
        sensitivityY = 1f;
        aimSensitivity = 0.5f;
        smoothing = 0.1f;
        touchDeadzone = 10f;
        distance = 4f;
        height = 1.5f;
        SaveSensitivitySettings();
    }

    #endregion

    #region Debug

    public void DrawDebugInfo()
    {
        if (!showDebugInfo) return;

        string debugText = $@"
=== CAMERA DEBUG INFO ===
Sensitivity X: {sensitivityX:F2}
Sensitivity Y: {sensitivityY:F2}
Aim Sensitivity: {aimSensitivity:F2}
Smoothing: {smoothing:F3}
Deadzone: {touchDeadzone:F1}
Is Aiming: {isAiming}
Is Touching: {isTouching}
Touch Distance: {touchDistance:F1}
Last Swipe Delta: {lastSwipeDelta}
Current Rotation: X={currentX:F1}° Y={currentY:F1}°
Target Rotation: X={targetX:F1}° Y={targetY:F1}°
Camera Position: {cameraHolder.position}
";
        Debug.Log(debugText);
    }

    #endregion
}