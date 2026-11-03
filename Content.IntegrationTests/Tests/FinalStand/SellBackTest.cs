using Content.IntegrationTests.Fixtures;
using Content.Server._FinalStand.Economy;
using Content.Server._FinalStand.Shop;
using Content.Shared._FinalStand.Economy;
using Content.Shared._FinalStand.Shop;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Mind;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.FinalStand;

// Selling a weapon back to its shop returns 70% of its price and of what was spent upgrading it.
[TestFixture]
[TestOf(typeof(FSShopWeaponSystem))]
public sealed class SellBackTest : GameTest
{
    // Selling needs a mind that belongs to a player.
    public override PoolSettings PoolSettings => new() { Connected = true, Dirty = true };

    [Test]
    [TestCase(0, 350)]
    [TestCase(1000, 1050)]
    public async Task SellingPaysSeventyPercent(int upgradeSpend, int expected)
    {
        var map = await Pair.CreateTestMap();

        await Server.WaitAssertion(() =>
        {
            var minds = SEntMan.System<SharedMindSystem>();
            var wallets = SEntMan.System<FSPlayerWalletSystem>();

            var body = SEntMan.SpawnEntity("MobHuman", map.GridCoords);
            var mind = minds.CreateMind(Pair.Player!.UserId);
            minds.TransferTo(mind, body);
            SEntMan.EnsureComponent<FSPlayerWalletComponent>(mind);

            // The Viper's shop sells it for 500.
            var weapon = SEntMan.SpawnEntity("WeaponPistolViper", map.GridCoords);
            SEntMan.EnsureComponent<FSWeaponUpgradeStateComponent>(weapon).TotalSpent = upgradeSpend;
            Assert.That(SEntMan.System<SharedHandsSystem>().TryPickupAnyHand(body, weapon), "couldn't hand over the pistol");
            var shop = SEntMan.SpawnEntity("FSWeaponShopViper", map.GridCoords);
            Assert.That(SEntMan.GetComponent<FSShopWeaponComponent>(shop).Price, Is.EqualTo(500));

            var before = wallets.GetCredits(mind);
            var sell = new FSShopSellMessage
            {
                Actor = body,
                Entity = SEntMan.GetNetEntity(shop),
                UiKey = FSShopWeaponUiKey.Key,
            };
            SEntMan.EventBus.RaiseLocalEvent(shop, (object) sell, true);

            Assert.That(wallets.GetCredits(mind) - before, Is.EqualTo(expected));
        });
    }
}
