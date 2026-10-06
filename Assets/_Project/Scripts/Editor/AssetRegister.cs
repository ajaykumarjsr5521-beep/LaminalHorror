using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace NocturneAnnex.Editor
{
    /// <summary>
    /// Reads the third-party asset and IP register (docs/09-asset-ip-register.md) and checks it. Rule of the register:
    /// nothing REQUIRES_REVIEW ships, and every audio file in the project has a row that is CLEARED or ORIGINAL.
    /// </summary>
    public static class AssetRegister
    {
        public const string Path = "docs/09-asset-ip-register.md";
        static readonly string[] Statuses = { "CLEARED", "REQUIRES_REVIEW", "ORIGINAL", "REJECTED" };
        static readonly string[] Shippable = { "CLEARED", "ORIGINAL" };
        public static readonly string[] AudioExtensions = { ".wav", ".ogg", ".mp3", ".aif", ".aiff", ".flac" };

        public class Row
        {
            public string Id, Asset, UsedIn, Status;
        }

        /// <summary>Register rows from the markdown table: lines whose first cell looks like AB-12 and that have 11 cells.</summary>
        public static List<Row> Parse(string markdown)
        {
            var rows = new List<Row>();
            foreach (var raw in markdown.Split('\n'))
            {
                var line = raw.Trim();
                if (!line.StartsWith("|")) continue;
                var cells = line.Trim('|').Split('|').Select(c => c.Trim()).ToArray();
                if (cells.Length < 11 || !Regex.IsMatch(cells[0], @"^[A-Z]{2,}-\d+$")) continue;
                rows.Add(new Row { Id = cells[0], Asset = cells[1], UsedIn = cells[8], Status = cells[9] });
            }
            return rows;
        }

        public static List<Row> Load() => Parse(File.ReadAllText(Path));

        /// <summary>Rows with a status that is not one of the four allowed values.</summary>
        public static List<string> StatusProblems(IEnumerable<Row> rows) =>
            rows.Where(r => !Statuses.Contains(r.Status)).Select(r => $"{r.Id} has unknown status '{r.Status}'").ToList();

        /// <summary>Audio files (project-relative paths) that have no register row, or whose row is not CLEARED or ORIGINAL.</summary>
        public static List<string> AudioProblems(IEnumerable<Row> rows, IEnumerable<string> audioFiles)
        {
            var list = rows.ToList();
            var problems = new List<string>();
            foreach (var file in audioFiles.Where(f => AudioExtensions.Contains(System.IO.Path.GetExtension(f).ToLowerInvariant())))
            {
                string name = System.IO.Path.GetFileName(file);
                var row = list.FirstOrDefault(r => r.Asset.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0);
                if (row == null) problems.Add($"{file}: no register row");
                else if (!Shippable.Contains(row.Status)) problems.Add($"{file}: row {row.Id} is {row.Status}");
            }
            return problems;
        }

        /// <summary>The release gate: every row must be CLEARED or ORIGINAL, none REQUIRES_REVIEW or REJECTED.</summary>
        public static List<string> ReleaseProblems(IEnumerable<Row> rows) =>
            rows.Where(r => !Shippable.Contains(r.Status)).Select(r => $"{r.Id} {r.Asset}: {r.Status}").ToList();

        /// <summary>Throws with the list of problems. Used before a release build.</summary>
        public static void CheckForRelease()
        {
            var rows = Load();
            var problems = StatusProblems(rows).Concat(ReleaseProblems(rows)).ToList();
            if (problems.Count > 0)
                throw new InvalidOperationException("Asset register (docs/09) blocks the release:\n" + string.Join("\n", problems));
        }
    }
}
