namespace Content.Server._Crescent.NPC;

/// <summary>
/// Crescent: switches the mob's internals on once its loadout has been equipped.
/// </summary>
/// <remarks>
/// Boarding NPCs are spawned in hardsuits with a tank and a breath mask, but nothing in the HTN ever
/// presses the internals button, so they would suffocate the moment they stepped into vacuum - which is
/// most of the places anyone wants to use them.
/// </remarks>
[RegisterComponent]
public sealed partial class NpcStartInternalsComponent : Component;
