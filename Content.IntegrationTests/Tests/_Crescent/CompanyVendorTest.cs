using System.Linq;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server._Crescent.Diplomacy;
using Content.Shared._Crescent.CompanyVendors;
using Content.Shared._Crescent.Diplomacy;
using Content.Shared._Crescent.Factions;
using Content.Shared._Crescent.HullrotFaction;
using Content.Shared.Shuttles.Components;
using Content.Shared.VendingMachines;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Crescent;

/// <summary>
/// Drives the franchise vendors the way a player does: hold a stock module, click the machine, wait out the do-after.
/// </summary>
public sealed class CompanyVendorTest : InteractionTest
{
    private const string ShiCoffee = "VendingMachineCoffeeShinohara";
    private const string TfcfCoffee = "VendingMachineCoffeeTFCF";

    [Test]
    public async Task RestockReplacesStockAtModuleLevel()
    {
        await SetPlayerFaction("SHI");
        await SpawnTarget(ShiCoffee);

        // A fresh machine starts on a level 1 module, not the whole vending pack.
        AssertVendor(ShiCoffee, level: 1, stock: 20);

        await InteractUsing("CompanyVendorRestockShinoharaCoffee2");

        AssertVendor(ShiCoffee, level: 2, stock: 50);
        Assert.That(Hands.ActiveHandEntity, Is.Null, "The stock module should be used up.");

        await InteractUsing("CompanyVendorRestockShinoharaCoffee3");
        AssertVendor(ShiCoffee, level: 3, stock: 100);
    }

    [Test]
    public async Task SoldOutMachineIsRefilled()
    {
        await SetPlayerFaction("SHI");
        await SpawnTarget(ShiCoffee);

        await Server.WaitPost(() =>
        {
            foreach (var entry in SEntMan.GetComponent<VendingMachineComponent>(STarget!.Value).Inventory.Values)
                entry.Amount = 0;
        });
        AssertVendor(ShiCoffee, level: 1, stock: 0);

        await InteractUsing("CompanyVendorRestockShinoharaCoffee1");
        AssertVendor(ShiCoffee, level: 1, stock: 20);
    }

    [Test]
    public async Task RivalModuleConvertsMachineWhileRestocking()
    {
        await SetPlayerFaction("TFSC");
        await SpawnTarget(ShiCoffee);

        Assert.That(MachineFaction(ShiCoffee), Is.EqualTo("SHI"));
        await Server.WaitPost(() => SEntMan.GetComponent<VendingMachineComponent>(STarget!.Value).WirePulseEjectCount = 2);

        // Test grid has no faction, so it is open market and needs no agreement.
        await InteractUsing("CompanyVendorRestockTFCFCoffee2");

        AssertVendor(TfcfCoffee, level: 2, stock: 50);
        Assert.That(CountVendors(ShiCoffee), Is.Zero, "The Shinohara machine should have been replaced.");
        Assert.That(Hands.ActiveHandEntity, Is.Null, "The stock module should be used up.");
        Assert.That(MachineFaction(TfcfCoffee), Is.EqualTo("TFSC"), "The machine should now belong to the stock's company.");
        Assert.That(LiveVendor(TfcfCoffee).Comp2.WirePulseEjectCount, Is.EqualTo(2),
            "A takeover must not reset the eject-wire pulse limit.");
    }

    [Test]
    public async Task NonMemberCannotInstallModule()
    {
        await SetPlayerFaction("DSM");
        await SpawnTarget(ShiCoffee);

        await InteractUsing("CompanyVendorRestockShinoharaCoffee3");

        AssertVendor(ShiCoffee, level: 1, stock: 20);
        Assert.That(Hands.ActiveHandEntity, Is.Not.Null, "A rejected module must stay in the player's hand.");
    }

    [Test]
    public async Task WrongProductLineIsRejected()
    {
        await SetPlayerFaction("SHI");
        await SpawnTarget(ShiCoffee);

        await InteractUsing("CompanyVendorRestockShinoharaCanteen3");

        AssertVendor(ShiCoffee, level: 1, stock: 20);
        Assert.That(Hands.ActiveHandEntity, Is.Not.Null);
    }

