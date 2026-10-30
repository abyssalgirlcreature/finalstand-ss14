using Robust.Shared.Audio;

namespace Content.Shared._FinalStand.Gambling;

[RegisterComponent]
public sealed partial class FSSlotMachineComponent : Component
{
    [DataField]
    public List<int> Stakes = [1000, 10000, 50000];

    [DataField]
    public List<FSSlotPayout> Payouts = new();

    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(2);

    [DataField]
    public SoundSpecifier PullSound = new SoundPathSpecifier("/Audio/Machines/machine_vend.ogg");

    public TimeSpan NextPull;
}

[DataDefinition]
public sealed partial class FSSlotPayout
{
    [DataField(required: true)]
    public int Weight;

    [DataField(required: true)]
    public float Multiplier;

    [DataField(required: true)]
    public LocId Message;

    [DataField]
    public SoundSpecifier? Sound;
}
