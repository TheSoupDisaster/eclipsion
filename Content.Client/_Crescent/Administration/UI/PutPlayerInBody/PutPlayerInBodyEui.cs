using Content.Client.Eui;
using Content.Shared._Crescent.Administration;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Client._Crescent.Administration.UI.PutPlayerInBody;

[UsedImplicitly]
public sealed class PutPlayerInBodyEui : BaseEui
{
    private readonly PutPlayerInBodyWindow _window;

    public PutPlayerInBodyEui()
    {
        _window = new PutPlayerInBodyWindow();
        _window.OnPlayerChosen += player => SendMessage(new PutPlayerInBodySelectMessage(player));
        _window.OnClose += () => SendMessage(new CloseEuiMessage());
    }

    public override void Opened()
    {
        _window.OpenCentered();
    }

    public override void Closed()
    {
        base.Closed();
        _window.Close();
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not PutPlayerInBodyEuiState s)
            return;

        _window.SetTarget(s.TargetName, s.CurrentOccupant);
    }
}
