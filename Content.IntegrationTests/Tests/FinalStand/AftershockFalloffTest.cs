#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Content.IntegrationTests.Fixtures;
using Content.Server._FinalStand.Melee;
using Content.Server.NPC.HTN;
using Content.Shared._FinalStand.Melee;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Tests.FinalStand;

// The fire axe's Aftershock hits everything around the slam again, but each zombie past the nearest takes less.
[TestFixture]
[TestOf(typeof(FSMeleeJuiceSystem))]
public sealed class AftershockFalloffTest : GameTest
{
    public override PoolSettings PoolSettings => new() { Connected = false };

    private const string Axe = "FireAxe";
    private const string User = "MobHuman";
    private const string Zombie = "FSZombieNormal";

    [Test]
    public async Task EachZombieFurtherOutTakesLess()
    {
        var entMan = Server.ResolveDependency<IEntityManager>();
        var timing = Server.ResolveDependency<IGameTiming>();
        var juice = entMan.System<FSMeleeJuiceSystem>();
        var map = await Pair.CreateTestMap();

        var zombies = new EntityUid[3];
        await Server.WaitPost(() =>
        {
            var user = entMan.SpawnEntity(User, map.GridCoords.Offset(new Vector2(-3f, 0f)));
            var axe = entMan.SpawnEntity(Axe, map.GridCoords);
            Vector2[] offsets = [new(0.6f, 0f), new(0f, 1.2f), new(-1.8f, 0f)];
            for (var i = 0; i < zombies.Length; i++)
            {
                zombies[i] = entMan.SpawnEntity(Zombie, map.GridCoords.Offset(offsets[i]));
                entMan.RemoveComponent<HTNComponent>(zombies[i]);
            }

            var profile = juice.Profile((axe, entMan.GetComponent<FSMeleeJuiceComponent>(axe)), true).Copy();
            profile.StaggerRadius = 3f;
            profile.StaggerDamage = 0.5f;
            var damage = new DamageSpecifier { DamageDict = { ["Slash"] = 20 } };

            var pending = typeof(FSMeleeJuiceSystem).GetNestedType("PendingAftershock", BindingFlags.NonPublic)!;
            var shock = Activator.CreateInstance(pending, user, axe, entMan.System<SharedTransformSystem>().ToMapCoordinates(map.GridCoords), damage, profile, timing.CurTime)!;
            var list = (IList) typeof(FSMeleeJuiceSystem).GetField("_aftershocks", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(juice)!;
            list.Add(shock);
        });

        await Server.WaitRunTicks(2);

        await Server.WaitAssertion(() =>
        {
            var taken = zombies.Select(z => entMan.GetComponent<DamageableComponent>(z).Damage.DamageDict.GetValueOrDefault("Slash").Float()).ToArray();
            Assert.That(taken[0], Is.GreaterThan(0f), "the aftershock missed the nearest zombie");
            Assert.That(taken[1] / taken[0], Is.EqualTo(0.75f).Within(0.02f), "the second zombie didn't take 75% of the first");
            Assert.That(taken[2] / taken[0], Is.EqualTo(0.5625f).Within(0.02f), "the third zombie didn't take 75% of the second");
        });
    }
}
