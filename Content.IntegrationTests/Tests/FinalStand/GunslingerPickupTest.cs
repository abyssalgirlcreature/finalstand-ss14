#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.Server._FinalStand.Perks;
using Content.Shared._FinalStand.Perks;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Mind;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.FinalStand;

// A gun's fire rate keeps the perks of whoever held it when it last refreshed. Starter and bought pistols refreshed
// on the floor, with no holder, and pistols are never wielded, so Gunslinger never reached them; slotting or
// unslotting the perk mid-round didn't change a gun already in hand either.
[TestFixture]
[TestOf(typeof(FSPerkBuffSystem))]
public sealed class GunslingerPickupTest : GameTest
{
    public override PoolSettings PoolSettings => new() { Connected = true, Dirty = true };

    private static readonly EntProtoId Pistol = "WeaponPistolMk58";
    private const string Gunslinger = "Gunslinger";

    [Test]
    public async Task APistolTakesGunslingerWhenDrawnAndDropsItWhenUnslotted()
    {
        var map = await Pair.CreateTestMap();

        await Server.WaitAssertion(() =>
        {
            var minds = SEntMan.System<SharedMindSystem>();
            var hands = SEntMan.System<SharedHandsSystem>();

            var body = SEntMan.SpawnEntity("MobHuman", map.GridCoords);
            var mindId = minds.CreateMind(ServerSession!.UserId, "Gunslinger");
            minds.TransferTo(mindId, body, mind: SEntMan.GetComponent<MindComponent>(mindId));
            var perks = SEntMan.EnsureComponent<FSPerkLevelsComponent>(mindId);
            perks.Levels[Gunslinger] = 1;
            perks.Slots[0] = Gunslinger;
            perks.Loaded = true;
            perks.Invalidate();

            // Spawned like a bought gun: on the floor first.
            var gun = SEntMan.SpawnEntity(Pistol, map.GridCoords);
            var plain = SEntMan.GetComponent<GunComponent>(gun).FireRateModified;
            var boosted = plain * (1f + FSPerkBonusConstants.GunslingerFireRatePerLevel);

            Assert.That(hands.TryPickupAnyHand(body, gun), "test precondition: the pistol was picked up");
            Assert.That(SEntMan.GetComponent<GunComponent>(gun).FireRateModified, Is.EqualTo(boosted).Within(0.001f),
                "the drawn pistol didn't take Gunslinger");

            perks.Slots[0] = string.Empty;
            perks.Invalidate();
            var changed = new FSPerksChangedEvent(mindId, perks);
            SEntMan.EventBus.RaiseLocalEvent(body, ref changed);
            Assert.That(SEntMan.GetComponent<GunComponent>(gun).FireRateModified, Is.EqualTo(plain).Within(0.001f),
                "the held pistol kept Gunslinger after it was unslotted");
        });
    }
}
