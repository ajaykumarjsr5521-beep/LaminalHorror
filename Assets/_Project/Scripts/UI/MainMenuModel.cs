using System;
using NocturneAnnex.Core;
using NocturneAnnex.Save;

namespace NocturneAnnex.UI
{
    public enum MenuOutcome { None, Continue, StartNewGame, NeedsConfirmation, Failed }

    /// <summary>
    /// Main menu logic with no UI dependency. Continue is offered only for a readable save. Starting a new game over
    /// an existing save (including an unreadable one, which F-06 moves aside rather than deleting) needs confirmation.
    /// </summary>
    public class MainMenuModel
    {
        readonly Func<LoadResult> _peekSave;
        readonly Func<bool> _clearSave;

        public bool ContinueAvailable { get; private set; }
        public bool HasExistingSave { get; private set; }
        public bool AwaitingNewGameConfirmation { get; private set; }

        /// <summary>Player-facing warning about an unreadable save, or null.</summary>
        public string Warning { get; private set; }

        /// <summary>Player-facing error from the last failed action, or null.</summary>
        public string Error { get; private set; }

        public MainMenuModel(Func<LoadResult> peekSave, Func<bool> clearSave)
        {
            _peekSave = peekSave ?? throw new ArgumentNullException(nameof(peekSave));
            _clearSave = clearSave ?? throw new ArgumentNullException(nameof(clearSave));
            Refresh();
        }

        /// <summary>Re-reads the save state; call when the menu is shown.</summary>
        public void Refresh()
        {
            var r = _peekSave();
            ContinueAvailable = r.Ok;
            HasExistingSave = r.Status != LoadStatus.Missing;
            Warning = (r.Status == LoadStatus.Corrupt || r.Status == LoadStatus.UnsupportedVersion) ? r.Message : null;
            Error = null;
            AwaitingNewGameConfirmation = false;
        }

        public MenuOutcome RequestContinue()
        {
            Error = null;
            return ContinueAvailable ? MenuOutcome.Continue : MenuOutcome.None;
        }

        public MenuOutcome RequestNewGame()
        {
            Error = null;
            if (HasExistingSave)
            {
                AwaitingNewGameConfirmation = true;
                return MenuOutcome.NeedsConfirmation;
            }
            return StartNew();
        }

        public MenuOutcome ConfirmNewGame()
        {
            if (!AwaitingNewGameConfirmation) return MenuOutcome.None;
            AwaitingNewGameConfirmation = false;
            return StartNew();
        }

        public void CancelNewGame()
        {
            AwaitingNewGameConfirmation = false;
        }

        MenuOutcome StartNew()
        {
            if (!_clearSave())
            {
                Error = Loc.Get("menu.new_game_failed");
                return MenuOutcome.Failed;
            }
            ContinueAvailable = false;
            HasExistingSave = false;
            Warning = null;
            return MenuOutcome.StartNewGame;
        }
    }
}
