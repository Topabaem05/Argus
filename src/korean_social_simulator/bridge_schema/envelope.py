from __future__ import annotations

import re
from typing import ClassVar

from pydantic import BaseModel, ConfigDict, Field, field_validator, model_validator

from korean_social_simulator.bridge_schema.errors import StructuredError
from korean_social_simulator.bridge_schema.events import (
    AgentDialogueEvent,
    AgentEmotionEvent,
    AgentMoveEvent,
    AgentSpawnEvent,
    ConflictUpdateEvent,
    GroupUpdateEvent,
    UnityAck,
)
from korean_social_simulator.bridge_schema.physics import PhysicsRequest, PhysicsResult

_SEMVER_PATTERN = re.compile(
    r"^(?P<major>0|[1-9]\d*)\.(?P<minor>0|[1-9]\d*)\.(?P<patch>0|[1-9]\d*)$"
)
_PayloadModel = type[BaseModel]


class BridgeEnvelope(BaseModel):
    """Versioned JSON envelope shared by Argus, Unity, replay, and physics paths."""

    model_config = ConfigDict(extra="forbid")

    schema_version: str
    message_id: str = Field(min_length=1)
    correlation_id: str | None = None
    session_id: str = Field(min_length=1)
    sequence: int = Field(ge=0)
    sent_at_ms: int = Field(ge=0)
    type: str = Field(min_length=1)
    payload: dict[str, object] = Field(default_factory=dict)

    _PAYLOAD_MODELS: ClassVar[dict[str, _PayloadModel]] = {
        "agent.spawn": AgentSpawnEvent,
        "agent.move": AgentMoveEvent,
        "agent.dialogue": AgentDialogueEvent,
        "agent.emotion": AgentEmotionEvent,
        "group.update": GroupUpdateEvent,
        "conflict.update": ConflictUpdateEvent,
        "unity.ack": UnityAck,
        "physics.request": PhysicsRequest,
        "physics.result": PhysicsResult,
        "bridge.error": StructuredError,
        "unity.error": StructuredError,
        "adapter.error": StructuredError,
    }

    @field_validator("schema_version")
    @classmethod
    def _validate_schema_version(cls, value: str) -> str:
        match = _SEMVER_PATTERN.fullmatch(value)
        if match is None:
            raise ValueError("schema_version must use MAJOR.MINOR.PATCH semantic versioning.")
        if match.group("major") != "1":
            raise ValueError("Unsupported bridge schema major version.")
        return value

    @model_validator(mode="after")
    def _validate_payload_for_type(self) -> BridgeEnvelope:
        payload_model = self._PAYLOAD_MODELS.get(self.type)
        if payload_model is not None:
            payload_model.model_validate(self.payload)
        return self
