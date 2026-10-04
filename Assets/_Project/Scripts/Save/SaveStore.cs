using System;
using System.IO;
using NocturneAnnex.Core;

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

        public SaveStore(string path) { _path = path; }

        public string Path => _path;
        public bool Exists => File.Exists(_path);

        public WriteResult Write(SaveData data)
        {
            try
            {
                data.SavedAtUtc = DateTime.UtcNow.ToString("o");
                AtomicFile.Write(_path, SaveSerializer.ToJson(data), written => SaveSerializer.FromJson(written).Ok);
                return new WriteResult(true, null);
            }
            catch (InvalidDataException)
            {
                return new WriteResult(false, Loc.Get("save.verify_failed"));
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                return new WriteResult(false, Loc.Format("save.write_failed", e.Message));
            }
        }

        public LoadResult Load()
        {
            if (!File.Exists(_path)) return new LoadResult(LoadStatus.Missing, null, Loc.Get("save.missing"));
            try { return SaveSerializer.FromJson(File.ReadAllText(_path)); }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                return new LoadResult(LoadStatus.Corrupt, null, Loc.Format("save.open_failed", e.Message));
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
