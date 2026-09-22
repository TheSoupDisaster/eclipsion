using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Abilities.Psionics;
using Robust.Shared.Console;
using Robust.Shared.Player;
using Content.Server.Abilities.Psionics;
using Robust.Shared.Prototypes;
using Content.Shared.Psionics;

namespace Content.Server.Psionics;

[AdminCommand(AdminFlags.Logs)]
public sealed class ListPsionicsCommand : IConsoleCommand
{
    public string Command => "lspsionics";
    public string Description => Loc.GetString("command-lspsionic-description");
    public string Help => Loc.GetString("command-lspsionic-help");
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        var query = entMan.EntityQueryEnumerator<ActorComponent, PsionicComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out var actor, out var psionic, out var meta))
        {
            var powers = string.Join(", ", psionic.ActivePowers.Select(power => power.ID));

            // Print the net id: that is what every other psionic command parses.
            shell.WriteLine($"{meta.EntityName} ({entMan.GetNetEntity(uid)}) - {actor.PlayerSession.Name} - " +
                            $"level {psionic.PsionicLevel}, {psionic.SkillPoints} point(s) - [{powers}]");
        }
    }
}

[AdminCommand(AdminFlags.Fun)]
public sealed class AddPsionicPowerCommand : IConsoleCommand
{
    public string Command => "addpsionicpower";
    public string Description => Loc.GetString("command-addpsionicpower-description");
    public string Help => Loc.GetString("command-addpsionicpower-help");
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        var psionicPowers = entMan.System<PsionicAbilitiesSystem>();
        var protoMan = IoCManager.Resolve<IPrototypeManager>();

        if (args.Length != 2)
        {
            shell.WriteError(Help);
            return;
        }

        if (!PsionicCommandHelper.TryResolveEntity(shell, args[0], entMan, out var uid))
            return;

        if (!protoMan.TryIndex<PsionicPowerPrototype>(args[1], out var powerProto))
        {
            shell.WriteError(Loc.GetString("addpsionicpower-args-two-error"));
            return;
        }

        entMan.EnsureComponent<PsionicComponent>(uid.Value, out var psionic);
        psionicPowers.InitializePsionicPower(uid.Value, powerProto, psionic);
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => PsionicCommandHelper.TargetCompletion(),
            2 => CompletionResult.FromHintOptions(
                CompletionHelper.PrototypeIDs<PsionicPowerPrototype>(),
                "<power>"),
            _ => CompletionResult.Empty,
        };
    }
}

[AdminCommand(AdminFlags.Fun)]
public sealed class AddRandomPsionicPowerCommand : IConsoleCommand
{
    public string Command => "addrandompsionicpower";
    public string Description => Loc.GetString("command-addrandompsionicpower-description");
    public string Help => Loc.GetString("command-addrandompsionicpower-help");
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        var psionicPowers = entMan.System<PsionicAbilitiesSystem>();

        if (args.Length != 1)
        {
            shell.WriteError(Help);
            return;
        }

        if (!PsionicCommandHelper.TryResolveEntity(shell, args[0], entMan, out var uid))
            return;

        psionicPowers.AddRandomPsionicPower(uid.Value, true);
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length == 1 ? PsionicCommandHelper.TargetCompletion() : CompletionResult.Empty;
    }
}

[AdminCommand(AdminFlags.Fun)]
public sealed class RemovePsionicPowerCommand : IConsoleCommand
{
    public string Command => "removepsionicpower";
    public string Description => Loc.GetString("command-removepsionicpower-description");
    public string Help => Loc.GetString("command-removepsionicpower-help");
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        var psionicPowers = entMan.System<PsionicAbilitiesSystem>();
        var protoMan = IoCManager.Resolve<IPrototypeManager>();

        if (args.Length != 2)
        {
            shell.WriteError(Help);
            return;
        }

        if (!PsionicCommandHelper.TryResolveEntity(shell, args[0], entMan, out var uid))
            return;

        if (!protoMan.TryIndex<PsionicPowerPrototype>(args[1], out var powerProto))
        {
            shell.WriteError(Loc.GetString("removepsionicpower-args-two-error"));
            return;
        }

        if (!entMan.TryGetComponent<PsionicComponent>(uid, out var psionicComponent))
        {
            shell.WriteError(Loc.GetString("removepsionicpower-not-psionic-error"));
            return;
        }

