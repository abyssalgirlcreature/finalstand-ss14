using Content.Shared._FinalStand.Ailments;

namespace Content.Server._FinalStand.Mobs;

[RegisterComponent]
[Access(typeof(FSRevenantBoltSystem), typeof(FSRevenantSystem))]
public sealed partial class FSRevenantBoltComponent : Component
{
    [DataField] public float Damage = 16f;
    [DataField] public FSInjurySpec Injury = new() { Bleed = 2 };
    [DataField] public float PollInterval = 0.05f;

    [DataField] public float HitRadius = 0.35f;

    public EntityUid Shooter;
    public float ResistanceBypass;

    public float PollAccum;
    // A bolt passes through bodies but hits each one once.
    public readonly HashSet<EntityUid> Hit = new();
}
