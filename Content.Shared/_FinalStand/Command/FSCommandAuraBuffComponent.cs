using Content.Shared._FinalStand.Command;

namespace Content.Server._FinalStand.Command;

[RegisterComponent]
public sealed partial class FSCommandAuraBuffComponent : Component
{
    public readonly Dictionary<FSCommandAura, EntityUid> Sources = new();
}
