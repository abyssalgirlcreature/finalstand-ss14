#nullable enable
using System.Numerics;
using System.Reflection;
using Content.IntegrationTests.Fixtures;
using Content.Server._FinalStand.Mobs;
using Content.Server._FinalStand.Upgrades.Effects;
using Content.Server.NPC.HTN;
using Content.Shared._FinalStand.Mobs;
using Content.Shared.Damage.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests.FinalStand;

[TestFixture]
[TestOf(typeof(FSRevenantSystem))]
public sealed class RevenantStunTest : GameTest
{
    public override PoolSettings PoolSettings => new() { Connected = false, Dirty = true };

    private async Task<(EntityUid Revenant, EntityUid Victim)> Setup()
    {
        var entMan = Server.ResolveDependency<IEntityManager>();
        var mapSystem = entMan.System<SharedMapSystem>();
        var map = await Pair.CreateTestMap();

        EntityUid revenant = default, victim = default;
        await Server.WaitPost(() =>
        {
            for (var x = -2; x <= 3; x++)
            for (var y = -2; y <= 3; y++)
                mapSystem.SetTile(map.Grid, new Vector2i(x, y), map.Tile.Tile);

            revenant = entMan.SpawnEntity("FSZombieRevenant", new EntityCoordinates(map.Grid, new Vector2(0.5f, 0.5f)));
            victim = entMan.SpawnEntity("MobHuman", new EntityCoordinates(map.Grid, new Vector2(1.5f, 0.5f)));
            entMan.RemoveComponent<HTNComponent>(revenant);
        });
        await Server.WaitRunTicks(1);
        return (revenant, victim);
    }

    private static void WindUpExecute(FSRevenantComponent comp, EntityUid victim)
    {
        comp.IsExecuting = true;
        comp.ExecuteWindupAccum = 0f;
        comp.ExecuteTarget = victim;
    }

    // A stun (concussion rounds, a flashlight) used to leave the execute wind-up running, so it still landed.
    [Test]
    public async Task AStunBreaksAnExecuteWindUp()
    {
        var entMan = Server.ResolveDependency<IEntityManager>();
        var (revenant, victim) = await Setup();

        await Server.WaitPost(() =>
        {
            WindUpExecute(entMan.GetComponent<FSRevenantComponent>(revenant), victim);
            Assert.That(entMan.System<FSStunOverrideSystem>().TryForceStun(revenant, TimeSpan.FromSeconds(2)),
                "test precondition: the revenant was stunned");
        });
        await Server.WaitRunTicks(30);

        await Server.WaitAssertion(() =>
        {
            var comp = entMan.GetComponent<FSRevenantComponent>(revenant);
            Assert.That(comp.IsExecuting, Is.False, "still winding up while stunned");
            Assert.That(entMan.GetComponent<DamageableComponent>(victim).TotalDamage.Float(),
                Is.LessThan(comp.ExecuteDamage / 2f), "the execute landed through the stun");
        });
    }

    // The flashlight makes it retreat; the retreat has to drop the execute as well.
    [Test]
    public async Task ARetreatDropsAnExecuteWindUp()
    {
        var entMan = Server.ResolveDependency<IEntityManager>();
        var (revenant, victim) = await Setup();

        await Server.WaitPost(() =>
        {
            WindUpExecute(entMan.GetComponent<FSRevenantComponent>(revenant), victim);
            entMan.System<FSRevenantSystem>().ForceRetreat(revenant);
        });
        await Server.WaitRunTicks(30);

        await Server.WaitAssertion(() =>
        {
            var comp = entMan.GetComponent<FSRevenantComponent>(revenant);
            Assert.That(entMan.GetComponent<DamageableComponent>(victim).TotalDamage.Float(),
                Is.LessThan(comp.ExecuteDamage / 2f), "the execute landed after the retreat");
        });
    }

    // Its combo holds the victim for every hit, so wave-scaled hits killed inside the grab from wave 20 on.
    [Test]
    public async Task ASliceIgnoresWaveDamageScaling()
    {
        var entMan = Server.ResolveDependency<IEntityManager>();
        var (revenant, victim) = await Setup();

        await Server.WaitAssertion(() =>
        {
            entMan.EnsureComponent<FSWaveDamageScaleComponent>(revenant).MeleeDamageMultiplier = 3f;
            var comp = entMan.GetComponent<FSRevenantComponent>(revenant);
            var system = entMan.System<FSRevenantSystem>();
            var before = entMan.GetComponent<DamageableComponent>(victim).TotalDamage.Float();

            typeof(FSRevenantSystem).GetMethod("DoVerticalSlice", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(system, [revenant, comp, victim, entMan.System<SharedTransformSystem>().GetMapCoordinates(victim)]);

            var dealt = entMan.GetComponent<DamageableComponent>(victim).TotalDamage.Float() - before;
            Assert.That(dealt, Is.EqualTo(comp.SliceDamage).Within(0.5f), "the slice was wave-scaled");
        });
    }
}
