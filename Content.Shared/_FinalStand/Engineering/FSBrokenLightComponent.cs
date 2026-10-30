using Robust.Shared.GameStates;

namespace Content.Shared._FinalStand.Engineering;

[RegisterComponent, NetworkedComponent]
public sealed partial class FSBrokenLightComponent : Component
{
    [DataField]
    public TimeSpan SparkInterval = TimeSpan.FromSeconds(3);

    [DataField]
    public float RepairDelay = 2f;

    [DataField]
    public float GlowRadius = 1.6f;

    [DataField]
    public float GlowEnergy = 0.55f;

    [DataField]
    public Color GlowColor = Color.FromHex("#FF8A3D");

    public TimeSpan NextSpark;

    public bool Glowing;
}
