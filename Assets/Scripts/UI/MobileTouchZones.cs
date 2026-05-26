using UnityEngine;

/// <summary>Shared screen split for movement (left) vs camera look (right).</summary>
public static class MobileTouchZones
{
    public const float DefaultMovementZonePercent = 0.4f;

    public static bool IsMovementZone(Vector2 screenPosition, float movementZonePercent = DefaultMovementZonePercent)
    {
        return screenPosition.x <= Screen.width * movementZonePercent;
    }

    public static bool IsCameraZone(Vector2 screenPosition, float movementZonePercent = DefaultMovementZonePercent)
    {
        return screenPosition.x >= Screen.width * movementZonePercent;
    }
}
