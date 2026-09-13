using System.Numerics;
using Content.Server.Explosion.EntitySystems;
using Content.Shared._Crescent.Weapons.FieldArtillery;
using Content.Shared.Projectiles;

namespace Content.Server._Crescent.Weapons.FieldArtillery;

public sealed class FieldArtilleryShellSystem : EntitySystem
{
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly List<(EntityUid Shell, Vector2 Target)> _detonating = new();

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<FieldArtilleryShellComponent, ProjectileComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var shell, out var projectile, out var xform))
        {
            var position = _transform.GetWorldPosition(xform);

            if (!shell.Armed)
            {
                if (!TryComp<FieldArtilleryComponent>(projectile.Weapon, out var gun) ||
                    gun.PendingTarget is not { } target ||
                    target.MapId != xform.MapID)
                {
                    continue;
                }

                gun.PendingTarget = null;
                shell.Armed = true;
                shell.Origin = position;
                shell.Target = target.Position;
                shell.Distance = Vector2.Distance(position, target.Position);
            }

            if (Vector2.DistanceSquared(position, shell.Origin) < shell.Distance * shell.Distance)
                continue;

            _detonating.Add((uid, shell.Target));
        }

        foreach (var (uid, target) in _detonating)
        {
            _transform.SetWorldPosition(uid, target);
            _explosion.TriggerExplosive(uid);
        }

        _detonating.Clear();
    }
}
