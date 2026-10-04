using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using NocturneAnnex.Inventory;

namespace NocturneAnnex.Editor
{
    /// <summary>
    /// Authored item data for Floor B1 (placeholder prose, owner to review). Generates the item assets and the database.
    /// The three dates are NOT written in the notes: they appear on in-world props, and the notes say where to look.
    /// </summary>
    public static class LevelItems
    {
        public const string DataDir = "Assets/_Project/Data";
        public const string DatabasePath = DataDir + "/ItemDatabase.asset";

        public const string StampId = "brass_stamp";
        public const string MemoId = "note_memo";
        public const string LedgerId = "note_ledger";
        public const string TapeId = "note_tape";

        /// <summary>Lights failed on the 14th, ledger sealed on the 3rd, red circle on the 9th, read in that order.</summary>
        public const string Code = "1439";
        public static readonly string[] Days = { "14", "3", "9" };

        static readonly (string id, string name, ItemKind kind, string body)[] Defs =
        {
            (StampId, "Brass Stamp", ItemKind.Key,
                "A heavy brass stamp. The handle is engraved: RECORDS OFFICE."),
            (MemoId, "Night Shift Memo", ItemKind.Note,
                "Filing runs on the dates, not the names.\n\nThe first date is the day the lights failed. It is on the calendar in this room.\nThe second is the day the ledger was sealed. The ledger is in the stacks.\nThe third is circled in red on the calendar by the loading dock.\n\nThe code for the Records Office is the three days, in that order, written one after another as plain numbers. The stamp for the Records Office door is in the stacks, on the far shelf."),
            (LedgerId, "Ledger Page 12", ItemKind.Note,
                "Entry struck through twice. Someone has written the date again underneath, smaller, as if to hide it.\n\nThe day the ledger was sealed is written on the spine of the open ledger here.\n\nStamp returned to the end shelf, far side."),
            (TapeId, "Answering Machine Tape", ItemKind.Note,
                "\"...if you are hearing this, I could not stay. The calendar by the loading dock has one day circled in red. Do not trust the clock in the Records Office. Use the dates.\""),
        };

        [MenuItem("Build/Create Level Items")]
        public static ItemDatabase CreateAll()
        {
            Directory.CreateDirectory(DataDir);
            var db = AssetDatabase.LoadAssetAtPath<ItemDatabase>(DatabasePath);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<ItemDatabase>();
                AssetDatabase.CreateAsset(db, DatabasePath);
            }
            db.Items.Clear();
            foreach (var (id, name, kind, body) in Defs)
            {
                string path = $"{DataDir}/Item_{id}.asset";
                var item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
                if (item == null)
                {
                    item = ScriptableObject.CreateInstance<ItemDefinition>();
                    AssetDatabase.CreateAsset(item, path);
                }
                item.Id = id;
                item.DisplayName = name;
                item.Kind = kind;
                item.Body = body;
                EditorUtility.SetDirty(item);
                db.Items.Add(item);
            }
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            return db;
        }

        public static ItemDefinition Get(ItemDatabase db, string id)
        {
            // Read the list directly: the database's lookup cache can be stale right after the assets were rewritten.
            var item = db.Items.FirstOrDefault(i => i != null && i.Id == id);
            if (item == null) throw new System.InvalidOperationException($"Level item '{id}' is missing from the database.");
            return item;
        }
    }
}
