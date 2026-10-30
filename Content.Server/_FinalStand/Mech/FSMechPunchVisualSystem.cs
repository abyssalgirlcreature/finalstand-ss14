using System.Numerics;
using Content.Shared.Mech.Components;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server._FinalStand.Mech;

public sealed partial class FSMechPunchVisualSystem : EntitySystem
{
    [Dependency] private SharedTransformSystem _transform = default!;

    private static readonly EntProtoId FistProto = "FSEffectMechFist";

    private const float Reach = 1.4f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MechComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnMeleeHit(EntityUid uid, MechComponent comp, MeleeHitEvent args)
    {
        if (!args.IsHit)
            return;

        var origin = _transform.GetWorldPosition(uid);
        var heading = args.Direction ?? TowardTarget(args.HitEntities, origin);
        if (heading.LengthSquared() < 0.01f)
            return;

        heading = Vector2.Normalize(heading);
        var fist = Spawn(FistProto, new MapCoordinates(origin + heading * Reach, Transform(uid).MapID));
        _transform.SetWorldRotation(fist, heading.ToWorldAngle());
    }

    private Vector2 TowardTarget(IReadOnlyList<EntityUid> hits, Vector2 origin)
    {
        foreach (var hit in hits)
        {
            if (!TerminatingOrDeleted(hit))
                return _transform.GetWorldPosition(hit) - origin;
        }

        return Vector2.Zero;
    }
}
