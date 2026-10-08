import json
import pathlib
import random

from fastapi.testclient import TestClient

from agents.strategic_graph import StrategicPlanner
from main import create_app
from memory.store import Store
from models.commands import MAX_DURATION, MIN_DURATION, Strategy, StrategyCommand
from models.events import StrategyRequest

CONTRACT = pathlib.Path(__file__).parent / "contract"


def test_unity_request_fixture_is_accepted_and_response_matches_unity_fixture_keys():
    """The same two files are read by the Unity StrategyContractTests, so both sides break together."""
    body = json.loads((CONTRACT / "request.json").read_text())
    StrategyRequest.model_validate(body)
    r = TestClient(create_app(Store())).post("/strategy", json=body)
    assert r.status_code == 200
    expected = json.loads((CONTRACT / "response.json").read_text())
    assert set(r.json()) == set(expected)
    StrategyCommand.model_validate({k: v for k, v in r.json().items() if k != "request_id"})


def random_request(rng: random.Random) -> StrategyRequest:
    def share():
        return round(rng.random(), 3)

    return StrategyRequest(
        request_id="f", trigger=rng.choice(["NEW_ROOM", "DEATH", "ENCOUNTER_ENDED"]),
        run_frequency=share(), walk_frequency=share(), crouch_frequency=share(), noise_frequency=share(), risk_tolerance=share(),
        average_chase_seconds=rng.random() * 60, puzzle_seconds=rng.random() * 300, hide_success_rate=share(),
        death_count=rng.randint(0, 6), hide_count=rng.randint(0, 20),
        hide_usage=[{"key": rng.choice(["cupboard", "bed", "closet"]), "value": share()} for _ in range(rng.randint(0, 3))],
        route_usage=[{"key": rng.choice(["WEST", "EAST"]), "value": share()} for _ in range(rng.randint(0, 2))])


def test_fairness_every_output_is_inside_hard_caps_over_1000_random_profiles():
    rng = random.Random(7)
    clock_t = [0.0]
    p = StrategicPlanner(clock=lambda: clock_t[0], cooldown=0)
    seen = set()
    for _ in range(1000):
        clock_t[0] += 100
        c = p(random_request(rng))
        seen.add(c.strategy)
        assert MIN_DURATION <= c.duration_seconds <= MAX_DURATION
        assert 0 <= c.priority <= 1 and 0 <= c.intensity <= 1 and 0 <= c.confidence <= 1
        assert c.strategy in set(Strategy)
        if c.strategy == Strategy.INCREASE_HIDING_PRESSURE:
            assert c.intensity <= 0.8                       # pressure is never maximal
    assert len(seen) >= 3                                   # the planner actually varies


def test_schema_has_no_position_or_target_fields():
    for model in (StrategyCommand, StrategyRequest):
        for name in model.model_fields:
            assert not any(w in name for w in ("position", "target", "transform", "speed", "coordinate", "spot_id"))


def test_pressure_is_never_permanent():
    """Same hiding profile every 100 s for an hour: relief must appear regularly."""
    rng_req = StrategyRequest(request_id="x", trigger="NEW_ROOM", run_frequency=.3, walk_frequency=.6, crouch_frequency=.1,
                              noise_frequency=.1, risk_tolerance=.4, average_chase_seconds=5, puzzle_seconds=30, hide_success_rate=.8,
                              death_count=0, hide_count=6, hide_usage=[{"key": "cupboard", "value": .9}])
    t = [0.0]
    p = StrategicPlanner(clock=lambda: t[0], cooldown=0)
    out = []
    for _ in range(36):
        t[0] += 100
        out.append(p(rng_req).strategy)
    assert Strategy.RELAX_PRESSURE in out
    assert max(len(list(g)) for g in _runs(out, Strategy.INCREASE_HIDING_PRESSURE)) <= 2


def _runs(items, value):
    run = []
    for i in items:
        if i == value:
            run.append(i)
        else:
            if run:
                yield run
            run = []
    if run:
        yield run
    yield []
