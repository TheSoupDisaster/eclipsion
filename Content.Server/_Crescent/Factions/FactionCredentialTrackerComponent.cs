using Robust.Shared.GameObjects;

namespace Content.Server._Crescent.Factions;

/// <summary>
/// Remembers the exact physical faction cards issued to a member so dismissal can revoke them even after the member
/// moves them out of their ID slot.
/// </summary>
[RegisterComponent, Access(typeof(FactionIdCardSystem), typeof(FactionRecruitmentConsoleSystem))]
public sealed partial class FactionCredentialTrackerComponent : Component
{
    public readonly HashSet<EntityUid> Cards = new();
    public string Faction = string.Empty;
}
