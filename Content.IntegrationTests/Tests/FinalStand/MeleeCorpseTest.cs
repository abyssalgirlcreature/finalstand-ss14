#nullable enable
using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.Server.NPC.HTN;
using Content.Shared.CombatMode;
using Content.Shared.Damage.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Weapons.Melee;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests.FinalStand;

// Many kills are worked out on the server alone, so a client can swing at a zombie that already died. The server
// takes that swing to the nearest living enemy in front instead of hitting the corpse (the knife's Coup de Grace
// spent its free swing on the body it had just executed).
[TestFixture]
[TestOf(typeof(SharedMeleeWeaponSystem))]
public sealed class MeleeCorpseTest : GameTest
{
    public override PoolSettings PoolSettings => new() { Connected = false, Dirty = true };

    [Test]
    public async Task ASwingAtACorpseHitsTheLivingZombieBesideIt()
    {
        var entMan = Server.ResolveDependency<IEntityManager>();
        var mapSystem = entMan.System<SharedMapSystem>();
        var map = await Pair.CreateTestMap();

        EntityUid user = default, corpse = default, living = default;
        await Server.WaitPost(() =>
        {
            for (var x = -2; x <= 3; x++)
            for (var y = -2; y <= 3; y++)
                mapSystem.SetTile(map.Grid, new Vector2i(x, y), map.Tile.Tile);

            user = entMan.SpawnEntity("MobHuman", new EntityCoordinates(map.Grid, new Vector2(0.5f, 0.5f)));
            corpse = entMan.SpawnEntity("FSZombieNormal", new EntityCoordinates(map.Grid, new Vector2(1.5f, 0.5f)));
            // Off to the side, so the corpse doesn't shadow it from the swing.
            living = entMan.SpawnEntity("FSZombieNormal", new EntityCoordinates(map.Grid, new Vector2(1.4f, 1.0f)));
            entMan.RemoveComponent<HTNComponent>(corpse);
            entMan.RemoveComponent<HTNComponent>(living);

            entMan.System<MobStateSystem>().ChangeMobState(corpse, MobState.Dead);
            entMan.System<SharedCombatModeSystem>().SetInCombatMode(user, true);
        });
        await Server.WaitRunTicks(2);

        float corpseBefore = 0, livingBefore = 0;
        await Server.WaitPost(() =>
        {
            corpseBefore = entMan.GetComponent<DamageableComponent>(corpse).TotalDamage.Float();
            livingBefore = entMan.GetComponent<DamageableComponent>(living).TotalDamage.Float();

            var melee = entMan.System<SharedMeleeWeaponSystem>();
            Assert.That(melee.AttemptLightAttack(user, user, entMan.GetComponent<MeleeWeaponComponent>(user), corpse),
                "test precondition: the swing was made");
        });
        await Server.WaitRunTicks(1);

        await Server.WaitAssertion(() =>
        {
            Assert.That(entMan.GetComponent<DamageableComponent>(corpse).TotalDamage.Float(), Is.EqualTo(corpseBefore),
                "the swing hit the corpse");
            Assert.That(entMan.GetComponent<DamageableComponent>(living).TotalDamage.Float(), Is.GreaterThan(livingBefore),
                "the swing didn't go to the living zombie");
        });
    }
}
