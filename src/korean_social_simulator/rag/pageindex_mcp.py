"""Mocked PageIndex MCP adapter for testing RAG integration."""

from __future__ import annotations

from korean_social_simulator.errors import RetrievalError
from korean_social_simulator.models import RetrievedContext, RetrievedSection


class MockPageIndexMCP:
    """Mock PageIndex MCP adapter for offline testing.

    Returns canned responses and does not require network access or API keys.
    """

    def __init__(self, fail_mode: bool = False):
        self._fail_mode = fail_mode
        self._calls: list[str] = []

    def retrieve(self, query: str, required: bool = False) -> RetrievedContext:
        """Mock retrieval returning canned sections or failing.

        When fail_mode is True, simulates a retrieval failure. The behavior
        depends on `required`: required failures raise RetrievalError, optional
        failures return unavailable status with a warning.
        """
        self._calls.append(query)

        if self._fail_mode:
            if required:
                raise RetrievalError(f"Mock retrieval failed for query: {query}")
            return RetrievedContext(
                provider="pageindex_mock",
                status="unavailable",
                query=query,
                warnings=["Mock retrieval unavailable (optional mode)."],
            )

        return RetrievedContext(
            provider="pageindex_mock",
            status="available",
            query=query,
            sections=[
                RetrievedSection(
                    section_id="sec-001",
                    content_preview=f"Mock response for query: {query}",
                    source_path="mock/source/doc.pdf",
                    page=1,
                )
            ],
        )
