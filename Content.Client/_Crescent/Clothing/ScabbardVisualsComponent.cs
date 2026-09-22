namespace Content.Client._Crescent.Clothing;

/// <summary>
/// Uses the scabbard's ItemMapper layer to select its occupied belt appearance.
/// </summary>
[RegisterComponent]
public sealed partial class ScabbardVisualsComponent : Component
{
    [DataField]
    public string FilledPrefix = "sheathed";
}
