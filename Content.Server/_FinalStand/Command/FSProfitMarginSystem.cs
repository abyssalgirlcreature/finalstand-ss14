using Content.Shared._FinalStand.Command;
using Content.Shared.Mind;

namespace Content.Server._FinalStand.Command;

public sealed partial class FSProfitMarginSystem : EntitySystem
{
    [Dependency] private FSCommandAuraSystem _auras = default!;
    [Dependency] private SharedMindSystem _mind = default!;

    public float Multiplier(EntityUid earner)
    {
        return _auras.Covers(earner, FSCommandAura.ProfitMargin)
            ? 1f + FSCommandAuraRules.ProfitMarginCredits
            : 1f;
    }

    public float MultiplierForMind(EntityUid mindId)
    {
        return TryComp<MindComponent>(mindId, out var mind) && mind.CurrentEntity is { } body
            ? Multiplier(body)
            : 1f;
    }
}
