namespace Content.Shared._Crescent.Chasm;

[RegisterComponent]
public sealed partial class PreventChasmFallingComponent : Component
{
    [DataField]
    public bool DeleteOnUse = true;
}
