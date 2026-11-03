using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Content.IntegrationTests.Fixtures;
using Content.Server._FinalStand.Economy;
using Content.Shared.Mind;
using Prometheus;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.FinalStand;

// fs_economy_tracker.py reads income from these metrics, so every wallet path has to publish the right kind,
// player and source. The registry is process-wide and shared with other tests, hence before/after deltas.
[TestFixture]
[TestOf(typeof(FSPlayerWalletSystem))]
public sealed class WalletMetricsTest : GameTest
{
    // The player label is the session's name, so the mind needs a connected player.
    public override PoolSettings PoolSettings => new() { Connected = true, Dirty = true };

    [Test]
    public async Task CreditFlowsArePublished()
    {
        var server = Pair.Server;
        var entMan = server.ResolveDependency<IEntityManager>();
        var wallets = entMan.System<FSPlayerWalletSystem>();
        var minds = entMan.System<SharedMindSystem>();
        var player = Pair.Player!;

        var before = Snapshot();

        await server.WaitAssertion(() =>
        {
            var mind = minds.CreateMind(player.UserId, "Earner").Owner;
            wallets.GiveCredits(mind, 100, FSCreditSource.Mentor);
            wallets.AddCredits(mind, 50, FSCreditSource.Admin);
            Assert.That(wallets.TryDeductCredits(mind, 30, new FSPurchase(FSPurchaseKind.Gacha, "test")), Is.True);
            Assert.That(wallets.TryDeductCredits(mind, 20, new FSPurchase(FSPurchaseKind.Transfer, "test"), spending: false), Is.True);
            Assert.That(wallets.DeductUpTo(mind, 10, new FSPurchase(FSPurchaseKind.Respawn, "test")), Is.EqualTo(10));
            Assert.That(wallets.GetCredits(mind), Is.EqualTo(90));
        });

        var after = Snapshot();
        var name = player.Name;

        Assert.Multiple(() =>
        {
            Assert.That(Delta(before, after, name, "earned", "Mentor"), Is.EqualTo(100), "GiveCredits isn't published as earned");
            Assert.That(Delta(before, after, name, "added", "Admin"), Is.EqualTo(50), "AddCredits isn't published as added");
            Assert.That(Delta(before, after, name, "spent", "Gacha"), Is.EqualTo(30), "a purchase isn't published as spent");
            Assert.That(Delta(before, after, name, "spent", "Respawn"), Is.EqualTo(10), "DeductUpTo isn't published as spent");
            Assert.That(Delta(before, after, name, "transfer_out", "Transfer"), Is.EqualTo(20), "a transfer isn't published as transfer_out");
            Assert.That(after.GetValueOrDefault($"fs_credits_balance{{player=\"{name}\"}}"), Is.EqualTo(90),
                "the balance gauge doesn't match the wallet");
        });
    }

    private static double Delta(Dictionary<string, double> before, Dictionary<string, double> after, string player, string kind, string source)
    {
        var key = $"fs_credits_total{{player=\"{player}\",kind=\"{kind}\",source=\"{source}\"}}";
        return after.GetValueOrDefault(key) - before.GetValueOrDefault(key);
    }

    private static Dictionary<string, double> Snapshot()
    {
        using var stream = new MemoryStream();
        Metrics.DefaultRegistry.CollectAndExportAsTextAsync(stream).GetAwaiter().GetResult();

        var values = new Dictionary<string, double>();
        foreach (var line in Encoding.UTF8.GetString(stream.ToArray()).Split('\n'))
        {
            if (!line.StartsWith("fs_credits_", StringComparison.Ordinal))
                continue;

            var space = line.LastIndexOf(' ');
            values[line[..space]] = double.Parse(line[(space + 1)..], CultureInfo.InvariantCulture);
        }

        return values;
    }
}
