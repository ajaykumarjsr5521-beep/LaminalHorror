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
