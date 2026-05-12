## Simulation Report: community_kiosk_feedback_001

## Summary

- run_id: community_kiosk_feedback_001
- status: success
- scenario_title: Community center kiosk feedback
- scenario_hypothesis: Clear privacy notices and staff fallback options reduce anxiety about kiosk use.
- event_count: 85
- turn_count: 4

## Metrics

| metric_name | value |
| --- | --- |
| trust_score | 0.85 |
| confusion_rate | 0.0 |
| event_count | 85 |
| turn_count | 4 |
| agent_count | 8 |

## Event Examples

```json
{
  "actor_id": null,
  "event_type": "system",
  "payload": {
    "dry_run": true,
    "input": {
      "accepted_attachment_count": 2,
      "attachments": [
        {
          "accepted": true,
          "extension": ".png",
          "filename": "kiosk-wireframe.png",
          "kind": "image",
          "path": "scenario-assets/kiosk-wireframe.png",
          "reason": "accepted",
          "size_bytes": 245760
        },
        {
          "accepted": true,
          "extension": ".md",
          "filename": "privacy-notice.md",
          "kind": "text",
          "path": "scenario-assets/privacy-notice.md",
          "reason": "accepted",
          "size_bytes": 4096
        }
      ],
      "chat_text": "A district community center wants feedback on a new self-service kiosk for appointment check-in, certificate requests, and class registration. Compare a basic kiosk flow with a version that shows a clear privacy notice and an easy staff-help button.",
      "rejected_attachment_count": 0,
      "topic_summary": "an and appointment basic button center certificate check class clear community compare"
    },
    "phase": "input_summary"
  },
  "run_id": "community_kiosk_feedback_001",
  "timestamp": "2026-05-08T12:05:41.583186+00:00",
  "turn": 0
}
```

```json
{
  "actor_id": null,
  "event_type": "system",
  "payload": {
    "dry_run": true,
    "phase": "persona_selection",
    "selected_personas": [
      {
        "agent_id": "agent-p-015",
        "confidence": 0.362,
        "display_name": "40대 변호사",
        "matched_terms": [],
        "persona_uuid": "p-015",
        "reason": "Selected as a deterministic diversity candidate for the simulation.",
        "safety_notes": [
          "이것은 합성 페르소나입니다.",
          "실제 사용자 행동을 예측하지 않습니다."
        ]
      },
      {
        "agent_id": "agent-p-001",
        "confidence": 0.362,
        "display_name": "30대 직장인",
        "matched_terms": [],
        "persona_uuid": "p-001",
        "reason": "Selected as a deterministic diversity candidate for the simulation.",
        "safety_notes": [
          "이것은 합성 페르소나입니다.",
          "실제 사용자 행동을 예측하지 않습니다."
        ]
      },
      {
        "agent_id": "agent-p-007",
        "confidence": 0.362,
        "display_name": "20대 신입 개발자",
        "matched_terms": [],
        "persona_uuid": "p-007",
        "reason": "Selected as a deterministic diversity candidate for the simulation.",
        "safety_notes": [
          "이것은 합성 페르소나입니다.",
          "실제 사용자 행동을 예측하지 않습니다."
        ]
      },
      {
        "agent_id": "agent-p-013",
        "confidence": 0.362,
        "display_name": "20대 간호사",
        "matched_terms": [],
        "persona_uuid": "p-013",
        "reason": "Selected as a deterministic diversity candidate for the simulation.",
        "safety_notes": [
          "이것은 합성 페르소나입니다.",
          "실제 사용자 행동을 예측하지 않습니다."
        ]
      },
      {
        "agent_id": "agent-p-004",
        "confidence": 0.362,
        "display_name": "50대 공무원",
        "matched_terms": [],
        "persona_uuid": "p-004",
        "reason": "Selected as a deterministic diversity candidate for the simulation.",
        "safety_notes": [
          "이것은 합성 페르소나입니다.",
          "실제 사용자 행동을 예측하지 않습니다."
        ]
      },
      {
        "agent_id": "agent-p-014",
        "confidence": 0.362,
        "display_name": "60대 주부",
        "matched_terms": [],
        "persona_uuid": "p-014",
        "reason": "Selected as a deterministic diversity candidate for the simulation.",
        "safety_notes": [
          "이것은 합성 페르소나입니다.",
          "실제 사용자 행동을 예측하지 않습니다."
        ]
      },
      {
        "agent_id": "agent-p-006",
        "confidence": 0.362,
        "display_name": "60대 은퇴자",
        "matched_terms": [],
        "persona_uuid": "p-006",
        "reason": "Selected as a deterministic diversity candidate for the simulation.",
        "safety_notes": [
          "이것은 합성 페르소나입니다.",
          "실제 사용자 행동을 예측하지 않습니다."
        ]
      },
      {
        "agent_id": "agent-p-011",
        "confidence": 0.362,
        "display_name": "50대 교사",
        "matched_terms": [],
        "persona_uuid": "p-011",
        "reason": "Selected as a deterministic diversity candidate for the simulation.",
        "safety_notes": [
          "이것은 합성 페르소나입니다.",
          "실제 사용자 행동을 예측하지 않습니다."
        ]
      }
    ]
  },
  "run_id": "community_kiosk_feedback_001",
  "timestamp": "2026-05-08T12:05:41.583202+00:00",
  "turn": 0
}
```

```json
{
  "actor_id": null,
  "event_type": "agent_action",
  "payload": {
    "bridge_payload": {
      "badge_label": "discussion",
      "group_id": "community_kiosk_feedback_001-discussion",
      "member_agent_ids": [
        "agent-p-015",
        "agent-p-001",
        "agent-p-007",
        "agent-p-013",
        "agent-p-004",
        "agent-p-014",
        "agent-p-006",
        "agent-p-011"
      ]
    },
    "bridge_type": "group.update",
    "dry_run": true,
    "phase": "group_formation"
  },
  "run_id": "community_kiosk_feedback_001",
  "timestamp": "2026-05-08T12:05:41.583215+00:00",
  "turn": 0
}
```

## Safety Notes

- This is a synthetic simulation for public-service UX exploration.
- Personas are synthetic and must not be used to profile real people.
- Results should be treated as design hypotheses, not population predictions.


## Warnings

- accessibility_concern_rate
- staff_assistance_intent


## Errors

- No errors recorded.

## Limitations

This is a synthetic simulation. Results are NOT real-world predictions. All participants are synthetic personas generated by Nemotron-Personas-Korea. This report should be used for hypothesis generation and risk analysis only, not as evidence of real-world behavior. External validation is required before making decisions based on these results.

## Follow-up Validation

Recommended: Validate findings through real-user studies, expert review, or A/B testing before making product decisions.