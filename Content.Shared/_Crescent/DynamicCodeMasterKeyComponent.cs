using Robust.Shared.GameStates;

namespace Content.Shared._Crescent;

/// <summary>
///     Crescent - an access item carrying this opens every dynamic-coded reader (player ship doors) and
///     unlocks any ship's shuttle console as captain, without the ship's keys being registered on it.
///     Handed out and taken back by the admin "Grant/Revoke All Access" tricks.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DynamicCodeMasterKeyComponent : Component
{
}
