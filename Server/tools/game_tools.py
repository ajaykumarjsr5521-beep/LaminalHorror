"""LangChain tools the strategist can read. Read-only, and each returns only fields that are safe to show an LLM:
ratios and categories, never positions or specific hide spots. Deterministic facts (doors, keys) are deliberately absent."""
import json

from langchain_core.tools import BaseTool, tool

from memory.store import Store
from models.commands import MAX_DURATION, MIN_DURATION, Strategy
from models.events import StrategyRequest


def difficulty_from(req: StrategyRequest) -> float:
    """0..1 from deaths and hide success; higher means the player is doing well, so more pressure is fair."""
    base = 0.5 + 0.1 * min(req.hide_count, 5) * req.hide_success_rate - 0.15 * min(req.death_count, 3)
    return round(max(0.0, min(1.0, base)), 3)


def build_tools(req: StrategyRequest, store: Store | None = None) -> list[BaseTool]:
    @tool
    def get_player_behavior() -> str:
        """Movement, noise and risk ratios for the current player (all 0..1 except chase and puzzle seconds)."""
        return json.dumps({
            "run_frequency": req.run_frequency, "walk_frequency": req.walk_frequency, "crouch_frequency": req.crouch_frequency,
            "noise_frequency": req.noise_frequency, "risk_tolerance": req.risk_tolerance,
            "average_chase_seconds": req.average_chase_seconds, "puzzle_seconds": req.puzzle_seconds,
        })

    @tool
    def get_hiding_statistics() -> str:
        """How often and how successfully the player hides, by hiding-spot TYPE (not specific spots)."""
        return json.dumps({"hide_count": req.hide_count, "hide_success_rate": req.hide_success_rate,
                           "usage_by_type": {k.key: k.value for k in req.hide_usage}})

    @tool
    def get_room_statistics() -> str:
        """Share of time per room and the route groups the player repeats."""
        return json.dumps({"room_time": {k.key: k.value for k in req.room_time}, "routes": {k.key: k.value for k in req.route_usage}})

    @tool
    def get_player_death_history() -> str:
        """Number of deaths so far this run and logged death events."""
        logged = store.count_events("DEATH") if store else 0
        return json.dumps({"deaths_this_run": req.death_count, "deaths_logged": logged})

    @tool
    def get_current_difficulty() -> str:
        """Estimated difficulty 0..1 (higher = player coping well, more pressure is fair)."""
        return json.dumps({"difficulty": difficulty_from(req)})

    @tool
    def get_available_strategies() -> str:
        """The only strategies that may be chosen, with the allowed duration range in seconds."""
        return json.dumps({"strategies": [s.value for s in Strategy], "duration_seconds": [MIN_DURATION, MAX_DURATION]})

    return [get_player_behavior, get_hiding_statistics, get_room_statistics, get_player_death_history,
            get_current_difficulty, get_available_strategies]


def gather(req: StrategyRequest, store: Store | None = None) -> dict:
    """Runs every tool once and returns name -> parsed result. One cheap pass instead of an LLM tool loop (cost control)."""
    return {t.name: json.loads(t.invoke({})) for t in build_tools(req, store)}
