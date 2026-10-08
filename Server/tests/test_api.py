import pytest
from fastapi.testclient import TestClient

from main import create_app
from memory.store import Store
from models.commands import Strategy, StrategyCommand


def profile(**kw):
    base = dict(request_id="req-1", trigger="NEW_ROOM", run_frequency=0.5, walk_frequency=0.4, crouch_frequency=0.1,
                noise_frequency=0.2, risk_tolerance=0.5, average_chase_seconds=8, puzzle_seconds=30, hide_success_rate=0.6,
                death_count=1, hide_count=3, hide_usage=[{"key": "cupboard", "value": 1.0}])
    base.update(kw)
    return base


@pytest.fixture
def client():
    store = Store()
    c = TestClient(create_app(store))
    c.store = store
    return c


def test_health(client):
    assert client.get("/health").json() == {"status": "ok"}


def test_events_persist(client):
    body = [{"type": "NOISE", "time": 1.5, "value": 0.7}, {"type": "HIDE", "time": 3, "value": 1, "tag": "cupboard"}]
    assert client.post("/events", json=body).json() == {"stored": 2}
    assert client.store.count_events() == 2 and client.store.count_events("HIDE") == 1


def test_unknown_event_type_rejected(client):
    assert client.post("/events", json=[{"type": "TELEPORT", "time": 1}]).status_code == 422


def test_strategy_echoes_request_id_and_logs(client):
    r = client.post("/strategy", json=profile())
    assert r.status_code == 200
    j = r.json()
    assert j["request_id"] == "req-1" and j["strategy"] == "INCREASE_HIDING_PRESSURE"
    assert client.store.strategy_log()[0][0] == "req-1"


def test_request_id_header_roundtrip(client):
    r = client.get("/health", headers={"x-request-id": "abc123"})
    assert r.headers["x-request-id"] == "abc123"


def test_profile_with_extra_field_rejected(client):
    assert client.post("/strategy", json=profile(player_position=[1, 2, 3])).status_code == 422


def test_profile_out_of_range_rejected(client):
    assert client.post("/strategy", json=profile(run_frequency=1.5)).status_code == 422


def test_schema_rejects_unknown_strategy_and_ranges():
    ok = dict(strategy="INCREASE_HIDING_PRESSURE", priority=.8, intensity=.6, duration_seconds=90, confidence=.8, reason_code="X_Y")
    StrategyCommand(**ok)
    for bad in (dict(strategy="TELEPORT"), dict(priority=1.2), dict(duration_seconds=5), dict(duration_seconds=500),
                dict(reason_code="has spaces"), dict(position=[1, 2, 3])):
        with pytest.raises(Exception):
            StrategyCommand(**{**ok, **bad})


def test_bad_planner_output_is_not_returned():
    class Evil:
        def model_dump(self):
            return dict(strategy="TELEPORT", priority=1, intensity=1, duration_seconds=30, confidence=1, reason_code="X")

    c = TestClient(create_app(Store(), planner=lambda req: Evil()), raise_server_exceptions=False)
    assert c.post("/strategy", json=profile()).status_code == 500


def test_valid_planner_flows_through():
    cmd = StrategyCommand(strategy=Strategy.INCREASE_HIDING_PRESSURE, priority=.8, intensity=.65, duration_seconds=90,
                          confidence=.87, reason_code="REPEATED_SUCCESSFUL_HIDING")
    c = TestClient(create_app(Store(), planner=lambda req: cmd))
    j = c.post("/strategy", json=profile()).json()
    assert j["strategy"] == "INCREASE_HIDING_PRESSURE" and j["duration_seconds"] == 90


def test_strategy_request_writes_semantic_memory():
    app = create_app(Store())
    c = TestClient(app)
    c.post("/strategy", json=profile())
    assert app.state.memory.vectors.count() >= 1


def test_death_event_creates_memory_and_vector_failure_does_not_break_api():
    app = create_app(Store())
    c = TestClient(app)
    c.post("/events", json=[{"type": "DEATH", "time": 4, "tag": "ROOM_6"}])
    assert app.state.memory.vectors.get("death:ROOM_6") is not None

    def boom(*a, **k):
        raise RuntimeError("chroma down")

    app.state.memory.vectors.upsert = boom
    assert c.post("/events", json=[{"type": "DEATH", "time": 9, "tag": "ROOM_7"}]).status_code == 200
    assert c.post("/strategy", json=profile()).status_code == 200
