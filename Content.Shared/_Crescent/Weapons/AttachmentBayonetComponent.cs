using Robust.Shared.GameStates;

namespace Content.Shared._Crescent.Weapons;

/// <summary>
///     Component to indicate a valid bayonet for weapon attachment
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AttachmentBayonetComponent : AttachmentComponent
{ }
