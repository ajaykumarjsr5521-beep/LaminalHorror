"""Horror Director schemas. The director picks from a closed catalogue; Unity executes it with its own data and rate limits."""
from enum import Enum

from pydantic import BaseModel, ConfigDict, Field


class HorrorEvent(str, Enum):
    DO_NOTHING = "DO_NOTHING"
    SILENCE = "SILENCE"
    DISTANT_FOOTSTEPS = "DISTANT_FOOTSTEPS"
    FALSE_FOOTSTEPS = "FALSE_FOOTSTEPS"
    LIGHT_FLICKER = "LIGHT_FLICKER"
    DOOR_MOVEMENT = "DOOR_MOVEMENT"
    OBJECT_FALL = "OBJECT_FALL"
    SHADOW_EVENT = "SHADOW_EVENT"
    DISTANT_BREATHING = "DISTANT_BREATHING"
    FALSE_ENTITY_SIGHTING = "FALSE_ENTITY_SIGHTING"
    ENVIRONMENTAL_MOVEMENT = "ENVIRONMENTAL_MOVEMENT"
    ENTITY_APPEARANCE = "ENTITY_APPEARANCE"


# Short, rare, contextual. The server picks one of these; nothing is generated freely, so nothing can break tone or schema.
LINES = {
    "AGAIN": "You again.",
    "REMEMBER": "I remember.",
    "HID_BEFORE": "You hid here before.",
    "NOT_THIS_TIME": "Not this time.",
    "FOUND": "Found you.",
}
LINE_INTERVAL_SECONDS = 600.0


class HorrorRequest(BaseModel):
    """Inputs are all 0..1 or seconds. No positions."""

    model_config = ConfigDict(extra="forbid")

    request_id: str = Field(min_length=1, max_length=64)
    stress: float = Field(ge=0, le=1)                # heartbeat / panic estimate
    entity_proximity: float = Field(ge=0, le=1)      # 1 = adjacent, 0 = far
    entity_visible: bool = False
    darkness: float = Field(ge=0, le=1)
    recent_noise: float = Field(ge=0, le=1)
    chase_pressure: float = Field(ge=0, le=1)
    isolation: float = Field(ge=0, le=1)
    in_safe_zone: bool = False
    seconds_since_scare: float = Field(ge=0)
    recent_scares: int = Field(ge=0, le=20)
    death_count: int = Field(ge=0)
    memory_pressure: float = Field(default=0, ge=0, le=1)
    hide_success_rate: float = Field(default=0, ge=0, le=1)


class HorrorSuggestion(BaseModel):
    request_id: str
    event: HorrorEvent
    tension: float = Field(ge=0, le=1)
    line_id: str | None = None
    line: str | None = None
    reason_code: str = Field(max_length=48, pattern=r"^[A-Z0-9_]+$")
