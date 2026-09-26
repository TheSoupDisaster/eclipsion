using Robust.Shared.GameStates;

namespace Content.Shared._Crescent.Factions;

/// <summary>
///     The faction advertised by an ID card. This is deliberately stored on the card rather than its holder:
///     anti-boarder defences and faction HUDs authenticate the credential being worn, including stolen credentials.
///     Networked so faction HUDs can resolve other mobs' credentials client-side. Only the server's
///     FactionIdCardSystem writes it.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FactionIdCardComponent : Component
{
    [DataField, AutoNetworkedField]
    public string Faction = string.Empty;
}
