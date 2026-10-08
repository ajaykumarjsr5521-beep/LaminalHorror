from agents.rules import analyze
from agents.strategic_graph import StrategicPlanner
from memory.embeddings import DIM, HashEmbedder
from memory.memory_manager import DAY, MemoryManager
from memory.vector_store import VectorMemory
from models.commands import Strategy
from models.events import EventType, GameEvent, StrategyRequest
from models.memory import Memory


class Clock:
    def __init__(self):
        self.t = 1_000_000.0

    def __call__(self):
        return self.t


def req(**kw) -> StrategyRequest:
    base = dict(request_id="r", trigger="NEW_ROOM", run_frequency=0.3, walk_frequency=0.6, crouch_frequency=0.1,
                noise_frequency=0.1, risk_tolerance=0.4, average_chase_seconds=5, puzzle_seconds=30, hide_success_rate=0.8,
                death_count=0, hide_count=5, hide_usage=[{"key": "cupboard", "value": 0.8}, {"key": "bed", "value": 0.2}],
                route_usage=[{"key": "WEST", "value": 0.7}])
    base.update(kw)
    return StrategyRequest(**base)


def mgr(clock=None):
    clock = clock or Clock()
    return MemoryManager(VectorMemory(), clock=clock), clock


def test_embedding_is_deterministic_and_normalised():
    e = HashEmbedder()
    a, b = e.embed(["Player hides in cupboards"])[0], e.embed(["Player hides in cupboards"])[0]
    assert a == b and len(a) == DIM and abs(sum(x * x for x in a) - 1) < 1e-6


def test_pattern_memories_are_created_from_a_profile():
    m, _ = mgr()
    made = m.record_request(req(noise_frequency=0.8))
    assert {x.kind for x in made} == {"HIDE_PATTERN", "NOISE_HABIT", "ROUTE_HABIT"}
    assert m.vectors.count() == 3


def test_same_pattern_updates_one_record_and_raises_importance():
    m, _ = mgr()
    m.record_request(req())
    first = m.vectors.get("hide:cupboard")
    m.record_request(req())
    second = m.vectors.get("hide:cupboard")
    assert m.vectors.count() == 2 and second.count == 2 and second.importance > first.importance


def test_retrieval_returns_relevant_memory_first():
    m, _ = mgr()
    m.record_request(req(noise_frequency=0.8))
    m.record_event(GameEvent(type=EventType.DEATH, time=5, tag="ROOM_6"))
    sig = analyze(req())
    out = m.retrieve(req(), sig)
    assert out and "cupboard" in out[0]
    assert len(out) <= 3


def test_death_memory_is_retrievable_by_room_filter():
    m, clock = mgr()
    m.record_event(GameEvent(type=EventType.DEATH, time=5, tag="ROOM_6"))
    m.record_event(GameEvent(type=EventType.DEATH, time=6, tag="ROOM_2"))
    got = m.vectors.query("player died", clock(), room="ROOM_6")
    assert [g.room for g in got] == ["ROOM_6"]


def test_expired_memory_is_excluded():
    m, clock = mgr()
    m.record_request(req())
    assert m.retrieve(req(), analyze(req()))
    clock.t += 30 * DAY
    assert m.retrieve(req(), analyze(req())) == []


def test_low_importance_is_not_stored():
    m, _ = mgr()
    assert m.vectors.count() == 0
    assert m.record_request(req(hide_count=0, hide_usage=[], route_usage=[])) == []
    assert m.vectors.count() == 0


def test_memory_text_never_contains_positions_or_spot_ids():
    m, _ = mgr()
    m.record_request(req(noise_frequency=0.9))
    for key in ("hide:cupboard", "noise", "route:WEST"):
        text = m.vectors.get(key).text.lower()
        assert "position" not in text and "cupboard_" not in text


def test_planner_still_returns_a_strategy_when_vector_store_fails():
    class Broken(VectorMemory):
        def query(self, *a, **k):
            raise RuntimeError("chroma down")

    m = MemoryManager(Broken(), clock=Clock())
    p = StrategicPlanner(memory=m.retrieve)
    assert p(req()).strategy == Strategy.INCREASE_HIDING_PRESSURE
    assert "memory" in p.last_state["error"]


def test_memories_flow_into_the_graph():
    m, _ = mgr()
    m.record_request(req())
    seen = {}

    def proposer(r, s, mem):
        seen["mem"] = mem
        from models.commands import NO_STRATEGY
        return NO_STRATEGY

    StrategicPlanner(proposer=proposer, memory=m.retrieve)(req())
    assert any("cupboard" in x for x in seen["mem"])


def test_persistent_store_survives_reopen(tmp_path):
    clock = Clock()
    a = MemoryManager(VectorMemory(path=str(tmp_path)), clock=clock)
    a.record_request(req())
    b = MemoryManager(VectorMemory(path=str(tmp_path)), clock=clock)
    assert b.vectors.get("hide:cupboard").count == 1


def test_memory_schema_rejects_out_of_range():
    import pytest
    with pytest.raises(Exception):
        Memory(key="k", text="t", kind="X", importance=2, created_at=0, expires_at=1)
