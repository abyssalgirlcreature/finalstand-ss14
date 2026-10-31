#nullable enable
using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.Server._FinalStand.Mobs;
using Content.Server.Gravity;
using Content.Shared._FinalStand.Mobs;
using Content.Shared.ActionBlocker;
using Content.Shared.Gravity;
using Content.Shared.Movement.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Player;

namespace Content.IntegrationTests.Tests.FinalStand;

// Clients draw a flamer's fire along the way the zombie faces, while the server burns along its aim. The two came
// apart in two ways: anything that rechecked whether the mob could move (a knockback ending, a stun) let it walk
// mid-attack and turn to face where it went, and the aim was set as a grid-relative rotation, wrong on a rotated grid.
[TestFixture]
public sealed class CasterAimTest : GameTest
{
    public override PoolSettings PoolSettings => new() { Connected = true, Dirty = true };

    [TestCase("FSZombieFlamethrower")]
    [TestCase("FSZombieTesla")]
    public async Task ACasterStaysPutThroughAMovementRecheck(string caster)
    {
        var entMan = Server.ResolveDependency<IEntityManager>();
        var mapSystem = entMan.System<SharedMapSystem>();
        var blocker = entMan.System<ActionBlockerSystem>();
        var players = Server.ResolveDependency<ISharedPlayerManager>();
        var map = await Pair.CreateTestMap();

        EntityUid zombie = default;
        await Server.WaitPost(() =>
        {
            for (var x = -8; x <= 8; x++)
            for (var y = -3; y <= 3; y++)
                mapSystem.SetTile(map.Grid, new Vector2i(x, y), map.Tile.Tile);

            var gravity = entMan.EnsureComponent<GravityComponent>(map.Grid);
            entMan.System<GravitySystem>().EnableGravity(map.Grid, gravity);

            var target = entMan.SpawnEntity("MobHuman", new EntityCoordinates(map.Grid, new Vector2(2.5f, 0.5f)));
            players.SetAttachedEntity(Pair.Player, target, true);
            zombie = entMan.SpawnEntity(caster, new EntityCoordinates(map.Grid, new Vector2(-1.5f, 0.5f)));
        });

        bool Casting() => entMan.TryGetComponent(zombie, out FSFlamethrowerComponent? flamer)
            ? flamer.IsWindingUp || flamer.IsFiring
            : entMan.GetComponent<FSTeslaZombieComponent>(zombie).IsFiring;

        var casting = false;
        for (var i = 0; i < 300 && !casting; i++)
        {
            await Server.WaitRunTicks(1);
            await Server.WaitPost(() => casting = Casting());
        }
        Assert.That(casting, $"{caster} never attacked");

        Vector2 start = default;
        await Server.WaitAssertion(() =>
        {
            Assert.That(entMan.GetComponent<InputMoverComponent>(zombie).CanMove, Is.False, "not held while attacking");

            // What a knockback running out or a stun ending does.
            blocker.UpdateCanMove(zombie);
            Assert.That(entMan.GetComponent<InputMoverComponent>(zombie).CanMove, Is.False,
                "a movement recheck let it walk mid-attack");
            start = entMan.System<SharedTransformSystem>().GetWorldPosition(zombie);
        });

        await Server.WaitRunTicks(10);
        await Server.WaitAssertion(() =>
        {
            if (!Casting())
                return;

            var moved = entMan.System<SharedTransformSystem>().GetWorldPosition(zombie) - start;
            Assert.That(moved.Length(), Is.LessThan(0.1f), "it walked while attacking");
        });
    }

    [Test]
    [TestOf(typeof(FSFlamethrowerSystem))]
    public async Task AFlamerFacesItsAimOnARotatedGrid()
    {
        var entMan = Server.ResolveDependency<IEntityManager>();
        var mapSystem = entMan.System<SharedMapSystem>();
        var xforms = entMan.System<SharedTransformSystem>();
        var players = Server.ResolveDependency<ISharedPlayerManager>();
        var map = await Pair.CreateTestMap();

        EntityUid flamer = default;
        await Server.WaitPost(() =>
        {
            for (var x = -8; x <= 8; x++)
            for (var y = -3; y <= 3; y++)
                mapSystem.SetTile(map.Grid, new Vector2i(x, y), map.Tile.Tile);

            xforms.SetLocalRotation(map.Grid, Angle.FromDegrees(180));
            var gravity = entMan.EnsureComponent<GravityComponent>(map.Grid);
            entMan.System<GravitySystem>().EnableGravity(map.Grid, gravity);

            var target = entMan.SpawnEntity("MobHuman", new EntityCoordinates(map.Grid, new Vector2(2.5f, 0.5f)));
            players.SetAttachedEntity(Pair.Player, target, true);
            flamer = entMan.SpawnEntity("FSZombieFlamethrower", new EntityCoordinates(map.Grid, new Vector2(-1.5f, 0.5f)));
        });

        var firing = false;
        for (var i = 0; i < 300 && !firing; i++)
        {
            await Server.WaitRunTicks(1);
            await Server.WaitPost(() => firing = entMan.GetComponent<FSFlamethrowerComponent>(flamer).IsFiring);
        }
        Assert.That(firing, "the flamer never fired");

        await Server.WaitAssertion(() =>
        {
            var aim = entMan.GetComponent<FSFlamethrowerComponent>(flamer).FiringDirection;
            // A mob's local up is the way it faces.
            var facing = xforms.GetWorldRotation(flamer).RotateVec(Vector2.UnitY);
            Assert.That(Vector2.Dot(facing, aim), Is.GreaterThan(0.95f), $"faces {facing} but aims {aim}");
        });
    }
}
