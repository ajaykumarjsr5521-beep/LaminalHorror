using System.Text;

namespace NocturneAnnex.Strategy
{
    /// <summary>Builds the AI debug panel text. Pure, so it is testable. The panel reads state only; it changes nothing.</summary>
    public static class StrategyDebugText
    {
        public static string Build(StrategyExecutor ex, StrategyClient client, float now, string entityState = "-", float suspicion = 0f, float detection = 0f, string target = "-", string lastHorrorEvent = "-")
        {
            var sb = new StringBuilder(256);
            sb.AppendLine("REAL-TIME AI");
            sb.Append("State: ").AppendLine(entityState);
            sb.Append("Suspicion: ").AppendLine(suspicion.ToString("0.00"));
            sb.Append("Player Detection: ").AppendLine(detection.ToString("0.00"));
            sb.Append("Target: ").AppendLine(target);
            if (ex.HasActive)
            {
                sb.Append("Strategy: ").AppendLine(ex.Active.Strategy.ToString());
                sb.Append("Strategy Remaining: ").Append(ex.Remaining(now).ToString("0")).AppendLine(" s");
            }
            else sb.AppendLine("Strategy: none (baseline AI)");
            sb.AppendLine();
            sb.AppendLine("STRATEGIC AI");
            sb.Append("Last Decision: ").AppendLine(ex.HasActive ? ex.Active.Strategy.ToString() : "-");
            sb.Append("Confidence: ").AppendLine(ex.HasActive ? ex.Active.Confidence.ToString("0.00") : "-");
            sb.Append("Reason: ").AppendLine(ex.HasActive && !string.IsNullOrEmpty(ex.Active.ReasonCode) ? ex.Active.ReasonCode : "-");
            sb.Append("Requests / Applied / Rejected: ").Append(client.RequestsSent).Append(" / ").Append(client.CommandsApplied).Append(" / ").AppendLine(client.CommandsRejected.ToString());
            sb.Append("Last Error: ").AppendLine(string.IsNullOrEmpty(client.LastError) ? "-" : client.LastError);
            sb.Append("Horror Event: ").AppendLine(lastHorrorEvent);
            sb.Append("LLM/Server: ").Append(client.State.ToString().ToUpperInvariant());
            return sb.ToString();
        }
    }
}
