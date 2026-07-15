"""Deterministic game-domain SFT data for compact Korean minibot models."""

from __future__ import annotations

import hashlib
import json
import random
from dataclasses import dataclass
from typing import Literal

Split = Literal["train", "validation", "test"]

SYSTEM_PROMPT = """당신은 회사 운영 게임의 AI 직원입니다.
직원의 성격, 기분, 사장 호감도, 업무 적합성과 최근 기억만 사용해 반응하세요.
현실 세계의 사실을 만들지 말고 반드시 한 개의 JSON 객체만 출력하세요.
허용 action: accept, reluctant_accept, refuse, complain
허용 side_action: null, gossip, consider_quit
필드: action, dialogue, efficiency(0~1), mood_change(-10~10), side_action
"""

_ACTIONS = (
    "assign_task",
    "praise",
    "scold",
    "snack",
    "raise",
    "bonus",
    "party",
    "fire",
    "gossip",
    "scout",
)
_PERSONALITIES = (
    ("성실", "내성적"),
    ("수다쟁이", "외향적"),
    ("게으른", "유머러스"),
    ("의리있는", "신중"),
    ("소심한", "완벽주의"),
)
_TASKS = ("carry", "deliver", "document", "sales", "maintenance")
_MOODS = (5, 12, 24, 35, 50, 72, 92)
_LOYALTIES = (-95, -60, -20, 0, 35, 70, 95)


@dataclass(frozen=True)
class GameSFTExample:
    example_id: str
    split: Split
    messages: list[dict[str, str]]
    expected: dict[str, object]
    metadata: dict[str, object]

    def to_record(self) -> dict[str, object]:
        return {
            "id": self.example_id,
            "messages": self.messages,
            "expected": self.expected,
            "metadata": self.metadata,
        }


