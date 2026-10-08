using NUnit.Framework;
using NocturneAnnex.Strategy;

namespace NocturneAnnex.Tests.EditMode
{
    public class HorrorSuggestionGateTests
    {
        static HorrorSuggestionDto S(string ev, string lineId = null) => new HorrorSuggestionDto { event_name = ev, line_id = lineId, line = "ignored text" };

        [Test] public void Unknown_event_is_ignored()
        {
            var g = new HorrorSuggestionGate();
            Assert.AreEqual(GateResult.UnknownEvent, g.Check(S("EXPLODE_PLAYER"), 100f));
            Assert.AreEqual(GateResult.UnknownEvent, g.Check(null, 100f));
            Assert.AreEqual(GateResult.UnknownEvent, g.Check(S(""), 100f));
            Assert.IsNull(g.AcceptedEvent);
        }

        [Test] public void Catalogue_event_accepted()
        {
            var g = new HorrorSuggestionGate();
            Assert.AreEqual(GateResult.Accepted, g.Check(S("FALSE_FOOTSTEPS"), 100f));
            Assert.AreEqual("FALSE_FOOTSTEPS", g.AcceptedEvent);
            Assert.IsFalse(g.IsPause);
        }

        [Test] public void Do_nothing_and_silence_are_valid_pauses_that_never_hit_the_gap()
        {
            var g = new HorrorSuggestionGate();
            g.Check(S("LIGHT_FLICKER"), 100f);
            Assert.AreEqual(GateResult.Accepted, g.Check(S("DO_NOTHING"), 101f));
            Assert.IsTrue(g.IsPause);
            Assert.AreEqual(GateResult.Accepted, g.Check(S("SILENCE"), 102f));
            Assert.IsTrue(g.IsPause);
        }

        [Test] public void Scare_gap_of_45_seconds_holds()
        {
            var g = new HorrorSuggestionGate();
            Assert.AreEqual(GateResult.Accepted, g.Check(S("LIGHT_FLICKER"), 100f));
            Assert.AreEqual(GateResult.TooSoon, g.Check(S("SHADOW_EVENT"), 144f));
            Assert.AreEqual(GateResult.Accepted, g.Check(S("SHADOW_EVENT"), 145f));
        }

        [Test] public void Line_text_comes_from_local_list_and_is_rate_limited()
        {
            var g = new HorrorSuggestionGate();
            g.Check(S("SHADOW_EVENT", "AGAIN"), 100f);
            Assert.AreEqual("You again.", g.AcceptedLine);       // not "ignored text"
            g.Check(S("SHADOW_EVENT", "REMEMBER"), 200f);
            Assert.IsNull(g.AcceptedLine);                       // inside ten minutes
            g.Check(S("SHADOW_EVENT", "REMEMBER"), 701f);
            Assert.AreEqual("I remember.", g.AcceptedLine);
        }

        [Test] public void Unknown_line_id_is_rejected_and_pause_carries_no_line()
        {
            var g = new HorrorSuggestionGate();
            Assert.AreEqual(GateResult.BadLine, g.Check(S("SHADOW_EVENT", "DIE_NOW"), 100f));
            Assert.IsNull(g.AcceptedEvent);
            Assert.AreEqual(GateResult.Accepted, g.Check(S("SILENCE", "AGAIN"), 100f));
            Assert.IsNull(g.AcceptedLine);
        }

        [Test] public void Parse_maps_server_json_and_survives_garbage()
        {
            var d = HorrorSuggestionGate.Parse("{\"request_id\":\"h1\",\"event\":\"FALSE_FOOTSTEPS\",\"tension\":0.2,\"line_id\":null,\"reason_code\":\"X\"}");
            Assert.AreEqual("FALSE_FOOTSTEPS", d.event_name);
            Assert.IsNull(HorrorSuggestionGate.Parse("not json"));
            Assert.IsNull(HorrorSuggestionGate.Parse(""));
        }
    }
}
