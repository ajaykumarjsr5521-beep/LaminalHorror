"""Horror Director: tension model plus pacing rules. Pure and deterministic; an LLM is not needed for pacing.

TENSION = proximity + uncertainty + darkness + noise + chase + memory + isolation - safe zone - recent scare - cooldown
Pacing target: anticipation -> uncertainty -> escalation -> release -> silence -> anticipation (never scare after scare).
DO_NOTHING and SILENCE are valid, frequent results."""
import time
from collections.abc import Callable

from models.horror import LINE_INTERVAL_SECONDS, LINES, HorrorEvent, HorrorRequest, HorrorSuggestion

MIN_GAP = 45.0            # matches the existing TensionDirector scare gap
LONG_QUIET = 180.0
ENTITY_APPEARANCE_MIN_GAP = 240.0


def tension(r: HorrorRequest) -> float:
    uncertainty = 0.0 if r.entity_visible else 0.5 * r.entity_proximity  # unseen but near is the scariest
    raw = (0.30 * r.entity_proximity + 0.10 * uncertainty + 0.15 * r.darkness + 0.10 * r.recent_noise
           + 0.20 * r.chase_pressure + 0.05 * r.memory_pressure + 0.10 * r.isolation)
    raw += 0.10 * r.stress
    raw -= 0.40 if r.in_safe_zone else 0.0
    raw -= 0.30 * max(0.0, 1 - r.seconds_since_scare / 120.0)   # recent scare
    raw -= 0.10 * min(r.recent_scares, 5) / 5                   # fatigue
    return round(max(0.0, min(1.0, raw)), 3)


class HorrorDirector:
    def __init__(self, clock: Callable[[], float] = time.monotonic) -> None:
        self._clock = clock
        self._last_line: float | None = None

    def decide(self, r: HorrorRequest) -> HorrorSuggestion:
        t = tension(r)
        event, reason = self._pick(r, t)
        line_id = self._line(r, event)
        return HorrorSuggestion(request_id=r.request_id, event=event, tension=t, line_id=line_id,
                                line=LINES.get(line_id) if line_id else None, reason_code=reason)

    @staticmethod
    def _pick(r: HorrorRequest, t: float) -> tuple[HorrorEvent, str]:
        E = HorrorEvent
        if r.in_safe_zone:
            return E.DO_NOTHING, "SAFE_ZONE"
        if r.chase_pressure > 0.5 or r.entity_visible:
            return E.DO_NOTHING, "REAL_THREAT_ACTIVE"               # the real entity is the scare; add nothing
        if r.seconds_since_scare < MIN_GAP:
            return E.DO_NOTHING, "SCARE_GAP"
        if r.seconds_since_scare < MIN_GAP * 2 and r.recent_scares >= 2:
            return E.SILENCE, "RELEASE_AFTER_SCARES"
        if t >= 0.6 and r.seconds_since_scare >= ENTITY_APPEARANCE_MIN_GAP and r.stress >= 0.5 and r.entity_proximity >= 0.5:
            return E.ENTITY_APPEARANCE, "ESCALATION_PEAK"
        if t >= 0.45:
            return E.SHADOW_EVENT, "HIGH_TENSION_UNCERTAINTY"
        if t >= 0.3:
            return E.LIGHT_FLICKER, "BUILD_ANTICIPATION"
        if r.seconds_since_scare >= LONG_QUIET and t < 0.3:
            return E.FALSE_FOOTSTEPS, "LONG_QUIET_LOW_TENSION"
        if r.seconds_since_scare >= MIN_GAP * 2 and r.isolation > 0.6:
            return E.DISTANT_BREATHING, "ISOLATED_PLAYER"
        return E.DO_NOTHING, "LET_SILENCE_WORK"

    def _line(self, r: HorrorRequest, event: HorrorEvent) -> str | None:
        """Rare contextual line, only with an event that is not a pause, with enough history, at most one per interval."""
        if event in (HorrorEvent.DO_NOTHING, HorrorEvent.SILENCE) or r.in_safe_zone:
            return None
        now = self._clock()
        if self._last_line is not None and now - self._last_line < LINE_INTERVAL_SECONDS:
            return None
        pick = None
        if event == HorrorEvent.ENTITY_APPEARANCE:
            pick = "FOUND" if r.death_count == 0 else "NOT_THIS_TIME"
        elif r.death_count >= 1:
            pick = "AGAIN" if r.death_count == 1 else "REMEMBER"
        elif r.hide_success_rate >= 0.7 and r.memory_pressure > 0.3:
            pick = "HID_BEFORE"
        if pick:
            self._last_line = now
        return pick
