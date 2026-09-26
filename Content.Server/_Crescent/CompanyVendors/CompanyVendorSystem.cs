using System.Linq;
using System.Text.Json;
using Content.Server._Crescent.Diplomacy;
using Content.Server.DoAfter;
using Content.Server.GameTicking;
using Content.Server.Popups;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Server.VendingMachines;
using Content.Shared._Crescent.CompanyVendors;
using Content.Shared._Crescent.Diplomacy;
using Content.Shared._Crescent.Factions;
using Content.Shared._Crescent.HullrotFaction;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Shuttles.Components;
using Content.Shared.VendingMachines;
using Robust.Shared.ContentPack;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._Crescent.CompanyVendors;

/// <summary>
/// Runs the Shinohara/TFCF franchise vendors: level-capped stock that only a company restock module refills,
/// takeovers of a rival company's machine under a trade agreement, and round-to-round persistence of all of it.
/// </summary>
/// <remarks>
/// Mapped machines are keyed by their station and tile, so persistence needs no per-machine map data. The key can
/// only be resolved once the station exists, which is after the map's MapInit, so machines queue up and are
/// matched against the save a tick later — before anyone can have reached them.
/// </remarks>
public sealed class CompanyVendorSystem : EntitySystem
{
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly FactionMachineSystem _factionMachines = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IResourceManager _resources = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly RatDiplomacySystem _diplomacy = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly VendingMachineSystem _vending = default!;

    private static readonly ResPath SavePath = new("/company_vendors.json");
    private const int SaveVersion = 1;

    /// <summary>How long a machine waits for its station to appear before it is given up on as non-persistent.</summary>
    private static readonly TimeSpan ResolveTimeout = TimeSpan.FromSeconds(30);

    /// <summary>Stock only changes on a sale, so a periodic diff is enough; round end takes a final snapshot.</summary>
    private static readonly TimeSpan SnapshotInterval = TimeSpan.FromSeconds(10);

    private readonly Dictionary<string, CompanyVendorRecord> _records = new();
    private readonly Dictionary<string, EntityUid> _live = new();
    private readonly Dictionary<EntityUid, TimeSpan> _pending = new();
    private readonly Dictionary<(string Company, string Product), EntProtoId> _machineFor = new();
    private TimeSpan _nextSnapshot;

