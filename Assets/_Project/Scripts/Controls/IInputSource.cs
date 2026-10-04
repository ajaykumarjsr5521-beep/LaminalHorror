namespace NocturneAnnex.Controls
{
    public interface IInputSource
    {
        /// <summary>Writes this source's contribution for the current frame.</summary>
        void Poll(ref PlayerInputState state);

        /// <summary>Drops all held/pending input (app pause, focus loss, scene change).</summary>
        void ResetState();
    }
}
