"""Game state manager — creates and mutates the mutable GameState."""

from __future__ import annotations

import uuid
from dataclasses import dataclass, field

from korean_social_simulator.errors import SimulationError
from korean_social_simulator.models import (
    Company,
    EmployeeStats,
    GameEmployee,
    GameState,
    SimulationEvent,
)

_EMPLOYEE_NAME_POOL = [
    "김민수",
    "이지은",
    "박정훈",
    "최유리",
    "정대현",
    "강미라",
    "윤서준",
    "조하늘",
    "임수빈",
    "오지호",
    "한별이",
    "신예림",
    "권혁준",
    "배소영",
    "남궁진",
    "서윤아",
    "황보람",
    "모태훈",
    "양미경",
    "주현우",
]

_PERSONALITY_TAGS = [
    ["성실", "내성적"],
    ["수다쟁이", "외향적"],
    ["게으른", "유머러스"],
    ["의리있는", "신중"],
    ["소심한", "완벽주의"],
]


@dataclass
class GameStateManager:
    game_state: GameState = field(default_factory=lambda: GameState(game_id=_new_id()))

    @classmethod
    def new_game(cls, player_specs: list[tuple[str, str]], max_rounds: int = 5) -> GameStateManager:
        if len(player_specs) < 1 or len(player_specs) > 4:
            raise SimulationError("Player count must be 1-4.")
        player_ids = [pid for pid, _ in player_specs]
        companies: dict[str, Company] = {}
        emp_idx = 0
        for pid, company_name in player_specs:
            employees: list[GameEmployee] = []
            for _ in range(3):
                emp = GameEmployee(
                    employee_id=f"emp-{emp_idx:03d}",
                    display_name=_EMPLOYEE_NAME_POOL[emp_idx % len(_EMPLOYEE_NAME_POOL)],
                    personality_tags=_PERSONALITY_TAGS[emp_idx % len(_PERSONALITY_TAGS)],
                    stats=_roll_stats(emp_idx),
                    mood=50,
                    loyalty_map=dict.fromkeys(player_ids, 0),
                    reputation_perception=dict.fromkeys(player_ids, 0),
                    employed_by=pid,
                )
                employees.append(emp)
                emp_idx += 1
            companies[pid] = Company(
                player_id=pid,
                company_name=company_name,
                funds=10000,
                employees=employees,
            )
        state = GameState(
            game_id=_new_id(),
            max_rounds=max_rounds,
            companies=companies,
        )
        return cls(game_state=state)

    def company(self, player_id: str) -> Company:
        return self.game_state.company(player_id)

    def employee(self, employee_id: str) -> GameEmployee:
        for company in self.game_state.companies.values():
            for emp in company.employees:
                if emp.employee_id == employee_id:
                    return emp
        raise SimulationError(f"Employee not found: {employee_id}")

    def reassign_employee(self, employee_id: str, new_player_id: str) -> None:
        emp = self.employee(employee_id)
        old_owner = emp.employed_by
        if old_owner:
            self.company(old_owner).employees = [
                e for e in self.company(old_owner).employees if e.employee_id != employee_id
            ]
        emp.employed_by = new_player_id
        self.company(new_player_id).employees.append(emp)
        if new_player_id not in emp.loyalty_map:
            emp.loyalty_map[new_player_id] = 0
            emp.reputation_perception[new_player_id] = 0

    def remove_employee(self, employee_id: str) -> None:
        emp = self.employee(employee_id)
        emp.is_active = False
        emp.employed_by = None

    def adjust_mood(self, employee_id: str, delta: int) -> None:
        emp = self.employee(employee_id)
        emp.mood = max(0, min(100, emp.mood + delta))

    def adjust_loyalty(self, employee_id: str, player_id: str, delta: int) -> None:
        emp = self.employee(employee_id)
        current = emp.loyalty_map.get(player_id, 0)
        emp.loyalty_map[player_id] = max(-100, min(100, current + delta))

    def adjust_funds(self, player_id: str, delta: int) -> None:
        company = self.company(player_id)
        company.funds = max(0, company.funds + delta)
        if company.funds == 0 and not company.active_employees():
            company.is_bankrupt = True

    def log_event(self, event_type: str, actor_id: str | None, payload: dict[str, object]) -> None:
        self.game_state.event_log.append(
            SimulationEvent(
                run_id=self.game_state.game_id,
                turn=self.game_state.round_number,
                event_type=event_type,  # type: ignore[arg-type]
                actor_id=actor_id,
                payload=payload,
            )
        )

    def rankings(self) -> list[tuple[str, int]]:
        ranked = sorted(
            self.game_state.companies.items(),
            key=lambda kv: kv[1].value(),
            reverse=True,
        )
        return [(pid, c.value()) for pid, c in ranked]

    def bankrupt_players(self) -> list[str]:
        return [pid for pid, c in self.game_state.companies.items() if c.is_bankrupt]


def _new_id() -> str:
    return uuid.uuid4().hex[:12]


def _roll_stats(seed: int) -> EmployeeStats:
    import random

    rng = random.Random(seed * 7919 + 1)
    return EmployeeStats(
        stamina=rng.randint(3, 9),
        intelligence=rng.randint(3, 9),
        speed=rng.randint(3, 9),
        communication=rng.randint(3, 9),
    )
