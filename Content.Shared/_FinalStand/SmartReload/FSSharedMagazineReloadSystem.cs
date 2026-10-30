using Content.Shared._FinalStand.Utility;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Shared._FinalStand.SmartReload;

public sealed partial class FSSharedMagazineReloadSystem : EntitySystem
{
    [Dependency] private ItemSlotsSystem _slots = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeAllEvent<FSReloadRequestEvent>(OnReloadRequest);
    }

    public bool IsMagazineFed(EntityUid gun)
    {
        return !HasComp<RevolverAmmoProviderComponent>(gun)
               && (HasComp<MagazineAmmoProviderComponent>(gun) || HasComp<ChamberMagazineAmmoProviderComponent>(gun));
    }

    private void OnReloadRequest(FSReloadRequestEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } user)
            return;

        var gun = GetEntity(ev.Gun);
        if (!gun.IsValid()
            || _mobState.IsIncapacitated(user)
            || !HasComp<GunComponent>(gun)
            || !_hands.IsHolding(user, gun)
            || !IsMagazineFed(gun))
            return;

        if (!TryComp<FSReloadSpeedComponent>(user, out var speed) || !speed.MagazineReady)
        {
            _popup.PopupClient("No compatible magazine found.", gun, user);
            return;
        }

        var hasMag = FSItemSlots.TryGetSlot(EntityManager, _slots, gun, SharedGunSystem.MagazineSlot, out var slot)
                     && slot!.Item != null;
        var delay = hasMag ? speed.MagEject + speed.MagInsert : speed.MagInsert;
        if (delay <= TimeSpan.Zero)
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager, user, delay, new FSMagReloadDoAfterEvent(), eventTarget: gun)
        {
            NeedHand           = true,
            BreakOnMove        = false,
            BreakOnDamage      = false,
            BreakOnHandChange  = true,
            BlockDuplicate     = true,
            DuplicateCondition = DuplicateConditions.SameEvent | DuplicateConditions.SameTarget,
        };

        if (!_doAfter.TryStartDoAfter(doAfterArgs))
            return;

        if (!TryComp<FSReloadProfileComponent>(gun, out var profile) || profile.Sound is not { } sound)
            return;

        var baseline = hasMag ? FSReloadTimes.MagEject + FSReloadTimes.MagInsert : FSReloadTimes.MagInsert;
        var pitch = Math.Clamp((float) (baseline / delay), 1f, 2f);
        profile.Playing = _audio.PlayPredicted(sound, gun, user, AudioParams.Default.WithPitchScale(pitch))?.Entity;
    }
}

public static class FSReloadTimes
{
    public static readonly TimeSpan MagEject = TimeSpan.FromSeconds(0.25);
    public static readonly TimeSpan MagInsert = TimeSpan.FromSeconds(0.55);
}
