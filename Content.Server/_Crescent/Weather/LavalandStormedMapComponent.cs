using Content.Shared._Crescent.Weather;
using Robust.Shared.Prototypes;

namespace Content.Server._Crescent.Weather;

[RegisterComponent]
public sealed partial class LavalandStormedMapComponent : Component
{
    [DataField]
    public float Accumulator;

    [DataField]
    public ProtoId<LavalandWeatherPrototype> CurrentWeather;

    [DataField]
    public float Duration;

    [DataField]
    public float NextDamage = 10f;

    [DataField]
    public float DamageAccumulator;
}
