using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Crescent.Shelter;

[Serializable, NetSerializable]
public sealed partial class ShelterCapsuleDeployDoAfterEvent : SimpleDoAfterEvent;
