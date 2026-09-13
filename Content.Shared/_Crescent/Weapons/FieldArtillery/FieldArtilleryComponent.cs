using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared._Crescent.Weapons.FieldArtillery;

/// <summary>
/// Crew-served artillery piece. It fires only while anchored, only for a gunner buckled into it, and only inside
/// a cone around the direction it was wrenched down facing. The round lives in the gun's own ammo provider, and the
/// gunner can't feed it from the seat, so it takes a second crewman to keep it firing.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FieldArtilleryComponent : Component
{
    /// <summary>
    /// Full width of the firing cone in degrees, centred on the gun's facing.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float FiringArc = 99f;

    /// <summary>
    /// Shots aimed further than this land at this distance along the same bearing.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MaxRange = 10f;

    /// <summary>
    /// Shots aimed closer than this are refused, so the crew can't shell their own position.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MinRange = 3f;

    /// <summary>
    /// Impact point of the shot currently being fired. The server hands it to the shell on the next update.
    /// </summary>
    [ViewVariables]
    public MapCoordinates? PendingTarget;
}
