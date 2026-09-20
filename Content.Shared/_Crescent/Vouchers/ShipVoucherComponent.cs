using Content.Shared.Shipyard.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Crescent.Vouchers;

[RegisterComponent, NetworkedComponent]
public sealed partial class ShipVoucherComponent : Component
{
    [DataField("ship")]
    public ProtoId<VesselPrototype> Ship;

    [DataField("requiresShipInConsole")]
    public bool RequiresShipInConsole;
}
