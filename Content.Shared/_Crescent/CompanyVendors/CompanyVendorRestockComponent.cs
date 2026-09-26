using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Crescent.CompanyVendors;

/// <summary>
/// A company stock module. Used on a machine of the same product line it replaces the machine's stock with a full
/// load at this module's level; used on a rival company's machine it converts the machine to this company.
/// </summary>
[RegisterComponent]
public sealed partial class CompanyVendorRestockComponent : Component
{
    [DataField(required: true)]
    public string Company = string.Empty;

    [DataField(required: true)]
    public string Product = string.Empty;

    [DataField]
    public int Level = 1;

    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(5);
}

[Serializable, NetSerializable]
public sealed partial class CompanyVendorRestockDoAfterEvent : SimpleDoAfterEvent
{
}
