"""Request and event schemas sent by Unity (snake_case, same names as the Unity StrategyRequestDto)."""
from enum import Enum

from pydantic import BaseModel, ConfigDict, Field


class EventType(str, Enum):
    MOVE = "MOVE"
    NOISE = "NOISE"
    HIDE = "HIDE"
    ROUTE_USED = "ROUTE_USED"
    ROOM_ENTERED = "ROOM_ENTERED"
    DEATH = "DEATH"
    CHASE_ENDED = "CHASE_ENDED"
    PUZZLE_SOLVED = "PUZZLE_SOLVED"


class GameEvent(BaseModel):
    model_config = ConfigDict(extra="forbid")

    type: EventType
    time: float = Field(ge=0)
    value: float = 0
    tag: str | None = Field(default=None, max_length=48)


class KeyShare(BaseModel):
    key: str = Field(max_length=48)
    value: float = Field(ge=0, le=1)


class StrategyRequest(BaseModel):
    """Anonymous player profile: ratios and categories only, never positions or hide-spot ids."""

    model_config = ConfigDict(extra="forbid")

    request_id: str = Field(min_length=1, max_length=64)
    trigger: str = Field(max_length=32)
    run_frequency: float = Field(ge=0, le=1)
    walk_frequency: float = Field(ge=0, le=1)
    crouch_frequency: float = Field(ge=0, le=1)
    noise_frequency: float = Field(ge=0, le=1)
    risk_tolerance: float = Field(ge=0, le=1)
    average_chase_seconds: float = Field(ge=0)
    puzzle_seconds: float = Field(ge=0)
    hide_success_rate: float = Field(ge=0, le=1)
    death_count: int = Field(ge=0)
    hide_count: int = Field(ge=0)
    hide_usage: list[KeyShare] = []
    route_usage: list[KeyShare] = []
    room_time: list[KeyShare] = []
