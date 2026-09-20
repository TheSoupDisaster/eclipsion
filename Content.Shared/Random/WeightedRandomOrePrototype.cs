using Content.Shared.Mining;
using System.Linq;
using Robust.Shared.Prototypes;

namespace Content.Shared.Random;

/// <summary>
/// Linter-friendly version of weightedRandom for Ore prototypes.
/// </summary>
[Prototype("weightedRandomOre")]
public sealed partial class WeightedRandomOrePrototype : IWeightedRandomPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("weights")]
    public Dictionary<ProtoId<OrePrototype>, float> Weights { get; private set; } = new();

    private Dictionary<string, float>? _untypedWeights;

    /// <remarks>
    ///     Projects the validated, prototype-typed weights onto the untyped dictionary
    ///     <see cref="IWeightedRandomPrototype"/> exposes. Prototype ids are strings at
    ///     runtime, so the projection is purely a type change. It is built once per instance
    ///     rather than per access; reloading a prototype builds a new instance, so the cached
    ///     projection cannot go stale.
    /// </remarks>
    Dictionary<string, float> IWeightedRandomPrototype.Weights =>
        _untypedWeights ??= Weights.ToDictionary(static pair => pair.Key.Id, static pair => pair.Value);
}
