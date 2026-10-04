using System;
using System.Collections.Generic;

namespace NocturneAnnex.UI
{
    public readonly struct JournalEntry
    {
        public readonly string Id;
        public readonly string Title;
        public readonly string Body;

        public JournalEntry(string id, string title, string body)
        {
            Id = id;
            Title = title;
            Body = body;
        }
    }

    /// <summary>Journal list and selection, independent of the UI. Entries come in pickup order.</summary>
    public class JournalModel
    {
        readonly Func<IReadOnlyList<JournalEntry>> _source;
        IReadOnlyList<JournalEntry> _entries = Array.Empty<JournalEntry>();

        public IReadOnlyList<JournalEntry> Entries => _entries;
        public bool IsEmpty => _entries.Count == 0;
        public int SelectedIndex { get; private set; } = -1;
        public bool HasSelection => SelectedIndex >= 0;
        public JournalEntry Selected => _entries[SelectedIndex];

        public JournalModel(Func<IReadOnlyList<JournalEntry>> source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            Refresh();
        }

        /// <summary>Re-reads the entries. The selection is kept only if the same note is still there.</summary>
        public void Refresh()
        {
            string selectedId = HasSelection ? Selected.Id : null;
            // Snapshot: the source may reuse or mutate its list, which must not change what is selected.
            var latest = _source();
            _entries = latest == null ? Array.Empty<JournalEntry>() : new List<JournalEntry>(latest);
            SelectedIndex = -1;
            if (selectedId != null) SelectById(selectedId);
        }

        /// <summary>Returns false (and changes nothing) for an out-of-range index.</summary>
        public bool Select(int index)
        {
            if (index < 0 || index >= _entries.Count) return false;
            SelectedIndex = index;
            return true;
        }

        public bool SelectById(string id)
        {
            for (int i = 0; i < _entries.Count; i++)
                if (_entries[i].Id == id) return Select(i);
            return false;
        }

        public void ClearSelection() => SelectedIndex = -1;
    }
}
