"""Unit tests for no-op RAG retriever."""

from __future__ import annotations

from korean_social_simulator.rag.base import NoOpRetriever


def test_noop_retriever_returns_skipped() -> None:
    """No-op retriever returns skipped status without network."""
    retriever = NoOpRetriever()
    ctx = retriever.retrieve("test query")
    assert ctx.status == "skipped"
    assert ctx.provider == "noop"
    assert ctx.query == "test query"
    assert len(ctx.sections) == 0
    assert len(ctx.warnings) == 0
