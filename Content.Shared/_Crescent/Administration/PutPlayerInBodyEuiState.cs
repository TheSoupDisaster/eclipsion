using Content.Shared.Eui;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._Crescent.Administration;

/// <summary>
///     State for the admin window that drops an arbitrary player into an arbitrary body.
/// </summary>
[Serializable, NetSerializable]
public sealed class PutPlayerInBodyEuiState : EuiStateBase
{
    public NetEntity Target;

    /// <summary>
    ///     Name of the body the mind will be moved into, for display only.
    /// </summary>
    public string TargetName = string.Empty;

    /// <summary>
    ///     Username of whoever is currently controlling the body, if anyone.
    /// </summary>
    public string? CurrentOccupant;
}

/// <summary>
///     Sent when the admin picks the player that should take over the body.
/// </summary>
[Serializable, NetSerializable]
public sealed class PutPlayerInBodySelectMessage : EuiMessageBase
{
    public NetUserId Player;

    public PutPlayerInBodySelectMessage(NetUserId player)
    {
        Player = player;
    }
}
