using System.Collections.Frozen;
using Content.Shared._FinalStand.Command;
using Content.Shared._FinalStand.Deployables;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._FinalStand.Command;

public sealed partial class FSCommandAuraSystem : EntitySystem
{
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedJobSystem _jobs = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private static readonly FrozenDictionary<string, (FSCommandAura Aura, float Radius)> ByJob =
        new Dictionary<string, (FSCommandAura, float)>
        {
            ["Captain"] = (FSCommandAura.DownWithTheShip, 2f),
            ["HeadOfSecurity"] = (FSCommandAura.SecurityTraining, 2f),
            ["ResearchDirector"] = (FSCommandAura.AnalyticalMind, 1f),
            ["ChiefEngineer"] = (FSCommandAura.Reinforced, 1f),
            ["ChiefMedicalOfficer"] = (FSCommandAura.PassiveMedicine, 1f),
            ["Quartermaster"] = (FSCommandAura.ProfitMargin, 1f),
        }.ToFrozenDictionary();

    private static readonly TimeSpan SweepInterval = TimeSpan.FromSeconds(0.5);

    private readonly HashSet<Entity<ActorComponent>> _players = [];
    private readonly HashSet<Entity<FSDeployedByComponent>> _structures = [];
    private readonly List<EntityUid> _stale = [];

    private TimeSpan _nextSweep;

    public bool Covers(EntityUid uid, FSCommandAura aura)
    {
        return TryComp<FSCommandAuraBuffComponent>(uid, out var buff) && buff.Sources.ContainsKey(aura);
    }

    public bool TryGetSourceMind(EntityUid uid, FSCommandAura aura, out EntityUid sourceMind)
    {
        sourceMind = default;
        return TryComp<FSCommandAuraBuffComponent>(uid, out var buff)
               && buff.Sources.TryGetValue(aura, out sourceMind);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextSweep)
            return;
        _nextSweep = _timing.CurTime + SweepInterval;

        var buffs = EntityQueryEnumerator<FSCommandAuraBuffComponent>();
        while (buffs.MoveNext(out var uid, out var buff))
        {
            buff.Sources.Clear();
            _stale.Add(uid);
        }

        var sources = EntityQueryEnumerator<ActorComponent>();
        while (sources.MoveNext(out var uid, out _))
        {
            if (!_mobState.IsAlive(uid)
                || !_mind.TryGetMind(uid, out var mindId, out _)
                || !_jobs.MindTryGetJob(mindId, out var job)
                || !ByJob.TryGetValue(job.ID, out var entry))
                continue;

            Project(uid, mindId, entry.Aura, entry.Radius);
        }

        foreach (var uid in _stale)
        {
            if (TryComp<FSCommandAuraBuffComponent>(uid, out var buff) && buff.Sources.Count == 0)
                RemCompDeferred<FSCommandAuraBuffComponent>(uid);
        }
        _stale.Clear();
    }

    private void Project(EntityUid source, EntityUid sourceMind, FSCommandAura aura, float radius)
    {
        var pos = _transform.GetMapCoordinates(source);

        if (aura == FSCommandAura.Reinforced)
        {
            _structures.Clear();
            _lookup.GetEntitiesInRange(pos, radius, _structures);
            foreach (var (uid, _) in _structures)
                EnsureComp<FSCommandAuraBuffComponent>(uid).Sources[aura] = sourceMind;

            return;
        }

        _players.Clear();
        _lookup.GetEntitiesInRange(pos, radius, _players);
        foreach (var (uid, _) in _players)
        {
            if (uid == source && aura == FSCommandAura.DownWithTheShip)
                continue;

            EnsureComp<FSCommandAuraBuffComponent>(uid).Sources[aura] = sourceMind;
        }
    }
}
