using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Server.Mind;
using Content.Shared._Crescent.Administration;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Eui;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Player;

namespace Content.Server._Crescent.Administration;

/// <summary>
///     Lets an admin pick any connected player and shove them into the body this EUI was opened on.
///     Mostly exists so that people who ghosted by accident can be put back without hunting down entity uids.
/// </summary>
[UsedImplicitly]
public sealed class PutPlayerInBodyEui : BaseEui
{
    [Dependency] private readonly IAdminManager _admins = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IPlayerManager _players = default!;

    /// <summary>
    ///     Flag required both to open this and to actually move a mind. Same one <c>setmind</c> uses.
    /// </summary>
    public const AdminFlags RequiredFlags = AdminFlags.Admin;

    private readonly NetEntity _target;

    public PutPlayerInBodyEui(NetEntity target)
    {
        _target = target;
        IoCManager.InjectDependencies(this);
    }

    public override void Opened()
    {
        base.Opened();
        _admins.OnPermsChanged += OnPermsChanged;
    }

    public override void Closed()
    {
        base.Closed();
        _admins.OnPermsChanged -= OnPermsChanged;
    }

    private void OnPermsChanged(AdminPermsChangedEventArgs args)
    {
        if (args.Player == Player && !_admins.HasAdminFlag(Player, RequiredFlags))
            Close();
    }

    public override EuiStateBase GetNewState()
    {
        var state = new PutPlayerInBodyEuiState
        {
            Target = _target,
            TargetName = Loc.GetString("put-player-in-body-unknown-body"),
        };

        if (!_entity.TryGetEntity(_target, out var uid))
            return state;

        state.TargetName = GetName(uid.Value);

        if (_entity.TryGetComponent<ActorComponent>(uid, out var actor))
            state.CurrentOccupant = actor.PlayerSession.Name;

        return state;
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is not PutPlayerInBodySelectMessage select)
            return;

        if (!_admins.HasAdminFlag(Player, RequiredFlags))
            return;

        if (!_entity.TryGetEntity(_target, out var uid) || _entity.Deleted(uid))
        {
            Fail("put-player-in-body-body-gone");
            return;
        }

        if (!_players.TryGetSessionById(select.Player, out var session))
        {
            Fail("put-player-in-body-player-gone");
            return;
        }

        // TransferTo throws if the body is held by a session that isn't the one we're moving in, and it can only
        // clean that up itself when the occupant has a mind. A bodiless actor has to be sorted out by hand.
        if (_entity.TryGetComponent<ActorComponent>(uid, out var actor)
            && actor.PlayerSession != session
            && !_entity.System<MindSystem>().TryGetMind(uid.Value, out _, out _))
        {
            Fail("put-player-in-body-occupied");
            return;
        }

        _entity.System<MindSystem>().ControlMob(select.Player, uid.Value);

        _adminLog.Add(LogType.Mind,
            LogImpact.High,
            $"{Player:actor} put {session:player} into {_entity.ToPrettyString(uid.Value):subject}");

        if (Player.AttachedEntity is { } adminEnt)
        {
            _entity.System<SharedPopupSystem>().PopupEntity(
                Loc.GetString("put-player-in-body-success",
                    ("player", session.Name),
                    ("body", GetName(uid.Value))),
                adminEnt,
                adminEnt);
        }

        Close();
    }

    private string GetName(EntityUid uid)
    {
        return _entity.TryGetComponent<MetaDataComponent>(uid, out var meta)
            ? meta.EntityName
            : Loc.GetString("put-player-in-body-unknown-body");
    }

    private void Fail(string locId)
    {
        if (Player.AttachedEntity is not { } adminEnt)
            return;

        _entity.System<SharedPopupSystem>().PopupEntity(Loc.GetString(locId), adminEnt, adminEnt);
    }
}
