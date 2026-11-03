using Content.Client.Inventory;
using Content.Shared._FinalStand.Transmog;
using Content.Shared.Clothing;
using Robust.Client.GameObjects;

namespace Content.Client._FinalStand.Transmog;

public sealed partial class FSTransmogVisualsSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EquipmentVisualsUpdatedEvent>(OnVisualsUpdated);
        SubscribeLocalEvent<FSTransmogComponent, AfterAutoHandleStateEvent>(OnState);
    }

    private void OnVisualsUpdated(EquipmentVisualsUpdatedEvent args)
    {
        if (TryComp<FSTransmogComponent>(args.Equipee, out var transmog) && transmog.Hidden.Contains(args.Slot))
            SetLayers(args.Equipee, args.RevealedLayers, false);
    }

    private void OnState(Entity<FSTransmogComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (!TryComp<InventorySlotsComponent>(ent, out var slots))
            return;

        foreach (var (slot, keys) in slots.VisualLayerKeys)
            SetLayers(ent, keys, !ent.Comp.Hidden.Contains(slot));
    }

    private void SetLayers(EntityUid uid, HashSet<string> keys, bool visible)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        foreach (var key in keys)
        {
            if (_sprite.LayerMapTryGet((uid, sprite), key, out var index, false))
                _sprite.LayerSetVisible((uid, sprite), index, visible);
        }
    }
}
