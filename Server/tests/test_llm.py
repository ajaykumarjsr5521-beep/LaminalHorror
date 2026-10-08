import json
import time

import pytest
from langchain_core.runnables import RunnableLambda

from agents.llm_proposer import LlmProposer, from_env
from agents.strategic_graph import StrategicPlanner
from memory.store import Store
from models.commands import Strategy, StrategyCommand
from models.events import StrategyRequest
from tools.game_tools import build_tools, difficulty_from, gather


def req(**kw) -> StrategyRequest:
    base = dict(request_id="r", trigger="NEW_ROOM", run_frequency=0.3, walk_frequency=0.6, crouch_frequency=0.1,
                noise_frequency=0.1, risk_tolerance=0.4, average_chase_seconds=5, puzzle_seconds=30, hide_success_rate=0.8,
                death_count=1, hide_count=5, hide_usage=[{"key": "cupboard", "value": 0.8}], route_usage=[{"key": "WEST", "value": 0.7}],
                room_time=[{"key": "ROOM_1", "value": 1.0}])
    base.update(kw)
    return StrategyRequest(**base)


GOOD = StrategyCommand(strategy=Strategy.INCREASE_HIDING_PRESSURE, priority=.8, intensity=.65, duration_seconds=90,
                       confidence=.87, reason_code="REPEATED_SUCCESSFUL_HIDING")


def test_tools_return_only_safe_fields():
    out = gather(req(), Store())
    blob = json.dumps(out).lower()
    for banned in ("position", "spot_id", "transform", "x\":", "coordinates"):
        assert banned not in blob
    assert out["get_hiding_statistics"]["usage_by_type"] == {"cupboard": 0.8}
    assert set(out) == {"get_player_behavior", "get_hiding_statistics", "get_room_statistics", "get_player_death_history",
                        "get_current_difficulty", "get_available_strategies"}


def test_tools_are_read_only_and_have_descriptions():
    for t in build_tools(req()):
        assert t.description and not t.args  # no arguments: nothing the model can steer


def test_available_strategies_lists_closed_enum():
    out = gather(req())["get_available_strategies"]
    assert "TELEPORT" not in out["strategies"] and "INCREASE_HIDING_PRESSURE" in out["strategies"]


def test_difficulty_drops_with_deaths():
    assert difficulty_from(req(death_count=3)) < difficulty_from(req(death_count=0))
    assert 0 <= difficulty_from(req(death_count=0, hide_count=5, hide_success_rate=1)) <= 1


def test_structured_output_and_prompt_contents():
    seen = {}

    def fake(prompt_value):
        seen["text"] = prompt_value.to_string()
        return GOOD

    p = LlmProposer(RunnableLambda(fake))
    cmd = p(req(), {"hide_reliant": True, "trigger": "NEW_ROOM"}, ["Player hides in cupboards when chased."])
    assert cmd == GOOD
    assert "cupboard" in seen["text"] and "Player hides in cupboards" in seen["text"] and "never how it moves" in seen["text"]
    assert p.calls == 1


def test_dict_output_is_revalidated():
    p = LlmProposer(RunnableLambda(lambda pv: GOOD.model_dump()))
    assert p(req(), {}, []).strategy == Strategy.INCREASE_HIDING_PRESSURE
    bad = LlmProposer(RunnableLambda(lambda pv: {**GOOD.model_dump(), "strategy": "TELEPORT"}))
    with pytest.raises(Exception):
        bad(req(), {}, [])


def test_timeout_raises():
    p = LlmProposer(RunnableLambda(lambda pv: (time.sleep(0.5), GOOD)[1]), timeout=0.05)
    with pytest.raises(TimeoutError):
        p(req(), {}, [])


def test_graph_uses_llm_and_falls_back_on_invalid_output():
    ok = StrategicPlanner(proposer=LlmProposer(RunnableLambda(lambda pv: GOOD)))
    c = ok(req())
    assert c.reason_code == "REPEATED_SUCCESSFUL_HIDING" and ok.last_state["source"] == "llm"

    broken = StrategicPlanner(proposer=LlmProposer(RunnableLambda(lambda pv: {"strategy": "TELEPORT"})))
    c = broken(req())
    assert broken.last_state["source"] == "rules_fallback" and c.strategy == Strategy.INCREASE_HIDING_PRESSURE

    def boom(pv):
        raise ConnectionError("provider down")

    down = StrategicPlanner(proposer=LlmProposer(RunnableLambda(boom)))
    assert down(req()).strategy == Strategy.INCREASE_HIDING_PRESSURE
    assert down.last_state["source"] == "rules_fallback"


def test_from_env_unset_means_rules_only(monkeypatch):
    monkeypatch.delenv("STRATEGY_LLM", raising=False)
    assert from_env() is None


def test_from_env_bad_provider_means_rules_only(monkeypatch):
    monkeypatch.setenv("STRATEGY_LLM", "nonexistent_provider:model")
    assert from_env() is None
