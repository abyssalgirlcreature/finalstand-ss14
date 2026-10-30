using Content.Shared._FinalStand.Command;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Timing;

namespace Content.Server._FinalStand.Command;

public sealed partial class FSPassiveMedicineSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private MobStateSystem _mobState = default!;

    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(FSCommandAuraRules.PassiveMedicineSeconds);

    private TimeSpan _nextTick;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextTick)
            return;
        _nextTick = _timing.CurTime + TickInterval;

        var heal = FixedPoint2.New(-FSCommandAuraRules.PassiveMedicineHeal);
        var query = EntityQueryEnumerator<FSCommandAuraBuffComponent, DamageableComponent>();
        while (query.MoveNext(out var uid, out var buff, out var damageable))
        {
            if (!buff.Sources.ContainsKey(FSCommandAura.PassiveMedicine)
                || damageable.TotalDamage <= 0
                || _mobState.IsDead(uid))
                continue;

            _damageable.HealEvenly(uid, heal);
        }
    }
}
