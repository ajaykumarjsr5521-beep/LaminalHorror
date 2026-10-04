using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NocturneAnnex.Core;
using NocturneAnnex.Inventory;

namespace NocturneAnnex.UI
{
    /// <summary>
    /// List of collected notes with a reader pane on the same screen, so reading any note is two taps:
    /// open the journal, tap the note. The component sits on an always-active root; Panel is toggled.
    /// </summary>
    public class JournalView : MonoBehaviour
    {
        public GameObject Panel;
        public Transform ListRoot;
        public Button EntryTemplate;     // inactive prefab-style button cloned per entry
        public TMP_Text EmptyText;
        public TMP_Text HintText;
        public TMP_Text ReaderTitle;
        public TMP_Text ReaderBody;
        public Button CloseButton;

        PlayerInventory _inventory;
        JournalModel _model;
        ModalGate.Handle _modal;
        readonly List<GameObject> _rows = new List<GameObject>();

        public bool IsOpen => Panel.activeSelf;
        public IReadOnlyList<GameObject> Rows => _rows;

        void Awake()
        {
            CloseButton.onClick.AddListener(Close);
            EntryTemplate.gameObject.SetActive(false);
            Panel.SetActive(false);
        }

        public void Bind(PlayerInventory inventory)
        {
            _inventory = inventory;
            _model = new JournalModel(ReadNotes);
        }

        IReadOnlyList<JournalEntry> ReadNotes()
        {
            var list = new List<JournalEntry>();
            foreach (var def in _inventory.GetJournalNotes())
                list.Add(new JournalEntry(def.Id, def.DisplayName, def.Body));
            return list;
        }

        public void Open()
        {
            _model.Refresh();
            if (_modal == null) _modal = ModalGate.Open();
            Panel.SetActive(true);
            Render();
        }

        public void Close()
        {
            _modal?.Dispose();
            _modal = null;
            Panel.SetActive(false);
        }

        void OnDisable()
        {
            _modal?.Dispose();
            _modal = null;
        }

        void Render()
        {
            foreach (var row in _rows) Destroy(row);
            _rows.Clear();

            for (int i = 0; i < _model.Entries.Count; i++)
            {
                int index = i;
                var row = Instantiate(EntryTemplate, ListRoot);
                row.gameObject.SetActive(true);
                row.GetComponentInChildren<TMP_Text>().text = _model.Entries[i].Title;
                row.onClick.AddListener(() => { _model.Select(index); Render(); });
                _rows.Add(row.gameObject);
            }

            EmptyText.gameObject.SetActive(_model.IsEmpty);
            EmptyText.text = Loc.Get("journal.empty");
            HintText.gameObject.SetActive(!_model.IsEmpty && !_model.HasSelection);
            HintText.text = Loc.Get("journal.select_hint");

            bool reading = _model.HasSelection;
            ReaderTitle.text = reading ? _model.Selected.Title : string.Empty;
            ReaderBody.text = reading ? _model.Selected.Body : string.Empty;
        }
    }
}
