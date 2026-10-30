using Content.Server._FinalStand.Perks;
using Content.Shared._FinalStand.Command;

namespace Content.Server._FinalStand.Command;

public sealed partial class FSSecurityTrainingSystem : EntitySystem
{
    [Dependency] private FSCommandAuraSystem _auras = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FSIncomingDamageModifyEvent>(OnIncomingDamage);
    }

    public float OutgoingMultiplier(EntityUid attacker)
    {
        return _auras.Covers(attacker, FSCommandAura.SecurityTraining)
            ? 1f + FSCommandAuraRules.SecurityTrainingDamage
            : 1f;
    }

    private void OnIncomingDamage(ref FSIncomingDamageModifyEvent ev)
    {
        if (_auras.Covers(ev.Target, FSCommandAura.SecurityTraining) && ev.Args.Damage.GetTotal() > 0)
            ev.Args.Damage *= 1f - FSCommandAuraRules.SecurityTrainingResist;
    }
}
