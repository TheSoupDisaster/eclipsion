using Content.Server._NF.Contraband.Systems;
using Content.Shared.Stacks;
using Robust.Shared.Prototypes;

namespace Content.Server._NF.Contraband.Components;

[RegisterComponent]
[Access(typeof(ContrabandSystem))]
public sealed partial class ContrabandPalletConsoleComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("cashType")]
    public ProtoId<StackPrototype> RewardType = "FrontierUplinkCoin";
}
