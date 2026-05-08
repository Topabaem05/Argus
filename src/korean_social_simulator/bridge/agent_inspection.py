from __future__ import annotations

import json
from pathlib import Path

from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, ConfigDict, Field

from korean_social_simulator.bridge_schema import StructuredError
from korean_social_simulator.errors import StorageError
from korean_social_simulator.models import AgentProfile

_PUBLIC_AGENT_FIELDS = (
    "agent_id",
    "display_name",
    "language",
    "background",
    "goals",
    "safety_notes",
)


class AgentProfilesLoadRequest(BaseModel):
    """HTTP request body for loading public agent inspection profiles."""

    model_config = ConfigDict(extra="forbid")

    path: str = Field(min_length=1)


class AgentInspectionController:
    """Public-only selected-agent inspection state."""

    def __init__(self) -> None:
        self._profiles_by_id: dict[str, AgentProfile] = {}
        self._loaded_path: Path | None = None

    def load(self, profiles_path: str | Path) -> dict[str, object]:
        """Load AgentProfile artifacts from profiles.json."""
        path = Path(profiles_path)
        if not path.exists():
            raise StorageError(f"Agent profiles not found: {path}")

        try:
            payload = json.loads(path.read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError) as exc:
            raise StorageError(f"Failed to read agent profiles {path}: {exc}") from exc
        if not isinstance(payload, list):
            raise StorageError(f"Agent profiles JSON must be a list: {path}")

        profiles = [AgentProfile.model_validate(item) for item in payload]
        self._profiles_by_id = {profile.agent_id: profile for profile in profiles}
        self._loaded_path = path
        return self.status()

    def public_agent(self, agent_id: str) -> dict[str, object]:
        """Return only allowlisted public fields for one agent."""
        profile = self._profiles_by_id.get(agent_id)
        if profile is None:
            raise KeyError(agent_id)

        profile_payload = profile.model_dump(mode="json")
        return {field: profile_payload[field] for field in _PUBLIC_AGENT_FIELDS}

    def status(self) -> dict[str, object]:
        """Return loaded inspection state without raw profile contents."""
        return {
            "loaded": self._loaded_path is not None,
            "loaded_path": str(self._loaded_path) if self._loaded_path is not None else None,
            "agent_count": len(self._profiles_by_id),
        }


def register_agent_inspection_routes(
    app: FastAPI,
    controller: AgentInspectionController,
) -> None:
    """Register public selected-agent inspection endpoints."""

    @app.post("/agents/load")
    def load_agents(request: AgentProfilesLoadRequest) -> dict[str, object]:
        try:
            return controller.load(request.path)
        except (StorageError, ValueError) as exc:
            raise HTTPException(status_code=400, detail=str(exc)) from exc

    @app.get("/agents/{agent_id}/public")
    def inspect_public_agent(agent_id: str) -> dict[str, object]:
        try:
            return controller.public_agent(agent_id)
        except KeyError as exc:
            error = StructuredError(
                error_id=f"agent-not-found-{agent_id}",
                source="bridge",
                severity="error",
                message="Public agent not found.",
                recoverable=True,
                details={"agent_id": agent_id},
            )
            raise HTTPException(status_code=404, detail=error.model_dump(mode="json")) from exc