def generate_game_sft_examples(*, count: int = 4000, seed: int = 42) -> list[GameSFTExample]:
    """Generate balanced synthetic examples without using a teacher model."""

    if count < 1:
        raise ValueError("count must be positive")
    rng = random.Random(seed)
    examples: list[GameSFTExample] = []
    for index in range(count):
        action = _ACTIONS[index % len(_ACTIONS)]
        personality = _PERSONALITIES[(index // len(_ACTIONS)) % len(_PERSONALITIES)]
        mood = _MOODS[(index // (len(_ACTIONS) * len(_PERSONALITIES))) % len(_MOODS)]
        loyalty = _LOYALTIES[rng.randrange(len(_LOYALTIES))]
        task = _TASKS[rng.randrange(len(_TASKS))]
        stats = {
            "stamina": rng.randint(2, 10),
            "intelligence": rng.randint(2, 10),
            "speed": rng.randint(2, 10),
            "communication": rng.randint(2, 10),
        }
        expected = _expected_response(
            action=action,
            personality=personality,
            mood=mood,
            loyalty=loyalty,
            task=task,
            stats=stats,
            variant=index,
        )
        prompt = _build_prompt(
            action=action,
            personality=personality,
            mood=mood,
            loyalty=loyalty,
            task=task,
            stats=stats,
            variant=index,
        )
        example_id = hashlib.sha256(f"{seed}:{index}:{prompt}".encode()).hexdigest()[:16]
        split = _split_for_id(example_id)
        examples.append(
            GameSFTExample(
                example_id=example_id,
                split=split,
                messages=[
                    {"role": "system", "content": SYSTEM_PROMPT},
                    {"role": "user", "content": prompt},
                    {
                        "role": "assistant",
                        "content": json.dumps(expected, ensure_ascii=False, separators=(",", ":")),
                    },
                ],
                expected=expected,
                metadata={
                    "action": action,
                    "personality": list(personality),
                    "mood": mood,
                    "loyalty": loyalty,
                    "task": task,
                    "stats": stats,
                },
            )
        )
    return examples


def split_game_sft_examples(
    examples: list[GameSFTExample],
) -> dict[Split, list[GameSFTExample]]:
    result: dict[Split, list[GameSFTExample]] = {
        "train": [],
        "validation": [],
        "test": [],
    }
    for example in examples:
        result[example.split].append(example)
    return result


def _split_for_id(example_id: str) -> Split:
    bucket = int(example_id[:8], 16) % 100
    if bucket < 10:
        return "test"
    if bucket < 20:
        return "validation"
    return "train"


def _build_prompt(
    *,
    action: str,
    personality: tuple[str, str],
    mood: int,
    loyalty: int,
    task: str,
    stats: dict[str, int],
    variant: int,
) -> str:
    memory = (
        "어제 사장에게 칭찬을 받았다"
        if variant % 3 == 0
        else "최근 업무가 몰려 피곤하다"
        if variant % 3 == 1
        else "동료와 회사 소문을 들었다"
    )
    target = "상대 회사 직원" if action in {"gossip", "scout"} else "현재 사장"
    return f"""[직원 정보]
이름: 미니봇-{variant % 97:02d}
성격: {', '.join(personality)}
능력: 체력 {stats['stamina']}, 지능 {stats['intelligence']}, 속도 {stats['speed']}, 소통 {stats['communication']}
현재 기분: {mood}/100
사장 호감도: {loyalty}/100
최근 기억: {memory}

[게임 명령]
액션: {action}
업무 종류: {task}
대상: {target}

[판단 규칙]
기분과 호감도가 매우 낮으면 거부할 수 있습니다.
업무 적합도가 낮으면 효율을 낮추고 불만을 표현할 수 있습니다.
성격과 기억에 맞는 짧은 한국어 대사를 생성하세요."""


def _expected_response(
    *,
    action: str,
    personality: tuple[str, str],
    mood: int,
    loyalty: int,
    task: str,
    stats: dict[str, int],
    variant: int,
) -> dict[str, object]:
    suitability = _task_suitability(task, stats)
    personality_set = set(personality)

    if action == "fire":
        outcome = "complain"
        side_action: str | None = "consider_quit"
        efficiency = 0.0
        mood_change = -10
        dialogue = "갑자기 해고라니 너무하네요. 정리하고 나가겠습니다."
    elif action == "gossip" and "의리있는" in personality_set:
        outcome = "refuse"
        side_action = None
        efficiency = 0.0
        mood_change = -2
        dialogue = "확인되지 않은 소문은 퍼뜨리지 않겠습니다."
    elif action == "scout" and loyalty >= 35:
        outcome = "refuse"
        side_action = None
        efficiency = 0.0
        mood_change = 1
        dialogue = "지금 회사에 남겠습니다. 제안은 고맙습니다."
    elif mood <= 15 or loyalty <= -80:
        outcome = "refuse"
        side_action = "consider_quit" if loyalty <= -80 else None
        efficiency = 0.0
        mood_change = -8
        dialogue = "지금 상태로는 이 명령을 수행하기 어렵습니다."
    elif mood <= 35 or loyalty < 0 or suitability <= 3:
        outcome = "complain" if "게으른" in personality_set or variant % 4 == 0 else "reluctant_accept"
        side_action = "gossip" if "수다쟁이" in personality_set else None
        efficiency = max(0.25, min(0.65, suitability / 10.0))
        mood_change = -5 if outcome == "complain" else -3
        dialogue = "부담되지만 우선 처리해 보겠습니다." if outcome == "reluctant_accept" else "또 이 업무인가요? 일단 해볼게요."
    else:
        outcome = "accept"
        side_action = None
        efficiency = max(0.65, min(1.0, 0.55 + suitability / 20.0 + mood / 500.0))
        mood_change = 4 if action in {"praise", "snack", "raise", "bonus", "party"} else 1
        dialogue = "네, 사장님. 바로 처리하겠습니다."

    return {
        "action": outcome,
        "dialogue": dialogue,
        "efficiency": round(efficiency, 2),
        "mood_change": mood_change,
        "side_action": side_action,
    }


def _task_suitability(task: str, stats: dict[str, int]) -> int:
    if task == "carry":
        return stats["stamina"]
    if task == "deliver":
        return stats["speed"]
    if task == "document":
        return stats["intelligence"]
    if task == "sales":
        return stats["communication"]
    return round((stats["stamina"] + stats["intelligence"]) / 2)
