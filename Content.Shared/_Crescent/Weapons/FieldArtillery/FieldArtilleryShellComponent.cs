using System.Numerics;

namespace Content.Shared._Crescent.Weapons.FieldArtillery;

/// <summary>
/// Artillery shell that flies over everything in its path and detonates at the point its field gun was aimed at.
/// </summary>
[RegisterComponent]
public sealed partial class FieldArtilleryShellComponent : Component
{
    [ViewVariables]
    public bool Armed;

    [ViewVariables]
    public Vector2 Origin;

    [ViewVariables]
    public Vector2 Target;

    [ViewVariables]
    public float Distance;
}
