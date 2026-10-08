"""Deterministic analysis and rule planner. This is the default strategist and the fallback when an LLM is absent or invalid."""
from models.commands import NO_STRATEGY, Strategy, StrategyCommand
from models.events import StrategyRequest

HIDE_MIN_COUNT = 3
HIDE_RELIANCE_SHARE = 0.6
HIDE_SUCCESS = 0.6
NOISY = 0.6
ROUTE_SHARE = 0.6
STRUGGLING_DEATHS = 2
STRUGGLING_HIDE_SUCCESS = 0.3


def _top(items) -> tuple[str | None, float]:
    best = max(items, key=lambda i: i.value, default=None)
    return (best.key, best.value) if best else (None, 0.0)


def analyze(req: StrategyRequest) -> dict:
    """Turns the raw profile into named signals. Pure and deterministic."""
    hide_type, hide_share = _top(req.hide_usage)
    route, route_share = _top(req.route_usage)
    return {
        "hide_reliant": req.hide_count >= HIDE_MIN_COUNT and hide_share >= HIDE_RELIANCE_SHARE and req.hide_success_rate >= HIDE_SUCCESS,
        "hide_type": hide_type,
        "noisy": req.noise_frequency >= NOISY,
        "route_repeat": route if route_share >= ROUTE_SHARE else None,
        "struggling": req.death_count >= STRUGGLING_DEATHS and req.hide_success_rate < STRUGGLING_HIDE_SUCCESS,
        "trigger": req.trigger,
    }


def _cmd(strategy: Strategy, priority: float, intensity: float, duration: float, confidence: float, reason: str) -> StrategyCommand:
    return StrategyCommand(strategy=strategy, priority=round(priority, 3), intensity=round(intensity, 3),
                           duration_seconds=duration, confidence=round(confidence, 3), reason_code=reason)


def propose(req: StrategyRequest, signals: dict) -> StrategyCommand:
    """First matching rule wins. Fairness comes first: a struggling player gets relief before any new pressure."""
    if signals["struggling"]:
        return _cmd(Strategy.RELAX_PRESSURE, 0.8, 0.6, 120, 0.8, "PLAYER_STRUGGLING")
    if signals["hide_reliant"]:
        return _cmd(Strategy.INCREASE_HIDING_PRESSURE, 0.6 + 0.2 * req.hide_success_rate, min(0.8, 0.3 + 0.5 * req.hide_success_rate),
                    90, 0.75, "REPEATED_SUCCESSFUL_HIDING")
    if signals["noisy"]:
        return _cmd(Strategy.INCREASE_INVESTIGATION, 0.6, min(0.8, req.noise_frequency), 90, 0.7, "FREQUENT_NOISE")
    if signals["route_repeat"]:
        return _cmd(Strategy.CHANGE_PATROL_PREFERENCE, 0.5, 0.5, 120, 0.65, "REPEATED_ROUTE")
    return NO_STRATEGY
