using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server._FinalStand.Economy;
using Content.Server._FinalStand.Spawners;
using Content.Server.NPC.HTN;
using Content.Shared._FinalStand.Economy;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.FinalStand;

// Hit money is paid per point of damage, so a heavy gun and a light one earn the same for the same zombie. Damage past
// what was left to kill it doesn't pay, and neither does hitting a corpse.
[TestFixture]
[TestOf(typeof(FSMoneyOnHitSystem))]
public sealed class HitMoneyByDamageTest : GameTest
{
    public override PoolSettings PoolSettings => new() { Connected = true, Dirty = true };

    // A normal zombie off the wave spawner: 75 HP, a 450 cap per player.
    private const string Zombie = "FSZombieNormal";
    private const int CreditsPerHp = 2;

    [Test]
    public async Task HitMoneyFollowsDamageDealt()
    {
        var map = await Pair.CreateTestMap();

        await Server.WaitAssertion(() =>
        {
            var minds = SEntMan.System<SharedMindSystem>();
            var damageable = SEntMan.System<DamageableSystem>();
            var wallets = SEntMan.System<FSPlayerWalletSystem>();

            var body = SEntMan.SpawnEntity("MobHuman", map.GridCoords);
            var mind = minds.CreateMind(Pair.Player!.UserId, "Defender");
            minds.TransferTo(mind, body);
            SEntMan.EnsureComponent<FSPlayerWalletComponent>(mind);

            // The zombies here aren't the wave's, so killing one pays nothing and the wallet only moves for hits.
            int HitMoney() => wallets.GetCredits(mind);

            EntityUid Spawn()
            {
                var zombie = SEntMan.SpawnEntity(Zombie, map.GridCoords);
                SEntMan.RemoveComponent<HTNComponent>(zombie);
                SEntMan.RemoveComponent<WaveSpawnedTagComponent>(zombie);
                return zombie;
            }

            int Hit(EntityUid zombie, float amount)
            {
                var before = HitMoney();
                damageable.TryChangeDamage(zombie, new DamageSpecifier { DamageDict = { ["Blunt"] = amount } },
                    ignoreResistances: true, origin: body, ignoreGlobalModifiers: true);
                return HitMoney() - before;
            }

            Assert.Multiple(() =>
            {
                // Five 15s and one 75 kill the same zombie and pay the same.
                var light = Spawn();
                var lightPay = Enumerable.Range(0, 5).Sum(_ => Hit(light, 15));
                Assert.That(SEntMan.System<MobStateSystem>().IsDead(light), "five 15s didn't kill a 75 HP zombie");
                Assert.That(lightPay, Is.EqualTo(75 * CreditsPerHp), "light hits didn't pay per damage");

                var heavy = Spawn();
                Assert.That(Hit(heavy, 75), Is.EqualTo(75 * CreditsPerHp), "the killing hit didn't pay");

                // Overkill pays only for the life that was left; a corpse pays nothing.
                var overkill = Spawn();
                Assert.That(Hit(overkill, 50), Is.EqualTo(50 * CreditsPerHp));
                Assert.That(Hit(overkill, 500), Is.EqualTo(25 * CreditsPerHp), "damage past the zombie's life paid");
                Assert.That(Hit(overkill, 30), Is.Zero, "hitting a corpse paid");

                // Fractions carry over: two 0.75 HP grazes make 3 credits between them.
                var graze = Spawn();
                Assert.That(Hit(graze, 0.75f) + Hit(graze, 0.75f), Is.EqualTo(3), "fractions of a credit were lost");
            });
        });
    }
}
