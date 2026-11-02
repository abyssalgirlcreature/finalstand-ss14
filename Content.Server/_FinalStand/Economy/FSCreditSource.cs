namespace Content.Server._FinalStand.Economy;

// Where credits came from. Everything above Sale is an earning (GiveCredits).
public enum FSCreditSource : byte
{
    Kill,
    Assist,
    SecurityBonus,
    Hit,
    WaveClear,
    PrepSkip,
    Objective,
    CatchUp,
    Research,
    Medical,
    Engineering,
    Mentor,
    Perk,
    KillBonus,

    // Credits that aren't earnings (AddCredits).
    Sale,
    Refund,
    Transfer,
    Admin,
}

public enum FSPurchaseKind : byte
{
    Weapon,
    Upgrade,
    Armor,
    Bag,
    MysteryBox,
    Gacha,
    Respawn,
    Transfer,
}

// What a charge paid for. Id is the shop, upgrade, tier or item prototype it was bought from.
public readonly record struct FSPurchase(FSPurchaseKind Kind, string Id, int Level = 0);
