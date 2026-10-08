"""Embeddings. The default is a small deterministic hashed bag-of-words: no download, no network, same vector every run.
Swap in a real sentence-embedding model by implementing Embedder.embed (same dimension for the whole collection)."""
import hashlib
import math
import re
from typing import Protocol

DIM = 256
_TOKEN = re.compile(r"[a-z0-9]+")
_STOP = {"the", "a", "an", "and", "of", "in", "to", "is", "when", "with", "this", "that", "it", "on", "at", "for"}


class Embedder(Protocol):
    def embed(self, texts: list[str]) -> list[list[float]]: ...


class HashEmbedder:
    def embed(self, texts: list[str]) -> list[list[float]]:
        return [self._one(t) for t in texts]

    @staticmethod
    def _one(text: str) -> list[float]:
        v = [0.0] * DIM
        for tok in _TOKEN.findall(text.lower()):
            if tok in _STOP:
                continue
            stem = tok[:-1] if tok.endswith("s") and len(tok) > 3 else tok
            h = int.from_bytes(hashlib.sha1(stem.encode()).digest()[:4], "big")
            v[h % DIM] += 1.0
        n = math.sqrt(sum(x * x for x in v)) or 1.0
        return [x / n for x in v]
