import pytest

from agents.strategic_graph import StrategicPlanner
from models.commands import NO_STRATEGY, Strategy, StrategyCommand
from models.events import StrategyRequest


class Clock:
    def __init__(self):
        self.t = 1000.0

    def __call__(self):
        return self.t


def req(**kw) -> StrategyRequest:
    base = dict(request_id="r", trigger="NEW_ROOM", run_frequency=0.3, walk_frequency=0.6, crouch_frequency=0.1,
                noise_frequency=0.1, risk_tolerance=0.4, average_chase_seconds=5, puzzle_seconds=30, hide_success_rate=0.5,
                death_count=0, hide_count=0)
    base.update(kw)
    return StrategyRequest(**base)


HIDER = dict(hide_count=5, hide_success_rate=0.8, hide_usage=[{"key": "cupboard", "value": 0.8}, {"key": "bed", "value": 0.2}])


def planner(**kw):
    clock = Clock()
    return StrategicPlanner(clock=clock, **kw), clock


def test_quiet_player_gets_no_strategy():
    p, _ = planner()
    assert p(req()).strategy == Strategy.NONE
    assert p.last_state["trace"] == ["OBSERVE", "ANALYZE", "RETRIEVE_MEMORY", "PLAN", "SELECT_STRATEGY", "MONITOR", "ADAPT"]


def test_hider_gets_hiding_pressure_within_caps():
    p, _ = planner()
    c = p(req(**HIDER))
    assert c.strategy == Strategy.INCREASE_HIDING_PRESSURE
    assert c.reason_code == "REPEATED_SUCCESSFUL_HIDING" and c.intensity <= 0.8 and 10 <= c.duration_seconds <= 180


def test_noisy_player_gets_investigation():
    p, _ = planner()
    assert p(req(noise_frequency=0.8)).strategy == Strategy.INCREASE_INVESTIGATION


def test_repeated_route_changes_patrol():
    p, _ = planner()
    assert p(req(route_usage=[{"key": "WEST", "value": 0.7}])).strategy == Strategy.CHANGE_PATROL_PREFERENCE


def test_struggling_player_gets_relief_before_pressure():
    p, _ = planner()
    c = p(req(death_count=3, hide_success_rate=0.1, noise_frequency=0.9))
    assert c.strategy == Strategy.RELAX_PRESSURE


def test_cooldown_blocks_second_call_then_expires():
    p, clock = planner()
    assert p(req(**HIDER)).strategy == Strategy.INCREASE_HIDING_PRESSURE
    clock.t += 5
    c = p(req(**HIDER))
    assert c.strategy == Strategy.NONE and c.reason_code == "COOLDOWN" and p.last_state["trace"] == ["OBSERVE", "COOLDOWN"]
    clock.t += 40
    assert p(req(**HIDER)).strategy != Strategy.NONE


def test_death_bypasses_cooldown():
    p, clock = planner()
    p(req(**HIDER))
    clock.t += 1
    assert p(req(trigger="DEATH", **HIDER)).strategy != Strategy.NONE


def test_same_pressure_twice_is_followed_by_relief():
    p, clock = planner()
    seen = []
    for _ in range(3):
        seen.append(p(req(**HIDER)))
        clock.t += 100
    assert [c.strategy for c in seen] == [Strategy.INCREASE_HIDING_PRESSURE] * 2 + [Strategy.RELAX_PRESSURE]
    assert seen[2].reason_code == "PRESSURE_BREAK"


def test_valid_proposer_is_used():
    cmd = StrategyCommand(strategy=Strategy.CHANGE_SEARCH_PRIORITY, priority=.5, intensity=.5, duration_seconds=60, confidence=.9, reason_code="LLM_PICK")
    p, _ = planner(proposer=lambda r, s, m: cmd)
    assert p(req()).strategy == Strategy.CHANGE_SEARCH_PRIORITY and p.last_state["source"] == "llm"


@pytest.mark.parametrize("bad", ["raise", "invalid"])
def test_failing_or_invalid_proposer_falls_back_to_rules(bad):
    class Bad:
        def model_dump(self):
            return dict(strategy="TELEPORT", priority=1, intensity=1, duration_seconds=30, confidence=1, reason_code="X")

    def proposer(r, s, m):
        if bad == "raise":
            raise TimeoutError("llm timeout")
        return Bad()

    p, _ = planner(proposer=proposer)
    c = p(req(**HIDER))
    assert c.strategy == Strategy.INCREASE_HIDING_PRESSURE
    assert p.last_state["source"] == "rules_fallback" and "proposer" in p.last_state["error"]


def test_memory_failure_does_not_stop_planning():
    def boom(r, s):
        raise RuntimeError("vector db down")

    p, _ = planner(memory=boom)
    assert p(req(**HIDER)).strategy == Strategy.INCREASE_HIDING_PRESSURE
    assert "memory" in p.last_state["error"]


def test_memories_reach_the_proposer():
    got = {}

    def proposer(r, s, m):
        got["m"] = m
        return NO_STRATEGY

    p, _ = planner(proposer=proposer, memory=lambda r, s: ["Player uses cupboards when chased."])
    p(req())
    assert got["m"] == ["Player uses cupboards when chased."]


def test_graph_crash_returns_no_strategy():
    p, _ = planner()
    p.graph = None  # simulate a broken graph
    assert p(req()).strategy == Strategy.NONE
