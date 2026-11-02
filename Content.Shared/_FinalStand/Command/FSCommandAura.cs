namespace Content.Shared._FinalStand.Command;

public enum FSCommandAura : byte
{
    DownWithTheShip,
    SecurityTraining,
    AnalyticalMind,
    Reinforced,
    PassiveMedicine,
    ProfitMargin,
}

public static class FSCommandAuraText
{
    public static string NameLocId(FSCommandAura aura) => $"fs-command-aura-{Key(aura)}";

    public static string EffectLocId(FSCommandAura aura) => $"fs-command-aura-{Key(aura)}-effect";

    private static string Key(FSCommandAura aura) => aura switch
    {
        FSCommandAura.DownWithTheShip => "down-with-the-ship",
        FSCommandAura.SecurityTraining => "security-training",
        FSCommandAura.AnalyticalMind => "analytical-mind",
        FSCommandAura.Reinforced => "reinforced",
        FSCommandAura.PassiveMedicine => "passive-medicine",
        FSCommandAura.ProfitMargin => "profit-margin",
        _ => throw new ArgumentOutOfRangeException(nameof(aura)),
    };
}

public static class FSCommandAuraRules
{
    public const float SecurityTrainingDamage = 0.06f;
    public const float SecurityTrainingResist = 0.10f;
    public const float ReinforcedResist = 0.10f;
    public const float ProfitMarginCredits = 0.10f;
    public const float PassiveMedicineHeal = 1f;
    public const float PassiveMedicineSeconds = 3f;
    public const float AnalyticalMindResearchPerDamage = 0.05f;
}
