using Content.Shared._Crescent.Factions;
using Content.Shared.Access.Systems;
using Content.Shared.Inventory;

namespace Content.Client._Crescent.Diplomacy;

/// <summary>
///     Reads the faction credential another mob is wearing, for faction HUDs. Mirrors the server's
///     FactionIdCardSystem.TryGetWornFaction: only the ID slot counts, so a card merely held in hand does not.
/// </summary>
public sealed class FactionCredentialReaderSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedIdCardSystem _idCard = default!;

    public bool TryGetWornFaction(EntityUid uid, out string faction)
    {
        faction = string.Empty;

        if (!_inventory.TryGetSlotEntity(uid, "id", out var idItem) ||
            !_idCard.TryGetIdCard(idItem.Value, out var idCard) ||
            !TryComp<FactionIdCardComponent>(idCard, out var credential) ||
            string.IsNullOrWhiteSpace(credential.Faction))
        {
            return false;
        }

        faction = credential.Faction;
        return true;
    }
}
