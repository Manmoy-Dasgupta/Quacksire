using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Movement joystick only — ignores touches outside the left movement zone.
/// </summary>
public class PlayerMovementJoystick : VariableJoystick
{
    [SerializeField, Range(0.2f, 0.6f)] private float movementZoneScreenPercent = MobileTouchZones.DefaultMovementZonePercent;

    public override void OnPointerDown(PointerEventData eventData)
    {
        if (!IsMovementZone(eventData))
            return;

        base.OnPointerDown(eventData);
    }

    public override void OnDrag(PointerEventData eventData)
    {
        if (!IsMovementZone(eventData))
        {
            base.OnPointerUp(eventData);
            return;
        }

        base.OnDrag(eventData);
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);
    }

    bool IsMovementZone(PointerEventData eventData)
    {
        return MobileTouchZones.IsMovementZone(eventData.position, movementZoneScreenPercent);
    }
}
