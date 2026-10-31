namespace Content.Shared._FinalStand.Upgrades.Effects;

// Left on a wave enemy by the chainsaw's Rip and Tear; pays that player if they finish it.
[RegisterComponent]
public sealed partial class FSPendingKillBonusComponent : Component
{
    // Share of the kill's own credits, paid on top.
    [DataField] public float MoneyPercent = 0f;

    [ViewVariables]
    public EntityUid? Payee;
}
