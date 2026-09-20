using System.Numerics;
using Robust.Shared.Prototypes;

namespace Content.Server._Crescent.SpaceEvents.Components;

[RegisterComponent, Access(typeof(AnomalyStationRule))]
public sealed partial class AnomalyStationRuleComponent : Component
{
    [DataField]
    public float MinCoord = -3000f;

    [DataField]
    public float MaxCoord = 3000f;

    [DataField(required: true)]
    public string StationMapPath;

    [DataField(required: true)]
    public LocId Announcement;

    [DataField("artifactSpawnerPrototype")]
    public EntProtoId ArtifactSpawnerPrototype = "RandomArtifactSpawner";

    public Vector2 Coordinates;
}