    [Test]
    public async Task TakeoverNeedsTradeAgreementWithHost()
    {
        var diplomacy = Server.System<RatDiplomacySystem>();
        var previous = FactionRelation.Neutral;

        await Server.WaitPost(() =>
        {
            var iff = SEntMan.EnsureComponent<IFFComponent>(MapData.Grid);
#pragma warning disable RA0002
            iff.Faction = "DSM";
#pragma warning restore RA0002
            previous = diplomacy.GetRelation("DSM", "TFSC");
            diplomacy.SetRelation("DSM", "TFSC", FactionRelation.Neutral, persist: false);
        });

        try
        {
            await SetPlayerFaction("TFSC");
            await SpawnTarget(ShiCoffee);

            await InteractUsing("CompanyVendorRestockTFCFCoffee1");
            AssertVendor(ShiCoffee, level: 1, stock: 20);
            Assert.That(Hands.ActiveHandEntity, Is.Not.Null, "Without a deal the module must be refused.");

            await Server.WaitPost(() => diplomacy.SetRelation("DSM", "TFSC", FactionRelation.Trade, persist: false));

            // Same module, still in hand, now goes in.
            await Interact();
            AssertVendor(TfcfCoffee, level: 1, stock: 20);
            Assert.That(Hands.ActiveHandEntity, Is.Null);
        }
        finally
        {
            await Server.WaitPost(() => diplomacy.SetRelation("DSM", "TFSC", previous, persist: false));
        }
    }

    private async Task SetPlayerFaction(string faction)
    {
        await Server.WaitPost(() => SEntMan.EnsureComponent<HullrotFactionComponent>(SPlayer).Faction = faction);
    }

    private Entity<CompanyVendorComponent, VendingMachineComponent> LiveVendor(string prototype)
    {
        var machine = SEntMan.AllEntities<CompanyVendorComponent>()
            .Single(e => !SEntMan.IsQueuedForDeletion(e.Owner) && !SEntMan.Deleted(e.Owner)
                && SEntMan.GetComponent<MetaDataComponent>(e).EntityPrototype?.ID == prototype);
        return (machine.Owner, machine.Comp, SEntMan.GetComponent<VendingMachineComponent>(machine));
    }

    private string MachineFaction(string prototype)
    {
        return Server.System<FactionMachineSystem>().GetFaction(LiveVendor(prototype));
    }

    private int CountVendors(string prototype)
    {
        return SEntMan.AllEntities<CompanyVendorComponent>()
            .Count(e => SEntMan.GetComponent<MetaDataComponent>(e).EntityPrototype?.ID == prototype);
    }

    /// <summary>
    /// Asserts there is exactly one live machine of <paramref name="prototype"/> with the given level and stock.
    /// Looked up by prototype rather than <see cref="InteractionTest.Target"/>, since a takeover replaces the entity.
    /// </summary>
    private void AssertVendor(string prototype, int level, int stock)
    {
        var machines = SEntMan.AllEntities<CompanyVendorComponent>()
            .Where(e => !SEntMan.IsQueuedForDeletion(e.Owner) && !SEntMan.Deleted(e.Owner))
            .Where(e => SEntMan.GetComponent<MetaDataComponent>(e).EntityPrototype?.ID == prototype)
            .ToList();

        Assert.That(machines, Has.Count.EqualTo(1), $"Expected one {prototype}.");
        var machine = machines[0];
        var inventory = SEntMan.GetComponent<VendingMachineComponent>(machine).Inventory.Values;

        Assert.Multiple(() =>
        {
            Assert.That(machine.Comp.Level, Is.EqualTo(level), "Stock module level");
            Assert.That(inventory.Sum(e => (int) e.Amount), Is.EqualTo(stock), "Items in stock");
        });
    }
}
