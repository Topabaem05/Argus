"""Random event system — round-based random events per spec 2.6."""

from __future__ import annotations

import random
from dataclasses import dataclass

from korean_social_simulator.game.state import GameStateManager

EventKind = str  # "rain", "press", "flu", "festival", "big_contract", "strike"


@dataclass
class GameEvent:
    kind: EventKind
    description: str
    affected_players: list[str]
    effects: dict[str, object]


_EVENT_POOL: list[tuple[EventKind, str]] = [
    ("rain", "폭우 — 배달 시간 2배, 실외 작업 불가"),
    ("press", "언론 취재 — 평판 최고 회사 보너스 매출"),
    ("flu", "감기 유행 — 랜덤 직원 1~2명 결근"),
    ("festival", "지역 축제 — 배달 주문 2배"),
    ("big_contract", "대형 계약 — 전 회사 입찰 가능"),
    ("strike", "직원 파업 — 기분 최하위 회사 직원 1턴 정지"),
]


@dataclass
class RandomEventSystem:
    manager: GameStateManager

    def roll_event(self, round_number: int) -> GameEvent | None:
        rng = random.Random(round_number * 13 + 3)
        if rng.random() < 0.3:
            return None
        kind, desc = rng.choice(_EVENT_POOL)
        return self._apply(kind, desc, rng)

    def _apply(self, kind: EventKind, desc: str, rng: random.Random) -> GameEvent:
        player_ids = list(self.manager.game_state.companies.keys())
        if kind == "rain":
            affected = player_ids
            effects: dict[str, object] = {"delivery_time_multiplier": 2.0}
        elif kind == "press":
            ranked = self._ranked_by_reputation()
            top = ranked[0] if ranked else player_ids[0]
            self.manager.adjust_funds(top, 500)
            affected = [top]
            effects = {"bonus_revenue": 500, "top_player": top}
        elif kind == "flu":
            affected = []
            count = rng.randint(1, 2)
            for company in self.manager.game_state.companies.values():
                active = company.active_employees()
                if active:
                    victim = rng.choice(active)
                    victim.is_active = False
                    affected.append(victim.employee_id)
                    if len(affected) >= count:
                        break
            effects = {"absent_count": len(affected)}
        elif kind == "festival":
            affected = player_ids
            effects = {"order_multiplier": 2.0}
        elif kind == "big_contract":
            affected = player_ids
            effects = {"contract_available": True}
        elif kind == "strike":
            worst = self._worst_mood_company()
            affected = [worst] if worst else []
            if worst:
                for emp in self.manager.company(worst).active_employees():
                    self.manager.adjust_mood(emp.employee_id, -10)
            effects = {"strike_company": worst}
        else:
            affected = []
            effects = {}
        self.manager.log_event(
            "observation",
            None,
            {
                "random_event": kind,
                "description": desc,
                "affected": affected,
            },
        )
        return GameEvent(kind=kind, description=desc, affected_players=affected, effects=effects)

    def _ranked_by_reputation(self) -> list[str]:
        from korean_social_simulator.social.reputation import ReputationSystem

        rep = ReputationSystem(self.manager)
        scores = [(pid, rep.player_reputation(pid)) for pid in self.manager.game_state.companies]
        scores.sort(key=lambda kv: kv[1], reverse=True)
        return [pid for pid, _ in scores]

    def _worst_mood_company(self) -> str | None:
        worst_pid = None
        worst_mood = 101
        for pid, company in self.manager.game_state.companies.items():
            active = company.active_employees()
            if not active:
                continue
            avg = sum(e.mood for e in active) // len(active)
            if avg < worst_mood:
                worst_mood = avg
                worst_pid = pid
        return worst_pid

    def restore_absent(self) -> None:
        for company in self.manager.game_state.companies.values():
            for emp in company.employees:
                if not emp.is_active and emp.employed_by:
                    emp.is_active = True
