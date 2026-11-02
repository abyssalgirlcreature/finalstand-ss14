#nullable enable
using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.Server._FinalStand.Mobs;
using Content.Server.Gravity;
using Content.Shared._FinalStand.FriendlyFire;
using Content.Shared.Damage.Components;
using Content.Shared.Gravity;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.IntegrationTests.Tests.FinalStand;

// A revenant's bolts pass through bodies. On a floor with gravity they used to slow to a stop on whoever they were
// fired at and hit them again every 0.3 s until they faded: a point-blank volley on a bound player landed two to four
// times over.
[TestFixture]
[TestOf(typeof(FSRevenantBoltSystem))]
public sealed class RevenantBoltTest : GameTest
{
    public override PoolSettings PoolSettings => new() { Connected = false, Dirty = true };

    private const string Bolt = "FSRevenantBoltProjectile";

    [Test]
    public async Task ABoltFliesOnAndHitsItsTargetOnce()
    {
        var entMan = Server.ResolveDependency<IEntityManager>();
        var mapSystem = entMan.System<SharedMapSystem>();
        var map = await Pair.CreateTestMap();

        float Slash(EntityUid uid) => entMan.GetComponent<DamageableComponent>(uid).Damage.DamageDict
            .TryGetValue("Slash", out var slash) ? slash.Float() : 0f;

        EntityUid victim = default, bolt = default;
        float before = 0, damage = 0;
        await Server.WaitPost(() =>
        {
            for (var x = -2; x <= 12; x++)
            for (var y = -2; y <= 2; y++)
                mapSystem.SetTile(map.Grid, new Vector2i(x, y), map.Tile.Tile);

            var gravity = entMan.EnsureComponent<GravityComponent>(map.Grid);
            entMan.System<GravitySystem>().EnableGravity(map.Grid, gravity);

            victim = entMan.SpawnEntity("MobHuman", new EntityCoordinates(map.Grid, new Vector2(1.5f, 0.5f)));
            // Bolts hit wave participants; the wave rule marks players with this.
            entMan.EnsureComponent<FSFriendlyFireComponent>(victim);
            before = Slash(victim);

            // Point blank, as the combo's volley fires: a tile short of the target.
            bolt = entMan.SpawnEntity(Bolt, new EntityCoordinates(map.Grid, new Vector2(0.5f, 0.5f)));
            entMan.System<SharedPhysicsSystem>().SetLinearVelocity(bolt, new Vector2(12f, 0f));
            damage = entMan.GetComponent<FSRevenantBoltComponent>(bolt).Damage;
        });

        await Server.WaitRunTicks(10);
        await Server.WaitAssertion(() =>
        {
            Assert.That(entMan.GetComponent<PhysicsComponent>(bolt).LinearVelocity.Length(), Is.GreaterThan(10f),
                "the bolt slowed down on the floor");
        });

        // Longer than the bolt lives.
        await Server.WaitRunTicks(40);
        await Server.WaitAssertion(() =>
        {
            var taken = Slash(victim) - before;
            Assert.That(taken, Is.GreaterThan(damage * 0.5f), "test precondition: the bolt hit");
            Assert.That(taken, Is.LessThan(damage * 1.5f), "the bolt hit its target more than once");
        });
    }
}
