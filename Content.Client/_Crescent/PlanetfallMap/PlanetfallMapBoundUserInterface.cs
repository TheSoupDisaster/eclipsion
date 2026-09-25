using Robust.Client.UserInterface;

namespace Content.Client._Crescent.PlanetfallMap;

public sealed class PlanetfallMapBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    protected override void Open()
    {
        base.Open();
        this.CreateWindow<PlanetfallMapWindow>();
    }
}
