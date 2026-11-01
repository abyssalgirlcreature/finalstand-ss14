#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.FinalStand;

// Lung prototypes list their gas solution and LungSystem also ensures it at MapInit. When the lung's handler ran
// first, the solution manager added a second "Lung" solution next to it (an error per lung on a live server).
[TestFixture]
[TestOf(typeof(LungSystem))]
public sealed class LungSolutionTest : GameTest
{
    public override PoolSettings PoolSettings => new() { Connected = false, Dirty = true };

    // Bodies whose lungs come from the organ fill: a player species, the gas sacs that errored live, an animal.
    private static readonly EntProtoId[] Bodies = ["MobHuman", "MobSlimePerson", "MobMouse"];

    [Test]
    public async Task LungsGetOneGasSolution()
    {
        var server = Pair.Server;
        var entMan = server.ResolveDependency<IEntityManager>();
        var protos = server.ResolveDependency<IPrototypeManager>();
        var factory = server.ResolveDependency<IComponentFactory>();
        var map = await Pair.CreateTestMap();

        var lungs = protos.EnumeratePrototypes<EntityPrototype>()
            .Where(p => !p.Abstract && p.TryGetComponent<LungComponent>(out _, factory))
            .Select(p => new EntProtoId(p.ID))
            .ToList();
        Assert.That(lungs, Is.Not.Empty, "found no lung prototypes");

        var spawned = new List<EntityUid>();
        await server.WaitPost(() =>
        {
            foreach (var id in lungs.Concat(Bodies))
            {
                spawned.Add(entMan.SpawnEntity(id, map.GridCoords));
            }
        });
        await server.WaitRunTicks(1);

        await server.WaitAssertion(() =>
        {
            var containers = entMan.System<SharedContainerSystem>();
            var checkedLungs = 0;

            void Check(EntityUid uid)
            {
                if (!entMan.TryGetComponent(uid, out LungComponent? lung))
                    return;

                checkedLungs++;
                var manager = entMan.GetComponent<SolutionManagerComponent>(uid);
                var matching = containers.TryGetContainer(uid, manager.Container, out var container)
                    ? container.ContainedEntities.Count(e =>
                        entMan.TryGetComponent(e, out SolutionComponent? solution) && solution.Id == lung.SolutionName)
                    : 0;
                Assert.That(matching, Is.EqualTo(1),
                    $"{entMan.ToPrettyString(uid)} has {matching} \"{lung.SolutionName}\" solutions");
            }

            Assert.Multiple(() =>
            {
                foreach (var uid in spawned)
                {
                    Check(uid);
                    if (!containers.TryGetContainer(uid, BodyComponent.ContainerID, out var organs))
                        continue;

                    foreach (var organ in organs.ContainedEntities)
                    {
                        Check(organ);
                    }
                }
            });

            // The species and the mouse each brought their own lungs.
            Assert.That(checkedLungs, Is.GreaterThanOrEqualTo(lungs.Count + Bodies.Length));
        });
    }
}
