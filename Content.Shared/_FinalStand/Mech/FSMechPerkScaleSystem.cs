using Content.Shared._FinalStand.Perks;
using Content.Shared.Mech.Components;
using Content.Shared.Vehicle.Components;
using Content.Shared.Vehicle.Systems;

namespace Content.Shared._FinalStand.Mech;

public sealed partial class FSMechPerkScaleSystem : EntitySystem
{
    [Dependency] private VehicleSystem _vehicle = default!;

    public bool IsPiloting(EntityUid user)
    {
        return TryComp<VehicleOperatorComponent>(user, out var op)
               && op.Vehicle is { } vehicle
               && HasComp<MechComponent>(vehicle);
    }

    public bool TryGetPilot(EntityUid mech, out EntityUid pilot)
    {
        pilot = default;
        if (!HasComp<MechComponent>(mech) || !_vehicle.TryGetOperator(mech, out var op))
            return false;

        pilot = op.Value.Owner;
        return true;
    }

    public static float Reduce(float multiplier)
        => 1f + (multiplier - 1f) * FSPerkBonusConstants.MechPerkScale;
}
