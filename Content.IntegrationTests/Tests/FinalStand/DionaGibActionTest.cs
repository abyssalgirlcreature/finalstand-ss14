#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Shared.Actions;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Species;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.FinalStand;

// A Diona can only gib itself while dead. Revived through crit and then healed, it used to remove the action a
// second time and log an error, so every revived Diona did (and the rookie revive test failed when it rolled one).
[TestFixture]
[TestOf(typeof(GibActionSystem))]
public sealed class DionaGibActionTest : GameTest
{
    public override PoolSettings PoolSettings => new() { Connected = false, Dirty = true };

    private static readonly EntProtoId Diona = "MobDiona";
    private static readonly EntProtoId GibAction = "DionaGibAction";

    [Test]
    public async Task ARevivedDionaLosesTheGibActionQuietly()
    {
        var server = Pair.Server;
        var entMan = server.ResolveDependency<IEntityManager>();
        var mobState = entMan.System<MobStateSystem>();
        var actions = entMan.System<SharedActionsSystem>();
        var map = await Pair.CreateTestMap();

        EntityUid diona = default;
        await server.WaitPost(() => diona = entMan.SpawnEntity(Diona, map.GridCoords));
        await server.WaitRunTicks(1);

        bool CanGib() => actions.GetActions(diona)
            .Any(a => entMan.GetComponent<MetaDataComponent>(a.Owner).EntityPrototype?.ID == GibAction.Id);

        await server.WaitAssertion(() =>
        {
            Assert.That(CanGib(), Is.False, "a living Diona can gib itself");

            mobState.ChangeMobState(diona, MobState.Dead);
            Assert.That(CanGib(), "a dead Diona can't gib itself");

            mobState.ChangeMobState(diona, MobState.Critical);
            mobState.ChangeMobState(diona, MobState.Alive);
            Assert.That(CanGib(), Is.False, "a revived Diona kept the gib action");

            mobState.ChangeMobState(diona, MobState.Dead);
            Assert.That(CanGib(), "the gib action didn't come back on the next death");
        });
    }
}
