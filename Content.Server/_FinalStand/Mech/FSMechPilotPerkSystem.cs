using Content.Server._FinalStand.Perks;
using Content.Server._FinalStand.Titles;
using Content.Shared._FinalStand.Mech;
using Content.Shared._FinalStand.Perks;
using Content.Shared.Damage.Systems;
using Content.Shared.Mech.Components;
using Content.Shared.Mind;

namespace Content.Server._FinalStand.Mech;

public sealed partial class FSMechPilotPerkSystem : EntitySystem
{
    [Dependency] private FSMechPerkScaleSystem _mechScale = default!;
    [Dependency] private FSTitleBuffSystem _titleBuffs = default!;
    [Dependency] private SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MechComponent, DamageModifyEvent>(OnMechDamaged);
    }

    private void OnMechDamaged(Entity<MechComponent> ent, ref DamageModifyEvent args)
    {
        if (args.Damage.GetTotal() <= 0
            || !_mechScale.TryGetPilot(ent, out var pilot)
            || !_mind.TryGetMind(pilot, out var mindId, out _)
            || !TryComp<FSPerkLevelsComponent>(mindId, out var perks))
            return;

        var taken = FSMechPerkScaleSystem.Reduce(
            FSArchetypeSystem.Bonus(perks).DamageTaken * _titleBuffs.ForBody(pilot).DamageTaken);

        if (Math.Abs(taken - 1f) > 0.0001f)
            args.Damage *= taken;
    }
}
