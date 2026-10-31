#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Shared.Damage.Components;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Temperature.Components;
using Content.Shared.Temperature.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.FinalStand;

// Players take no heat damage from body temperature: burning hurts through the fire itself. Temperature damage
// ignored armour, kept climbing under sustained fire and went on for minutes after the fire was out. Cold still hurts.
[TestFixture]
public sealed class PlayerHeatDamageTest : GameTest
{
    public override PoolSettings PoolSettings => new() { Connected = false, Dirty = true };

    [Test]
    public async Task PlayerSpeciesTakeNoHeatDamageFromTemperature()
    {
        var entMan = Server.ResolveDependency<IEntityManager>();
        var protos = Server.ResolveDependency<IPrototypeManager>();
        var factory = Server.ResolveDependency<IComponentFactory>();

        var species = protos.EnumeratePrototypes<SpeciesPrototype>().Where(s => s.RoundStart).ToList();
        Assert.That(species, Is.Not.Empty);

        Assert.Multiple(() =>
        {
            foreach (var s in species)
            {
                Assert.That(protos.Index(s.Prototype).TryGetComponent<TemperatureDamageComponent>(out var temp, factory),
                    $"{s.Prototype} has no temperature damage");
                Assert.That(temp!.HeatDamage.Empty, $"{s.Prototype} takes heat damage from its temperature");
                Assert.That(temp.ColdDamage.Empty, Is.False, $"{s.Prototype} lost its cold damage");
            }
        });

        // And in play: a body far over its heat threshold takes no more Heat damage than one at a normal temperature.
        var map = await Pair.CreateTestMap();
        EntityUid body = default, control = default;
        await Server.WaitPost(() =>
        {
            control = entMan.SpawnEntity("MobHuman", map.GridCoords);
            body = entMan.SpawnEntity("MobHuman", map.GridCoords);
            entMan.System<SharedTemperatureSystem>().ChangeHeat(body, 1e8f, ignoreHeatResistance: true);
        });
        await Server.WaitRunTicks(90);

        await Server.WaitAssertion(() =>
        {
            Assert.That(entMan.GetComponent<TemperatureComponent>(body).Temperature, Is.GreaterThan(500f),
                "test precondition: the body is hot");
            // The airless test map hurts both bodies alike; only the hot one could add temperature damage.
            float Heat(EntityUid uid) => entMan.GetComponent<DamageableComponent>(uid).Damage.DamageDict
                .GetValueOrDefault("Heat").Float();
            Assert.That(Heat(body), Is.EqualTo(Heat(control)).Within(0.01f), "a hot body took heat damage");
        });
    }
}
