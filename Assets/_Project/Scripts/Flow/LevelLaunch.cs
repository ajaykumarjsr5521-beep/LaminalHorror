namespace NocturneAnnex.Flow
{
    /// <summary>
    /// One-shot hand-off from the main menu to the level scene: whether to resume the save or start fresh.
    /// Consumed once by the level so a later reload of the scene never repeats a stale request.
    /// </summary>
    public static class LevelLaunch
    {
        public static bool ContinueSave { get; private set; }

        public static void RequestContinue() => ContinueSave = true;
        public static void RequestNewGame() => ContinueSave = false;

        public static bool ConsumeContinue()
        {
            bool value = ContinueSave;
            ContinueSave = false;
            return value;
        }
    }
}
