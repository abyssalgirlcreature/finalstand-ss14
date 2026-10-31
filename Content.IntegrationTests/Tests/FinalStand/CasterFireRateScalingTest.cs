#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.Server._FinalStand.GameTicking.Rules;
using Content.Shared._FinalStand.Mobs;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.FinalStand;

// Late waves speed up flamers and tesla zombies. The speed-up used to start from fixed cooldowns (4 s and 5 s), so
// their prototypes' own 3 s cooldowns were thrown away from wave 16 on and they attacked slower than before.
[TestFixture]
[TestOf(typeof(WaveEnemyScalingSystem))]
public sealed class CasterFireRateScalingTest : GameTest
{
    public override PoolSettings PoolSettings => new() { Connected = false, Dirty = true };

    [Test]
    public async Task WaveScalingSpeedsUpTheCastersOwnCooldown()
    {
        var entMan = Server.ResolveDependency<IEntityManager>();
        var map = await Pair.CreateTestMap();

        await Server.WaitAssertion(() =>
        {
            var scaling = entMan.System<WaveEnemyScalingSystem>();
            var flamer = entMan.SpawnEntity("FSZombieFlamethrower", map.GridCoords);
            var tesla = entMan.SpawnEntity("FSZombieTesla", map.GridCoords);
            var flamerBase = entMan.GetComponent<FSFlamethrowerComponent>(flamer).AttackCooldown;
            var teslaBase = entMan.GetComponent<FSTeslaZombieComponent>(tesla).AttackCooldown;

            // Wave 20 doubles the rate.
            scaling.ScaleEnemyFireRate(flamer, 20);
            scaling.ScaleEnemyFireRate(tesla, 20);

            Assert.Multiple(() =>
            {
                Assert.That(entMan.GetComponent<FSFlamethrowerComponent>(flamer).AttackCooldown,
                    Is.EqualTo(flamerBase / 2f).Within(0.001f), "the flamer's cooldown didn't scale from its own");
                Assert.That(entMan.GetComponent<FSTeslaZombieComponent>(tesla).AttackCooldown,
                    Is.EqualTo(teslaBase / 2f).Within(0.001f), "the tesla zombie's cooldown didn't scale from its own");
            });
        });
    }
}
