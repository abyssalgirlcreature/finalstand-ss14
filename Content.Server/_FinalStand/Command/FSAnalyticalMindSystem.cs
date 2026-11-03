using Content.Server._FinalStand.Leveling;
using Content.Server._FinalStand.Weapons;
using Content.Shared._FinalStand.Command;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Mind;

namespace Content.Server._FinalStand.Command;

public sealed partial class FSAnalyticalMindSystem : EntitySystem
{
    [Dependency] private FSCommandAuraSystem _auras = default!;
    [Dependency] private FSHarvesterWeaponSystem _harvester = default!;
    [Dependency] private SharedMindSystem _mind = default!;

    private const string Source = "analytical-mind";

    private readonly Dictionary<EntityUid, float> _pending = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FSEnemyDamageTrackerComponent, DamageChangedEvent>(OnEnemyDamaged);
    }

    private void OnEnemyDamaged(Entity<FSEnemyDamageTrackerComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased
            || args.DamageDelta is not { } delta
            || args.Origin is not { } attacker
            || !_auras.TryGetSourceMind(attacker, FSCommandAura.AnalyticalMind, out var director)
            || !_mind.TryGetMind(attacker, out var attackerMind, out _))
            return;

        var earned = delta.GetTotal().Float() * FSCommandAuraRules.AnalyticalMindResearchPerDamage;
        if (earned <= 0f)
            return;

        _pending.TryGetValue(attackerMind, out var carried);
        var total = carried + earned;
        var amount = (int) MathF.Floor(total);
        _pending[attackerMind] = total - amount;

        if (amount > 0)
            _harvester.TryGrantHitResearch(attacker, ent.Owner, amount, Source, creditMind: director);
    }
}
