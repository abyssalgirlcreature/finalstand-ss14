using Content.Server._FinalStand.Effects;
using Content.Shared._FinalStand.Engineering;
using Content.Shared._FinalStand.Particles;
using Content.Shared.Damage.Systems;
using Content.Shared.Interaction;
using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Tools.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._FinalStand.Engineering;

public sealed partial class FSBrokenLightSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private FSFlairSystem _flair = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private PointLightSystem _pointLight = default!;
    [Dependency] private SharedLightBulbSystem _bulb = default!;
    [Dependency] private SharedPoweredLightSystem _poweredLight = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedToolSystem _tool = default!;

    private static readonly ProtoId<FSParticleEmitterPrototype> Sparks = "FSWeldSparks";
    private static readonly string[] RepairQuality = ["Welding"];

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FSBrokenLightComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<FSBrokenLightComponent, FSLightRepairDoAfterEvent>(OnRepaired);
    }

    private bool IsBroken(EntityUid uid)
    {
        return TryComp<PoweredLightComponent>(uid, out var light)
               && _poweredLight.GetBulb(uid, light) is { } bulb
               && TryComp<LightBulbComponent>(bulb, out var bulbComp)
               && bulbComp.State == LightBulbState.Broken;
    }

    private void OnInteractUsing(Entity<FSBrokenLightComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !IsBroken(ent))
            return;

        args.Handled = _tool.UseTool(args.Used, args.User, ent.Owner,
            ent.Comp.RepairDelay, RepairQuality, new FSLightRepairDoAfterEvent());
    }

    private void OnRepaired(Entity<FSBrokenLightComponent> ent, ref FSLightRepairDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || !IsBroken(ent))
            return;

        args.Handled = true;

        if (!TryComp<PoweredLightComponent>(ent, out var light)
            || _poweredLight.GetBulb(ent, light) is not { } bulb)
            return;

        _bulb.SetState(bulb, LightBulbState.Normal);
        _damageable.ClearAllDamage(ent.Owner);
        ent.Comp.Glowing = false;
        _poweredLight.SetState(ent.Owner, light.On, light);
        _popup.PopupEntity(Loc.GetString("fs-light-repaired"), ent, args.User);
    }

    private void SetGlow(Entity<FSBrokenLightComponent> ent, bool on)
    {
        if (ent.Comp.Glowing == on || !TryComp<PointLightComponent>(ent, out var pointLight))
            return;

        ent.Comp.Glowing = on;
        if (!on)
            return;

        _pointLight.SetRadius(ent.Owner, ent.Comp.GlowRadius, pointLight);
        _pointLight.SetEnergy(ent.Owner, ent.Comp.GlowEnergy, pointLight);
        _pointLight.SetColor(ent.Owner, ent.Comp.GlowColor, pointLight);
        _pointLight.SetEnabled(ent.Owner, true, pointLight);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<FSBrokenLightComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            var broken = IsBroken(uid);
            if (!broken)
            {
                comp.Glowing = false;
                continue;
            }

            SetGlow((uid, comp), true);

            if (now < comp.NextSpark)
                continue;

            comp.NextSpark = now + comp.SparkInterval;
            _flair.Burst(Sparks, uid);
        }
    }
}
