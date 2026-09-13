using Content.Server.Antag.Components;
using Content.Server.Ghost.Roles.Components;
using Robust.Shared.Enums;
using Robust.Shared.Network.Messages;
using Robust.Shared.Placement;

namespace Content.Server.Ghost.Roles;

// Eclipsion: only admin-sourced ghost roles are offered to ghosts. Ghost roles that come from the
// world itself - salvage and wreck mobs, pAIs and positronic brains, sentient artifacts, cognizine,
// random sentience, zombified NPCs, mapped spawners - stay on their entities but never get listed.
public sealed partial class GhostRoleSystem
{
    /// <summary>
    /// True while an allowed admin/sandbox entity placement is spawning. Everything that registers a
    /// ghost role inside that window (including mobs a placed spawner creates on MapInit) is admin-sourced.
    /// </summary>
    private bool _adminPlacementScope;

    /// <summary>
    /// Called by the placement permission check once an entity placement request has been allowed.
    /// The engine spawns the entity synchronously right after and then raises <see cref="PlacementEntityEvent"/>.
    /// </summary>
    public void BeginAdminPlacement(MsgPlacement msg)
    {
        if (msg.PlaceType == PlacementManagerMessage.RequestPlacement && !msg.IsTile)
            _adminPlacementScope = true;
    }

    private void OnPlacementEntity(PlacementEntityEvent ev)
    {
        if (ev.PlacementEventAction != PlacementEventAction.Create)
            return;

        _adminPlacementScope = false;

        // Toggleable ghost roles (pAI etc.) only add their GhostRole when switched on later.
        if (HasComp<ToggleableGhostRoleComponent>(ev.EditedEntity))
            EnsureComp<AdminGhostRoleComponent>(ev.EditedEntity);
    }

    /// <summary>
    /// Whether this entity's ghost role may be offered to ghosts.
    /// </summary>
    public bool IsAdminGhostRole(EntityUid uid)
    {
        // Antag spawners only come from game rules, and Eclipsion's event table has none - admins start those.
        if (HasComp<AdminGhostRoleComponent>(uid) || HasComp<GhostRoleAntagSpawnerComponent>(uid))
            return true;

        if (!_adminPlacementScope)
            return false;

        EnsureComp<AdminGhostRoleComponent>(uid);
        return true;
    }
}
