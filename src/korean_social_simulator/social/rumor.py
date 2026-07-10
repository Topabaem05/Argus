"""Rumor engine — creation, credibility, propagation, and decay."""

from __future__ import annotations

import random
import uuid
from dataclasses import dataclass, field

from korean_social_simulator.game.state import GameStateManager
from korean_social_simulator.models import GameEmployee

RumorKind = str  # "salary", "personality", "ability", "positive", "fake"

_RUMOR_TEMPLATES: dict[RumorKind, list[str]] = {
    "salary": ["월급 밀린대", "급여 안 준대"],
    "personality": ["엄청 무섭대", "화를 잘 내대"],
    "ability": ["회사 곧 망한대", "실적이 엉망이래"],
    "positive": ["복지가 좋대", "간식 많대"],
    "fake": ["사기 친대", "횡령했대"],
}

_RUMOR_INTENSITY: dict[RumorKind, int] = {
    "salary": 20,
    "personality": 15,
    "ability": 10,
    "positive": -10,
    "fake": 25,
}


@dataclass
class Rumor:
    rumor_id: str
    kind: RumorKind
    content: str
    about_player_id: str
    originator_employee_id: str
    spread_count: int = 0
    rounds_alive: int = 0
    is_disproven: bool = False


@dataclass
class RumorEngine:
    manager: GameStateManager
    _rumors: list[Rumor] = field(default_factory=list)

    def create_rumor(
        self, kind: RumorKind, about_player_id: str, originator_employee_id: str
    ) -> Rumor:
        rng = random.Random(uuid.uuid4().int & 0xFFFFFFFF)
        content = rng.choice(_RUMOR_TEMPLATES[kind])
        rumor = Rumor(
            rumor_id=f"rumor-{uuid.uuid4().hex[:8]}",
            kind=kind,
            content=f"{about_player_id} 사장 {content}",
            about_player_id=about_player_id,
            originator_employee_id=originator_employee_id,
        )
        self._rumors.append(rumor)
        self.manager.log_event(
            "observation",
            originator_employee_id,
            {
                "rumor_created": rumor.rumor_id,
                "about": about_player_id,
                "kind": kind,
            },
        )
        return rumor

    def credibility(self, rumor: Rumor, listener: GameEmployee) -> float:
        originator = self.manager.employee(rumor.originator_employee_id)
        listener_loyalty_to_originator = listener.loyalty_to(originator.employed_by or "")
        listener_loyalty_to_target = listener.loyalty_to(rumor.about_player_id)
        is_gossip_personality = any(
            tag in listener.personality_tags for tag in ["수다쟁이", "외향적"]
        )
        base = 0.4
        base += listener_loyalty_to_originator / 200.0
        base -= listener_loyalty_to_target / 200.0
        if is_gossip_personality:
            base += 0.15
        if rumor.kind == "fake":
            base -= 0.2
        return max(0.05, min(0.95, base))

    def spread(self, rumor: Rumor) -> list[str]:
        if rumor.is_disproven:
            return []
        rng = random.Random(hash(rumor.rumor_id) % 2**32)
        new_listeners: list[str] = []
        intensity = _RUMOR_INTENSITY[rumor.kind]
        for company in self.manager.game_state.companies.values():
            for emp in company.employees:
                if not emp.is_active or emp.employee_id == rumor.originator_employee_id:
                    continue
                if rumor.rumor_id in emp.memory:
                    continue
                cred = self.credibility(rumor, emp)
                if rng.random() < cred:
                    emp.memory.append(f"소문: {rumor.content}")
                    self.manager.adjust_loyalty(emp.employee_id, rumor.about_player_id, -intensity)
                    if rumor.kind != "positive":
                        self.manager.adjust_mood(emp.employee_id, -intensity // 2)
                    rumor.spread_count += 1
                    new_listeners.append(emp.employee_id)
        rumor.rounds_alive += 1
        self.manager.log_event(
            "observation",
            rumor.about_player_id,
            {
                "rumor_spread": rumor.rumor_id,
                "new_listeners": len(new_listeners),
            },
        )
        return new_listeners

    def decay_round(self) -> None:
        for rumor in self._rumors:
            rumor.rounds_alive += 1
        self._rumors = [r for r in self._rumors if r.rounds_alive < 3 and not r.is_disproven]

    def active_rumors(self) -> list[Rumor]:
        return [r for r in self._rumors if not r.is_disproven]

    def disprove(self, rumor_id: str) -> None:
        for r in self._rumors:
            if r.rumor_id == rumor_id:
                r.is_disproven = True
                self.manager.log_event(
                    "system",
                    r.about_player_id,
                    {
                        "rumor_disproven": rumor_id,
                    },
                )
