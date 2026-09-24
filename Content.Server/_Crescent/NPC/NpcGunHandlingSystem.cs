using System.Diagnostics.CodeAnalysis;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Wieldable;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server._Crescent.NPC;

/// <summary>
/// Crescent: gets a held gun into a state an NPC can actually fire it from.
/// </summary>
/// <remarks>
/// Hullrot guns are not point-and-click the way upstream's are. A rifle spawns unwielded with the bolt
/// open, a pump shotgun has to be worked between shots, and the HTN has no operator for any of it - so an
/// NPC handed a faction primary walks up to its target, aims, and then fails every shot silently, because
/// GunRequiresWield cancels ShotAttemptedEvent and an open bolt refuses to feed ammo. This does those
/// steps on the NPC's behalf while it is in ranged combat.
/// </remarks>
public sealed class NpcGunHandlingSystem : EntitySystem
{
    [Dependency] private readonly GunSystem _gun = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly WieldableSystem _wieldable = default!;

    private const string MagazineSlot = "gun_magazine";

    /// <summary>
    /// Gap between handling steps.
    /// </summary>
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(0.5);

    private static readonly TimeSpan ResupplyRetryDelay = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Readies <paramref name="gunUid"/> for <paramref name="npc"/>, doing at most one step per call.
    /// </summary>
    /// <returns>True when the gun is ready to be fired this tick.</returns>
    public bool TryReadyGun(EntityUid npc, EntityUid gunUid)
    {
        // Doesn't apply to a creature that *is* the gun, or to one holding something with no handling
        // steps at all, and those shouldn't pick up the bookkeeping component either.
        if (npc == gunUid || !NeedsHandling(gunUid))
            return true;

        var comp = EnsureComp<NpcGunHandlingComponent>(npc);
        var curTime = _timing.CurTime;

        // A reload in progress blocks everything else until it lands.
        if (comp.ResupplyEnd is { } end)
        {
            if (curTime < end)
                return false;

            comp.ResupplyEnd = null;
            Resupply(gunUid, comp);
        }

        if (comp.NextAttempt > curTime)
            return false;

        // One step per tick, rate limited, so a gun that can't be readied doesn't spam wield/rack
        // popups at everyone standing nearby.
        if (comp.Wield && NeedsWield(gunUid, out var wieldable))
        {
            comp.NextAttempt = curTime + RetryDelay;

            // If the wield itself cannot happen - no free hand, something blocking it - there is nothing
            // further to try, so let the shot go ahead and be refused the way it was before rather than
            // freezing the NPC here forever.
            if (_wieldable.TryWield(gunUid, wieldable, npc))
                return false;
        }
        else if (comp.Resupply && TryStartResupply(gunUid, comp, curTime))
        {
            comp.NextAttempt = curTime + RetryDelay;
            return false;
        }
        else if (comp.Cycle && TryCycle(npc, gunUid))
        {
            comp.NextAttempt = curTime + RetryDelay;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Cheap check so the common case - a mob whose "gun" is its own innate attack - costs nothing.
    /// </summary>
    private bool NeedsHandling(EntityUid gunUid)
    {
        // ChamberMagazine derives from Magazine but is its own registered component, so HasComp on the
        // base type does not match it - it has to be asked for by name.
        return HasComp<WieldableComponent>(gunUid)
               || HasComp<MagazineAmmoProviderComponent>(gunUid)
               || HasComp<ChamberMagazineAmmoProviderComponent>(gunUid)
               || HasComp<BallisticAmmoProviderComponent>(gunUid);
    }

    private bool NeedsWield(EntityUid gunUid, [NotNullWhen(true)] out WieldableComponent? wieldable)
    {
        return TryComp(gunUid, out wieldable)
               && !wieldable.Wielded
               && HasComp<GunRequiresWieldComponent>(gunUid);
    }

    /// <summary>
    /// Closes an open bolt, or works the action on a gun that doesn't eject for itself.
    /// </summary>
    private bool TryCycle(EntityUid npc, EntityUid gunUid)
    {
        if (TryComp<ChamberMagazineAmmoProviderComponent>(gunUid, out var chamber) &&
            chamber.BoltClosed == false)
        {
            _gun.SetBoltClosed(gunUid, chamber, true, npc);
            return true;
        }

        if (TryComp<BallisticAmmoProviderComponent>(gunUid, out var ballistic) &&
            !ballistic.AutoCycle &&
            !ballistic.Cycled)
        {
            _gun.ManualCycle(gunUid, ballistic, _transform.GetMapCoordinates(gunUid), npc);
            return true;
        }

        return false;
    }

    private bool TryStartResupply(EntityUid gunUid, NpcGunHandlingComponent comp, TimeSpan curTime)
    {
        if (comp.ResupplyCount is <= 0)
            return false;

        if (!IsDry(gunUid))
            return false;

        if (!CanResupply(gunUid))
        {
            comp.NextAttempt = curTime + ResupplyRetryDelay;
            return false;
        }

        comp.ResupplyEnd = curTime + comp.ResupplyDelay;
        return true;
    }

    /// <summary>
    /// Whether the gun has nothing left to fire. A chambered round still counts, so this waits for the
    /// gun to run properly dry rather than reloading on the last shot.
    /// </summary>
    /// <remarks>
    /// Spent cases left in a gun that doesn't eject for itself are part of <see cref="GetAmmoCountEvent"/>,
    /// so counting that alone would leave such a gun looking loaded forever and it would never resupply.
    /// </remarks>
    private bool IsDry(EntityUid gunUid)
    {
        if (TryComp<BallisticAmmoProviderComponent>(gunUid, out var ballistic))
        {
            if (ballistic.UnspawnedCount > 0)
                return false;

            foreach (var ent in ballistic.Entities)
            {
                if (!TryComp<CartridgeAmmoComponent>(ent, out var cartridge) || !cartridge.Spent)
                    return false;
            }

            return true;
        }

        var ammoEv = new GetAmmoCountEvent();
        RaiseLocalEvent(gunUid, ref ammoEv);
        return ammoEv.Count == 0;
    }

    private bool CanResupply(EntityUid gunUid)
    {
        if (_itemSlots.TryGetSlot(gunUid, MagazineSlot, out var slot))
            return slot.StartingItem != null;

        return TryComp<BallisticAmmoProviderComponent>(gunUid, out var ballistic) && ballistic.Proto != null;
    }

    /// <summary>
    /// Stands in for the spare magazines an NPC would be carrying: a fresh magazine in the well, or a
    /// topped-up internal magazine for guns that load loose rounds.
    /// </summary>
    private void Resupply(EntityUid gunUid, NpcGunHandlingComponent comp)
    {
        var refilled = _itemSlots.TryGetSlot(gunUid, MagazineSlot, out var slot)
            ? ReplaceMagazine(gunUid, slot)
            : RefillInternal(gunUid);

        if (!refilled)
        {
            // Whatever stopped it isn't going to change, and retrying forever would leave the NPC
            // permanently reloading. Let it fall back to dropping the gun and finding another.
            comp.Resupply = false;
            return;
        }

        if (comp.ResupplyCount is { } remaining)
            comp.ResupplyCount = remaining - 1;

        _gun.UpdateAmmoCount(gunUid);
    }

    private bool ReplaceMagazine(EntityUid gunUid, ItemSlot slot)
    {
        if (slot.StartingItem is not { } proto)
            return false;

        var spent = slot.Item;

        if (spent != null && !_itemSlots.TryEject(gunUid, slot, null, out _, excludeUserAudio: true))
            return false;

        var mag = Spawn(proto, _transform.GetMapCoordinates(gunUid));

        if (_itemSlots.TryInsert(gunUid, slot, mag, null, excludeUserAudio: true))
        {
            if (spent != null)
                QueueDel(spent.Value);

            return true;
        }

        // The fresh magazine wouldn't go in, so put the empty one back rather than leaving the NPC
        // holding a gun with no magazine at all.
        QueueDel(mag);

        if (spent != null)
            _itemSlots.TryInsert(gunUid, slot, spent.Value, null, excludeUserAudio: true);

        return false;
    }

    private bool RefillInternal(EntityUid gunUid)
    {
        if (!TryComp<BallisticAmmoProviderComponent>(gunUid, out var ballistic) || ballistic.Proto == null)
            return false;

        // Guns that don't auto-eject leave their spent case sitting in the container; clear those out
        // first or the refill has nowhere to go.
        foreach (var ent in ballistic.Entities.ToArray())
        {
            if (!TryComp<CartridgeAmmoComponent>(ent, out var cartridge) || !cartridge.Spent)
                continue;

            ballistic.Entities.Remove(ent);
            _container.Remove(ent, ballistic.Container, force: true);
            QueueDel(ent);
        }

        var free = ballistic.Capacity - ballistic.Entities.Count;

        if (free <= 0)
            return false;

        ballistic.UnspawnedCount = free;
        ballistic.Cycled = true;
        _gun.UpdateBallisticAppearance(gunUid, ballistic);
        Dirty(gunUid, ballistic);
        return true;
    }
}