        if (!psionicComponent.ActivePowers.Contains(powerProto))
        {
            shell.WriteError(Loc.GetString("removepsionicpower-not-contains-error"));
            return;
        }

        psionicPowers.RemovePsionicPower(uid.Value, psionicComponent, powerProto, true);
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
            return PsionicCommandHelper.TargetCompletion();

        if (args.Length != 2)
            return CompletionResult.Empty;

        // Offer only what the target actually has, when the target is already known.
        var entMan = IoCManager.Resolve<IEntityManager>();
        var playerMan = IoCManager.Resolve<ISharedPlayerManager>();
        EntityUid? uid = null;
        if (playerMan.TryGetSessionByUsername(args[0], out var session))
            uid = session.AttachedEntity;
        else if (NetEntity.TryParse(args[0], out var netEntity))
            entMan.TryGetEntity(netEntity, out uid);

        if (entMan.TryGetComponent<PsionicComponent>(uid, out var psionic))
        {
            return CompletionResult.FromHintOptions(
                psionic.ActivePowers.Select(power => new CompletionOption(power.ID, power.Name)),
                "<power>");
        }

        return CompletionResult.FromHintOptions(CompletionHelper.PrototypeIDs<PsionicPowerPrototype>(), "<power>");
    }
}

[AdminCommand(AdminFlags.Fun)]
public sealed class RemoveAllPsionicPowersCommand : IConsoleCommand
{
    public string Command => "removeallpsionicpowers";
    public string Description => Loc.GetString("command-removeallpsionicpowers-description");
    public string Help => Loc.GetString("command-removeallpsionicpowers-help");
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        var psionicPowers = entMan.System<PsionicAbilitiesSystem>();

        if (args.Length != 1)
        {
            shell.WriteError(Help);
            return;
        }

        if (!PsionicCommandHelper.TryResolveEntity(shell, args[0], entMan, out var uid))
            return;

        if (!entMan.HasComponent<PsionicComponent>(uid))
        {
            shell.WriteError(Loc.GetString("removeallpsionicpowers-not-psionic-error"));
            return;
        }

        psionicPowers.RemoveAllPsionicPowers(uid.Value);
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length == 1 ? PsionicCommandHelper.TargetCompletion() : CompletionResult.Empty;
    }
}

/// <summary>
/// Makes a target psionic outright, with no powers: the same starting point a Latent Psychic
/// rolls a character with. Level one, one point, and the general tree to spend it in.
/// </summary>
[AdminCommand(AdminFlags.Fun)]
public sealed class MakePsionicCommand : IConsoleCommand
{
    public string Command => "makepsionic";
    public string Description => Loc.GetString("command-make-psionic-description");
    public string Help => Loc.GetString("command-make-psionic-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length > 1)
        {
            shell.WriteError(Help);
            return;
        }

        var entMan = IoCManager.Resolve<IEntityManager>();
        EntityUid uid;

        if (args.Length == 0)
        {
            if (shell.Player?.AttachedEntity is not { } self)
            {
                shell.WriteError(Loc.GetString("command-psionic-no-self"));
                return;
            }

            uid = self;
        }
        else if (!PsionicCommandHelper.TryResolveEntity(shell, args[0], entMan, out var resolved))
        {
            return;
        }
        else
        {
            uid = resolved.Value;
        }

        if (entMan.TryGetComponent<PsionicComponent>(uid, out var existing))
        {
            shell.WriteError(Loc.GetString(
                "command-make-psionic-already",
                ("target", entMan.ToPrettyString(uid)),
                ("level", existing.PsionicLevel),
                ("points", existing.SkillPoints)));
            return;
        }

        // Mindbreaking is what keeps a psion from ever progressing again, so a mindbroken target
        // would otherwise get the component and none of the point of having it.
        var demindbroken = entMan.RemoveComponent<MindbrokenComponent>(uid);

        var psionic = entMan.AddComponent<PsionicComponent>(uid);

        shell.WriteLine(Loc.GetString(
            demindbroken ? "command-make-psionic-granted-demindbroken" : "command-make-psionic-granted",
            ("target", entMan.ToPrettyString(uid)),
            ("level", psionic.PsionicLevel),
            ("points", psionic.SkillPoints)));
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length == 1 ? PsionicCommandHelper.TargetCompletion() : CompletionResult.Empty;
    }
}
