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

            // Hiding
            { "hide.prompt.enter", "Hide" },
            { "hide.prompt.exit", "Leave" },
            { "hide.message.seen", "It is watching. There is no time." },

            // Lives
            { "run.lives_left_1", "One life remains." },
            { "run.over", "The building keeps you. It begins again." },

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

            // Keypad, note reader, journal
            { "menu.close", "Close" },
            { "menu.pause", "Pause" },
            { "touch.interact", "Use" },
            { "touch.sprint", "Run" },
            { "touch.crouch", "Crouch" },
            { "keypad.title", "Enter code" },
            { "keypad.enter", "Enter" },
            { "keypad.clear", "Delete" },
            { "keypad.incorrect", "That doesn't seem right." },
            { "keypad.solved", "Unlocked." },
            { "journal.title", "Journal" },
            { "journal.empty", "You haven't found any notes yet." },
            { "journal.select_hint", "Select a note to read it." },

            // Content notice (first launch)
            { "notice.title", "Before you play" },
            { "notice.body", "This game contains frightening themes, dark scenes and sudden loud sounds. It can include flashing lights.\n\nIn Settings you can turn on Reduce flicker and flashing, Reduce camera motion, Captions and larger text. You can pause at any time." },
            { "notice.ok", "I understand" },
            { "menu.content_notice", "Content notice" },

            // Level flow
            { "level.checkpoint_saved", "Checkpoint saved." },
            { "level.save_failed", "Couldn't save your progress." },
            { "level.exit_locked", "The way out is still locked." },
            { "level.end.title", "Floor B1 complete" },
            { "level.end.body", "You made it out. For now." },

            // Horror event captions
            { "event.light_buzz", "[lights buzzing]" },
            { "event.door_slam", "[a door slams]" },
            { "event.prop_shift", "[something shifts nearby]" },
            { "event.whisper", "[faint whispering]" },
            { "event.misfile", "[the air feels wrong]" },
            { "event.figure", "[a shape in the dark]" },

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
            { "settings.text_size", "Text size" },
            { "settings.text_size.small", "Small" },
            { "settings.text_size.medium", "Medium" },
            { "settings.text_size.large", "Large" },
            { "settings.reduce_flicker", "Reduce flicker and flashing" },
            { "settings.reduce_motion", "Reduce camera motion" },

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
