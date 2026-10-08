"""REST endpoints for the strategic layer. Unity calls these on triggers only, never per frame."""
import logging
from collections.abc import Callable

from fastapi import APIRouter

from memory.memory_manager import MemoryManager
from memory.store import Store
from models.commands import StrategyCommand, StrategyResponse
from agents.horror_director import HorrorDirector
from models.events import GameEvent, StrategyRequest
from models.horror import HorrorRequest, HorrorSuggestion


log = logging.getLogger("strategy")


def _safely(fn) -> None:
    try:
        fn()
    except Exception:
        log.exception("memory write failed; continuing")


def build_router(store: Store, planner: Callable[[StrategyRequest], StrategyCommand], memory: MemoryManager | None = None,
                 director: HorrorDirector | None = None) -> APIRouter:
    r = APIRouter()
    director = director or HorrorDirector()

    @r.get("/health")
    def health() -> dict:
        return {"status": "ok"}

    @r.post("/events")
    def events(batch: list[GameEvent]) -> dict:
        n = store.add_events(batch)
        for e in batch:
            _safely(lambda e=e: memory.record_event(e) if memory else None)
        return {"stored": n}

    @r.post("/strategy", response_model=StrategyResponse)
    def strategy(req: StrategyRequest) -> StrategyResponse:
        _safely(lambda: memory.record_request(req) if memory else None)  # semantic memory is optional
        cmd = StrategyCommand.model_validate(planner(req).model_dump())  # re-validate whatever the planner returned
        store.log_strategy(req.request_id, req.trigger, cmd.strategy.value, cmd.confidence, cmd.reason_code)
        return StrategyResponse(request_id=req.request_id, **cmd.model_dump())

    @r.post("/horror", response_model=HorrorSuggestion)
    def horror(req: HorrorRequest) -> HorrorSuggestion:
        return director.decide(req)

    return r
