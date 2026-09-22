using Robust.Shared.GameStates;

namespace Content.Shared._Crescent.WeskerGrab;

/// <summary>
/// Turns the owner's combat mode grab into a takedown. Whoever they get hold of drops everything
/// they were carrying and is left on the floor unable to act for a moment.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class WeskerGrabComponent : Component
{
    /// <summary>
    /// How long the target is stunned and kept down for.
    /// </summary>
    [DataField]
    public TimeSpan StunTime = TimeSpan.FromSeconds(4);

    /// <summary>
    /// Minimum time between two takedowns. Without it the owner could re-grab the same target
    /// the moment they stand up and hold them stunned forever.
    /// </summary>
    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(8);

    [ViewVariables]
    public TimeSpan NextGrab;
}
