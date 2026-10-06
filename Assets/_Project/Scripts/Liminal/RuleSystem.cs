using System;
using System.Collections.Generic;

namespace NocturneAnnex.Liminal
{
    /// <summary>A rule of the place, e.g. "do not turn around when you hear footsteps". Violated while its forbidden flag is true.</summary>
    public class Rule
    {
        public readonly string Id;
        public readonly string ForbiddenFlag;

        public Rule(string id, string forbiddenFlag)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("A rule needs an id.", nameof(id));
            if (string.IsNullOrEmpty(forbiddenFlag)) throw new ArgumentException("A rule needs a flag to watch.", nameof(forbiddenFlag));
            Id = id;
            ForbiddenFlag = forbiddenFlag;
        }
    }

    /// <summary>
    /// Watches named flags the game sets (PlayerTurnedAround, EnteredElevator, LightsBlue...) and reports a violation once when a rule's
    /// flag turns true. It reports again only after the flag went false. A rule can be replaced late in a level, but only if the change
    /// was announced to the player first.
    /// </summary>
    public class RuleSystem
    {
        readonly List<Rule> _rules = new List<Rule>();
        readonly HashSet<string> _active = new HashSet<string>();

        public IReadOnlyList<Rule> Rules => _rules;

        public event Action<string> Violated;
        public event Action<string, string> Mutated;   // old id, new id

        public void Add(Rule rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            if (_rules.Exists(r => r.Id == rule.Id)) throw new ArgumentException($"Duplicate rule id '{rule.Id}'.", nameof(rule));
            _rules.Add(rule);
        }

        /// <summary>Checks the flags. Returns the ids of rules newly violated on this call.</summary>
        public List<string> Evaluate(Func<string, bool> flag)
        {
            var violated = new List<string>();
            foreach (var r in _rules)
            {
                bool on = flag(r.ForbiddenFlag);
                if (on && _active.Add(r.Id)) violated.Add(r.Id);
                else if (!on) _active.Remove(r.Id);
            }
            foreach (var id in violated) Violated?.Invoke(id);
            return violated;
        }

        /// <summary>Replaces a rule. Throws unless the change was announced (an environmental cue the player can notice).</summary>
        public void Mutate(string oldId, Rule replacement, bool announced)
        {
            if (!announced) throw new InvalidOperationException("A rule may only change after the change was announced to the player.");
            int i = _rules.FindIndex(r => r.Id == oldId);
            if (i < 0) throw new ArgumentException($"No rule '{oldId}'.", nameof(oldId));
            if (replacement.Id != oldId && _rules.Exists(r => r.Id == replacement.Id)) throw new ArgumentException($"Duplicate rule id '{replacement.Id}'.", nameof(replacement));
            _rules[i] = replacement;
            _active.Remove(oldId);
            Mutated?.Invoke(oldId, replacement.Id);
        }
    }
}
