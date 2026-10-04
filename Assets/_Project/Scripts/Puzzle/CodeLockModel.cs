using System.Text;

namespace NocturneAnnex.Puzzle
{
    public enum SubmitResult { Incomplete, Wrong, Correct }

    /// <summary>
    /// Digit-entry logic for a code lock. A wrong code clears the entry and can be retried
    /// immediately: there is deliberately no attempt limit or lockout.
    /// </summary>
    public class CodeLockModel
    {
        readonly string _code;
        readonly StringBuilder _entry = new StringBuilder();

        public int Length => _code.Length;
        public string Entry => _entry.ToString();
        public bool IsSolved { get; private set; }

        /// <summary>Returns null if the code is a valid configuration, otherwise the problem.</summary>
        public static string ValidateCode(string code)
        {
            if (string.IsNullOrEmpty(code)) return "Code is empty.";
            foreach (char c in code)
                if (c < '0' || c > '9') return $"Code '{code}' contains a non-digit.";
            return null;
        }

        public CodeLockModel(string code)
        {
            var problem = ValidateCode(code);
            if (problem != null) throw new System.ArgumentException(problem, nameof(code));
            _code = code;
        }

        /// <summary>Ignored once solved, when full, or for non-digits. Returns true if accepted.</summary>
        public bool EnterDigit(char digit)
        {
            if (IsSolved || digit < '0' || digit > '9' || _entry.Length >= _code.Length) return false;
            _entry.Append(digit);
            return true;
        }

        public void Backspace()
        {
            if (!IsSolved && _entry.Length > 0) _entry.Length--;
        }

        public void Clear()
        {
            if (!IsSolved) _entry.Clear();
        }

        public SubmitResult Submit()
        {
            if (IsSolved) return SubmitResult.Correct;
            if (_entry.Length < _code.Length) return SubmitResult.Incomplete;
            bool ok = _entry.ToString() == _code;
            _entry.Clear();
            if (ok) IsSolved = true;
            return ok ? SubmitResult.Correct : SubmitResult.Wrong;
        }

        /// <summary>Restores a previously solved state (save/load).</summary>
        public void RestoreSolved(bool solved)
        {
            IsSolved = solved;
            _entry.Clear();
        }
    }
}
