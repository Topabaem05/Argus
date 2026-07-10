"""Task system — generates, assigns, progresses, and judges work tasks."""

from __future__ import annotations

import random
import uuid
from dataclasses import dataclass

from korean_social_simulator.errors import SimulationError
from korean_social_simulator.game.state import GameStateManager
from korean_social_simulator.models import (
    GameEmployee,
    GameTask,
    Order,
    TaskCategory,
    TaskStatus,
)

_TASK_TEMPLATES: list[tuple[TaskCategory, str]] = [
    ("carry", "3번 창고에서 짐 5박스 이동"),
    ("deliver", "B거래처로 서류 배달"),
    ("deliver", "고객에게 상품 배송"),
    ("document", "계약서 작성 및 회계 처리"),
    ("document", "월간 보고서 작성"),
    ("sales", "신규 고객 상담"),
    ("sales", "거래처 미팅"),
    ("maintenance", "사무실 청소"),
    ("maintenance", "장비 점검"),
    ("carry", "이사 짐 운반"),
]

_CUSTOMER_NAMES = [
    "테크노마트",
    "그린바이오",
    "블루커머스",
    "실버로지스틱스",
    "옐로미디어",
    "레드푸드",
    "퍼플헬스케어",
    "오렌지건설",
]


@dataclass
class TaskSystem:
    manager: GameStateManager

    def generate_orders(self, round_number: int, count: int = 3) -> list[Order]:
        rng = random.Random(round_number * 31 + 7)
        orders: list[Order] = []
        for i in range(count):
            cat, _desc = rng.choice(_TASK_TEMPLATES)
            difficulty = rng.randint(2, 8)
            reward = difficulty * 150 + 100
            orders.append(
                Order(
                    order_id=f"order-{round_number:02d}-{i:02d}",
                    customer_name=rng.choice(_CUSTOMER_NAMES),
                    task_category=cat,
                    difficulty=difficulty,
                    reward=reward,
                    deadline_round=round_number + 1,
                )
            )
        return orders

    def create_task_from_order(self, order: Order, round_number: int) -> GameTask:
        _cat, desc_template = next(t for t in _TASK_TEMPLATES if t[0] == order.task_category)
        return GameTask(
            task_id=f"task-{uuid.uuid4().hex[:8]}",
            category=order.task_category,
            description=f"{order.customer_name}: {desc_template}",
            difficulty=order.difficulty,
            reward=order.reward,
            created_round=round_number,
        )

    def assign_task(self, task: GameTask, employee_id: str) -> None:
        emp = self.manager.employee(employee_id)
        if not emp.is_active:
            raise SimulationError(f"Employee {employee_id} is not active.")
        task.assigned_to = employee_id
        task.status = "assigned"
        if task not in self.manager.game_state.task_queue:
            self.manager.game_state.task_queue.append(task)
        self.manager.log_event(
            "agent_action",
            emp.employed_by,
            {
                "task_assigned": task.task_id,
                "employee": employee_id,
                "category": task.category,
            },
        )

    def progress_task(self, task_id: str) -> TaskStatus:
        task = self._find_task(task_id)
        if task.status not in ("assigned", "in_progress"):
            return task.status
        if not task.assigned_to:
            raise SimulationError(f"Task {task_id} has no assignee.")
        emp = self.manager.employee(task.assigned_to)
        stat = emp.stats.score_for(task.category)
        loyalty = emp.loyalty_to(emp.employed_by or "")
        mood_factor = (emp.mood - 50) / 100.0
        loyalty_factor = loyalty / 100.0
        base = stat / 10.0
        success_prob = max(
            0.3, min(0.95, 0.4 + base * 0.3 + mood_factor * 0.15 + loyalty_factor * 0.15)
        )
        rng = random.Random(hash(task_id) % 2**32)
        task.status = "in_progress"
        increment = 0.3 + success_prob * (0.3 + rng.random() * 0.4)
        task.progress = min(1.0, task.progress + increment)
        if task.progress >= 1.0:
            task.status = "completed"
            self._on_complete(task, emp)
        elif rng.random() > success_prob * 1.3:
            task.status = "failed"
            self._on_fail(task, emp)
        return task.status

    def _on_complete(self, task: GameTask, emp: GameEmployee) -> None:
        company = self.manager.company(emp.employed_by or "")
        company.completed_tasks += 1
        company.customer_satisfaction = min(100, company.customer_satisfaction + 3)
        self.manager.adjust_funds(emp.employed_by or "", task.reward)
        self.manager.adjust_mood(emp.employee_id, 5)
        self.manager.log_event(
            "metric_hook",
            emp.employed_by,
            {
                "task_completed": task.task_id,
                "reward": task.reward,
            },
        )

    def _on_fail(self, task: GameTask, emp: GameEmployee) -> None:
        company = self.manager.company(emp.employed_by or "")
        company.failed_tasks += 1
        company.customer_satisfaction = max(0, company.customer_satisfaction - 5)
        self.manager.adjust_mood(emp.employee_id, -10)
        self.manager.log_event(
            "metric_hook",
            emp.employed_by,
            {
                "task_failed": task.task_id,
                "penalty": 0,
            },
        )

    def process_round(self, round_number: int) -> dict[str, int]:
        results = {"completed": 0, "failed": 0, "pending": 0}
        for task in self.manager.game_state.task_queue:
            if task.status in ("assigned", "in_progress"):
                status = self.progress_task(task.task_id)
                if status == "completed":
                    results["completed"] += 1
                elif status == "failed":
                    results["failed"] += 1
                else:
                    results["pending"] += 1
            elif task.status == "pending":
                results["pending"] += 1
        return results

    def _find_task(self, task_id: str) -> GameTask:
        for t in self.manager.game_state.task_queue:
            if t.task_id == task_id:
                return t
        raise SimulationError(f"Task not found: {task_id}")
