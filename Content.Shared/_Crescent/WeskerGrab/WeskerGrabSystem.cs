using Content.Shared.CombatMode;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._Crescent.WeskerGrab;

/// <summary>
/// Applies the takedown described on <see cref="WeskerGrabComponent"/> the moment the owner
/// gets hold of someone while in combat mode.
/// </summary>
public sealed class WeskerGrabSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedCombatModeSystem _combatMode = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WeskerGrabComponent, PullStartedMessage>(OnPullStarted);
    }

    private void OnPullStarted(Entity<WeskerGrabComponent> ent, ref PullStartedMessage args)
    {
        // The message is raised on both ends of the pull, so ignore the one where we are the target.
        if (args.PullerUid != ent.Owner)
            return;

        if (_net.IsClient)
            return;

        // A grab only counts as a takedown if it was meant as one.
        if (!_combatMode.IsInCombatMode(ent.Owner))
            return;

        if (_timing.CurTime < ent.Comp.NextGrab)
            return;

        var target = args.PulledUid;

        // Nothing to take down if they are already out of the fight.
        if (!_mobState.IsAlive(target))
            return;

        ent.Comp.NextGrab = _timing.CurTime + ent.Comp.Cooldown;

        // AlwaysDrop empties their hands on the way down.
        _stun.TryKnockdown(target, ent.Comp.StunTime, true, DropHeldItemsBehavior.AlwaysDrop);
        _stun.TryStun(target, ent.Comp.StunTime, true);

        _popup.PopupEntity(Loc.GetString("wesker-grab-popup", ("user", ent.Owner), ("target", target)),
            target,
            PopupType.LargeCaution);
    }
}
