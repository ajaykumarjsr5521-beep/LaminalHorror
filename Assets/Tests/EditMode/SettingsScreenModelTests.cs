using System;
using System.Collections.Generic;
using NUnit.Framework;
using NocturneAnnex.Core;
using NocturneAnnex.Settings;
using NocturneAnnex.UI;

namespace NocturneAnnex.Tests.EditMode
{
    public class SettingsScreenModelTests
    {
        readonly List<SettingsData> _applied = new List<SettingsData>();
        readonly List<SettingsData> _saved = new List<SettingsData>();
        bool _saveSucceeds;

        [SetUp]
        public void SetUp()
        {
            _applied.Clear();
            _saved.Clear();
            _saveSucceeds = true;
        }

        SettingsScreenModel Make(SettingsData current = null) =>
            new SettingsScreenModel(current ?? SettingsData.CreateDefault(), 3,
                d => { if (_saveSucceeds) _saved.Add(d); return _saveSucceeds ? new WriteResult(true, null) : new WriteResult(false, "disk full"); },
                d => _applied.Add(d));

        [Test]
        public void Fresh_IsNotDirty_AndAppliesNothing()
        {
            var m = Make();
            Assert.IsFalse(m.IsDirty);
            Assert.AreEqual(0, _applied.Count);
        }

        [Test]
        public void Edit_MarksDirty_AndPreviewsLive()
        {
            var m = Make();
            m.Working.LookSensitivity = 2f;
            m.NotifyChanged();
            Assert.IsTrue(m.IsDirty);
            Assert.AreEqual(1, _applied.Count);
            Assert.AreEqual(2f, _applied[0].LookSensitivity);
            Assert.AreEqual(0, _saved.Count, "preview must not persist");
        }

        [Test]
        public void NotifyChanged_ClampsOutOfRangeEdits()
        {
            var m = Make();
            m.Working.LookSensitivity = 99f;
            m.NotifyChanged();
            Assert.AreEqual(SettingsData.MaxSensitivity, m.Working.LookSensitivity);
            Assert.AreEqual(SettingsData.MaxSensitivity, _applied[0].LookSensitivity);
        }

        [Test]
        public void Save_Persists_AndClearsDirty()
        {
            var m = Make();
            m.Working.StoryMode = true;
            m.NotifyChanged();
            Assert.IsTrue(m.Save());
            Assert.AreEqual(1, _saved.Count);
            Assert.IsTrue(_saved[0].StoryMode);
            Assert.IsFalse(m.IsDirty);
            Assert.IsNull(m.Error);
        }

        [Test]
        public void FailedSave_KeepsEditsAndDirty_AndReportsError()
        {
            _saveSucceeds = false;
            var m = Make();
            m.Working.InvertY = true;
            m.NotifyChanged();
            Assert.IsFalse(m.Save());
            Assert.AreEqual("disk full", m.Error);
            Assert.IsTrue(m.IsDirty);
            Assert.IsTrue(m.Working.InvertY);
        }

        [Test]
        public void Revert_RestoresLastSavedValues_AndReappliesThem()
        {
            var m = Make();
            m.Working.LookSensitivity = 2f;
            m.NotifyChanged();
            m.Revert();
            Assert.AreEqual(1f, m.Working.LookSensitivity);
            Assert.IsFalse(m.IsDirty);
            Assert.AreEqual(1f, _applied[_applied.Count - 1].LookSensitivity, "preview must be undone in the live game");
        }

        [Test]
        public void Revert_AfterSave_GoesBackToTheSavedValues_NotTheOriginals()
        {
            var m = Make();
            m.Working.MasterVolume = 0.5f;
            m.NotifyChanged();
            m.Save();
            m.Working.MasterVolume = 0.1f;
            m.NotifyChanged();
            m.Revert();
            Assert.AreEqual(0.5f, m.Working.MasterVolume, 1e-4f);
        }

        [Test]
        public void ResetToDefaults_RestoresDefaults_AndPreviews()
        {
            var m = Make(new SettingsData { LookSensitivity = 2.5f, CaptionsEnabled = false });
            m.ResetToDefaults();
            Assert.AreEqual(1f, m.Working.LookSensitivity);
            Assert.IsTrue(m.Working.CaptionsEnabled);
            Assert.IsTrue(m.IsDirty, "defaults differ from what was saved until the player saves");
            Assert.AreEqual(1, _applied.Count);
        }

        [Test]
        public void WorkingCopy_IsIndependentOfTheSettingsPassedIn()
        {
            var original = SettingsData.CreateDefault();
            var m = Make(original);
            m.Working.LookSensitivity = 3f;
            Assert.AreEqual(1f, original.LookSensitivity);
        }

        [Test]
        public void NullArguments_AreRejectedExplicitly()
        {
            Assert.Throws<ArgumentNullException>(() => new SettingsScreenModel(null, 3, d => new WriteResult(true, null), d => { }));
            Assert.Throws<ArgumentNullException>(() => new SettingsScreenModel(SettingsData.CreateDefault(), 3, null, d => { }));
            Assert.Throws<ArgumentNullException>(() => new SettingsScreenModel(SettingsData.CreateDefault(), 3, d => new WriteResult(true, null), null));
        }
    }
}