    public override void Initialize()
    {
        base.Initialize();

        Load();
        BuildMachineTable();

        SubscribeLocalEvent<CompanyVendorComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<CompanyVendorComponent, ExaminedEvent>(OnVendorExamined);
        SubscribeLocalEvent<CompanyVendorComponent, CompanyVendorRestockDoAfterEvent>(OnRestockDoAfter);
        SubscribeLocalEvent<CompanyVendorRestockComponent, AfterInteractEvent>(OnRestockAfterInteract);
        SubscribeLocalEvent<CompanyVendorRestockComponent, ExaminedEvent>(OnRestockExamined);
        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRunLevelChanged);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_pending.Count > 0)
            ResolvePending();

        if (_timing.CurTime < _nextSnapshot)
            return;

        _nextSnapshot = _timing.CurTime + SnapshotInterval;
        if (SnapshotAll())
            Save();
    }

    #region Machine lifecycle

    private void OnMapInit(Entity<CompanyVendorComponent> ent, ref MapInitEvent args)
    {
        // The vending machine filled itself from its whole pack on init; a franchise machine only ever holds what
        // its stock module allows.
        Fill(ent);
        _pending[ent] = _timing.CurTime + ResolveTimeout;

        // The machine belongs to whichever company's stock is in it. A takeover spawns the rival's prototype, so
        // this is also what flips a converted machine between SHI and TFSC.
        _factionMachines.SetFaction(ent, ent.Comp.Company);
    }

    private void ResolvePending()
    {
        foreach (var (uid, deadline) in _pending.ToArray())
        {
            if (TerminatingOrDeleted(uid) || !TryComp<CompanyVendorComponent>(uid, out var comp))
            {
                _pending.Remove(uid);
                continue;
            }

            var key = ResolveKey(uid, comp);
            if (key == null)
            {
                if (_timing.CurTime < deadline)
                    continue;

                // Built on a ship or somewhere else with no stable identity: works normally, just isn't saved.
                _pending.Remove(uid);
                continue;
            }

            _pending.Remove(uid);

            if (_live.TryGetValue(key, out var other) && other != uid && Exists(other))
            {
                Log.Warning($"Company vendor {ToPrettyString(uid)} resolved to key '{key}' already used by {ToPrettyString(other)}; it will not be persisted.");
                continue;
            }

            if (_records.TryGetValue(key, out var record))
                ApplyRecord((uid, comp), key, record);
            else
                Track((uid, comp), key);
        }
    }

    private string? ResolveKey(EntityUid uid, CompanyVendorComponent comp)
    {
        if (!string.IsNullOrWhiteSpace(comp.PersistId))
            return comp.PersistId.Trim();

        var xform = Transform(uid);
        if (xform.GridUid is not { } grid || xform.ParentUid != grid)
            return null;

        // A map-defined station ID survives renames and random station names; the station's name is the fallback
        // for maps that are made into stations by their game rule instead.
        string? station = null;
        if (TryComp<BecomesStationComponent>(grid, out var becomes) && !string.IsNullOrWhiteSpace(becomes.Id))
            station = becomes.Id;
        else if (_station.GetOwningStation(uid, xform) is { } owning)
            station = Name(owning);

        if (station == null)
            return null;

        var pos = xform.LocalPosition;
        return $"{station}@{(int) MathF.Floor(pos.X)},{(int) MathF.Floor(pos.Y)}";
    }

    private void Track(Entity<CompanyVendorComponent> ent, string key)
    {
        ent.Comp.ResolvedKey = key;
        _live[key] = ent;
    }

    private void ApplyRecord(Entity<CompanyVendorComponent> ent, string key, CompanyVendorRecord record)
    {
        // Another company took this machine over in an earlier round, so the map's prototype is out of date.
        if (record.Prototype != MetaData(ent).EntityPrototype?.ID
            && _prototypes.TryIndex<EntityPrototype>(record.Prototype, out var proto)
            && proto.TryGetComponent<CompanyVendorComponent>(out _, EntityManager.ComponentFactory))
        {
            var replaced = ReplaceMachine(ent, record.Prototype);
            ent = (replaced, Comp<CompanyVendorComponent>(replaced));
        }

        ent.Comp.Level = Math.Clamp(record.Level, 1, Math.Max(1, ent.Comp.Capacities.Count));
        SetStock(ent, record.Stock);
        Track(ent, key);
    }

    private EntityUid ReplaceMachine(EntityUid uid, string prototype)
    {
        var xform = Transform(uid);
        var rotation = xform.LocalRotation;
        var anchored = xform.Anchored;

        var replacement = SpawnAtPosition(prototype, xform.Coordinates);
        var newXform = Transform(replacement);
        _transform.SetLocalRotation(replacement, rotation, newXform);
        if (anchored && !newXform.Anchored)
            _transform.AnchorEntity((replacement, newXform));

        // Anti-exploit: the eject-wire pulse limit lives on the entity, so a fresh replacement must not reset it.
        if (TryComp<VendingMachineComponent>(uid, out var oldVend) && TryComp<VendingMachineComponent>(replacement, out var newVend))
            newVend.WirePulseEjectCount = oldVend.WirePulseEjectCount;

        // The replacement is placed deliberately; it must not go through key resolution a second time.
        _pending.Remove(replacement);
        if (_pending.Remove(uid, out var deadline))
            _pending[replacement] = deadline;

        // Queued rather than immediate because this can run inside an event raised on the old machine. Until the
        // deletion lands, the old machine must not snapshot over the replacement's record.
        if (TryComp<CompanyVendorComponent>(uid, out var old))
            old.ResolvedKey = null;

        QueueDel(uid);
        return replacement;
    }

    #endregion

    #region Stock

    /// <summary>
    /// Replaces the machine's stock with a full load at its current level, split across the pack in proportion to
    /// the pack's own starting amounts.
    /// </summary>
    private void Fill(Entity<CompanyVendorComponent> ent)
    {
        if (!TryComp<VendingMachineComponent>(ent, out var vend)
            || !_prototypes.TryIndex(vend.PackPrototypeId, out var pack))
            return;

        var weights = pack.StartingInventory.Where(p => p.Value > 0).ToList();
        var total = weights.Sum(p => (long) p.Value);
        var capacity = ent.Comp.Capacity;
        var stock = new Dictionary<string, uint>();
        if (total == 0 || capacity <= 0)
        {
            SetStock(ent, stock);
            return;
        }

        var allotted = 0;
        var remainders = new List<(string Id, double Fraction, uint Weight)>();
        foreach (var (id, weight) in weights)
        {
            var exact = (double) capacity * weight / total;
            var whole = (int) Math.Floor(exact);
            stock[id.Id] = (uint) whole;
            allotted += whole;
            remainders.Add((id.Id, exact - whole, weight));
        }

        // Largest remainder first so the load always adds up to exactly the capacity.
        foreach (var (id, _, _) in remainders.OrderByDescending(r => r.Fraction).ThenByDescending(r => r.Weight))
        {
            if (allotted >= capacity)
                break;

            stock[id]++;
            allotted++;
        }

        SetStock(ent, stock);
    }

    private void SetStock(Entity<CompanyVendorComponent> ent, Dictionary<string, uint> stock)
    {
        if (!TryComp<VendingMachineComponent>(ent, out var vend))
            return;

        // Capacity is enforced here as well, so a hand-edited save can't overfill a machine.
        var remaining = (uint) Math.Max(0, ent.Comp.Capacity);
        foreach (var entry in vend.Inventory.Values)
        {
            var amount = Math.Min(stock.GetValueOrDefault(entry.ID), remaining);
            entry.Amount = amount;
            remaining -= amount;
        }

        Dirty(ent, vend);
        _vending.UpdateVendingMachineInterfaceState(ent, vend);
        _vending.TryUpdateVisualState(ent, vend);
    }

    private static int CountStock(VendingMachineComponent vend)
    {
        return vend.Inventory.Values.Sum(e => (int) e.Amount);
    }

    #endregion

    #region Restock and takeover

    private void OnRestockAfterInteract(Entity<CompanyVendorRestockComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target
            || !TryComp<CompanyVendorComponent>(target, out var vendor))
            return;

        args.Handled = true;

        if (!CanApply(ent, (target, vendor), args.User, out var failure))
        {
            _popup.PopupEntity(failure, target, args.User, PopupType.SmallCaution);
            return;
        }

        var doAfter = new DoAfterArgs(EntityManager, args.User, ent.Comp.Delay, new CompanyVendorRestockDoAfterEvent(),
            target, target: target, used: ent)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            return;

        var message = vendor.Company == ent.Comp.Company
            ? "company-vendor-restock-start"
            : "company-vendor-takeover-start";
        _popup.PopupEntity(Loc.GetString(message, ("machine", target)), args.User, args.User);
    }

    private void OnRestockDoAfter(Entity<CompanyVendorComponent> ent, ref CompanyVendorRestockDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Used is not { } used
            || !TryComp<CompanyVendorRestockComponent>(used, out var restock))
            return;

        args.Handled = true;

        // Revalidate: relations or the machine itself may have changed during the do-after.
        if (!CanApply((used, restock), ent, args.User, out var failure))
        {
            _popup.PopupEntity(failure, ent, args.User, PopupType.SmallCaution);
            return;
        }

        var machine = ent;
        var takeover = ent.Comp.Company != restock.Company;
        if (takeover)
        {
            if (!_machineFor.TryGetValue((restock.Company, restock.Product), out var prototype))
            {
                Log.Error($"No company vendor prototype for {restock.Company}/{restock.Product}; {ToPrettyString(used)} cannot convert {ToPrettyString(ent)}.");
                return;
            }

            var key = ent.Comp.ResolvedKey;
            if (key != null)
                _live.Remove(key);

            var replacement = ReplaceMachine(ent, prototype);
            machine = (replacement, Comp<CompanyVendorComponent>(replacement));
            if (key != null)
                Track(machine, key);
        }

        machine.Comp.Level = Math.Clamp(restock.Level, 1, Math.Max(1, machine.Comp.Capacities.Count));
        Fill(machine);
        Del(used);

        var done = takeover ? "company-vendor-takeover-done" : "company-vendor-restock-done";
        _popup.PopupEntity(Loc.GetString(done,
                ("machine", machine.Owner),
                ("level", machine.Comp.Level),
                ("capacity", machine.Comp.Capacity)),
            machine, args.User, PopupType.Medium);

        // A takeover or a paid-for restock must not be lost to a crash before the next periodic snapshot.
        if (machine.Comp.ResolvedKey != null && Snapshot(machine))
            Save();
    }

    private bool CanApply(Entity<CompanyVendorRestockComponent> restock,
        Entity<CompanyVendorComponent> vendor,
        EntityUid user,
        out string failure)
    {
        failure = string.Empty;

        if (!string.Equals(restock.Comp.Product, vendor.Comp.Product, StringComparison.OrdinalIgnoreCase))
        {
            failure = Loc.GetString("company-vendor-wrong-product", ("module", restock.Owner), ("machine", vendor.Owner));
            return false;
        }

        // Stocking a franchise is the company's own job; a module in anyone else's hands is just cargo.
        var userFaction = FactionMachineSystem.NormalizeFaction(CompOrNull<HullrotFactionComponent>(user)?.Faction ?? string.Empty);
        if (userFaction != restock.Comp.Company)
        {
            failure = Loc.GetString("company-vendor-not-member",
                ("company", FactionDisplay.Abbreviation(restock.Comp.Company)));
            return false;
        }

        // Unaligned ground is an open market. Anywhere else the host has to be trading with (or allied to) the
        // company — which also means a broken deal locks the company out of servicing its own machines there.
        // The host is whoever holds the grid (captures update its IFF), not the machine: the machine itself is
        // stamped with the company running it.
        var host = GetHostFaction(vendor);
        if (host.Length == 0 || host == restock.Comp.Company)
            return true;

        var relation = _diplomacy.GetRelation(host, restock.Comp.Company);
        if (relation is FactionRelation.Trade or FactionRelation.Alliance)
            return true;

        failure = Loc.GetString("company-vendor-no-agreement",
            ("host", FactionDisplay.Abbreviation(host)),
            ("company", FactionDisplay.Abbreviation(restock.Comp.Company)));
        return false;
    }

    private string GetHostFaction(EntityUid vendor)
    {
        return Transform(vendor).GridUid is { } grid && TryComp<IFFComponent>(grid, out var iff)
            ? FactionMachineSystem.NormalizeFaction(iff.Faction)
            : string.Empty;
    }

    #endregion

    #region Examine

    private void OnVendorExamined(Entity<CompanyVendorComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange || !TryComp<VendingMachineComponent>(ent, out var vend))
            return;

        var stock = CountStock(vend);
        using (args.PushGroup(nameof(CompanyVendorComponent)))
        {
            args.PushMarkup(Loc.GetString("company-vendor-examine",
                ("company", FactionDisplay.Abbreviation(ent.Comp.Company)),
                ("level", ent.Comp.Level),
                ("stock", stock),
                ("capacity", ent.Comp.Capacity)));

            if (stock == 0)
                args.PushMarkup(Loc.GetString("company-vendor-examine-empty"));
        }
    }

    private void OnRestockExamined(Entity<CompanyVendorRestockComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("company-vendor-restock-examine",
            ("company", FactionDisplay.Abbreviation(ent.Comp.Company)),
            ("level", ent.Comp.Level)));
    }

    #endregion

    #region Persistence

    private void OnRunLevelChanged(GameRunLevelChangedEvent ev)
    {
        // Entities are still alive at round end but gone by the restart cleanup, so the final snapshot goes here.
        if (ev.New == GameRunLevel.PostRound && SnapshotAll())
            Save();
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _pending.Clear();
        _live.Clear();
    }

    private bool SnapshotAll()
    {
        var changed = false;
        var query = EntityQueryEnumerator<CompanyVendorComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.ResolvedKey != null)
                changed |= Snapshot((uid, comp));
        }

        return changed;
    }

    /// <summary>
    /// Records the machine's current state under its key. Returns whether anything changed.
    /// </summary>
    private bool Snapshot(Entity<CompanyVendorComponent> ent)
    {
        if (ent.Comp.ResolvedKey is not { } key
            || MetaData(ent).EntityPrototype?.ID is not { } prototype
            || !TryComp<VendingMachineComponent>(ent, out var vend))
            return false;

        var stock = vend.Inventory.Values
            .Where(e => e.Amount > 0)
            .ToDictionary(e => e.ID, e => e.Amount);

        if (_records.TryGetValue(key, out var existing)
            && existing.Prototype == prototype
            && existing.Level == ent.Comp.Level
            && existing.Stock.Count == stock.Count
            && existing.Stock.All(p => stock.TryGetValue(p.Key, out var amount) && amount == p.Value))
            return false;

        _records[key] = new CompanyVendorRecord
        {
            Prototype = prototype,
            Level = ent.Comp.Level,
            Stock = stock,
        };
        return true;
    }

    private void Load()
    {
        try
        {
            if (!_resources.UserData.TryReadAllText(SavePath, out var json))
                return;

            var data = JsonSerializer.Deserialize<CompanyVendorSaveData>(json);
            if (data is null || data.Version != SaveVersion)
                return;

            _records.Clear();
            foreach (var (key, record) in data.Machines)
            {
                if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(record.Prototype))
                    _records[key] = record;
            }
        }
        catch (Exception e)
        {
            Log.Error($"Failed to load company vendors: {e}");
        }
    }

    private void Save()
    {
        try
        {
            var data = new CompanyVendorSaveData { Version = SaveVersion, Machines = new(_records) };
            _resources.UserData.WriteAllText(SavePath, JsonSerializer.Serialize(data));
        }
        catch (Exception e)
        {
            Log.Error($"Failed to save company vendors: {e}");
        }
    }

    #endregion

    #region Prototypes

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<EntityPrototype>())
            BuildMachineTable();
    }

    /// <summary>
    /// Which machine a company installs for each product line, used when it takes over a rival's machine.
    /// </summary>
    private void BuildMachineTable()
    {
        _machineFor.Clear();
        foreach (var proto in _prototypes.EnumeratePrototypes<EntityPrototype>())
        {
            if (proto.Abstract || !proto.TryGetComponent<CompanyVendorComponent>(out var comp, EntityManager.ComponentFactory))
                continue;

            var slot = (comp.Company, comp.Product);
            if (_machineFor.TryGetValue(slot, out var existing))
            {
                Log.Warning($"Company vendors {existing} and {proto.ID} both claim {comp.Company}/{comp.Product}; takeovers will install {existing}.");
                continue;
            }

            _machineFor[slot] = proto.ID;
        }
    }

    #endregion
}

internal sealed class CompanyVendorSaveData
{
    public int Version { get; set; }
    public Dictionary<string, CompanyVendorRecord> Machines { get; set; } = new();
}

internal sealed class CompanyVendorRecord
{
    public string Prototype { get; set; } = string.Empty;
    public int Level { get; set; } = 1;
    public Dictionary<string, uint> Stock { get; set; } = new();
}
