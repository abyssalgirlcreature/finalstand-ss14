using Content.Shared._FinalStand.Deployables;
using Content.Client._FinalStand.Fortifications;
using Content.Shared._FinalStand.Fortifications;
using Robust.Client.GameObjects;

namespace Content.Client._FinalStand.Deployables;

public sealed partial class FSSentryTurretVisualizerSystem : VisualizerSystem<FSSentryTurretComponent>
{
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private const string GunLayer = "gun";
    private const string BaseLayer = "turretBase";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FSSentryTurretComponent, FSUpgradeVisualsChangedEvent>(OnUpgradeVisuals);
    }

    private void OnUpgradeVisuals(Entity<FSSentryTurretComponent> ent, ref FSUpgradeVisualsChangedEvent args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        AppearanceSystem.TryGetData<bool>(ent, FSSentryTurretVisuals.Firing, out var firing);
        ApplyTier((ent.Owner, sprite), CompOrNull<FSUpgradeableComponent>(ent), firing);
    }

    protected override void OnAppearanceChange(EntityUid uid, FSSentryTurretComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        var sprite = (uid, args.Sprite);

        if (AppearanceSystem.TryGetData<double>(uid, FSSentryTurretVisuals.Angle, out var theta, args.Component))
            _sprite.LayerSetRotation(sprite, GunLayer, new Angle(theta) - _transform.GetWorldRotation(uid));

        if (AppearanceSystem.TryGetData<bool>(uid, FSSentryTurretVisuals.Firing, out var firing, args.Component))
            ApplyTier(sprite, CompOrNull<FSUpgradeableComponent>(uid), firing);
    }

    private void ApplyTier(Entity<SpriteComponent?> sprite, FSUpgradeableComponent? upgradeable, bool firing)
    {
        var tier = TierOf(upgradeable);
        var suffix = tier == 1 ? string.Empty : tier.ToString();

        _sprite.LayerSetRsiState(sprite, GunLayer, (firing ? "fire" : "turret") + suffix);
        _sprite.LayerSetRsiState(sprite, BaseLayer, tier == 1 ? "base" : "base2");
    }

    private static int TierOf(FSUpgradeableComponent? upgradeable)
    {
        return upgradeable?.Modules.Count switch
        {
            null or 0 => 1,
            1 or 2 => 2,
            3 or 4 => 3,
            _ => 4,
        };
    }
}
