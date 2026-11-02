namespace Content.Shared._FinalStand.Shop;

// What a weapon shop pays for a weapon handed back: a share of its price and of what was spent upgrading it,
// so moving up to a better gun costs little. Shared so the sell button shows what the server pays.
public static class FSSellBack
{
    public const float Share = 0.7f;

    public static int Refund(int price, int upgradeSpend)
    {
        var raw = (price + upgradeSpend) * Share;
        return Math.Max(0, (int) Math.Round(raw / 50.0) * 50);
    }
}
