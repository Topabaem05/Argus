"""Tutorial system — onboarding for new players."""

from __future__ import annotations

from dataclasses import dataclass
from typing import TextIO

_TUTORIAL_STEPS = [
    (
        "게임 목표",
        "5라운드 동안 회사 가치를 가장 높게 만드세요. 가치 = 자금 + 직원x호감도 + 만족도입니다.",
    ),
    ("아침 페이즈", "주문이 들어옵니다. 직원에게 태스크를 할당하세요."),
    ("업무 페이즈", "직원이 태스크를 수행합니다. 기분과 능력치가 성공률에 영향을 줍니다."),
    ("이벤트 페이즈", "랜덤 이벤트가 발생합니다 (폭우, 감기, 대형계약 등)."),
    ("저녁 페이즈", "급여 지급, 정산, 소문 소멸이 일어납니다."),
    ("직원 관리", "칭찬(praise), 간식(snack), 보너스(bonus)로 기분과 호감도를 올리세요."),
    ("소문 시스템", "상대 회사에 소문을 퍼뜨려 호감도를 떨어뜨릴 수 있습니다."),
    ("승리 조건", "5라운드 후 가장 높은 회사 가치를 가진 플레이어가 승리합니다."),
    ("파산 조건", "모든 직원이 퇴사하면 즉시 파산합니다."),
]


@dataclass
class TutorialSystem:
    out: TextIO

    def print_tutorial(self) -> None:
        self.out.write("=== AI 회사 운영 게임 — 튜토리얼 ===\n\n")
        for i, (title, desc) in enumerate(_TUTORIAL_STEPS, 1):
            self.out.write(f"  {i}. {title}\n     {desc}\n\n")
        self.out.write("=== 튜토리얼 완료. 행운을 빕니다! ===\n")
