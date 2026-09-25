namespace Content.Shared._Crescent.CompanyVendors;

/// <summary>
/// A company franchise vendor (Shinohara coffee/rations or TFCF energy drinks/food). Unlike an ordinary vending machine it never refills
/// itself: stock only comes from a company restock module, and the stock level, the stock-module level and which
/// company runs the machine all carry over between rounds.
/// </summary>
/// <remarks>
/// Machines of the same <see cref="Product"/> line are interchangeable: a company member with a restock module can
/// take over a rival's machine, provided the host faction trades with (or is allied to) their company.
/// </remarks>
[RegisterComponent]
public sealed partial class CompanyVendorComponent : Component
{
    /// <summary>
    /// Faction ID of the company running this machine, e.g. "SHI" or "TFSC".
    /// </summary>
    [DataField(required: true)]
    public string Company = string.Empty;

    /// <summary>
    /// Product line: "Coffee" is the shared Shinohara coffee/TFCF energy drink slot; "Food" is the food slot.
    /// Only machines and restock modules of the same line fit each other.
    /// </summary>
    [DataField(required: true)]
    public string Product = string.Empty;

    /// <summary>
    /// Stock module level, 1-based index into <see cref="Capacities"/>.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int Level = 1;

    /// <summary>
    /// How many regular items the machine holds at each stock module level.
    /// </summary>
    [DataField]
    public List<int> Capacities = new() { 20, 50, 100 };

    /// <summary>
    /// Overrides the automatic persistence key. Leave empty for mapped machines: they are keyed by their station and
    /// tile, so a map does not need to hand-number every vendor.
    /// </summary>
    [DataField]
    public string? PersistId;

    /// <summary>
    /// The key this machine's state is saved under, once resolved. Null means the machine is not persisted.
    /// </summary>
    [ViewVariables]
    public string? ResolvedKey;

    public int Capacity => Capacities.Count == 0 ? 0 : Capacities[Math.Clamp(Level, 1, Capacities.Count) - 1];
}
