using Robust.Shared.Prototypes;

namespace Content.Shared._Crescent.Audio;

[RegisterComponent, AutoGenerateComponentState]
public sealed partial class BossMusicComponent : Component
{
    [AutoNetworkedField]
    [DataField] public ProtoId<BossMusicPrototype> SoundId;
}
