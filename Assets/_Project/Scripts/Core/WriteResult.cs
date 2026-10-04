namespace NocturneAnnex.Core
{
    /// <summary>Outcome of a persistence write. Error is a player-facing message when Ok is false.</summary>
    public readonly struct WriteResult
    {
        public readonly bool Ok;
        public readonly string Error;

        public WriteResult(bool ok, string error)
        {
            Ok = ok;
            Error = error;
        }
    }
}
