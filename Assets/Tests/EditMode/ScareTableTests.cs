#if UNITY_EDITOR
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using NocturneAnnex.Audio;
using NocturneAnnex.Core;
using NocturneAnnex.Horror;

namespace NocturneAnnex.Tests.EditMode
{
    /// <summary>F-14j: the authored scare table has the intended mix, and every event has a caption and a cue.</summary>
    public class ScareTableTests
    {
        const string Dir = "Assets/_Project/Data/Events";

        static HorrorEventAsset[] Load(bool rooms) =>
            Directory.GetFiles(Dir, "Event_*.asset")
                .Select(p => AssetDatabase.LoadAssetAtPath<HorrorEventAsset>(p.Replace('\\', '/')))
                .Where(a => a != null && a.Id.StartsWith("room_") == rooms)
                .ToArray();

        [Test]
        public void Level_B1_HasTwelveEvents_AboutFortyPercentHarmless_AndTwentyPercentEntityLinked()
        {
            var all = Load(false);
            Assert.AreEqual(12, all.Length);
            float harmless = all.Count(a => a.Tag == ScareTag.Harmless) / (float)all.Length;
            float linked = all.Count(a => a.Tag == ScareTag.EntityLinked) / (float)all.Length;
            Assert.That(harmless, Is.InRange(0.3f, 0.5f), "harmless share");
            Assert.That(linked, Is.InRange(0.1f, 0.3f), "entity-linked share");
        }

        [Test]
        public void EveryEvent_HasAUniqueId_ACaption_AndACue()
        {
            var cues = CueCatalog.CreateDefault();
            var all = Load(false).Concat(Load(true)).ToArray();
            Assert.AreEqual(all.Length, all.Select(a => a.Id).Distinct().Count());
            foreach (var a in all)
            {
                Assert.IsNotEmpty(a.CaptionKey, a.Id);
                Assert.IsTrue(DefaultStrings.English.ContainsKey(a.CaptionKey), a.Id + " caption " + a.CaptionKey);
                Assert.IsNotNull(cues.Find(a.CaptionKey), a.Id + " cue " + a.CaptionKey);
            }
        }

        [Test]
        public void RoomEvents_AreRepeatable_SoTheirSavedFlagsNeverBlockALaterRoom()
        {
            var rooms = Load(true);
            Assert.AreEqual(5, rooms.Length);
            foreach (var a in rooms) Assert.IsFalse(a.Once, a.Id);
        }

        [Test]
        public void ToyBoxAndEntityStepCues_Exist_WithCaptions()
        {
            var cues = CueCatalog.CreateDefault();
            foreach (var id in new[] { "toybox.spring", "toybox.beat", "entity.step.wood.0", "entity.step.normal.3" })
                Assert.IsNotNull(cues.Find(id), id);
            Assert.That(cues.Validate(k => DefaultStrings.English.ContainsKey(k)), Is.Empty);
        }
    }
}
#endif
