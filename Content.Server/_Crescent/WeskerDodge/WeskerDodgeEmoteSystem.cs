using Content.Server.Chat.Systems;
using Content.Shared._Crescent.WeskerDodge;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Crescent.WeskerDodge;

/// <summary>
/// Taunts after a dodge - the finger wag from the films. Rolled per dodge and rate limited,
/// so a long firefight gets the odd taunt rather than one per bullet.
/// </summary>
public sealed class WeskerDodgeEmoteSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WeskerDodgeComponent, WeskerDodgedEvent>(OnDodged);
    }

    private void OnDodged(Entity<WeskerDodgeComponent> ent, ref WeskerDodgedEvent args)
    {
        if (ent.Comp.DodgeEmote is not { } emote || _timing.CurTime < ent.Comp.NextEmote)
            return;

        if (!_random.Prob(ent.Comp.EmoteChance))
            return;

        ent.Comp.NextEmote = _timing.CurTime + ent.Comp.EmoteCooldown;
        _chat.TryEmoteWithChat(ent, emote, forceEmote: true);
    }
}
