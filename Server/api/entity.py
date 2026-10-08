"""REST endpoints for the strategic layer. Unity calls these on triggers only, never per frame."""
from collections.abc import Callable

from fastapi import APIRouter

from memory.store import Store
from models.commands import StrategyCommand, StrategyResponse
from models.events import GameEvent, StrategyRequest


def build_router(store: Store, planner: Callable[[StrategyRequest], StrategyCommand]) -> APIRouter:
    r = APIRouter()

    @r.get("/health")
    def health() -> dict:
        return {"status": "ok"}

    @r.post("/events")
    def events(batch: list[GameEvent]) -> dict:
        return {"stored": store.add_events(batch)}

    @r.post("/strategy", response_model=StrategyResponse)
    def strategy(req: StrategyRequest) -> StrategyResponse:
        cmd = StrategyCommand.model_validate(planner(req).model_dump())  # re-validate whatever the planner returned
        store.log_strategy(req.request_id, req.trigger, cmd.strategy.value, cmd.confidence, cmd.reason_code)
        return StrategyResponse(request_id=req.request_id, **cmd.model_dump())

    return r
