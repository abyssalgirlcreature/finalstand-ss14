using Content.Shared._FinalStand.SmartReload;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._FinalStand.SmartReload;

public sealed partial class FSReloadReadinessSystem : EntitySystem
{
    [Dependency] private FSSharedMagazineReloadSystem _magazine = default!;
    [Dependency] private FSSmartReloadSystem _reload = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedHandsSystem _hands = default!;

    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(0.2);

    private TimeSpan _nextSweep;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HandsComponent, DidEquipHandEvent>((uid, _, _) => Refresh(uid));
        SubscribeLocalEvent<HandsComponent, DidUnequipHandEvent>((uid, _, _) => Refresh(uid));
        SubscribeLocalEvent<FSGunReloadedEvent>(OnReloaded);
    }

    private void OnReloaded(ref FSGunReloadedEvent ev)
    {
        Refresh(ev.User);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextSweep)
            return;
        _nextSweep = _timing.CurTime + Interval;

        var query = EntityQueryEnumerator<ActorComponent, HandsComponent>();
        while (query.MoveNext(out var uid, out _, out _))
            Refresh(uid);
    }

    private void Refresh(EntityUid user)
    {
        if (!_hands.TryGetActiveItem(user, out var held)
            || held is not { } gun
            || !_magazine.IsMagazineFed(gun))
        {
            Clear(user);
            return;
        }

        var multiplier = _reload.GetReloadMultiplier(user, gun)
                         * (CompOrNull<FSReloadProfileComponent>(gun)?.TimeMultiplier ?? 1f);

        var magazine = _reload.FindBestMagazine(user, gun);
        var ready = magazine != null
                    && (!_reload.TryGetMagazineCount(magazine.Value, out var count) || count > 0);

        var comp = EnsureComp<FSReloadSpeedComponent>(user);
        var eject = FSReloadTimes.MagEject * multiplier;
        var insert = FSReloadTimes.MagInsert * multiplier;

        if (comp.MagazineReady == ready && comp.MagEject == eject && comp.MagInsert == insert)
            return;

        comp.MagazineReady = ready;
        comp.MagEject = eject;
        comp.MagInsert = insert;
        Dirty(user, comp);
    }

    private void Clear(EntityUid user)
    {
        if (!TryComp<FSReloadSpeedComponent>(user, out var comp) || !comp.MagazineReady)
            return;

        comp.MagazineReady = false;
        Dirty(user, comp);
    }
}
