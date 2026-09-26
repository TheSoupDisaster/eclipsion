using Content.Shared._Crescent.Diplomacy;

namespace Content.Client._Crescent.Diplomacy;

public sealed class FactionStatusSystem : EntitySystem
{
    public event Action<AllFactionRelationsUpdatedEvent>? RelationsUpdated;
    public event Action<PlayerFactionUpdatedEvent>? PlayerFactionUpdated;

    /// <summary>The last relations board the server sent, kept for anything that asks between updates.</summary>
    private Dictionary<string, Dictionary<string, FactionRelation>> _relations = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<AllFactionRelationsUpdatedEvent>(OnRelationsUpdated);
        SubscribeNetworkEvent<PlayerFactionUpdatedEvent>(OnPlayerFactionUpdated);
    }

    /// <summary>
    ///     The relation between two factions as last reported by the server. A faction is always allied with itself;
    ///     anything the server has not told us about reads as neutral.
    /// </summary>
    public FactionRelation GetRelation(string faction, string other)
    {
        if (faction == other)
            return FactionRelation.Alliance;

        return _relations.TryGetValue(faction, out var relations)
            ? relations.GetValueOrDefault(other, FactionRelation.Neutral)
            : FactionRelation.Neutral;
    }

    private void OnRelationsUpdated(AllFactionRelationsUpdatedEvent ev)
    {
        _relations = ev.AllRelations;
        RelationsUpdated?.Invoke(ev);
    }

    private void OnPlayerFactionUpdated(PlayerFactionUpdatedEvent ev)
    {
        PlayerFactionUpdated?.Invoke(ev);
    }
}
