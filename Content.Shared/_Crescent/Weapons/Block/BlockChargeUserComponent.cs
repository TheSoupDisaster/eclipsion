using Robust.Shared.GameStates;

namespace Content.Shared._Crescent.Weapons.Block;

[RegisterComponent, NetworkedComponent]
public sealed partial class BlockChargeUserComponent : Component
{
    [ViewVariables]
    public EntityUid BlockingWeapon;
}
