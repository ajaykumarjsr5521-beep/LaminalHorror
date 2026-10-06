using System.Collections.Generic;
using System.Linq;

namespace NocturneAnnex.Liminal
{
    /// <summary>Which legend entries (rumours, clues, evidence) the player has found. Each counts once.</summary>
    public class LegendProgress
    {
        readonly HashSet<string> _found = new HashSet<string>();

        public int Count => _found.Count;

        public bool Has(string id) => _found.Contains(id);

        /// <summary>Records an entry. Returns true only the first time.</summary>
        public bool Found(string id) => !string.IsNullOrEmpty(id) && _found.Add(id);

        public string[] Snapshot() => _found.OrderBy(x => x).ToArray();

        public void Restore(IEnumerable<string> ids)
        {
            _found.Clear();
            foreach (var id in ids ?? new string[0]) if (!string.IsNullOrEmpty(id)) _found.Add(id);
        }
    }
}
