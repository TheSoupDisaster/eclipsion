using Content.Client.Overlays;
using Content.Shared._Crescent.Factions;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client._Crescent.Diplomacy;

/// <summary>
///     Draws the faction badge of the ID each mob is wearing, for the neutral civilian faction HUD. Unlike
///     <see cref="ShowFactionRelationIconsSystem"/> it says nothing about diplomacy - just whose card it is.
/// </summary>
public sealed class ShowFactionAffiliationIconsSystem : EquipmentHudSystem<ShowFactionAffiliationIconsComponent>
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly FactionCredentialReaderSystem _credentials = default!;

    private const string IconPrefix = "FactionAffiliationIcon";
    private static readonly ProtoId<FactionIconPrototype> UnknownIcon = "FactionRelationIconUnknown";

    /// <summary>
    ///     Called from <see cref="ShowFactionRelationIconsSystem"/>, which owns the
    ///     InventoryComponent/GetStatusIconsEvent subscription - the engine only allows one directed handler per
    ///     component/event pair, and StatusIconComponent's is already taken by <see cref="ShowJobIconsSystem"/>.
    /// </summary>
    public void AddStatusIcons(EntityUid uid, ref GetStatusIconsEvent ev)
    {
        if (!IsActive)
            return;

        // A faction without a badge of its own still has a credential, so it falls through to unknown rather than
        // silently showing nothing.
        FactionIconPrototype? icon = null;
        if (_credentials.TryGetWornFaction(uid, out var faction))
            _prototype.TryIndex<FactionIconPrototype>(IconPrefix + faction, out icon);

        if (icon != null || _prototype.TryIndex(UnknownIcon, out icon))
            ev.StatusIcons.Add(icon);
    }
}
