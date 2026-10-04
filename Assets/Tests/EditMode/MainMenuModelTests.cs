using System;
using NUnit.Framework;
using NocturneAnnex.Core;
using NocturneAnnex.Save;
using NocturneAnnex.UI;

namespace NocturneAnnex.Tests.EditMode
{
    public class MainMenuModelTests
    {
        static LoadResult Ok() => new LoadResult(LoadStatus.Ok, new SaveData(), null);
        static LoadResult Missing() => new LoadResult(LoadStatus.Missing, null, "none");
        static LoadResult Corrupt() => new LoadResult(LoadStatus.Corrupt, null, "damaged file");
        static LoadResult Newer() => new LoadResult(LoadStatus.UnsupportedVersion, null, "newer save");

        int _clearCalls;
        bool _clearSucceeds;

        [SetUp]
        public void SetUp()
        {
            _clearCalls = 0;
            _clearSucceeds = true;
        }

        MainMenuModel Make(LoadResult save) =>
            new MainMenuModel(() => save, () => { _clearCalls++; return _clearSucceeds; });

        [Test]
        public void ReadableSave_EnablesContinue()
        {
            var m = Make(Ok());
            Assert.IsTrue(m.ContinueAvailable);
            Assert.IsTrue(m.HasExistingSave);
            Assert.IsNull(m.Warning);
            Assert.AreEqual(MenuOutcome.Continue, m.RequestContinue());
        }

        [Test]
        public void NoSave_DisablesContinue_AndNewGameStartsImmediately()
        {
            var m = Make(Missing());
            Assert.IsFalse(m.ContinueAvailable);
            Assert.AreEqual(MenuOutcome.None, m.RequestContinue());
            Assert.AreEqual(MenuOutcome.StartNewGame, m.RequestNewGame());
            Assert.IsFalse(m.AwaitingNewGameConfirmation);
        }

        [Test]
        public void NewGame_OverExistingSave_AsksFirst_AndDoesNotClearUntilConfirmed()
        {
            var m = Make(Ok());
            Assert.AreEqual(MenuOutcome.NeedsConfirmation, m.RequestNewGame());
            Assert.IsTrue(m.AwaitingNewGameConfirmation);
            Assert.AreEqual(0, _clearCalls);
            Assert.AreEqual(MenuOutcome.StartNewGame, m.ConfirmNewGame());
            Assert.AreEqual(1, _clearCalls);
            Assert.IsFalse(m.ContinueAvailable);
        }

        [Test]
        public void CancellingConfirmation_ChangesNothing()
        {
            var m = Make(Ok());
            m.RequestNewGame();
            m.CancelNewGame();
            Assert.IsFalse(m.AwaitingNewGameConfirmation);
            Assert.AreEqual(0, _clearCalls);
            Assert.IsTrue(m.ContinueAvailable);
        }

        [Test]
        public void Confirm_WithoutRequest_IsIgnored()
        {
            var m = Make(Ok());
            Assert.AreEqual(MenuOutcome.None, m.ConfirmNewGame());
            Assert.AreEqual(0, _clearCalls);
        }

        [TestCase(LoadStatus.Corrupt)]
        [TestCase(LoadStatus.UnsupportedVersion)]
        public void UnreadableSave_ShowsWarning_DisablesContinue_AndStillNeedsConfirmation(LoadStatus status)
        {
            var m = Make(status == LoadStatus.Corrupt ? Corrupt() : Newer());
            Assert.IsFalse(m.ContinueAvailable);
            Assert.IsNotNull(m.Warning);
            Assert.IsTrue(m.HasExistingSave, "the unreadable file counts as a save that would be replaced");
            Assert.AreEqual(MenuOutcome.NeedsConfirmation, m.RequestNewGame());
        }

        [Test]
        public void FailedClear_ReportsError_AndDoesNotStart()
        {
            _clearSucceeds = false;
            var m = Make(Ok());
            m.RequestNewGame();
            Assert.AreEqual(MenuOutcome.Failed, m.ConfirmNewGame());
            Assert.AreEqual(Loc.Get("menu.new_game_failed"), m.Error);
            Assert.IsFalse(m.AwaitingNewGameConfirmation);
            Assert.IsTrue(m.ContinueAvailable, "old save still there, so Continue stays available");
        }

        [Test]
        public void Refresh_PicksUpChangedSaveState_AndClearsStaleError()
        {
            var current = Missing();
            _clearSucceeds = false;
            var m = new MainMenuModel(() => current, () => { _clearCalls++; return _clearSucceeds; });
            m.RequestNewGame();
            Assert.IsNotNull(m.Error);
            current = Ok();
            m.Refresh();
            Assert.IsTrue(m.ContinueAvailable);
            Assert.IsNull(m.Error);
        }

        [Test]
        public void NullDelegates_AreRejectedExplicitly()
        {
            Assert.Throws<ArgumentNullException>(() => new MainMenuModel(null, () => true));
            Assert.Throws<ArgumentNullException>(() => new MainMenuModel(() => Ok(), null));
        }
    }
}
