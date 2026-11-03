using Robust.Shared.GameStates;

namespace Content.Shared._FinalStand.Command;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FSCommandAuraBuffComponent : Component
{
    public readonly Dictionary<FSCommandAura, EntityUid> Sources = new();

    [AutoNetworkedField]
    public HashSet<FSCommandAura> Active = new();
}
