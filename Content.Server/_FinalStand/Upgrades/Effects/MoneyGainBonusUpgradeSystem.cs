using Content.Server._FinalStand.Economy;
using Content.Shared._FinalStand.Upgrades.Effects;
using Content.Shared.Mind;
using Content.Shared.Mobs;

namespace Content.Server._FinalStand.Upgrades.Effects;

public sealed partial class MoneyGainBonusUpgradeSystem : EntitySystem
{
    [Dependency] private FSPlayerWalletSystem _wallet = default!;
    [Dependency] private FSWaveRewardsSystem _rewards = default!;
    [Dependency] private SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FSPendingKillBonusComponent, MobStateChangedEvent>(OnEnemyDied);
    }

    // Only the player the bonus was left for collects it, and only with their own kill.
    private void OnEnemyDied(EntityUid uid, FSPendingKillBonusComponent bonus, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead || args.OldMobState == MobState.Dead)
            return;
        if (args.Origin is not { } killer || killer != bonus.Payee || !_mind.TryGetMind(killer, out var mindId, out _))
            return;

        var credits = (int) MathF.Round(bonus.MoneyPercent * _rewards.KillCredits(uid));
        if (credits > 0)
            _wallet.GiveCredits(mindId, credits, FSCreditSource.KillBonus);
    }
}
