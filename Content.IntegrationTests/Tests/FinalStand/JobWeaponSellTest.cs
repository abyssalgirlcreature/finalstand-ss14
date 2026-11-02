using System.Collections.Generic;
using System.Reflection;
using Content.IntegrationTests.Fixtures;
using Content.Server._FinalStand.Economy;
using Content.Server._FinalStand.Shop;
using Content.Shared._FinalStand.Economy;
using Content.Shared._FinalStand.Shop;
using Content.Shared.GameTicking;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Mind;
using Content.Shared.Preferences;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;

namespace Content.IntegrationTests.Tests.FinalStand;

// A shop weapon that came with the player's job sells for nothing: latejoining handed out a fresh one to sell each time.
// What was spent upgrading it still refunds.
[TestFixture]
[TestOf(typeof(FSStartingWeaponLimitSystem))]
public sealed class JobWeaponSellTest : GameTest
{
    public override PoolSettings PoolSettings => new() { Connected = true, Dirty = true };

    private const string Pistol = "WeaponPistolViper";
    private const string Shop = "FSWeaponShopViper";

    private (EntityUid Body, EntityUid Mind) SpawnPlayer(TestMapData map)
    {
        var minds = SEntMan.System<SharedMindSystem>();
        var body = SEntMan.SpawnEntity("MobHuman", map.GridCoords);
        var mind = minds.CreateMind(Pair.Player!.UserId, "Recruit");
        minds.TransferTo(mind, body);
        SEntMan.EnsureComponent<FSPlayerWalletComponent>(mind);
        return (body, mind);
    }

    private EntityUid GivePistol(EntityUid body, TestMapData map, int upgradeSpend = 0)
    {
        var pistol = SEntMan.SpawnEntity(Pistol, map.GridCoords);
        SEntMan.EnsureComponent<FSWeaponUpgradeStateComponent>(pistol).TotalSpent = upgradeSpend;
        Assert.That(SEntMan.System<SharedHandsSystem>().TryPickupAnyHand(body, pistol), "couldn't hand over the pistol");
        return pistol;
    }

    private int Sell(EntityUid body, EntityUid mind, TestMapData map)
    {
        var wallets = SEntMan.System<FSPlayerWalletSystem>();
        var shop = SEntMan.SpawnEntity(Shop, map.GridCoords);
        var before = wallets.GetCredits(mind);
        SEntMan.EventBus.RaiseLocalEvent(shop, (object) new FSShopSellMessage
        {
            Actor = body,
            Entity = SEntMan.GetNetEntity(shop),
            UiKey = FSShopWeaponUiKey.Key,
        }, true);
        return wallets.GetCredits(mind) - before;
    }

    [Test]
    public async Task GearOnTheFirstSpawnIsMarkedIssued()
    {
        var map = await Pair.CreateTestMap();

        await Server.WaitAssertion(() =>
        {
            var (body, _) = SpawnPlayer(map);
            var pistol = GivePistol(body, map);

            // Earlier tests on this server may have spawned the same player already this round.
            ((HashSet<NetUserId>) typeof(FSStartingWeaponLimitSystem)
                .GetField("_issued", BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(SEntMan.System<FSStartingWeaponLimitSystem>())!).Clear();

            SEntMan.EventBus.RaiseEvent(EventSource.Local, new PlayerSpawnCompleteEvent(body, ServerSession!, null, true,
                true, 0, EntityUid.Invalid, HumanoidCharacterProfile.DefaultWithSpecies()));

            Assert.That(SEntMan.HasComponent<FSIssuedWeaponComponent>(pistol), "job gear wasn't marked as issued");
        });
    }

    [Test]
    public async Task AnIssuedWeaponOnlyRefundsItsUpgrades()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitRunTicks(70); // past the sell cooldown left by earlier tests

        await Server.WaitAssertion(() =>
        {
            var (body, mind) = SpawnPlayer(map);
            SEntMan.EnsureComponent<FSIssuedWeaponComponent>(GivePistol(body, map, upgradeSpend: 1000));

            Assert.That(Sell(body, mind, map), Is.EqualTo(FSSellBack.Refund(0, 1000)), "the shop paid for job gear");
        });
    }

    // Both pickup orders, so the result can't come from which hand the search looks at first.
    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public async Task ABoughtCopySellsBeforeJobGear(bool issuedFirst)
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitRunTicks(70);

        await Server.WaitAssertion(() =>
        {
            var (body, mind) = SpawnPlayer(map);
            var first = GivePistol(body, map);
            var second = GivePistol(body, map);
            var issued = issuedFirst ? first : second;
            SEntMan.EnsureComponent<FSIssuedWeaponComponent>(issued);

            Assert.That(Sell(body, mind, map), Is.EqualTo(FSSellBack.Refund(500, 0)), "the job copy went instead of the bought one");
            Assert.That(SEntMan.EntityExists(issued), "the job copy was sold");
        });
    }
}
