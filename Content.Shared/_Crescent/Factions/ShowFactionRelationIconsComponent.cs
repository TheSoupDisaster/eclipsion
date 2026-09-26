using Robust.Shared.GameStates;

namespace Content.Shared._Crescent.Factions;

/// <summary>
///     Shows every mob's diplomatic standing toward <see cref="Faction"/> above their head. The target's faction is
///     read from the ID card worn in their ID slot, so a stolen credential passes and a missing one shows as unknown.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ShowFactionRelationIconsComponent : Component
{
    /// <summary>
    ///     The faction whose diplomacy this HUD is keyed to. Left empty, it uses the wearer's own faction.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string Faction = string.Empty;
}
