using Content.Shared._FinalStand.Command;
using Content.Shared.Damage.Systems;

namespace Content.Server._FinalStand.Command;

public sealed partial class FSReinforcedSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FSCommandAuraBuffComponent, DamageModifyEvent>(OnDamageModify);
    }

    private void OnDamageModify(Entity<FSCommandAuraBuffComponent> ent, ref DamageModifyEvent args)
    {
        if (ent.Comp.Sources.ContainsKey(FSCommandAura.Reinforced) && args.Damage.GetTotal() > 0)
            args.Damage *= 1f - FSCommandAuraRules.ReinforcedResist;
    }
}
