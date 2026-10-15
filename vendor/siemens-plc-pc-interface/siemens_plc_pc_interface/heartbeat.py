"""Heartbeat counter and timeout state machine."""

from __future__ import annotations

from dataclasses import dataclass


_DINT_MAX = 2_147_483_647


@dataclass(frozen=True)
class HeartbeatStatus:
    """Current heartbeat health reported to the future runtime."""

    healthy: bool
    reason: str
    last_echo: int | None
    age_ms: int


class HeartbeatCounter:
    """Generate positive DINT heartbeat values with deterministic rollover."""

    def __init__(self, initial: int = 0) -> None:
        if not 0 <= initial <= _DINT_MAX:
            raise ValueError(
                f"initial must be between 0 and {_DINT_MAX}"
            )
        self._value = initial

    @property
    def value(self) -> int:
        return self._value

    def next(self) -> int:
        """Increment once, rolling from DINT maximum back to one."""
        self._value = 1 if self._value >= _DINT_MAX else self._value + 1
        return self._value


class HeartbeatMonitor:
    """
    Detect whether the PLC acknowledges the heartbeat actually sent.

    The runtime will write an incrementing PC heartbeat. The PLC copies that
    value to its echo tag. A healthy monitor requires an observed echo to
    exactly match a heartbeat written during the preceding cycle. An unrelated
    changing DINT must not satisfy the communication proof.
    """

    def __init__(self, timeout_ms: int, start_time: float = 0.0) -> None:
        if timeout_ms <= 0:
            raise ValueError("timeout_ms must be greater than zero")
        if start_time < 0:
            raise ValueError("start_time must be non-negative")

        self._timeout_seconds = timeout_ms / 1_000
        self._last_time = start_time
        self._last_progress_time = start_time
        self._last_echo: int | None = None
        self._has_acknowledgement = False

    def _check_time(self, now: float) -> None:
        if now < self._last_time:
            raise ValueError("now must not move backwards")
        self._last_time = now

    def observe(
        self,
        echo: int,
        expected_echo: int,
        now: float,
    ) -> HeartbeatStatus:
        """Record one echo and advance only for the expected sent value."""
        if isinstance(echo, bool) or not isinstance(echo, int):
            raise TypeError("echo must be an integer DINT value")
        if not -2_147_483_648 <= echo <= _DINT_MAX:
            raise ValueError("echo is outside the DINT range")
        if isinstance(expected_echo, bool) or not isinstance(expected_echo, int):
            raise TypeError("expected_echo must be an integer DINT value")
        if not 0 <= expected_echo <= _DINT_MAX:
            raise ValueError("expected_echo is outside the generated DINT range")

        self._check_time(now)
        self._last_echo = echo
        if echo == expected_echo:
            self._has_acknowledgement = True
            self._last_progress_time = now
        return self.status(now)

    def status(self, now: float) -> HeartbeatStatus:
        """Return health without changing the last observed echo."""
        self._check_time(now)
        age_seconds = now - self._last_progress_time
        age_ms = max(0, round(age_seconds * 1_000))

        if not self._has_acknowledgement:
            if age_seconds > self._timeout_seconds:
                return HeartbeatStatus(
                    healthy=False,
                    reason="heartbeat_not_acknowledged",
                    last_echo=self._last_echo,
                    age_ms=age_ms,
                )
            return HeartbeatStatus(
                healthy=False,
                reason="waiting_for_matching_echo",
                last_echo=self._last_echo,
                age_ms=age_ms,
            )

        if age_seconds > self._timeout_seconds:
            return HeartbeatStatus(
                healthy=False,
                reason="heartbeat_acknowledgement_stalled",
                last_echo=self._last_echo,
                age_ms=age_ms,
            )

        return HeartbeatStatus(
            healthy=True,
            reason="heartbeat_acknowledged",
            last_echo=self._last_echo,
            age_ms=age_ms,
        )
