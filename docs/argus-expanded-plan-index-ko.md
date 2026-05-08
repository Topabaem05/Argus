# Argus Expanded Plan - Korean Summary and Implementation Index

이 문서는 `/Users/guribbong/Downloads/argus_expanded_plan_sections`의 00-18번 계획 문서를 실행용 문서로 재정리한 요약본이다.

목적은 두 가지다.

1. 현재 계획이 실제로 요구하는 구현 범위를 짧게 파악한다.
2. Codex 코드 작업과 별개로, 문서 기반 구현 순서를 한눈에 추적한다.

## 1. 한 줄 요약

Argus의 핵심 목표는 그대로 유지된다.

- `kssim` CLI가 중심이다.
- 오프라인 deterministic dry-run이 기본이다.
- Pydantic v2 모델이 경계다.
- Unity는 시각화와 관찰 계층이다.
- 브리지, Hugging Face, LLM, NVIDIA API, Concordia, MuJoCo 계열은 모두 optional adapter 또는 fallback 경로여야 한다.
- 안전 정책은 정치적 설득, 실존 인물 프로파일링, 괴롭힘을 금지한다.

## 2. 계획의 실제 구현 순서

이 계획은 완성형 기능 묶음처럼 보이지만, 실행 관점에서는 다음 순서가 가장 자연스럽다.

1. 미니봇 rig, animation, navigation 안정화
2. Unity rendering 스타일 정리
3. chat bar와 attachment input
4. persona selection
5. mini-bot persona binding
6. individual evaluation
7. discussion and debate simulation
8. persona memory update and backup
9. backend bridge and optional API adapter
10. event logging, reporting, verification, risk handling, release docs

## 3. Implementation Index

### Phase A. Baseline and repository framing

- `00-introduction-and-scope.md`
- `00-planning-scope-and-baseline-assumptions.md`
- `01-repository-and-architecture-review-plan.md`

요점:

- 현재 repo 구조와 아키텍처를 먼저 확인한다.
- 어떤 기능이 이미 존재하고, 어떤 기능이 새로 필요한지 분리한다.
- 문서, spec, memory-bank, Unity tree, backend tree의 역할 경계를 정리한다.

### Phase B. Mini-bot quality foundation

- `02-mini-bot-rig-and-animation-audit-plan.md`
- `03-schoolroom-movement-and-navigation-plan.md`
- `04-mujoco-style-rendering-and-shader-plan.md`

요점:

- 미니봇의 rig 상태, clip 상태, root motion 정책을 정리한다.
- 벽 충돌, 회전 꼬임, 정지 복구, wander behavior를 안정화한다.
- 렌더링은 simple lit, flat lighting, solid color 계열의 가독성 중심 스타일을 목표로 한다.

### Phase C. User input and persona selection

- `05-2d-chat-bar-and-attachment-ui-plan.md`
- `06-persona-selection-plan.md`
- `07-mini-bot-persona-binding-plan.md`

요점:

- Unity 하단 chat bar에서 text와 attachment를 입력받는다.
- 파일 검증, 미리보기, 업로드, backend handoff를 정의한다.
- 최대 20명 수준의 persona selection과 mini-bot 1:1 binding을 준비한다.

### Phase D. Simulation phases and social dynamics

- `08-individual-evaluation-phase-plan.md`
- `09-discussion-and-debate-simulation-plan.md`
- `10-persona-memory-update-and-backup-plan.md`

요점:

- 개인 평가, 토론, stance 변화, 관계 변화, conflict/bonding, belief drift를 단계적으로 다룬다.
- persona memory update는 opt-in 이고 backup/rollback이 전제다.
- hidden state와 public display state를 명확히 분리해야 한다.

### Phase E. Backend bridge and observability

- `11-backend-bridge-and-nvidia-api-plan.md`
- `12-simulation-event-logging-and-reporting-plan.md`

요점:

- Unity는 backend bridge를 통해 simulation을 시작하고 이벤트를 받아야 한다.
- API key, long prompt, dataset write, report generation은 Unity 밖에 있어야 한다.
- 모든 중요한 simulation event는 audit 가능해야 하며, report는 저장된 event만으로 재생성 가능해야 한다.

### Phase F. Execution workflow, validation, and release

- `13-react-execution-workflow-plan.md`
- `14-implementation-phases.md`
- `15-verification-plan.md`
- `16-key-risks-and-mitigations.md`
- `17-documentation-deliverables.md`
- `18-overall-definition-of-done.md`

요점:

