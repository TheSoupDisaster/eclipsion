using System.Numerics;
using Content.Shared.Buckle.Components;
using Content.Shared.Maps;
using Content.Shared.Mobs.Systems;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._Crescent.WeskerDodge;

/// <summary>
/// Makes <see cref="WeskerDodgeComponent"/> owners untouchable by gunfire. Every way a round can reach them
/// (physics contact, the phase-prevention sweep, hitscan) is negated, and the owner blinks sideways out of
/// the line of fire. Incoming rounds are also predicted so the blink usually happens before they arrive.
/// </summary>
public sealed class WeskerDodgeSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    private readonly HashSet<Entity<ProjectileComponent>> _incoming = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WeskerDodgeComponent, PreventCollideEvent>(OnPreventCollide);
        SubscribeLocalEvent<WeskerDodgeComponent, ProjectileReflectAttemptEvent>(OnProjectileAttempt);
        SubscribeLocalEvent<WeskerDodgeComponent, HitScanReflectAttemptEvent>(OnHitscanAttempt);
    }

    private void OnPreventCollide(Entity<WeskerDodgeComponent> ent, ref PreventCollideEvent args)
    {
        if (!TryComp<ProjectileComponent>(args.OtherEntity, out var projectile) || !CanDodge(ent, projectile))
            return;

        args.Cancelled = true;
        // Physics is mid-step here, so the teleport itself waits for Update.
        QueueDodge(ent, args.OtherEntity, projectile);
    }

    private void OnProjectileAttempt(Entity<WeskerDodgeComponent> ent, ref ProjectileReflectAttemptEvent args)
    {
        if (args.Cancelled || !CanDodge(ent, args.Component))
            return;

        args.Cancelled = true;
        QueueDodge(ent, args.ProjUid, args.Component);
    }

    private void OnHitscanAttempt(Entity<WeskerDodgeComponent> ent, ref HitScanReflectAttemptEvent args)
    {
        if (args.Reflected || !IsActive(ent))
            return;

        // "Reflecting" along the same direction makes the beam carry on as if the owner was never there.
        args.Reflected = true;
        ent.Comp.PendingDirection ??= args.Direction;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var query = EntityQueryEnumerator<WeskerDodgeComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            if (!IsActive((uid, comp)))
            {
                comp.PendingDirection = null;
                continue;
            }

            comp.PendingDirection ??= FindIncomingShot((uid, comp), xform);

            if (comp.PendingDirection is not { } direction || _timing.CurTime < comp.NextDodge)
                continue;

            comp.PendingDirection = null;
            Dodge((uid, comp, xform), direction);
        }
    }

    private bool IsActive(Entity<WeskerDodgeComponent> ent)
    {
        return !_mobState.IsIncapacitated(ent);
    }

    private bool CanDodge(Entity<WeskerDodgeComponent> ent, ProjectileComponent projectile)
    {
        return projectile.Shooter != ent.Owner && IsActive(ent);
    }

    private void QueueDodge(Entity<WeskerDodgeComponent> ent, EntityUid projUid, ProjectileComponent projectile)
    {
        projectile.IgnoredEntities.Add(ent);

        if (ent.Comp.PendingDirection != null)
            return;

        var velocity = _physics.GetMapLinearVelocity(projUid);
        ent.Comp.PendingDirection = velocity.LengthSquared() > 0.01f
            ? velocity
            : _transform.GetWorldPosition(ent) - _transform.GetWorldPosition(projUid);
    }

    /// <summary>
    /// Looks for a live round whose path passes through the owner within the lookahead window.
    /// </summary>
    private Vector2? FindIncomingShot(Entity<WeskerDodgeComponent> ent, TransformComponent xform)
    {
        var mapPos = _transform.GetMapCoordinates(ent, xform);
        if (mapPos.MapId == MapId.Nullspace)
            return null;

        _incoming.Clear();
        _lookup.GetEntitiesInRange(mapPos, ent.Comp.ScanRange, _incoming, LookupFlags.Dynamic | LookupFlags.Sundries);

        foreach (var (projUid, projectile) in _incoming)
        {
            if (projectile.DamagedEntity || projectile.Shooter == ent.Owner || projectile.IgnoredEntities.Contains(ent))
                continue;

            if (projectile is { Weapon: null, OnlyCollideWhenShot: true })
                continue;

            var velocity = _physics.GetMapLinearVelocity(projUid);
            var speedSquared = velocity.LengthSquared();
            if (speedSquared < 1f)
                continue;

            var toOwner = mapPos.Position - _transform.GetWorldPosition(projUid);
            var time = Vector2.Dot(toOwner, velocity) / speedSquared;
            if (time < 0f || time > ent.Comp.LookaheadTime)
                continue;

            if ((toOwner - velocity * time).Length() > ent.Comp.HitRadius)
                continue;

            projectile.IgnoredEntities.Add(ent);
            return velocity;
        }

        return null;
    }

    private void Dodge(Entity<WeskerDodgeComponent, TransformComponent> ent, Vector2 shotDirection)
    {
        // Buckled or otherwise stuck - the shot is still negated, just no blink.
        if (TryComp<BuckleComponent>(ent, out var buckle) && buckle.Buckled)
            return;

        var mapPos = _transform.GetMapCoordinates(ent, ent.Comp2);
        if (mapPos.MapId == MapId.Nullspace)
            return;

        if (!TryFindDodgeSpot(ent, mapPos, shotDirection, out var target))
            return;

        ent.Comp1.NextDodge = _timing.CurTime + ent.Comp1.Cooldown;

        SpawnEffect(ent.Comp1, mapPos);
        _transform.SetMapCoordinates((ent.Owner, ent.Comp2), target);
        _physics.SetLinearVelocity(ent, Vector2.Zero);
        SpawnEffect(ent.Comp1, target);

        _audio.PlayPvs(ent.Comp1.DodgeSound, ent);
        _popup.PopupEntity(Loc.GetString("wesker-dodge-popup", ("user", ent.Owner)), ent, PopupType.Small);

        var ev = new WeskerDodgedEvent();
        RaiseLocalEvent(ent, ref ev);
    }

    private void SpawnEffect(WeskerDodgeComponent comp, MapCoordinates coords)
    {
        if (comp.DodgeEffect is { } effect)
            Spawn(effect, coords);
    }

    /// <summary>
    /// Steps perpendicular to the shot, left or right and nothing else, so the blink always reads
    /// as a sidestep rather than a jump across the room.
    /// </summary>
    private bool TryFindDodgeSpot(Entity<WeskerDodgeComponent, TransformComponent> ent,
        MapCoordinates origin,
        Vector2 shotDirection,
        out MapCoordinates target)
    {
        var comp = ent.Comp1;
        var shot = shotDirection.LengthSquared() > 0f ? Vector2.Normalize(shotDirection) : Vector2.UnitX;
        var side = new Vector2(-shot.Y, shot.X);
        if (_random.Prob(0.5f))
            side = -side;

        var onGrid = ent.Comp2.GridUid != null;

        // Straight out to one side and nowhere else. The longest step is tried first so he clears
        // the line of fire where there is room, and shuffles aside by a tile where there is not.
        for (var distance = comp.MaxDistance; distance >= comp.MinDistance; distance -= 1f)
        {
            if (IsValidSpot(ent, origin, side * distance, onGrid, out target))
                return true;

            if (IsValidSpot(ent, origin, -side * distance, onGrid, out target))
                return true;
        }

        target = default;
        return false;
    }

    private bool IsValidSpot(EntityUid uid, MapCoordinates origin, Vector2 offset, bool requireGrid, out MapCoordinates target)
    {
        target = new MapCoordinates(origin.Position + offset, origin.MapId);

        // No blinking through walls.
        var ray = new CollisionRay(origin.Position, Vector2.Normalize(offset), (int) CollisionGroup.Impassable);
        foreach (var _ in _physics.IntersectRay(origin.MapId, ray, offset.Length(), uid))
            return false;

        if (!_map.TryFindGridAt(origin.MapId, target.Position, out var gridUid, out var grid))
            return !requireGrid;

        if (!_map.TryGetTileRef(gridUid, grid, target.Position, out var tile) || tile.Tile.IsEmpty)
            return false;

        return !_turf.IsTileBlocked(tile, CollisionGroup.Impassable);
    }
}

/// <summary>
/// Raised on the owner after it blinked out of the way of a shot.
/// </summary>
[ByRefEvent]
public readonly record struct WeskerDodgedEvent;
