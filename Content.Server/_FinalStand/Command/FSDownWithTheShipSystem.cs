using Content.Shared._FinalStand.Command;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;

namespace Content.Server._FinalStand.Command;

public sealed partial class FSDownWithTheShipSystem : EntitySystem
{
    [Dependency] private FSCommandAuraSystem _auras = default!;
    [Dependency] private MobThresholdSystem _thresholds = default!;

    private readonly HashSet<EntityUid> _covered = [];
    private readonly List<EntityUid> _dropped = [];

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FSCommandAuraBuffComponent, UpdateMobStateEvent>(OnUpdateMobState,
            after: [typeof(MobThresholdSystem)]);
    }

    private void OnUpdateMobState(Entity<FSCommandAuraBuffComponent> ent, ref UpdateMobStateEvent args)
    {
        if (args.State == MobState.Critical && ent.Comp.Sources.ContainsKey(FSCommandAura.DownWithTheShip))
            args.State = MobState.Alive;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var uid in _covered)
        {
            if (!_auras.Covers(uid, FSCommandAura.DownWithTheShip))
                _dropped.Add(uid);
        }

        foreach (var uid in _dropped)
        {
            _covered.Remove(uid);
            if (!TerminatingOrDeleted(uid))
                _thresholds.VerifyThresholds(uid);
        }
        _dropped.Clear();

        var query = EntityQueryEnumerator<FSCommandAuraBuffComponent>();
        while (query.MoveNext(out var uid, out var buff))
        {
            if (buff.Sources.ContainsKey(FSCommandAura.DownWithTheShip))
                _covered.Add(uid);
        }
    }
}
