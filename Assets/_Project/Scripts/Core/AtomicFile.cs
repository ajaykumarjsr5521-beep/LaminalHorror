using System;
using System.IO;

namespace NocturneAnnex.Core
{
    /// <summary>
    /// Writes a text file so that an interrupted or failed write never damages the existing file:
    /// content goes to "path.tmp", is optionally verified, and only then replaces the target.
    /// </summary>
    public static class AtomicFile
    {
        public static string TempPathFor(string path) => path + ".tmp";

        /// <exception cref="InvalidDataException">The written content failed verification; the target is untouched.</exception>
        /// <exception cref="IOException">The write failed; the target is untouched.</exception>
        /// <exception cref="UnauthorizedAccessException">No permission; the target is untouched.</exception>
        public static void Write(string path, string content, Func<string, bool> verify = null)
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            var temp = TempPathFor(path);
            File.WriteAllText(temp, content);
            if (verify != null && !verify(File.ReadAllText(temp)))
                throw new InvalidDataException("Written content failed verification: " + path);

            if (File.Exists(path)) File.Replace(temp, path, null);
            else File.Move(temp, path);
        }
    }
}
