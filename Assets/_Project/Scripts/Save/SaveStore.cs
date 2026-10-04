using System;
using System.IO;

namespace NocturneAnnex.Save
{
    public readonly struct WriteResult
    {
        public readonly bool Ok;
        public readonly string Error;
        public WriteResult(bool ok, string error) { Ok = ok; Error = error; }
    }

    /// <summary>
    /// Single-slot file store. Writes go to a temp file first and replace the live file only after the
    /// temp content verifies, so an interrupted write can never damage the existing save.
    /// </summary>
    public class SaveStore
    {
        readonly string _path;
        string TempPath => _path + ".tmp";

        public SaveStore(string path) { _path = path; }

        public string Path => _path;
        public bool Exists => File.Exists(_path);

        public WriteResult Write(SaveData data)
        {
            try
            {
                data.SavedAtUtc = DateTime.UtcNow.ToString("o");
                var json = SaveSerializer.ToJson(data);
                var dir = System.IO.Path.GetDirectoryName(_path);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                File.WriteAllText(TempPath, json);
                if (!SaveSerializer.FromJson(File.ReadAllText(TempPath)).Ok)
                    return new WriteResult(false, "The save could not be verified after writing; your previous save was kept.");

                if (File.Exists(_path)) File.Replace(TempPath, _path, null);
                else File.Move(TempPath, _path);
                return new WriteResult(true, null);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                return new WriteResult(false, "Saving failed: " + e.Message + " Your previous save was kept.");
            }
        }

        public LoadResult Load()
        {
            if (!File.Exists(_path)) return new LoadResult(LoadStatus.Missing, null, "No save found.");
            try { return SaveSerializer.FromJson(File.ReadAllText(_path)); }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                return new LoadResult(LoadStatus.Corrupt, null, "The save file could not be opened: " + e.Message);
            }
        }

        /// <summary>
        /// Moves an unusable save aside (never deletes it) so a new game can start.
        /// Returns the new path, or null if there was nothing to move or the move failed.
        /// </summary>
        public string Quarantine()
        {
            if (!File.Exists(_path)) return null;
            var target = _path + ".corrupt-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
            try { File.Move(_path, target); return target; }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException) { return null; }
        }

        /// <summary>Removes the live save (used by New Game after the player confirmed).</summary>
        public bool Delete()
        {
            try { if (File.Exists(_path)) File.Delete(_path); return true; }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException) { return false; }
        }
    }
}
