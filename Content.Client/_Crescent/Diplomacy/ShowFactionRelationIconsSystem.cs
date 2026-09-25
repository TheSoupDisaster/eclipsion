using Content.Client.Overlays;
using Content.Shared._Crescent.Diplomacy;
using Content.Shared._Crescent.Factions;
using Content.Shared._Crescent.HullrotFaction;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Client.Player;
using Robust.Shared.Prototypes;

namespace Content.Client._Crescent.Diplomacy;

/// <summary>
///     Draws each mob's diplomatic standing toward the faction a <see cref="ShowFactionRelationIconsComponent"/> HUD
///     is keyed to. The target's side comes from the ID card in their ID slot, never from the mob itself, so a
///     stolen credential reads as its issuing faction and a bare-faced boarder reads as unknown.
/// </summary>
public sealed class ShowFactionRelationIconsSystem : EquipmentHudSystem<ShowFactionRelationIconsComponent>
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly FactionCredentialReaderSystem _credentials = default!;
    [Dependency] private readonly FactionStatusSystem _factionStatus = default!;
    [Dependency] private readonly ShowFactionAffiliationIconsSystem _affiliation = default!;

    private static readonly ProtoId<FactionIconPrototype> OwnIcon = "FactionRelationIconOwn";
    private static readonly ProtoId<FactionIconPrototype> AlliedIcon = "FactionRelationIconAllied";
    private static readonly ProtoId<FactionIconPrototype> NeutralIcon = "FactionRelationIconNeutral";
    private static readonly ProtoId<FactionIconPrototype> HostileIcon = "FactionRelationIconHostile";
    private static readonly ProtoId<FactionIconPrototype> UnknownIcon = "FactionRelationIconUnknown";

    /// <summary>The faction the worn HUD is keyed to; empty means "whoever is wearing it".</summary>
    private string _hudFaction = string.Empty;

    public override void Initialize()
    {
        base.Initialize();

        // Not StatusIconComponent: ShowJobIconsSystem already holds that directed subscription, and a second one
        // throws when the first player list arrives. Only mobs with an inventory can wear a credential anyway.
        SubscribeLocalEvent<InventoryComponent, GetStatusIconsEvent>(OnGetStatusIconsEvent);
    }

    protected override void UpdateInternal(RefreshEquipmentHudEvent<ShowFactionRelationIconsComponent> args)
    {
        base.UpdateInternal(args);

        _hudFaction = string.Empty;
        foreach (var comp in args.Components)
        {
            if (string.IsNullOrWhiteSpace(comp.Faction))
                continue;

            _hudFaction = comp.Faction;
            break;
        }
    }

    protected override void DeactivateInternal()
    {
        base.DeactivateInternal();

        _hudFaction = string.Empty;
    }

    private void OnGetStatusIconsEvent(EntityUid uid, InventoryComponent _, ref GetStatusIconsEvent ev)
    {
        _affiliation.AddStatusIcons(uid, ref ev);

        if (!IsActive)
            return;

        var viewerFaction = GetViewerFaction();
        if (string.IsNullOrEmpty(viewerFaction))
            return;

        var iconId = UnknownIcon;
        if (_credentials.TryGetWornFaction(uid, out var targetFaction))
        {
            iconId = targetFaction == viewerFaction
                ? OwnIcon
                : _factionStatus.GetRelation(viewerFaction, targetFaction) switch
                {
                    FactionRelation.War => HostileIcon,
                    FactionRelation.Alliance => AlliedIcon,
                    _ => NeutralIcon,
                };
        }

        if (_prototype.TryIndex(iconId, out var iconPrototype))
            ev.StatusIcons.Add(iconPrototype);
    }

    private string? GetViewerFaction()
    {
        if (!string.IsNullOrEmpty(_hudFaction))
            return _hudFaction;

        return TryComp<HullrotFactionComponent>(_player.LocalEntity, out var member) ? member.Faction : null;
    }
}
