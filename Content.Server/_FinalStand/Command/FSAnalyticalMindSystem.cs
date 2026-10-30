using Content.Server._FinalStand.GameTicking.Rules;
using Content.Server._FinalStand.Research;
using Content.Shared._FinalStand.Command;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;

namespace Content.Server._FinalStand.Command;

public sealed partial class FSAnalyticalMindSystem : EntitySystem
{
    [Dependency] private FSCommandAuraSystem _auras = default!;
    [Dependency] private FSResearchSystem _research = default!;

    private const string Source = "Analytical Mind";

    private readonly Dictionary<EntityUid, float> _pending = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FSEnemyDamageTrackingComponent, DamageChangedEvent>(OnEnemyDamaged);
    }

    private void OnEnemyDamaged(Entity<FSEnemyDamageTrackingComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased
            || args.DamageDelta is not { } delta
            || args.Origin is not { } attacker
            || !_auras.TryGetSourceMind(attacker, FSCommandAura.AnalyticalMind, out var director))
            return;

        var earned = delta.GetTotal().Float() * FSCommandAuraRules.AnalyticalMindResearchPerDamage;
        if (earned <= 0f)
            return;

        _pending.TryGetValue(director, out var carried);
        var total = carried + earned;
        var grant = (int) MathF.Floor(total);
        _pending[director] = total - grant;

        if (grant > 0)
            _research.GrantResearchPoints(grant, Source, director);
    }
}
