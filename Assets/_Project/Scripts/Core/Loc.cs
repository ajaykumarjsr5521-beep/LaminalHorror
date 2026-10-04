using System;
using System.Collections.Generic;
using UnityEngine;

namespace NocturneAnnex.Core
{
    /// <summary>
    /// Player-facing text lookup. Keys are "area.name". A missing key is reported (error log once per key)
    /// and shown as "[key]" so the gap is visible, never silently blank.
    /// </summary>
    public static class Loc
    {
        static IReadOnlyDictionary<string, string> _table = DefaultStrings.English;
        static readonly HashSet<string> Reported = new HashSet<string>();

        public static void SetTable(IReadOnlyDictionary<string, string> table)
        {
            _table = table ?? throw new ArgumentNullException(nameof(table));
            Reported.Clear();
        }

        public static void ResetToDefault() => SetTable(DefaultStrings.English);

        public static bool Has(string key) => key != null && _table.ContainsKey(key);

        public static string Get(string key)
        {
            if (key != null && _table.TryGetValue(key, out var text)) return text;
            if (Reported.Add(key ?? "<null>")) Debug.LogError($"Loc: missing string key '{key}'.");
            return "[" + key + "]";
        }

        public static string Format(string key, params object[] args)
        {
            var text = Get(key);
            try { return string.Format(text, args); }
            catch (FormatException)
            {
                Debug.LogError($"Loc: key '{key}' has a bad format string: {text}");
                return text;
            }
        }
    }
}
