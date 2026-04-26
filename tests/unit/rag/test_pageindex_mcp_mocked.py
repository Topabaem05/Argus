"""Unit tests for mocked PageIndex MCP adapter."""

from __future__ import annotations

import pytest

from korean_social_simulator.errors import RetrievalError
from korean_social_simulator.rag.pageindex_mcp import MockPageIndexMCP


def test_optional_retrieval_failure_records_warning() -> None:
    """Optional retrieval failure returns unavailable with warning."""
    mcp = MockPageIndexMCP(fail_mode=True)
    ctx = mcp.retrieve("test query", required=False)
    assert ctx.status == "unavailable"
    assert len(ctx.warnings) == 1
    assert "unavailable" in ctx.warnings[0]


def test_required_retrieval_failure_raises() -> None:
    """Required retrieval failure raises RetrievalError."""
    mcp = MockPageIndexMCP(fail_mode=True)
    with pytest.raises(RetrievalError, match="Mock retrieval failed"):
        mcp.retrieve("test query", required=True)


def test_successful_retrieval_returns_available() -> None:
    """Successful retrieval returns sections with status available."""
    mcp = MockPageIndexMCP(fail_mode=False)
    ctx = mcp.retrieve("test query")
    assert ctx.status == "available"
    assert ctx.provider == "pageindex_mock"
    assert len(ctx.sections) == 1
    assert "test query" in ctx.sections[0].content_preview


def test_calls_are_tracked() -> None:
    """Queries are recorded for inspection."""
    mcp = MockPageIndexMCP()
    mcp.retrieve("query-1")
    mcp.retrieve("query-2")
    assert len(mcp._calls) == 2
    assert "query-1" in mcp._calls
