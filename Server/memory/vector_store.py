"""Chroma-backed semantic memory (local, no server). Exact game state never goes here; see memory/store.py."""
import chromadb

from memory.embeddings import Embedder, HashEmbedder
from models.memory import Memory


class VectorMemory:
    def __init__(self, path: str | None = None, embedder: Embedder | None = None, name: str = "semantic_memory") -> None:
        self._embedder = embedder or HashEmbedder()
        client = chromadb.PersistentClient(path=path) if path else chromadb.EphemeralClient()
        if not path:  # EphemeralClient instances share state in one process; isolate by unique name
            name = f"{name}_{id(self)}"
        self._col = client.get_or_create_collection(name, metadata={"hnsw:space": "cosine"})

    def upsert(self, m: Memory) -> None:
        self._col.upsert(
            ids=[m.key], documents=[m.text], embeddings=self._embedder.embed([m.text]),
            metadatas=[{"kind": m.kind, "room": m.room, "importance": m.importance, "created_at": m.created_at,
                        "expires_at": m.expires_at, "count": m.count}],
        )

    def get(self, key: str) -> Memory | None:
        r = self._col.get(ids=[key])
        if not r["ids"]:
            return None
        md = r["metadatas"][0]
        return Memory(key=key, text=r["documents"][0], **md)

    def count(self) -> int:
        return self._col.count()

    def query(self, text: str, now: float, k: int = 3, kind: str | None = None, room: str | None = None,
              min_importance: float = 0.0) -> list[Memory]:
        """Top-k similar, unexpired memories ranked by similarity, filtered by metadata."""
        conds: list[dict] = [{"expires_at": {"$gt": now}}, {"importance": {"$gte": min_importance}}]
        if kind:
            conds.append({"kind": kind})
        if room:
            conds.append({"room": room})
        n = self._col.count()
        if n == 0:
            return []
        res = self._col.query(query_embeddings=self._embedder.embed([text]), n_results=min(k, n), where={"$and": conds})
        out = []
        for key, doc, md in zip(res["ids"][0], res["documents"][0], res["metadatas"][0]):
            out.append(Memory(key=key, text=doc, **md))
        return out
