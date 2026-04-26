"""No-op RAG retriever for when RAG is disabled."""

from __future__ import annotations

from korean_social_simulator.models import RetrievedContext


class NoOpRetriever:
    """RAG retriever that always returns a skipped status."""

    def retrieve(self, query: str) -> RetrievedContext:
        """Return a skipped context when RAG is disabled."""
        return RetrievedContext(
            provider="noop",
            status="skipped",
            query=query,
        )
