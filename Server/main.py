"""FastAPI entry point. Run: uvicorn main:app --port 8765"""
import logging
import os
import uuid
from collections.abc import Callable

from fastapi import FastAPI, Request

from api.entity import build_router
from memory.store import Store
from agents.llm_proposer import from_env
from agents.strategic_graph import StrategicPlanner
from models.commands import StrategyCommand
from models.events import StrategyRequest

log = logging.getLogger("strategy")
Planner = Callable[[StrategyRequest], StrategyCommand]


def create_app(store: Store | None = None, planner: Planner | None = None) -> FastAPI:
    store = store or Store(os.environ.get("STRATEGY_DB", ":memory:"))
    planner = planner or StrategicPlanner(proposer=from_env(store))  # no STRATEGY_LLM: rules only
    app = FastAPI(title="Strategic AI", version="0.1")
    app.state.store = store

    @app.middleware("http")
    async def request_id_log(request: Request, call_next):
        rid = request.headers.get("x-request-id") or uuid.uuid4().hex[:8]
        response = await call_next(request)
        response.headers["x-request-id"] = rid
        log.info("%s %s -> %s [%s]", request.method, request.url.path, response.status_code, rid)
        return response

    app.include_router(build_router(store, planner))
    return app


app = create_app()
