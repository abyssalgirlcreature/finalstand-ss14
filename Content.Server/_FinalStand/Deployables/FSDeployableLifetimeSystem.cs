using Content.Server._FinalStand.GameTicking.Rules;
using Content.Shared._FinalStand.Deployables;
using Robust.Shared.Timing;

namespace Content.Server._FinalStand.Deployables;

public sealed partial class FSDeployableLifetimeSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private WaveGameRuleSystem _waveRule = default!;

    private const float ScanInterval = 0.5f;

    private float _accumulator;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FSDeployableLifetimeComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, FSDeployableLifetimeComponent comp, MapInitEvent args)
    {
        comp.ExpiresAt = _timing.CurTime + comp.Lifetime;
        Dirty(uid, comp);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _accumulator += frameTime;
        if (_accumulator < ScanInterval)
            return;

        var elapsed = TimeSpan.FromSeconds(_accumulator);
        _accumulator = 0f;

        var prep = _waveRule.GetPrepComponent() != null;
        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<FSDeployableLifetimeComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (prep)
            {
                comp.ExpiresAt += elapsed;
                Dirty(uid, comp);
                continue;
            }

            if (now >= comp.ExpiresAt)
                QueueDel(uid);
        }
    }
}
