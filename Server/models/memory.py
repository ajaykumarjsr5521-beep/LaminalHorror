"""Semantic memory record. Text is a short summary written by the server, never raw positions or hide-spot ids."""
from pydantic import BaseModel, ConfigDict, Field


class Memory(BaseModel):
    model_config = ConfigDict(extra="forbid")

    key: str                              # stable id: same pattern updates one record instead of adding duplicates
    text: str = Field(max_length=240)
    kind: str                             # DEATH, HIDE_PATTERN, NOISE_HABIT, ROUTE_HABIT, STRUGGLE
    room: str = ""
    importance: float = Field(ge=0, le=1)
    created_at: float
    expires_at: float
    count: int = 1
