using System;
using NocturneAnnex.Core;
using NocturneAnnex.Settings;

namespace NocturneAnnex.UI
{
    /// <summary>
    /// Settings screen logic with no UI dependency. Views bind to Working and call NotifyChanged after edits.
    /// Edits preview live; leaving without saving must call Revert so the preview does not stick.
    /// </summary>
    public class SettingsScreenModel
    {
        readonly SettingsData _baseline = new SettingsData();
        readonly Func<SettingsData, WriteResult> _save;
        readonly Action<SettingsData> _apply;
        readonly int _qualityLevelCount;

        /// <summary>The values being edited. Bind controls to this object.</summary>
        public SettingsData Working { get; } = new SettingsData();

        /// <summary>Player-facing error from the last failed save, or null.</summary>
        public string Error { get; private set; }

        public bool IsDirty => !Working.ValueEquals(_baseline);

        public SettingsScreenModel(SettingsData current, int qualityLevelCount,
            Func<SettingsData, WriteResult> save, Action<SettingsData> apply)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _apply = apply ?? throw new ArgumentNullException(nameof(apply));
            _qualityLevelCount = qualityLevelCount;
            _baseline.CopyFrom(current);
            Working.CopyFrom(current);
        }

        /// <summary>Call after any edit: keeps values in range and previews them live.</summary>
        public void NotifyChanged()
        {
            Working.Clamp(_qualityLevelCount);
            _apply(Working.Clone());
        }

        /// <summary>Persists the working values. On failure the error is kept for display and nothing changes.</summary>
        public bool Save()
        {
            Working.Clamp(_qualityLevelCount);
            var result = _save(Working.Clone());
            if (!result.Ok)
            {
                Error = result.Error;
                return false;
            }
            Error = null;
            _baseline.CopyFrom(Working);
            return true;
        }

        /// <summary>Discards unsaved edits and re-applies the last saved values.</summary>
        public void Revert()
        {
            Error = null;
            Working.CopyFrom(_baseline);
            _apply(Working.Clone());
        }

        public void ResetToDefaults()
        {
            Working.CopyFrom(SettingsData.CreateDefault());
            NotifyChanged();
        }
    }
}
