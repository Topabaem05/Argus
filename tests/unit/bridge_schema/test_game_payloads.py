from __future__ import annotations

import pytest
from pydantic import ValidationError

from korean_social_simulator.bridge_schema import BridgeEnvelope
from korean_social_simulator.bridge_schema.game import (
    EconomyUpdatePayload,
    GameStateSyncPayload,
    PlayerCommandPayload,
    RumorEventPayload,
    TaskUpdatePayload,
)


def _envelope(type_: str, payload: dict[str, object]) -> BridgeEnvelope:
    return BridgeEnvelope.model_validate(
        {
            "schema_version": "1.0.0",
            "message_id": "msg-game-001",
            "session_id": "session-game",
            "sequence": 0,
            "sent_at_ms": 1000,
            "type": type_,
            "payload": payload,
        }
    )


def test_player_command_payload_validates() -> None:
    payload = PlayerCommandPayload(
        command_id="cmd-1",
        player_id="p1",
        action="praise",
        target_employee_id="emp-001",
        round_number=1,
    )
    assert payload.action == "praise"


def test_player_command_envelope_round_trips() -> None:
    envelope = _envelope(
        "game.player_command",
        {
            "command_id": "cmd-1",
            "player_id": "p1",
            "action": "praise",
            "target_employee_id": "emp-001",
            "round_number": 1,
        },
    )
    assert envelope.type == "game.player_command"


def test_task_update_payload_validates() -> None:
    payload = TaskUpdatePayload(
        task_id="task-1",
        employee_id="emp-001",
        category="carry",
        status="completed",
        progress=1.0,
        reward=500,
    )
    assert payload.status == "completed"


def test_rumor_event_payload_validates() -> None:
    payload = RumorEventPayload(
        rumor_id="rumor-1",
        kind="salary",
        source_player_id="p2",
        target_player_id="p1",
        content="월급 밀린대",
        credibility=0.7,
    )
    assert payload.kind == "salary"


def test_economy_update_payload_validates() -> None:
    payload = EconomyUpdatePayload(
        player_id="p1",
        round_number=1,
        income=500,
        expenses=300,
        funds_after=10200,
    )
    assert payload.funds_after == 10200


def test_game_state_sync_payload_validates() -> None:
    payload = GameStateSyncPayload(
        game_id="game-1",
        round_number=3,
        phase="work",
        player_ids=["p1", "p2"],
        is_finished=False,
    )
    assert payload.round_number == 3


def test_invalid_rumor_kind_rejected() -> None:
    with pytest.raises(ValidationError):
        RumorEventPayload(
            rumor_id="r1",
            kind="not_a_real_kind",
            source_player_id="p1",
            target_player_id="p2",
            content="x",
            credibility=0.5,
        )


def test_progress_out_of_range_rejected() -> None:
    with pytest.raises(ValidationError):
        TaskUpdatePayload(
            task_id="t1",
            category="carry",
            status="pending",
            progress=1.5,
            reward=100,
        )
