namespace Content.Shared._Crescent.Chasm;

[ByRefEvent]
public record struct BeforeChasmFallingEvent(EntityUid Entity, bool Cancelled = false);
