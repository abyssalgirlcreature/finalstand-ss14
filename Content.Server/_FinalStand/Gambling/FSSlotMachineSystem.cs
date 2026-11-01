using Content.Server._FinalStand.Economy;
using Content.Shared._FinalStand.Gambling;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._FinalStand.Gambling;

public sealed partial class FSSlotMachineSystem : EntitySystem
{
    [Dependency] private FSPlayerWalletSystem _wallet = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FSSlotMachineComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
    }

    private void OnGetVerbs(Entity<FSSlotMachineComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;
        foreach (var stake in ent.Comp.Stakes)
        {
            args.Verbs.Add(new AlternativeVerb
            {
                Text = Loc.GetString("fs-slots-verb", ("stake", stake)),
                Priority = -stake,
                Act = () => TryPull(ent, user, stake),
            });
        }
    }

    private void TryPull(Entity<FSSlotMachineComponent> ent, EntityUid user, int stake)
    {
        if (_timing.CurTime < ent.Comp.NextPull)
            return;

        if (!_mind.TryGetMind(user, out var mindId, out _))
            return;

        if (!_wallet.TryDeductCredits(mindId, stake))
        {
            _popup.PopupEntity(Loc.GetString("fs-slots-broke"), ent, user);
            return;
        }

        ent.Comp.NextPull = _timing.CurTime + ent.Comp.Cooldown;
        _audio.PlayPvs(ent.Comp.PullSound, ent);

        if (Roll(ent.Comp.Payouts) is not { } payout)
            return;

        var won = (int) MathF.Round(stake * payout.Multiplier);
        if (won > 0)
            _wallet.AddCredits(mindId, won);

        _popup.PopupEntity(Loc.GetString(payout.Message, ("credits", won)), ent, user, payout.Popup);

        if (payout.Sound != null)
            _audio.PlayPvs(payout.Sound, ent);
    }

    private FSSlotPayout? Roll(List<FSSlotPayout> payouts)
    {
        var total = 0;
        foreach (var payout in payouts)
            total += payout.Weight;

        if (total <= 0)
            return null;

        var roll = _random.Next(total);
        foreach (var payout in payouts)
        {
            roll -= payout.Weight;
            if (roll < 0)
                return payout;
        }

        return payouts[^1];
    }
}