- 작업은 mini-bot animation fixes와 persona simulation features를 분리해서 진행한다.
- 검증은 Unity, backend, integration, performance로 나뉜다.
- 문서는 구현보다 먼저 정리되어야 한다.
- 완료 기준은 기능, 안전, 테스트, 문서, release readiness를 함께 만족해야 한다.

## 4. Repo mapping

현재 repo의 문서와 계획을 맞물려 보면 다음이 핵심 앵커다.

- `README.md`: 사용자-facing overview와 offline-first promise
- `docs/architecture.md`: 시스템 분해와 data flow
- `docs/design.md`: 설계 방향과 implementation style
- `docs/robot-asset-selection.md`: 로봇 asset 정책과 라이선스 경계
- `docs/persona-evaluation-arena-implementation-plan.md`: Unity bridge 쪽의 별도 handoff 요약
- `memory-bank/tasks.md`: 실행용 task graph와 progress anchor
- `memory-bank/progress.md`: 현재 상태 기록
- `specs/ai-unity-mujoco-bridge/`: 기존 bridge-oriented spec pack

이 계획 묶음은 기존 bridge spec보다 더 넓다.

- bridge only가 아니라 input, selection, evaluation, memory, reporting까지 포함한다.
- Unity UI와 persona simulation behavior를 함께 다룬다.
- documentation deliverables와 definition of done이 별도 장으로 존재한다.

## 5. 문서화된 요구사항 체크리스트

계획 문서에서 명시적으로 드러나는 요구사항은 다음과 같다.

- 오프라인 deterministic dry-run 유지
- optional adapters 유지
- Unity scene에서 mini-bot 가시화
- chat/attachment UI
- persona selection
- mini-bot persona binding
- individual evaluation
- discussion/debate simulation
- persona memory update와 backup/rollback
- backend bridge와 optional NVIDIA API
- event logging and reporting
- React execution workflow
- phase-based implementation ordering
- Unity/backend/integration/performance verification
- risk mitigation
- documentation deliverables
- definition of done

## 6. 아직 문서상 모호한 부분

다음 항목은 계획에는 나오지만, 현재 repo 문서만으로는 구현 방식이 확정되지 않는다.

- chat attachment의 실제 파일 전달 방식
  - local file reference인지, bytes upload인지, 둘 다 허용인지 불명확하다.
- persona selection의 정확한 랭킹 기준
  - similarity, rule-based filter, LLM-assisted scoring 중 무엇이 주도권을 가지는지 불명확하다.
- backend bridge의 프로토콜
  - HTTP, WebSocket, local process bridge 중 최종 우선순위가 고정되어 있지 않다.
- Unity와 backend 사이의 state authority
  - 어떤 state는 Python이 최종 권한인지, Unity가 observer인지 세부 경계가 더 필요하다.
- persona memory update의 저장 포맷과 승인 흐름
  - backup, rollback, apply-update의 구체적 승인 단위가 부족하다.
- MuJoCo 표기와 현재 repo의 deterministic fallback 방침
  - 계획 파일은 MuJoCo 용어를 포함하지만, 현재 repo 문서는 fallback physics 우선으로 정리되어 있다.
- `Simple Lit`, `URP`, `flat-lighting` 표현의 최종 우선순위
  - 렌더링 요구가 여러 파일에 분산되어 있고, 하나의 단일 기준으로 정리되어 있지 않다.

## 7. Practical reading order for implementers

새 작업자가 이 계획을 읽는다면 다음 순서가 가장 효율적이다.

1. `00`-`01`로 전체 범위와 repo 경계를 확인한다.
2. `02`-`04`로 Unity 시각 안정성을 먼저 잡는다.
3. `05`-`07`로 사용자 입력과 persona binding을 연결한다.
4. `08`-`10`으로 simulation semantics를 확장한다.
5. `11`-`12`로 bridge, logging, reporting을 연결한다.
6. `13`-`18`로 검증, 위험, 문서, 완료 기준을 잠근다.

## 8. Current takeaway

이 계획의 핵심은 "Unity를 붙인다"가 아니라, "Argus의 text simulation을 authoritative source로 유지한 채, Unity는 시각화와 관찰을 맡고, 입력-선택-평가-토론-메모리-보고서까지를 문서와 검증으로 묶는다"이다.

즉, 구현보다 먼저 필요한 것은 다음이다.

- repo 경계 정리
- scene/UI/bridge 역할 분리
- optional dependency 경계 유지
- 안전 정책 명시
- 검증 순서 고정
