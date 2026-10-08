import pytest
from fastapi.testclient import TestClient

from agents.horror_director import HorrorDirector, tension
from main import create_app
from memory.store import Store
from models.horror import LINES, HorrorEvent, HorrorRequest


class Clock:
    def __init__(self):
        self.t = 0.0

    def __call__(self):
        return self.t


def hr(**kw) -> HorrorRequest:
    base = dict(request_id="h1", stress=0.2, entity_proximity=0.1, entity_visible=False, darkness=0.3, recent_noise=0.0,
                chase_pressure=0.0, isolation=0.5, in_safe_zone=False, seconds_since_scare=300, recent_scares=0, death_count=0)
    base.update(kw)
    return HorrorRequest(**base)


def test_tension_rises_with_proximity_and_falls_in_safe_zone():
    far, near = tension(hr(entity_proximity=0.1)), tension(hr(entity_proximity=0.9))
    assert near > far
    assert tension(hr(entity_proximity=0.9, in_safe_zone=True)) < near


def test_recent_scare_lowers_tension():
    assert tension(hr(seconds_since_scare=5, entity_proximity=0.8)) < tension(hr(seconds_since_scare=300, entity_proximity=0.8))


def test_four_quiet_minutes_far_entity_gives_false_footsteps():
    s = HorrorDirector(Clock()).decide(hr(seconds_since_scare=240, entity_proximity=0.0, darkness=0.0, isolation=0.2, stress=0))
    assert s.event == HorrorEvent.FALSE_FOOTSTEPS


def test_never_scares_inside_the_gap():
    d = HorrorDirector(Clock())
    for secs in (0, 10, 44):
        assert d.decide(hr(seconds_since_scare=secs, entity_proximity=0.9, stress=1, darkness=1)).event == HorrorEvent.DO_NOTHING


def test_safe_zone_and_active_threat_do_nothing():
    d = HorrorDirector(Clock())
    assert d.decide(hr(in_safe_zone=True)).event == HorrorEvent.DO_NOTHING
    assert d.decide(hr(chase_pressure=0.9)).event == HorrorEvent.DO_NOTHING
    assert d.decide(hr(entity_visible=True)).event == HorrorEvent.DO_NOTHING


def test_silence_after_a_run_of_scares():
    assert HorrorDirector(Clock()).decide(hr(seconds_since_scare=60, recent_scares=3)).event == HorrorEvent.SILENCE


def test_do_nothing_is_common_not_rare():
    d = HorrorDirector(Clock())
    results = [d.decide(hr(seconds_since_scare=s, entity_proximity=p)).event for s in (10, 30, 50, 100, 150) for p in (0.0, 0.2)]
    assert results.count(HorrorEvent.DO_NOTHING) >= 3


def test_no_scare_spam_over_a_simulated_session():
    """Feed the director its own output: scares must stay at least MIN_GAP seconds apart."""
    d, since, fired = HorrorDirector(Clock()), 300.0, []
    for step in range(200):
        s = d.decide(hr(seconds_since_scare=since, entity_proximity=0.4, darkness=0.8, stress=0.6, recent_scares=len(fired[-3:])))
        since += 10
        if s.event not in (HorrorEvent.DO_NOTHING, HorrorEvent.SILENCE):
            fired.append(step * 10)
            since = 0
    gaps = [b - a for a, b in zip(fired, fired[1:])]
    assert fired and all(g >= 45 for g in gaps)


def test_lines_are_rare_contextual_and_from_the_fixed_list():
    clock = Clock()
    d = HorrorDirector(clock)
    first = d.decide(hr(death_count=1, entity_proximity=0.7, darkness=0.9, stress=0.5, seconds_since_scare=100))
    assert first.line_id == "AGAIN" and first.line == LINES["AGAIN"]
    clock.t += 100
    assert d.decide(hr(death_count=1, entity_proximity=0.7, darkness=0.9, stress=0.5, seconds_since_scare=100)).line is None
    clock.t += 600
    assert d.decide(hr(death_count=2, entity_proximity=0.7, darkness=0.9, stress=0.5, seconds_since_scare=100)).line == LINES["REMEMBER"]


def test_no_line_on_a_pause_or_for_a_first_time_player():
    d = HorrorDirector(Clock())
    assert d.decide(hr(death_count=3, in_safe_zone=True)).line is None
    assert d.decide(hr(death_count=0, seconds_since_scare=100, entity_proximity=0.7, darkness=0.9)).line is None


def test_api_horror_endpoint_and_validation():
    c = TestClient(create_app(Store()))
    body = hr(seconds_since_scare=240, entity_proximity=0.0, darkness=0.0, isolation=0.2, stress=0).model_dump()
    j = c.post("/horror", json=body).json()
    assert j["request_id"] == "h1" and j["event"] == "FALSE_FOOTSTEPS"
    assert c.post("/horror", json={**body, "stress": 3}).status_code == 422
    assert c.post("/horror", json={**body, "player_position": [1, 2, 3]}).status_code == 422


@pytest.mark.parametrize("name", [e.value for e in HorrorEvent])
def test_catalogue_names_are_upper_snake(name):
    assert name.isupper() and " " not in name
