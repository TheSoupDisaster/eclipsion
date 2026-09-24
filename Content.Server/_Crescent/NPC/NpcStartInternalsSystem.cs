using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.Clothing.Loadouts.Systems;

namespace Content.Server._Crescent.NPC;

/// <inheritdoc cref="NpcStartInternalsComponent"/>
public sealed class NpcStartInternalsSystem : EntitySystem
{
    [Dependency] private readonly InternalsSystem _internals = default!;

    public override void Initialize()
    {
        base.Initialize();

        // The loadout is what puts the mask and the tank on them, so this has to come after it.
        SubscribeLocalEvent<NpcStartInternalsComponent, MapInitEvent>(OnMapInit,
            after: [typeof(SharedLoadoutSystem)]);
    }

    private void OnMapInit(Entity<NpcStartInternalsComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<InternalsComponent>(ent, out var internals))
            return;

        // ToggleInternals is a toggle, not a switch-on: if the tank is already hooked up it disconnects
        // it again. InternalsSystem connects it itself when the mob is equipped somewhere it can't
        // breathe, which is exactly the case this component exists for, so check before toggling.
        if (_internals.AreInternalsWorking(ent, internals))
            return;

        _internals.ToggleInternals(ent, ent, true, internals);
    }
}
