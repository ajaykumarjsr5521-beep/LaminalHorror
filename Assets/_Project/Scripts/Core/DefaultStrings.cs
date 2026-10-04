using System.Collections.Generic;

namespace NocturneAnnex.Core
{
    /// <summary>Default (English) string table. Keep wording spoiler-free for puzzle-related text.</summary>
    public static class DefaultStrings
    {
        public static readonly IReadOnlyDictionary<string, string> English = new Dictionary<string, string>
        {
            // Doors
            { "door.prompt.open", "Open" },
            { "door.prompt.close", "Close" },
            { "door.prompt.locked", "Locked" },
            { "door.message.locked", "It's locked." },

            // Pickups and notes
            { "pickup.prompt", "Take {0}" },
            { "pickup.refused", "You can't carry any more." },
            { "note.prompt", "Read" },

            // Code lock
            { "codelock.prompt.use", "Use keypad" },
            { "codelock.prompt.unlocked", "Unlocked" },

            // Menus
            { "menu.continue", "Continue" },
            { "menu.new_game", "New Game" },
            { "menu.settings", "Settings" },
            { "menu.credits", "Credits" },
            { "menu.quit", "Quit" },
            { "menu.resume", "Resume" },
            { "menu.restart", "Restart" },
            { "menu.main_menu", "Main Menu" },
            { "menu.back", "Back" },
            { "menu.save", "Save" },
            { "menu.reset_defaults", "Reset to defaults" },
            { "confirm.new_game.title", "Start a new game?" },
            { "confirm.new_game.body", "Your current save will be replaced." },
            { "confirm.yes", "Yes, start over" },
            { "confirm.no", "Cancel" },
            { "menu.new_game_failed", "A new game could not be started: the old save could not be cleared." },

            { "menu.title", "Nocturne Annex" },
            { "credits.title", "Credits" },
            { "credits.body", "Nocturne Annex (working title)\n\nFont: Liberation Sans, SIL Open Font License 1.1.\n\nMade with Unity." },
            { "settings.quality_auto", "Automatic" },

            // Settings screen labels
            { "settings.look_sensitivity", "Look sensitivity" },
            { "settings.invert_y", "Invert vertical look" },
            { "settings.master_volume", "Master volume" },
            { "settings.music_volume", "Music volume" },
            { "settings.sfx_volume", "Effects volume" },
            { "settings.captions", "Captions" },
            { "settings.touch_scale", "Touch control size" },
            { "settings.story_mode", "Story mode (gentler pacing)" },
            { "settings.quality", "Graphics quality" },

            // Settings (shown to the player)
            { "settings.corrupt", "Your settings file could not be read, so default settings are being used. The old file was kept." },
            { "settings.newer", "Your settings were saved by a newer version of the game, so default settings are being used." },
            { "settings.write_failed", "Settings could not be saved: {0}" },

            // Save / load (shown to the player)
            { "save.missing", "No save found." },
            { "save.empty", "The save file is empty." },
            { "save.unreadable", "The save file could not be read: {0}" },
            { "save.damaged", "The save file is damaged or incomplete." },
            { "save.newer", "This save was made by a newer version of the game (save v{0}, game supports v{1})." },
            { "save.older", "This save is from an older, unsupported version (v{0})." },
            { "save.open_failed", "The save file could not be opened: {0}" },
            { "save.verify_failed", "The save could not be verified after writing; your previous save was kept." },
            { "save.write_failed", "Saving failed: {0} Your previous save was kept." },
        };
    }
}
