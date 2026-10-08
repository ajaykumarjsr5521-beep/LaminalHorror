"""event -> memory creation -> importance -> embedding -> vector store, and retrieval for the strategic graph.

Rules: only patterns and notable events become memories. The same pattern updates one record (a count and a higher importance),
which is the summarisation step. Memories expire after a time that grows with importance. Texts hold categories only."""
import time
from collections.abc import Callable

from memory.vector_store import VectorMemory
from models.events import GameEvent, StrategyRequest
from models.memory import Memory

BASE_IMPORTANCE = {"DEATH": 0.8, "STRUGGLE": 0.7, "HIDE_PATTERN": 0.6, "NOISE_HABIT": 0.4, "ROUTE_HABIT": 0.4}
STORE_THRESHOLD = 0.3
DAY = 86400.0


def ttl_seconds(importance: float) -> float:
    return importance * 7 * DAY


class MemoryManager:
    def __init__(self, vectors: VectorMemory, clock: Callable[[], float] = time.time, k: int = 3) -> None:
        self.vectors = vectors
        self.clock = clock
        self.k = k

    # creation ---------------------------------------------------------------------------------------------------------
    def _remember(self, key: str, text: str, kind: str, room: str = "") -> Memory | None:
        now = self.clock()
        prev = self.vectors.get(key)
        count = prev.count + 1 if prev else 1
        importance = min(1.0, BASE_IMPORTANCE[kind] + 0.1 * (count - 1))
        if importance < STORE_THRESHOLD:
            return None
        m = Memory(key=key, text=text, kind=kind, room=room, importance=importance, created_at=prev.created_at if prev else now,
                   expires_at=now + ttl_seconds(importance), count=count)
        self.vectors.upsert(m)
        return m

    def record_event(self, e: GameEvent, room: str = "") -> Memory | None:
        if e.type.value == "DEATH":
            where = room or e.tag or "unknown"
            return self._remember(f"death:{where}", f"Player died in {where}.", "DEATH", where)
        return None

    def record_request(self, r: StrategyRequest) -> list[Memory]:
        """Turns a behavior profile into pattern memories. Called on each strategy request."""
        made: list[Memory | None] = []
        if r.hide_count >= 3 and r.hide_usage:
            top = max(r.hide_usage, key=lambda i: i.value)
            if top.value >= 0.6 and r.hide_success_rate >= 0.5:
                made.append(self._remember(f"hide:{top.key}", f"Player repeatedly hides in {top.key} when chased and often survives.",
                                           "HIDE_PATTERN"))
        if r.noise_frequency >= 0.6:
            made.append(self._remember("noise", "Player makes frequent noise while moving.", "NOISE_HABIT"))
        if r.route_usage:
            top = max(r.route_usage, key=lambda i: i.value)
            if top.value >= 0.6:
                made.append(self._remember(f"route:{top.key}", f"Player keeps using the {top.key} route.", "ROUTE_HABIT"))
        if r.death_count >= 2 and r.hide_success_rate < 0.3:
            made.append(self._remember("struggle", "Player is struggling and hiding is not working for them.", "STRUGGLE"))
        return [m for m in made if m]

    # retrieval --------------------------------------------------------------------------------------------------------
    @staticmethod
    def query_text(signals: dict) -> str:
        parts = []
        if signals.get("hide_reliant"):
            parts.append(f"player hides in {signals.get('hide_type') or 'hiding places'} when chased")
        if signals.get("noisy"):
            parts.append("player makes noise")
        if signals.get("route_repeat"):
            parts.append(f"player uses {signals['route_repeat']} route")
        if signals.get("struggling"):
            parts.append("player struggling")
        return " ".join(parts) or "player behavior patterns"

    def retrieve(self, req: StrategyRequest, signals: dict) -> list[str]:
        """Memory texts for the graph (top-k, unexpired, importance above the store threshold)."""
        found = self.vectors.query(self.query_text(signals), self.clock(), k=self.k, min_importance=STORE_THRESHOLD)
        return [m.text for m in found]
