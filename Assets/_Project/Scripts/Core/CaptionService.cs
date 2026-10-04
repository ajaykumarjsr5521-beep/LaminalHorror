using System;
using System.Collections.Generic;

namespace NocturneAnnex.Core
{
    /// <summary>
    /// Queue of on-screen caption lines. Cues are posted by key (text comes from Loc), expire on their own,
    /// refresh instead of stacking when repeated, and are limited to MaxLines (lowest priority dropped first).
    /// Nothing is shown while Accessibility.CaptionsEnabled is off. Time is passed in so the logic is testable.
    /// </summary>
    public class CaptionService
    {
        public const float DefaultDurationSeconds = 3f;

        class Entry
        {
            public string Key;
            public string Text;
            public float ExpiresAt;
            public int Priority;
            public int Order;
        }

        readonly List<Entry> _active = new List<Entry>();
        int _nextOrder;

        public int MaxLines { get; set; } = 3;

        /// <summary>Raised when the visible lines changed.</summary>
        public event Action Changed;

        public int Count => _active.Count;

        /// <summary>Visible lines, oldest first.</summary>
        public IReadOnlyList<string> Lines
        {
            get
            {
                var lines = new List<string>(_active.Count);
                foreach (var e in _active) lines.Add(e.Text);
                return lines;
            }
        }

        public void Post(string key, float now, float durationSeconds = DefaultDurationSeconds, int priority = 0, params object[] args)
        {
            if (!Accessibility.CaptionsEnabled) return;
            string text = args != null && args.Length > 0 ? Loc.Format(key, args) : Loc.Get(key);

            var existing = _active.Find(e => e.Key == key);
            if (existing != null)
            {
                existing.Text = text;
                existing.ExpiresAt = now + durationSeconds;
                existing.Priority = Math.Max(existing.Priority, priority);
                Changed?.Invoke();
                return;
            }

            _active.Add(new Entry { Key = key, Text = text, ExpiresAt = now + durationSeconds, Priority = priority, Order = _nextOrder++ });
            while (_active.Count > Math.Max(1, MaxLines)) _active.Remove(LowestPriorityOldest());
            Changed?.Invoke();
        }

        /// <summary>Drops expired cues, and everything if captions were switched off.</summary>
        public void Tick(float now)
        {
            int before = _active.Count;
            if (!Accessibility.CaptionsEnabled) _active.Clear();
            else _active.RemoveAll(e => e.ExpiresAt <= now);
            if (_active.Count != before) Changed?.Invoke();
        }

        public void Clear()
        {
            if (_active.Count == 0) return;
            _active.Clear();
            Changed?.Invoke();
        }

        Entry LowestPriorityOldest()
        {
            Entry worst = _active[0];
            foreach (var e in _active)
                if (e.Priority < worst.Priority || (e.Priority == worst.Priority && e.Order < worst.Order)) worst = e;
            return worst;
        }
    }
}
