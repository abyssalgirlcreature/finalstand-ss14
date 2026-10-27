namespace Content.Shared._FinalStand.Perks;

public enum FSArchetypeTier : byte
{
    None,
    Archetype,
    Keystone,
    Rainbow,
}

public readonly record struct FSArchetypeBonus(float Damage, float DamageTaken, float Speed, float Credits)
{
    public static readonly FSArchetypeBonus None = new(1f, 1f, 1f, 1f);
}

public readonly record struct FSArchetype(PerkCategory Category, FSArchetypeTier Tier, int Count)
{
    public bool Active => Tier != FSArchetypeTier.None;
}

public static class FSArchetypeRules
{
    public const int ArchetypeCount = 3;
    public const int KeystoneCount = 5;
    public const int RainbowCount = 5;

    public static FSArchetype Resolve(IEnumerable<PerkCategory> slotted)
    {
        var counts = new Dictionary<PerkCategory, int>();
        foreach (var category in slotted)
            counts[category] = counts.GetValueOrDefault(category) + 1;

        if (counts.Count == RainbowCount)
            return new FSArchetype(PerkCategory.Purple, FSArchetypeTier.Rainbow, RainbowCount);

        var best = default(FSArchetype);
        foreach (var (category, count) in counts)
        {
            if (count > best.Count || count == best.Count && category < best.Category)
                best = new FSArchetype(category, TierFor(count), count);
        }

        return best;
    }

    public static FSArchetype Resolve(IEnumerable<string> slottedIds, Func<string, int> level)
    {
        var categories = new List<PerkCategory>();
        foreach (var id in slottedIds)
        {
            if (!string.IsNullOrEmpty(id) && level(id) > 0 && FSPerkDef.All.TryGetValue(id, out var def))
                categories.Add(def.Category);
        }

        return Resolve(categories);
    }

    public static FSArchetypeTier TierFor(int count)
        => count >= KeystoneCount ? FSArchetypeTier.Keystone
            : count >= ArchetypeCount ? FSArchetypeTier.Archetype
            : FSArchetypeTier.None;

    public static FSArchetypeBonus Bonus(FSArchetype archetype)
    {
        if (archetype.Tier == FSArchetypeTier.Rainbow)
            return new FSArchetypeBonus(1.04f, 0.96f, 1.03f, 1.06f);

        var keystone = archetype.Tier == FSArchetypeTier.Keystone;
        return archetype.Tier == FSArchetypeTier.None ? FSArchetypeBonus.None : archetype.Category switch
        {
            PerkCategory.Red => new FSArchetypeBonus(keystone ? 1.2f : 1.08f, 1f, 1f, 1f),
            PerkCategory.Blue => new FSArchetypeBonus(1f, keystone ? 0.8f : 0.92f, 1f, 1f),
            PerkCategory.Green => new FSArchetypeBonus(1f, 1f, keystone ? 1.15f : 1.06f, 1f),
            PerkCategory.Yellow => new FSArchetypeBonus(1f, 1f, 1f, keystone ? 1.3f : 1.12f),
            PerkCategory.Purple => keystone
                ? new FSArchetypeBonus(1.08f, 0.92f, 1.05f, 1.08f)
                : new FSArchetypeBonus(1.03f, 0.97f, 1.02f, 1.03f),
            _ => FSArchetypeBonus.None,
        };
    }

    public static string NameLocId(FSArchetype archetype)
        => archetype.Tier == FSArchetypeTier.Rainbow ? "fs-archetype-prism" : NameLocId(archetype.Category);

    public static string NameLocId(PerkCategory category) => category switch
    {
        PerkCategory.Red => "fs-archetype-slayer",
        PerkCategory.Blue => "fs-archetype-bulwark",
        PerkCategory.Green => "fs-archetype-operator",
        PerkCategory.Yellow => "fs-archetype-tycoon",
        _ => "fs-archetype-specialist",
    };

    public static string CategoryLocId(PerkCategory category) => category switch
    {
        PerkCategory.Red => "fs-archetype-category-offense",
        PerkCategory.Blue => "fs-archetype-category-defense",
        PerkCategory.Green => "fs-archetype-category-utility",
        PerkCategory.Yellow => "fs-archetype-category-economy",
        _ => "fs-archetype-category-hybrid",
    };

    public static string EffectLocId(FSArchetype archetype)
        => $"{NameLocId(archetype)}-{(archetype.Tier == FSArchetypeTier.Keystone ? "keystone" : "effect")}";
}
