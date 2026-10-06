using NUnit.Framework;
using NocturneAnnex.Liminal;

namespace NocturneAnnex.Tests.EditMode
{
    public class LiminalSupportTests
    {
        // ---------- QuietTimePolicy ----------

        [Test]
        public void QuietTime_GapsPerPhase()
        {
            Assert.AreEqual(90f, QuietTimePolicy.MinGapSeconds(EntityPhase.Clue));
            Assert.AreEqual(90f, QuietTimePolicy.MinGapSeconds(EntityPhase.Presence));
            Assert.AreEqual(45f, QuietTimePolicy.MinGapSeconds(EntityPhase.Hunt));
            Assert.AreEqual(180f, QuietTimePolicy.MinGapSeconds(EntityPhase.Aftermath));
        }

        [Test]
        public void QuietTime_Manifest_OnlyAfterTheGap_NeverBlocked_NeverInSurvival()
        {
            Assert.IsFalse(QuietTimePolicy.CanManifest(EntityPhase.Clue, 89f, 0f, false));
            Assert.IsTrue(QuietTimePolicy.CanManifest(EntityPhase.Clue, 90f, 0f, false));
            Assert.IsFalse(QuietTimePolicy.CanManifest(EntityPhase.Clue, 500f, 0f, true), "blocked");
            Assert.IsFalse(QuietTimePolicy.CanManifest(EntityPhase.Survival, 500f, 0f, false));
            Assert.IsTrue(QuietTimePolicy.CanManifest(EntityPhase.Hunt, 45f, 0f, false));
        }

        [Test]
        public void QuietTime_AfterAftermath_NeedsThreeMinutesOfQuiet()
        {
            Assert.IsFalse(QuietTimePolicy.CanManifest(EntityPhase.Aftermath, 500f, 100f, false), "only 100 s since the aftermath began");
            Assert.IsTrue(QuietTimePolicy.CanManifest(EntityPhase.Aftermath, 500f, 181f, false));
        }

        // ---------- AttentionTracker ----------

        [Test]
        public void Attention_CountsWatchedAndUnwatchedTime_AndResetsOnChange()
        {
            var a = new AttentionTracker();
            a.Tick(1f, false);
            a.Tick(2f, false);
            Assert.AreEqual(3f, a.UnwatchedSeconds, 1e-5f);
            a.Tick(0.5f, true);
            Assert.IsTrue(a.IsWatched);
            Assert.AreEqual(0.5f, a.WatchedSeconds, 1e-5f);
            Assert.AreEqual(0f, a.UnwatchedSeconds, "looking resets the time away");
            a.Tick(1f, false);
            Assert.AreEqual(0f, a.WatchedSeconds);
            Assert.AreEqual(1f, a.UnwatchedSeconds, 1e-5f);
            a.Reset();
            Assert.AreEqual(0f, a.UnwatchedSeconds);
        }

        // ---------- MusicStateSelector ----------

        static ExperienceState S(EntityPhase p, float tension = 0f, float dist = float.PositiveInfinity, float sinceAfter = 999f, bool vista = false) =>
            new ExperienceState { Phase = p, Tension = tension, EntityDistance = dist, SecondsSinceAftermath = sinceAfter, AtVista = vista };

        [Test]
        public void Music_Hunt_Silence_Presence_EntityNear()
        {
            Assert.AreEqual(MusicState.Hunting, MusicStateSelector.Select(S(EntityPhase.Hunt, 1f, 2f)));
            Assert.AreEqual(MusicState.Silence, MusicStateSelector.Select(S(EntityPhase.Survival)));
            Assert.AreEqual(MusicState.Silence, MusicStateSelector.Select(S(EntityPhase.Aftermath, sinceAfter: 10f)));
            Assert.AreEqual(MusicState.Exploration, MusicStateSelector.Select(S(EntityPhase.Aftermath, sinceAfter: 120f)));
            Assert.AreEqual(MusicState.Presence, MusicStateSelector.Select(S(EntityPhase.Presence)));
            Assert.AreEqual(MusicState.EntityNear, MusicStateSelector.Select(S(EntityPhase.Presence, dist: 8f)));
        }

        [Test]
        public void Music_Wonder_OnlyBeforePresence_Unease_ByPhaseOrTension_ElseExploration()
        {
            Assert.AreEqual(MusicState.Wonder, MusicStateSelector.Select(S(EntityPhase.Rumor, vista: true)));
            Assert.AreEqual(MusicState.Presence, MusicStateSelector.Select(S(EntityPhase.Presence, vista: true)), "no wonder once the entity is present");
            Assert.AreEqual(MusicState.Unease, MusicStateSelector.Select(S(EntityPhase.Clue)));
            Assert.AreEqual(MusicState.Unease, MusicStateSelector.Select(S(EntityPhase.Legend, tension: 0.6f)));
            Assert.AreEqual(MusicState.Exploration, MusicStateSelector.Select(S(EntityPhase.Legend, tension: 0.1f)));
            Assert.AreEqual(MusicState.Exploration, MusicStateSelector.Select(S(EntityPhase.Rumor)));
        }
    }
}
