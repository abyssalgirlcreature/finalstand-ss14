using Content.Shared.GameTicking;
using Content.Shared._FinalStand.Transmog;
using Content.Shared.Actions;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;

namespace Content.Server._FinalStand.Transmog;

public sealed partial class FSTransmogSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    private static readonly EntProtoId ActionProto = "FSTransmogAction";
    private static readonly string[] Slots = ["outerClothing", "head"];

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FSTransmogActionEvent>(OnToggle);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnSpawn);
    }

    private void OnSpawn(PlayerSpawnCompleteEvent ev)
    {
        if (!HasComp<InventoryComponent>(ev.Mob))
            return;

        EnsureComp<FSTransmogComponent>(ev.Mob);
        _actions.AddAction(ev.Mob, ActionProto);
    }

    private void OnToggle(FSTransmogActionEvent args)
    {
        if (args.Handled || !TryComp<FSTransmogComponent>(args.Performer, out var transmog))
            return;

        args.Handled = true;

        var hide = transmog.Hidden.Count == 0;
        transmog.Hidden.Clear();
        if (hide)
        {
            foreach (var slot in Slots)
                transmog.Hidden.Add(slot);
        }

        Dirty(args.Performer, transmog);
        _popup.PopupEntity(Loc.GetString(hide ? "fs-transmog-hidden" : "fs-transmog-shown"),
            args.Performer, args.Performer);
    }
}
