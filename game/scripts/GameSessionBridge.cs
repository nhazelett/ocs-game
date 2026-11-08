namespace LinesOfSupport.Game;

// Game-local integration seam, not a shared Sim contract. Replace once Fable's ADR-0005 and view models land.
// The referenced Sim project is still empty: never invent authoritative state or advance the displayed day here.
internal sealed class GameSessionBridge
{
    internal int Day => 0;
    internal int? ShopHours => null;
    internal long? CashCents => null;

    internal bool RequestNextDay() => false;
}
