using NocturneAnnex.Horror;

namespace NocturneAnnex.Strategy
{
    /// <summary>Applies a server horror suggestion: the gate checks it, then the local event runner plays the matching local event.</summary>
    public sealed class HorrorSuggestionBridge
    {
        public HorrorSuggestionGate Gate { get; } = new HorrorSuggestionGate();

        public bool Apply(HorrorEventRunner runner, string json, float now)
        {
            var suggestion = HorrorSuggestionGate.Parse(json);
            if (runner == null || suggestion == null) return false;
            if (Gate.Check(suggestion, now) != GateResult.Accepted) return false;
            return Gate.IsPause || runner.FireCatalogue(Gate.AcceptedEvent);
        }
    }
}
