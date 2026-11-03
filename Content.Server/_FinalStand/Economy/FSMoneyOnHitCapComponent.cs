namespace Content.Server._FinalStand.Economy;

[RegisterComponent]
[Access(typeof(FSMoneyOnHitSystem), typeof(Content.Server._FinalStand.Weapons.FSHarvesterWeaponSystem))]
public sealed partial class FSMoneyOnHitCapComponent : Component
{
    [DataField] public int MaxMoneyPerPlayer = 200;
    [DataField] public int ResearchCapMultiplier = 1;
    public Dictionary<EntityUid, int> MoneyGivenPerPlayer = new();
    // Fractions of a credit earned but not paid yet, so small hits add up.
    public Dictionary<EntityUid, float> UnpaidPerPlayer = new();
    public Dictionary<EntityUid, int> ResearchGivenPerPlayer = new();
}
