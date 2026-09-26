using Robust.Shared.GameStates;

namespace Content.Shared._Crescent.Factions;

/// <summary>
///     Shows which faction every mob belongs to, with no opinion on whether that faction is friend or foe. Like
///     <see cref="ShowFactionRelationIconsComponent"/>, the answer comes from the ID card worn in the target's ID slot.
///     Icons are looked up as <c>FactionAffiliationIcon{FactionId}</c>.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ShowFactionAffiliationIconsComponent : Component;
