using System.Numerics;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Crescent.WeskerDodge;

/// <summary>
/// Albert Wesker style bullet dodging. Projectiles and hitscan shots never touch the owner;
/// instead the owner blinks a few tiles sideways out of the line of fire.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class WeskerDodgeComponent : Component
{
    /// <summary>
    /// Shortest sideways teleport distance, in tiles. Used when the full step is blocked.
    /// </summary>
    [DataField]
    public float MinDistance = 1f;

    /// <summary>
    /// Longest sideways teleport distance, in tiles. He always steps straight out to one side,
    /// so this is the furthest he can ever end up from where he was standing.
    /// </summary>
    [DataField]
    public float MaxDistance = 2f;

    /// <summary>
    /// How far ahead in time an incoming round is predicted. A round that would reach the owner
    /// within this window triggers the dodge before it arrives.
    /// </summary>
    [DataField]
    public float LookaheadTime = 0.25f;

    /// <summary>
    /// Radius scanned for incoming rounds each tick.
    /// </summary>
    [DataField]
    public float ScanRange = 10f;

    /// <summary>
    /// How close a round's path has to pass to count as a hit.
    /// </summary>
    [DataField]
    public float HitRadius = 0.7f;

    /// <summary>
    /// Minimum time between two teleports. Shots are still negated during it,
    /// this only stops a shotgun spread from making the owner blink once per pellet.
    /// </summary>
    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(0.15);

    [DataField]
    public SoundSpecifier? DodgeSound = new SoundPathSpecifier("/Audio/Magic/blink.ogg",
        AudioParams.Default.WithVolume(-4f).WithVariation(0.1f));

    [DataField]
    public EntProtoId? DodgeEffect = "EffectWeskerDodge";

    /// <summary>
    /// Emote taunted after a dodge. Nothing is emoted if this is null.
    /// </summary>
    [DataField]
    public ProtoId<EmotePrototype>? DodgeEmote = "WeskerFingerWag";

    /// <summary>
    /// Chance for a dodge to be taunted, rolled once the cooldown below has passed.
    /// </summary>
    [DataField]
    public float EmoteChance = 0.35f;

    /// <summary>
    /// Minimum time between two taunts, so a firefight does not turn into an emote spam.
    /// </summary>
    [DataField]
    public TimeSpan EmoteCooldown = TimeSpan.FromSeconds(25);

    [ViewVariables]
    public TimeSpan NextEmote;

    [ViewVariables]
    public TimeSpan NextDodge;

    /// <summary>
    /// Direction of the shot that has to be dodged next tick, if any.
    /// </summary>
    [ViewVariables]
    public Vector2? PendingDirection;
}
