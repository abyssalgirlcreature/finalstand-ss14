using Robust.Shared.Prototypes;

namespace Content.Shared._FinalStand.Mech;

[RegisterComponent]
public sealed partial class FSMechFistsComponent : Component
{
    [DataField]
    public EntProtoId ActionProto = "FSMechFistsAction";

    [DataField]
    public EntityUid? Action;
}
