using System.Linq;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.Map;

namespace Content.Shared._Crescent.Weapons.FieldArtillery;

public sealed class FieldArtillerySystem : EntitySystem
{
    [Dependency] private readonly SharedBuckleSystem _buckle = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FieldArtilleryComponent, AttemptShootEvent>(OnAttemptShoot);
        SubscribeLocalEvent<FieldArtilleryComponent, AnchorStateChangedEvent>(OnAnchorStateChanged);
        SubscribeLocalEvent<FieldArtilleryComponent, StrapAttemptEvent>(OnStrapAttempt);
        SubscribeLocalEvent<FieldArtilleryComponent, InteractUsingEvent>(OnInteractUsing,
            before: new[] { typeof(SharedGunSystem) });
    }

    private void OnAttemptShoot(Entity<FieldArtilleryComponent> ent, ref AttemptShootEvent args)
    {
        ent.Comp.PendingTarget = null;

        if (args.Cancelled)
            return;

        var xform = Transform(ent);
        if (!xform.Anchored)
        {
            Refuse(ref args, "field-artillery-not-deployed");
            return;
        }

        if (!TryComp<BuckleComponent>(args.User, out var buckle) || buckle.BuckledTo != ent.Owner)
        {
            Refuse(ref args, "field-artillery-no-gunner");
            return;
        }

        if (args.ToCoordinates is not { } toCoordinates)
        {
            args.Cancelled = true;
            return;
        }

        var target = _transform.ToMapCoordinates(toCoordinates);
        if (target.MapId != xform.MapID)
        {
            args.Cancelled = true;
            return;
        }

        var (gunPosition, gunRotation) = _transform.GetWorldPositionRotation(ent);
        var delta = target.Position - gunPosition;
        var distance = delta.Length();

        if (distance < ent.Comp.MinRange)
        {
            Refuse(ref args, "field-artillery-too-close");
            return;
        }

        var offAxis = Angle.ShortestDistance(gunRotation.ToWorldVec().ToAngle(), delta.ToAngle());
        if (Math.Abs(offAxis.Degrees) > ent.Comp.FiringArc / 2f)
        {
            Refuse(ref args, "field-artillery-out-of-arc");
            return;
        }

        if (distance > ent.Comp.MaxRange)
            delta *= ent.Comp.MaxRange / distance;

        ent.Comp.PendingTarget = new MapCoordinates(gunPosition + delta, xform.MapID);
    }

    private void Refuse(ref AttemptShootEvent args, string message)
    {
        args.Cancelled = true;
        args.ResetCooldown = true;
        args.Message = Loc.GetString(message);
    }

    private void OnAnchorStateChanged(Entity<FieldArtilleryComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (args.Anchored || TerminatingOrDeleted(ent) || !TryComp<StrapComponent>(ent, out var strap))
            return;

        foreach (var buckled in strap.BuckledEntities.ToArray())
        {
            _buckle.Unbuckle(buckled, null);
        }
    }

    private void OnStrapAttempt(Entity<FieldArtilleryComponent> ent, ref StrapAttemptEvent args)
    {
        if (Transform(ent).Anchored)
            return;

        args.Cancelled = true;

        if (args.Popup)
            _popup.PopupClient(Loc.GetString("field-artillery-not-deployed"), ent, args.User);
    }

    private void OnInteractUsing(EntityUid uid, FieldArtilleryComponent comp, InteractUsingEvent args)
    {
        if (args.Handled ||
            !TryComp<BuckleComponent>(args.User, out var buckle) ||
            buckle.BuckledTo != uid ||
            !TryComp<BallisticAmmoProviderComponent>(uid, out var ammo) ||
            _whitelist.IsWhitelistFailOrNull(ammo.Whitelist, args.Used))
        {
            return;
        }

        args.Handled = true;
        _popup.PopupClient(Loc.GetString("field-artillery-gunner-cannot-load"), uid, args.User);
    }
}
