using Content.Server._FinalStand.Deployables;
using Content.Server._FinalStand.GameTicking.Rules;
using Content.Server._FinalStand.Perks;
using Content.Shared._FinalStand.Armor;
using Content.Shared._FinalStand.HMG;
using Content.Shared._FinalStand.Weapons;
using Content.Shared.GameTicking;
using Content.Shared._FinalStand.Perks;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Projectiles;

namespace Content.Server._FinalStand.Economy;

public sealed partial class FSMoneyOnHitSystem : EntitySystem
{
    [Dependency] private FSPlayerWalletSystem _wallet = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private MobThresholdSystem _thresholds = default!;
    [Dependency] private FSUndyingSystem _undying = default!;
    [Dependency] private FSPerkEarningsSystem _earnings = default!;
    [Dependency] private WaveGameRuleSystem _waveRule = default!;

    private EntityQuery<FSArmorComponent> _armorQuery;
    private EntityQuery<FSHarvesterComponent> _harvesterQuery;
    private EntityQuery<FSPerkLevelsComponent> _perksQuery;

    // Profiteer's share is a few percent of a hit, so the fraction carries over rather than rounding to nothing.
    private readonly Dictionary<EntityUid, float> _profiteerRemainder = new();

    // Paid per point of damage dealt, so every weapon earns the same for the same zombie. An MK58 bullet (16)
    // pays about what a hit paid before (30).
    private const float CreditsPerDamage = 2f;

    private EntityUid? _deployableHitTarget;
    private (EntityUid Target, int Amount)? _flatHit;

    // The Harvester lands 20 hitscans a second, so at full rate it caps every enemy almost
    // instantly. Its payout is scaled down to keep it a research weapon, not an income weapon.
    private const float HarvesterMoneyMultiplier = 0.30f;
    public override void Initialize()
    {
        base.Initialize();
        _armorQuery = GetEntityQuery<FSArmorComponent>();
        _harvesterQuery = GetEntityQuery<FSHarvesterComponent>();
        _perksQuery = GetEntityQuery<FSPerkLevelsComponent>();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(_ => _profiteerRemainder.Clear());
        SubscribeLocalEvent<FSMoneyOnHitCapComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<FSDeployableProjectileComponent, ProjectileHitEvent>(OnDeployableHit);
        SubscribeLocalEvent<FSFlatMoneyProjectileComponent, ProjectileHitEvent>(OnFlatHit);
    }

    // A projectile hit and its damage happen in the same call. If the hit changed no damage (armour, a resisted
    // hit) the flag would otherwise linger and mislabel someone else's next hit on that enemy.
    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        _deployableHitTarget = null;
        _flatHit = null;
    }

    private void OnDeployableHit(Entity<FSDeployableProjectileComponent> ent, ref ProjectileHitEvent args)
    {
        _deployableHitTarget = args.Target;
    }

    private void OnFlatHit(Entity<FSFlatMoneyProjectileComponent> ent, ref ProjectileHitEvent args)
    {
        _flatHit = (args.Target, ent.Comp.Amount);
    }

    private void OnDamageChanged(EntityUid uid, FSMoneyOnHitCapComponent cap, DamageChangedEvent args)
    {
        var fromDeployable = _deployableHitTarget == uid;
        _deployableHitTarget = null;
        int? flatAmount = _flatHit is { } flatHit && flatHit.Target == uid ? flatHit.Amount : null;
        _flatHit = null;

        if (!args.DamageIncreased || args.Origin == null)
            return;
        if (_armorQuery.TryGetComponent(uid, out var armor) && armor.CurrentArmor > 0)
            return;

        var damage = PaidDamage(uid, args);
        if (damage <= 0f)
            return;

        if (_undying.IsActive(args.Origin.Value))
            return;
        if (!_mind.TryGetMind(args.Origin.Value, out var mindId, out _))
            return;

        cap.MoneyGivenPerPlayer.TryGetValue(mindId, out var alreadyGiven);
        var max = (int) (cap.MaxMoneyPerPlayer * _waveRule.GetEnemyMoneyMultiplier());
        if (alreadyGiven >= max)
            return;

        var earned = flatAmount ?? damage * CreditsPerDamage;
        if (flatAmount == null && !fromDeployable && _hands.TryGetActiveItem(args.Origin.Value, out var heldItem))
        {
            if (TryComp<FSFlatMoneyPerHitComponent>(heldItem, out var flat))
                earned = flat.Amount;

            if (_harvesterQuery.HasComponent(heldItem.Value))
                earned *= HarvesterMoneyMultiplier;
        }

        earned += cap.UnpaidPerPlayer.GetValueOrDefault(mindId);
        var payout = (int) earned;
        cap.UnpaidPerPlayer[mindId] = earned - payout;
        if (payout <= 0)
            return;

        var give = Math.Min(payout, max - alreadyGiven);
        cap.MoneyGivenPerPlayer[mindId] = alreadyGiven + give;
        _wallet.GiveCredits(mindId, give, FSCreditSource.Hit);
        if (ProfiteerShare(mindId, give) is var share and > 0)
            _wallet.GiveCredits(mindId, share, FSCreditSource.Perk);
    }

    // What this hit took off the zombie's life: the damage it dealt, minus any past what was left to kill it. A hit on
    // a zombie that was already dead pays nothing.
    private float PaidDamage(EntityUid uid, DamageChangedEvent args)
    {
        if (args.DamageDelta is not { } delta)
            return 0f;

        var dealt = FixedPoint2.Zero;
        foreach (var amount in delta.DamageDict.Values)
        {
            if (amount > FixedPoint2.Zero)
                dealt += amount;
        }

        if (_thresholds.TryGetThresholdForState(uid, MobState.Dead, out var dead))
        {
            var before = args.Damageable.TotalDamage - delta.GetTotal();
            dealt = FixedPoint2.Min(dealt, dead.Value - before);
        }

        return dealt.Float();
    }

    // Profiteer is a percentage of hit money, so it follows the same cap, armour and Harvester rules.
    private int ProfiteerShare(EntityUid mindId, int amount)
    {
        if (!_perksQuery.TryComp(mindId, out var perks))
            return 0;

        var level = perks.GetSlottedLevel("Profiteer");
        if (level <= 0)
            return 0;

        var share = amount * level * FSPerkBonusConstants.ProfiteerFraction + _profiteerRemainder.GetValueOrDefault(mindId);
        var whole = (int) share;
        _profiteerRemainder[mindId] = share - whole;
        _earnings.RecordCredits(mindId, "Profiteer", whole);
        return whole;
    }
}
