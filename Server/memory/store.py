"""Exact game memory (SQLite). Deterministic facts only; semantic memory lives in the vector store (F-15g)."""
import sqlite3
import threading

from models.events import GameEvent


class Store:
    def __init__(self, path: str = ":memory:") -> None:
        self._db = sqlite3.connect(path, check_same_thread=False)
        self._lock = threading.Lock()
        with self._lock:
            self._db.executescript(
                """
                CREATE TABLE IF NOT EXISTS events(
                    id INTEGER PRIMARY KEY AUTOINCREMENT, type TEXT NOT NULL, time REAL NOT NULL, value REAL NOT NULL, tag TEXT);
                CREATE TABLE IF NOT EXISTS strategy_log(
                    id INTEGER PRIMARY KEY AUTOINCREMENT, request_id TEXT NOT NULL, trigger TEXT, strategy TEXT NOT NULL,
                    confidence REAL NOT NULL, reason_code TEXT NOT NULL);
                """
            )

    def add_events(self, events: list[GameEvent]) -> int:
        with self._lock:
            self._db.executemany(
                "INSERT INTO events(type,time,value,tag) VALUES(?,?,?,?)", [(e.type.value, e.time, e.value, e.tag) for e in events]
            )
            self._db.commit()
        return len(events)

    def count_events(self, type_: str | None = None) -> int:
        with self._lock:
            if type_:
                return self._db.execute("SELECT COUNT(*) FROM events WHERE type=?", (type_,)).fetchone()[0]
            return self._db.execute("SELECT COUNT(*) FROM events").fetchone()[0]

    def log_strategy(self, request_id: str, trigger: str, strategy: str, confidence: float, reason_code: str) -> None:
        with self._lock:
            self._db.execute(
                "INSERT INTO strategy_log(request_id,trigger,strategy,confidence,reason_code) VALUES(?,?,?,?,?)",
                (request_id, trigger, strategy, confidence, reason_code),
            )
            self._db.commit()

    def strategy_log(self) -> list[tuple]:
        with self._lock:
            return self._db.execute("SELECT request_id,trigger,strategy,confidence,reason_code FROM strategy_log ORDER BY id").fetchall()
