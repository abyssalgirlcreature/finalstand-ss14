using Content.Shared._FinalStand.Economy;
using Content.Shared.Mind;
using Prometheus;

namespace Content.Server._FinalStand.Economy;

// Publishes every credit change on the server's Prometheus endpoint (metrics.enabled) so income can be tracked
// from outside the game, per player, kind of flow and calling system. Read by fs_economy_tracker.py.
public sealed partial class FSPlayerWalletSystem
{
    private static readonly Counter CreditsMetric = Metrics.CreateCounter(
        "fs_credits_total",
        "Credits moved through player wallets since server start, by player, kind of flow and credit source or purchase kind.",
        new CounterConfiguration { LabelNames = new[] { "player", "kind", "source" } });

    private static readonly Gauge BalanceMetric = Metrics.CreateGauge(
        "fs_credits_balance",
        "Credits currently in each player's wallet.",
        new GaugeConfiguration { LabelNames = new[] { "player" } });

    // Kinds of flow. Amounts are always recorded as positive numbers; the kind says which way they went.
    private const string KindEarned = "earned";            // GiveCredits: kills, hits, perks, payouts (wave report EARNED)
    private const string KindTeamPayout = "team_payout";   // DistributeCredits: paid to every player at once
    private const string KindStarting = "starting";        // round-start credits
    private const string KindAdded = "added";              // AddCredits: refunds, sell-backs, transfers in, interest, admin
    private const string KindRemoved = "removed";          // a negative GiveCredits/AddCredits
    private const string KindSpent = "spent";              // purchases, respawn fees
    private const string KindTransferOut = "transfer_out"; // credits sent to another player
    private const string SourceStarting = "Starting";

    private readonly Dictionary<(string Player, string Kind, string Source), Counter.Child> _creditChildren = new();
    private readonly HashSet<string> _balancePlayers = new();

    private void RecordCredits(EntityUid mindId, int amount, string kind, string source)
    {
        if (amount == 0)
            return;

        var player = MetricsPlayerName(mindId);
        var key = (player, kind, source);
        if (!_creditChildren.TryGetValue(key, out var child))
        {
            child = CreditsMetric.WithLabels(player, kind, source);
            _creditChildren[key] = child;
        }

        child.Inc(Math.Abs(amount));
        PublishBalance(mindId, player);
    }

    private void PublishBalance(EntityUid mindId, string? player = null)
    {
        if (!TryComp<FSPlayerWalletComponent>(mindId, out var wallet))
            return;

        player ??= MetricsPlayerName(mindId);
        BalanceMetric.WithLabels(player).Set(wallet.Credits);
        _balancePlayers.Add(player);
    }

    // Wallets are emptied at round restart; drop the old balances so the next round starts clean.
    private void ResetBalanceMetrics()
    {
        foreach (var player in _balancePlayers)
        {
            BalanceMetric.RemoveLabelled(player);
        }

        _balancePlayers.Clear();
    }

    private string MetricsPlayerName(EntityUid mindId)
    {
        return TryComp<MindComponent>(mindId, out var mind) && mind.UserId is { } user
            ? ResolveUsername(user)
            : "unknown";
    }
}
