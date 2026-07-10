"""CLI game runner — drives a full game session through all systems."""

from __future__ import annotations

import sys
from dataclasses import dataclass
from typing import TextIO, cast

from korean_social_simulator.ai.prompt_builder import GamePromptBuilder
from korean_social_simulator.ai.slm_adapter import SLMRuntimeAdapter
from korean_social_simulator.game.commands import PlayerCommandSystem
from korean_social_simulator.game.economy import EconomicSystem
from korean_social_simulator.game.events import GameEvent, RandomEventSystem
from korean_social_simulator.game.loop import GameLoopController
from korean_social_simulator.game.scoring import ScoringSystem
from korean_social_simulator.game.state import GameStateManager
from korean_social_simulator.game.task_system import TaskSystem
from korean_social_simulator.models import CommandAction, GameEmployee, Order, PlayerCommand
from korean_social_simulator.social.memory import AgentMemorySystem
from korean_social_simulator.social.relationships import RelationshipGraph
from korean_social_simulator.social.rumor import Rumor, RumorEngine


@dataclass
class GameRunner:
    manager: GameStateManager
    out: TextIO = sys.stdout

    def run(self) -> dict[str, object]:
        memory = AgentMemorySystem()
        builder = GamePromptBuilder(memory)
        slm = SLMRuntimeAdapter(provider="none")
        task_sys = TaskSystem(self.manager)
        economy = EconomicSystem(self.manager, task_sys)
        events = RandomEventSystem(self.manager)
        scoring = ScoringSystem(self.manager)
        relationships = RelationshipGraph(self.manager)
        relationships.build_initial_bonds()
        rumor_engine = RumorEngine(self.manager)
        loop = GameLoopController(self.manager)
        cmd_sys = PlayerCommandSystem(self.manager)

        self._print_header()

        while not self.manager.game_state.is_finished:
            round_num = self.manager.game_state.round_number
            phase = self.manager.game_state.phase
            if phase == "morning":
                self._print_round_header(round_num)
                orders = economy.generate_round_orders(round_num, count=5)
                self._print_orders(orders)
                self._auto_assign_tasks(task_sys, round_num)
                self._auto_commands(cmd_sys, memory, builder, slm, round_num)
                if round_num % 2 == 0:
                    rumor = rumor_engine.create_rumor("salary", "p1", self._first_emp().employee_id)
                    rumor_engine.spread(rumor)
                    self._print_rumor(rumor)
            next_phase = loop.advance_phase()
            if next_phase == "work":
                results = task_sys.process_round(round_num)
                self._print_work_results(results)
            elif next_phase == "event":
                event = events.roll_event(round_num)
                self._print_event(event)
            elif next_phase == "evening":
                settle = economy.settle_round(round_num)
                self._print_settle(settle)
                rumor_engine.decay_round()
            if scoring.is_game_over():
                break

        self._print_footer(scoring)
        return scoring.final_report()

    def _first_emp(self) -> GameEmployee:
        for company in self.manager.game_state.companies.values():
            active = company.active_employees()
            if active:
                return active[0]
        raise RuntimeError("No active employees.")

    def _auto_assign_tasks(self, task_sys: TaskSystem, round_num: int) -> None:
        for _pid, company in self.manager.game_state.companies.items():
            active = company.active_employees()
            if not active:
                continue
            for emp in active:
                if not company.pending_orders:
                    break
                order = company.pending_orders.pop(0)
                task = task_sys.create_task_from_order(order, round_num)
                task_sys.assign_task(task, emp.employee_id)

    def _auto_commands(
        self,
        cmd_sys: PlayerCommandSystem,
        memory: AgentMemorySystem,
        builder: GamePromptBuilder,
        slm: SLMRuntimeAdapter,
        round_num: int,
    ) -> None:
        actions: list[CommandAction] = ["praise", "snack", "bonus"]
        for pid, company in self.manager.game_state.companies.items():
            active = company.active_employees()
            if not active:
                continue
            emp = active[0]
            action: CommandAction = actions[round_num % len(actions)]
            cmd = PlayerCommand(
                command_id=f"auto-{pid}-{round_num}",
                player_id=pid,
                action=action,
                target_employee_id=emp.employee_id,
                round_number=round_num,
                payload={"cost": 200 if action == "snack" else 500},
            )
            result = cmd_sys.execute(cmd)
            memory.remember(
                emp.employee_id,
                f"라운드{round_num}: {action} 받음 ({result.message})",
            )
            prompt = builder.build_command_prompt(emp, pid, cmd)
            response = slm.generate(prompt, builder.system_prompt())
            memory.remember(emp.employee_id, f"직원 반응: {response.dialogue}")

    def _print(self, text: str = "") -> None:
        self.out.write(text + "\n")

    def _print_header(self) -> None:
        state = self.manager.game_state
        self._print("=" * 60)
        self._print("  AI 회사 운영 게임 — 시뮬레이션 시작")
        self._print("=" * 60)
        for pid, company in state.companies.items():
            self._print(
                f"  {company.company_name} (사장: {pid}) — 자금 {company.funds}원, "
                f"직원 {len(company.active_employees())}명"
            )
        self._print()

    def _print_round_header(self, round_num: int) -> None:
        self._print("-" * 60)
        self._print(f"  라운드 {round_num} / {self.manager.game_state.max_rounds}")
        self._print("-" * 60)

    def _print_orders(self, orders: dict[str, list[Order]]) -> None:
        self._print("  [아침] 주문 접수:")
        for pid, order_list in orders.items():
            company = self.manager.company(pid)
            self._print(f"    {company.company_name}: {len(order_list)}건")
            for o in order_list:
                self._print(
                    f"      {o.customer_name} ({o.task_category}) "
                    f"난이도{o.difficulty} 보상{o.reward}원"
                )

    def _print_rumor(self, rumor: Rumor) -> None:
        self._print(f'  [소문] "{rumor.content}" (종류: {rumor.kind})')

    def _print_work_results(self, results: dict[str, int]) -> None:
        self._print(
            f"  [업무] 완료 {results['completed']} | 실패 {results['failed']} | "
            f"진행중 {results['pending']}"
        )

    def _print_event(self, event: GameEvent | None) -> None:
        if event:
            self._print(f"  [이벤트] {event.description}")
        else:
            self._print("  [이벤트] 이번 라운드는 특별 이벤트 없음")

    def _print_settle(self, settle: dict[str, dict[str, int]]) -> None:
        self._print("  [저녁] 정산:")
        for pid, s in settle.items():
            company = self.manager.company(pid)
            self._print(
                f"    {company.company_name}: 수입 {s['income']}원 / "
                f"지출 {s['expenses']}원 / 순이익 {s['net']:+d}원 "
                f"(잔액 {s['funds_after']}원)"
            )

    def _print_footer(self, scoring: ScoringSystem) -> None:
        report = scoring.final_report()
        rankings = cast(list[dict[str, object]], report["rankings"])
        self._print()
        self._print("=" * 60)
        self._print("  게임 종료 — 최종 결과")
        self._print("=" * 60)
        for entry in rankings:
            bankrupt = " [파산]" if entry["bankrupt"] else ""
            self._print(
                f"  {entry['rank']}. {entry['company_name']} (사장: {entry['player_id']}) "
                f"— 가치 {entry['value']}원{bankrupt}"
            )
            self._print(
                f"     자금 {entry['funds']}원 | 완료 {entry['completed_tasks']} | "
                f"실패 {entry['failed_tasks']} | 만족도 {entry['customer_satisfaction']} | "
                f"직원 {entry['employees']}명"
            )
        winner = report.get("winner")
        if winner:
            wcompany = self.manager.company(cast(str, winner)).company_name
            self._print()
            self._print(f"  승리: {wcompany} (사장: {winner})")
        self._print(f"  총 이벤트: {report['total_events']}개 | 라운드: {report['rounds_played']}")
        self._print("=" * 60)


def run_demo_game() -> dict[str, object]:
    mgr = GameStateManager.new_game(
        player_specs=[
            ("p1", "알파상사"),
            ("p2", "베타테크"),
            ("p3", "감마로직"),
            ("p4", "델타푸드"),
        ],
        max_rounds=5,
    )
    runner = GameRunner(manager=mgr)
    return runner.run()


if __name__ == "__main__":
    run_demo_game()
