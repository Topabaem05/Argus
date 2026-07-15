"""Social systems public API with cycle-safe lazy exports."""

from __future__ import annotations

__all__ = [
    "AgentMemorySystem",
    "RelationshipGraph",
    "ReputationSystem",
    "RumorEngine",
]


def __getattr__(name: str) -> object:
    """Load only the requested social subsystem to avoid package initialization cycles."""

    if name == "AgentMemorySystem":
        from korean_social_simulator.social.memory import AgentMemorySystem

        return AgentMemorySystem
    if name == "RelationshipGraph":
        from korean_social_simulator.social.relationships import RelationshipGraph

        return RelationshipGraph
    if name == "ReputationSystem":
        from korean_social_simulator.social.reputation import ReputationSystem

        return ReputationSystem
    if name == "RumorEngine":
        from korean_social_simulator.social.rumor import RumorEngine

        return RumorEngine
    raise AttributeError(f"module {__name__!r} has no attribute {name!r}")
