"""Strategy command schema. Mirrors the Unity StrategyCommand; the closed enum is the only thing the LLM may choose from."""
from enum import Enum

from pydantic import BaseModel, ConfigDict, Field


class Strategy(str, Enum):
    NONE = "NONE"
    INCREASE_HIDING_PRESSURE = "INCREASE_HIDING_PRESSURE"
    INCREASE_INVESTIGATION = "INCREASE_INVESTIGATION"
    CHANGE_PATROL_PREFERENCE = "CHANGE_PATROL_PREFERENCE"
    CHANGE_SEARCH_PRIORITY = "CHANGE_SEARCH_PRIORITY"
    RELAX_PRESSURE = "RELAX_PRESSURE"


MIN_DURATION = 10.0
MAX_DURATION = 180.0


class StrategyCommand(BaseModel):
    """A time-limited high-level request. No positions, targets or speeds exist in this schema."""

    model_config = ConfigDict(extra="forbid")

    strategy: Strategy
    priority: float = Field(ge=0, le=1)
    intensity: float = Field(ge=0, le=1)
    duration_seconds: float = Field(ge=MIN_DURATION, le=MAX_DURATION)
    confidence: float = Field(ge=0, le=1)
    reason_code: str = Field(max_length=48, pattern=r"^[A-Z0-9_]+$")


class StrategyResponse(StrategyCommand):
    request_id: str


NO_STRATEGY = StrategyCommand(
    strategy=Strategy.NONE, priority=0, intensity=0, duration_seconds=MIN_DURATION, confidence=0, reason_code="NO_CHANGE"
)
