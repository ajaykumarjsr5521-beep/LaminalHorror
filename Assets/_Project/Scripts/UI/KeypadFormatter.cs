using System.Text;

namespace NocturneAnnex.UI
{
    /// <summary>Formats the keypad readout: typed digits then empty slots, e.g. "0 7 _ _".</summary>
    public static class KeypadFormatter
    {
        public static string Format(string entry, int length)
        {
            entry ??= string.Empty;
            var sb = new StringBuilder();
            for (int i = 0; i < length; i++)
            {
                if (i > 0) sb.Append(' ');
                sb.Append(i < entry.Length ? entry[i] : '_');
            }
            return sb.ToString();
        }
    }
}
