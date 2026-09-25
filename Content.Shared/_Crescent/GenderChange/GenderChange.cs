using Robust.Shared.Enums;

namespace Content.Shared._Crescent.GenderChange
{
    public record struct GenderChangeEvent(EntityUid Uid, Gender Gender);
}
