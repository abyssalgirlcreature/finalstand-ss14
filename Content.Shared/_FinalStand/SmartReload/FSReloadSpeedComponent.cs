using Robust.Shared.GameStates;

namespace Content.Shared._FinalStand.SmartReload;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FSReloadSpeedComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan MagEject;

    [DataField, AutoNetworkedField]
    public TimeSpan MagInsert;

    [DataField, AutoNetworkedField]
    public bool MagazineReady;
}
