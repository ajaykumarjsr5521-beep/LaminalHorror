"""LLM proposer: asks a chat model for ONE StrategyCommand using structured output.

The model only sees tool output and retrieved memory summaries, answers with the command schema, and is bounded by a timeout.
Anything wrong (timeout, provider error, schema violation) raises, and the graph falls back to the deterministic rules.
"""
import json
import os
from collections.abc import Callable
from concurrent.futures import ThreadPoolExecutor
from concurrent.futures import TimeoutError as FutureTimeout

from langchain_core.prompts import ChatPromptTemplate
from langchain_core.runnables import Runnable

from memory.store import Store
from models.commands import StrategyCommand
from models.events import StrategyRequest
from tools.game_tools import gather

SYSTEM = (
    "You are the strategist of a horror game entity. You decide WHAT the entity should try, never how it moves. "
    "Choose exactly one strategy from the allowed list. Rules: never exceed the allowed ranges; pressure must be temporary "
    "and fair; prefer NONE when the evidence is weak; if the player is struggling, choose RELAX_PRESSURE. "
    "reason_code is UPPER_SNAKE_CASE, at most 48 characters. You know nothing about positions or hiding places."
)
HUMAN = "Trigger: {trigger}\nObservations (JSON):\n{observations}\nRelevant memories:\n{memories}\nChoose the strategy."

PROMPT = ChatPromptTemplate.from_messages([("system", SYSTEM), ("human", HUMAN)])


class LlmProposer:
    """Callable matching the graph's Proposer signature. `structured_llm` must be a Runnable that returns a StrategyCommand."""

    def __init__(self, structured_llm: Runnable, store: Store | None = None, timeout: float = 2.0) -> None:
        self._chain = PROMPT | structured_llm
        self._store = store
        self._timeout = timeout
        self._pool = ThreadPoolExecutor(max_workers=1)
        self.calls = 0

    def __call__(self, req: StrategyRequest, signals: dict, memories: list) -> StrategyCommand:
        self.calls += 1
        payload = {
            "trigger": req.trigger,
            "observations": json.dumps({**gather(req, self._store), "signals": {k: v for k, v in signals.items() if k != "trigger"}}),
            "memories": "\n".join(f"- {m}" for m in memories[:3]) or "(none)",
        }
        fut = self._pool.submit(self._chain.invoke, payload)
        try:
            out = fut.result(timeout=self._timeout)
        except FutureTimeout as e:
            fut.cancel()
            raise TimeoutError("llm timeout") from e
        if isinstance(out, StrategyCommand):
            return out
        return StrategyCommand.model_validate(out)  # dict or other output: strict re-validation, raises when invalid


def from_env(store: Store | None = None) -> LlmProposer | None:
    """STRATEGY_LLM="provider:model" (for example anthropic:claude-haiku-4-5-20251001). Unset means no LLM: rules only."""
    spec = os.environ.get("STRATEGY_LLM")
    if not spec:
        return None
    try:
        from langchain.chat_models import init_chat_model

        model = init_chat_model(spec, temperature=0, timeout=2, max_retries=0)
        return LlmProposer(model.with_structured_output(StrategyCommand), store, float(os.environ.get("STRATEGY_LLM_TIMEOUT", "2")))
    except Exception:
        return None  # missing package, key or provider: stay on rules
