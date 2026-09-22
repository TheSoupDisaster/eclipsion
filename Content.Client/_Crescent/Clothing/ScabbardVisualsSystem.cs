using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Storage.Components;
using Robust.Client.GameObjects;

namespace Content.Client._Crescent.Clothing;

public sealed class ScabbardVisualsSystem : VisualizerSystem<ScabbardVisualsComponent>
{
    [Dependency] private readonly ClothingSystem _clothing = default!;

    protected override void OnAppearanceChange(EntityUid uid, ScabbardVisualsComponent component,
        ref AppearanceChangeEvent args)
    {
        if (!AppearanceSystem.TryGetData<ShowLayerData>(uid, StorageMapVisuals.LayerChanged,
                out var layers, args.Component))
            return;

        // Reuses the same whitelist-driven state as the ground/inventory sprite.
        // SetEquippedPrefix also refreshes the wearer's equipment layers.
        _clothing.SetEquippedPrefix(uid,
            layers.QueuedEntities.Contains(component.FilledPrefix) ? component.FilledPrefix : null);
    }
}
