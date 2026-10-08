"""LangGraph strategic state machine.

OBSERVE -> (cooldown? stop) -> ANALYZE -> RETRIEVE_MEMORY -> PLAN -> SELECT_STRATEGY -> MONITOR -> ADAPT

It runs only when Unity sends a trigger. It never touches game state directly: its only output is one validated
StrategyCommand. PLAN asks an optional proposer (LLM, F-15f) and falls back to the deterministic rules on any failure.
"""
import time
from collections.abc import Callable
from typing import Any, TypedDict

from langgraph.checkpoint.memory import InMemorySaver
from langgraph.graph import END, START, StateGraph

from agents import rules
from models.commands import NO_STRATEGY, Strategy, StrategyCommand
from models.events import StrategyRequest

Proposer = Callable[[StrategyRequest, dict, list], StrategyCommand]
MemoryFn = Callable[[StrategyRequest, dict], list]

COOLDOWN_SECONDS = 30.0
URGENT_TRIGGERS = {"DEATH"}          # these bypass the cooldown
PRESSURE_BREAK_AFTER = 2             # the same strategy twice in a row is followed by relief


class StrategicState(TypedDict, total=False):
    request: StrategyRequest
    signals: dict
    memories: list
    candidate: StrategyCommand | None
    command: StrategyCommand
    trace: list[str]
    error: str | None
    last_call: float
    history: list[str]
    source: str


def _t(state: StrategicState, name: str) -> list[str]:
    return [*state.get("trace", []), name]


def build_graph(proposer: Proposer | None = None, memory: MemoryFn | None = None,
                clock: Callable[[], float] = time.monotonic, cooldown: float = COOLDOWN_SECONDS):
    def observe(s: StrategicState) -> dict:
        return {"trace": ["OBSERVE"], "error": None, "candidate": None, "source": "none"}

    def gate(s: StrategicState) -> str:
        urgent = s["request"].trigger in URGENT_TRIGGERS
        last = s.get("last_call")
        if not urgent and last is not None and clock() - last < cooldown:
            return "cooldown"
        return "go"

    def cooled(s: StrategicState) -> dict:
        return {"command": NO_STRATEGY.model_copy(update={"reason_code": "COOLDOWN"}), "trace": _t(s, "COOLDOWN"), "source": "cooldown"}

    def analyze(s: StrategicState) -> dict:
        return {"signals": rules.analyze(s["request"]), "trace": _t(s, "ANALYZE")}

    def retrieve_memory(s: StrategicState) -> dict:
        try:
            mem = memory(s["request"], s["signals"]) if memory else []
        except Exception as e:  # vector store down: carry on without semantic memory
            return {"memories": [], "error": f"memory: {e}", "trace": _t(s, "RETRIEVE_MEMORY")}
        return {"memories": mem, "trace": _t(s, "RETRIEVE_MEMORY")}

    def plan(s: StrategicState) -> dict:
        if proposer:
            try:
                cand = StrategyCommand.model_validate(proposer(s["request"], s["signals"], s.get("memories", [])).model_dump())
                return {"candidate": cand, "source": "llm", "trace": _t(s, "PLAN")}
            except Exception as e:  # invalid, timeout, provider error: deterministic fallback
                return {"candidate": rules.propose(s["request"], s["signals"]), "source": "rules_fallback",
                        "error": f"proposer: {e}", "trace": _t(s, "PLAN")}
        return {"candidate": rules.propose(s["request"], s["signals"]), "source": "rules", "trace": _t(s, "PLAN")}

    def select_strategy(s: StrategicState) -> dict:
        cand = s.get("candidate") or NO_STRATEGY
        hist = s.get("history", [])
        if cand.strategy not in (Strategy.NONE, Strategy.RELAX_PRESSURE) and hist[-PRESSURE_BREAK_AFTER:] == [cand.strategy.value] * PRESSURE_BREAK_AFTER:
            cand = StrategyCommand(strategy=Strategy.RELAX_PRESSURE, priority=0.6, intensity=0.5, duration_seconds=60,
                                   confidence=0.7, reason_code="PRESSURE_BREAK")
        return {"command": cand, "trace": _t(s, "SELECT_STRATEGY")}

    def monitor(s: StrategicState) -> dict:
        return {"trace": _t(s, "MONITOR")}  # Unity reports outcomes through /events; used by the next trigger

    def adapt(s: StrategicState) -> dict:
        cmd = s["command"]
        out: dict[str, Any] = {"trace": _t(s, "ADAPT")}
        if cmd.strategy != Strategy.NONE:
            out["history"] = [*s.get("history", []), cmd.strategy.value][-10:]
            out["last_call"] = clock()
        return out

    g = StateGraph(StrategicState)
    for name, fn in [("observe", observe), ("cooled", cooled), ("analyze", analyze), ("retrieve_memory", retrieve_memory),
                     ("plan", plan), ("select_strategy", select_strategy), ("monitor", monitor), ("adapt", adapt)]:
        g.add_node(name, fn)
    g.add_edge(START, "observe")
    g.add_conditional_edges("observe", gate, {"cooldown": "cooled", "go": "analyze"})
    g.add_edge("cooled", END)
    for a, b in [("analyze", "retrieve_memory"), ("retrieve_memory", "plan"), ("plan", "select_strategy"),
                 ("select_strategy", "monitor"), ("monitor", "adapt"), ("adapt", END)]:
        g.add_edge(a, b)
    return g.compile(checkpointer=InMemorySaver())


class StrategicPlanner:
    """Adapter used by the API: one persistent thread per session so cooldown and history survive between calls."""

    def __init__(self, proposer: Proposer | None = None, memory: MemoryFn | None = None,
                 clock: Callable[[], float] = time.monotonic, cooldown: float = COOLDOWN_SECONDS, session: str = "default") -> None:
        self.graph = build_graph(proposer, memory, clock, cooldown)
        self.cfg = {"configurable": {"thread_id": session}}
        self.last_state: dict = {}

    def __call__(self, req: StrategyRequest) -> StrategyCommand:
        try:
            self.last_state = self.graph.invoke({"request": req}, self.cfg)
            return self.last_state["command"]
        except Exception:
            return NO_STRATEGY  # a broken graph must never break the game
