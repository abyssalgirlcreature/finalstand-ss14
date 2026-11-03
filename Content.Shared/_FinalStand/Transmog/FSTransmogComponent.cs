using Robust.Shared.GameStates;

namespace Content.Shared._FinalStand.Transmog;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class FSTransmogComponent : Component
{
    [DataField, AutoNetworkedField]
    public HashSet<string> Hidden = new();
}
