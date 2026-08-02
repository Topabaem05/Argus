# React Three Fiber 고정 아이소메트릭 오피스 프로토타입 설계

- 상태: 사용자 승인 설계의 저장소 명세화
- 작성일: 2026-08-02
- 대상 저장소: `Topabaem05/Argus`
- 대상 브랜치: `agent/r3f-office-prototype`
- 구현 대상: `web/office-sim`

## 1. 결정 요약

독립 실행형 브라우저 프로토타입을 새로 만든다. Python 브리지, Unity, LLM, 네트워크 서버 없이 React, TypeScript, Three.js와 React Three Fiber만으로 실행된다.

핵심 결정은 다음과 같다.

1. 카메라는 회전·팬이 없는 고정 직교 아이소메트릭 뷰다.
2. 플레이어 Minibot 한 대는 `WASD`/방향키로 직접 이동하고 `E`로 상호작용한다.
3. 다른 Minibot 네 대는 NPC이며 Utility AI와 A* 격자 경로 탐색으로 자율 행동한다.
4. 사무실 가구는 업로드된 `3D Voxel Office Pack - Voxel File.zip`에서 선별한다.
5. 선별된 `.vox` 파일은 별도 GLB 변환 없이 Three.js 공식 `VOXLoader`로 직접 로드한다. 프로토타입 단계에서 불필요한 에셋 변환 파이프라인을 만들지 않는다.
6. Argus Unity Minibot의 외형과 상태 명칭은 참조하되, 웹 Minibot은 Three.js 프리미티브로 독립 재구성한다.
7. 물리 엔진은 사용하지 않는다. 정적 AABB, 에이전트 원형 충돌체, 격자 점유와 예약으로 이동을 처리한다.
8. 시뮬레이션 코어와 R3F 렌더링을 분리해 향후 Argus 브리지 연결이 가능하도록 한다.

## 2. 목표와 비목표

### 2.1 목표

- 브라우저를 열면 사무실 전체가 한 화면에 읽히는 고정 아이소메트릭 장면을 제공한다.
- 플레이어가 즉시 Minibot을 이동하고 PC, 프린터, 커피 머신, 소파, NPC와 상호작용할 수 있게 한다.
- 네 NPC가 서로 다른 역할 성향에 따라 업무, 출력, 커피, 휴식, 대화를 선택하게 한다.
- NPC가 장애물을 통과하거나 동일 상호작용 지점을 중복 점유하지 않게 한다.
- Minibot의 `Idle`, `Walk`, `Work`, `Talk`, `Cheer`, `Complain`, `Refuse`, `Gossip`, `Quit` 상태를 절차 애니메이션으로 표현한다.
- 서버가 없어도 행동이 결정적이고 재현 가능하도록 시드 기반 난수를 사용한다.
- 테스트 가능한 프레임워크 비의존 시뮬레이션 코어를 만든다.

### 2.2 비목표

- Python Argus 브리지 연결
- Unity 씬 대체 또는 Unity 코드 삭제
- LLM/SLM 기반 대화와 판단
- 멀티플레이어, 로그인, 저장 서버
- 일반 목적 NavMesh 생성기
- 리깅된 캐릭터 또는 모션 캡처 애니메이션
- 사무실 편집기와 에셋 배치 UI
- 모바일 터치 조작 최적화
- 업로드된 185개 항목 전체 사용

## 3. 출처와 라이선스

### 3.1 사무실 에셋

업로드 ZIP의 `README.txt`에 기록된 정보:

- 팩 이름: `3D Voxel Office Pack`
- 저자: `MariaIsMe`
- 원본 페이지: `https://mariaisme.itch.io/3d-voxel-office`
- 라이선스: `Attribution 4.0 International (CC BY 4.0)`
- 라이선스 본문: `https://creativecommons.org/licenses/by/4.0/`

ZIP은 디렉터리 항목과 미리보기 이미지를 포함해 185개 항목이며, 개별 모델은 주로 16, 32 voxel 단위로 구성된다.

프로토타입은 다음을 반드시 포함한다.

- 앱 하단의 짧은 표시: `Office voxel assets by MariaIsMe — CC BY 4.0`
- `web/office-sim/THIRD_PARTY_NOTICES.md`
- `web/office-sim/public/assets/office/asset-manifest.json`
- 원본 파일명, 저자, 원본 페이지, 라이선스 링크, 사용 파일 목록

itch.io 페이지의 추가 설명보다 ZIP 내부의 CC BY 4.0 고지를 보수적으로 적용한다. 에셋 팩 자체를 별도 다운로드 상품처럼 재배포하지 않고 프로토타입에 필요한 선별 파일만 포함한다.

### 3.2 Minibot

Argus의 기존 `CuteRobotPrefabInitializer`가 사용하는 캡슐 몸통, 구형 머리, 안테나, 바이저 실루엣을 코드로 재구성한다. 기존 Unity 프리팹이나 외부 로봇 바이너리는 복사하지 않는다.

## 4. 사용자 경험

### 4.1 카메라

- 직교 카메라
- 초기 위치: `(13, 13, 13)`
- 주시점: `(0, 0.8, 0)`
- 회전, 마우스 팬, 휠 줌 비활성화
- 화면 비율에 따라 `orthographicSize`만 자동 계산해 전체 사무실 경계를 유지
- 북쪽과 서쪽 벽만 배치하는 dollhouse 구성으로 시야 가림 방지
- 창 크기가 작아져도 플레이어와 주요 상호작용 구역은 화면 밖으로 잘리지 않아야 함

### 4.2 조작

| 입력 | 동작 |
|---|---|
| `WASD` 또는 방향키 | 카메라 기준 평면 이동 |
| `Shift` | 이동 속도 1.45배 |
| `E` | 가장 적합한 근접 대상과 상호작용 |
| 이동 입력 | 진행 중인 플레이어 상호작용 취소 |
| `Esc` | 열린 도움말 또는 디버그 패널 닫기 |

플레이어 이동 속도는 기본 `3.4 world units/s`, 달리기는 `4.93 world units/s`다. 플레이어 충돌 반경은 `0.34`, NPC 충돌 반경은 `0.31`이다.

### 4.3 화면 UI

- 좌상단: 현재 위치/행동 상태와 조작 도움말
- 화면 하단 중앙: `E · 커피 마시기` 형태의 상호작용 프롬프트
- NPC 머리 위: 이름과 현재 행동 아이콘, 짧은 말풍선
- 우상단: NPC 네 대의 현재 상태를 보여주는 접을 수 있는 상태 패널
- 우하단: 최근 이벤트 최대 세 개
- 하단 가장자리: 에셋 저작자와 CC BY 4.0 표시
- 개발 모드에서만 격자, 경로, 예약 지점, 충돌체를 표시하는 디버그 토글

## 5. 사무실 월드

### 5.1 좌표와 격자

- 바닥 크기: `20 × 14 world units`
- 월드 경계: X `[-10, 10]`, Z `[-7, 7]`
- 내비게이션 셀 크기: `0.5`
- 격자 크기: `40 × 28`
- voxel 정규화: `16 voxels = 1 world unit`
- `.vox` 로드 후 바운딩 박스의 중심을 X/Z 원점에 맞추고 `minY = 0`이 되도록 자동 보정
- 각 에셋의 회전, 추가 스케일, 충돌 footprint는 매니페스트에서 명시

### 5.2 구역

| 구역 | 위치 | 주요 오브젝트 |
|---|---|---|
| 플레이어 시작 구역 | 남서 | 플레이어 시작점, 개인 책상 |
| 개발 업무 구역 | 서쪽 중앙 | 큐비클 2개, PC 2개, 의자 |
| 관리 업무 구역 | 동쪽 중앙 | 프린터, 캐비닛, 문서 테이블 |
| 휴게 구역 | 북동 | 커피 머신, 머그, 작은 테이블 |
| 라운지 | 남동 | 소파, 커피 테이블, 식물 |
| 공용 이동축 | 중앙 | NPC 교차 이동과 대화 공간 |

### 5.3 선별 `.vox` 파일

초기 프로토타입은 다음 파일만 사용하며 같은 모델은 인스턴스/클론으로 재사용한다.

```text
Tables/Office_Table_White_2x1_01.vox
Tables/Office_Table_Coffee_02_White.vox
Chairs/Office_Chair_Black_01.vox
Chairs/Office_Couch_White_01.vox
Cubicles/Office_Cubicle_Light_01.vox
Misc/Electronics/Office_Misc_PC_01.vox
Misc/Electronics/Office_Misc_Printer.vox
Misc/Coffee/Office_Misc_Coffee_Machine_01.vox
Misc/Coffee/Office_Misc_Coffee_Mug.vox
Misc/Office_Misc_Cabinet_01.vox
Misc/Office_Misc_Papers.vox
Misc/Office_Misc_Plant_01.vox
Misc/Office_Misc_Wall_Clock_01.vox
```

바닥과 두 외벽은 코드로 생성한 단순 box geometry를 사용한다. 전체 통합 파일 `3D Voxel Office Pack.vox`는 로드하지 않는다.

## 6. 시스템 아키텍처

```text
DOM Input / HUD
      |
      v
PlayerController -----------+
                            |
Fixed-step SimulationWorld  |  10 Hz authoritative simulation
  - Agent runtime           |
  - Utility AI              |
  - Navigation grid         |
  - Reservations            |
  - Interactions            |
  - Event log               |
      |                     |
      +---- snapshots/events+
                |
                v
Zustand UI store + R3F render adapters
                |
                v
Canvas / Minibot visuals / Office assets
```

### 6.1 경계

- `SimulationWorld`가 위치, 목적지, 행동 상태, 필요도, 예약을 관리하는 유일한 권위다.
- React 컴포넌트는 시뮬레이션 상태를 직접 변경하지 않고 command를 전달한다.
- R3F 렌더링은 시뮬레이션 스냅샷 사이를 보간한다.
- Zustand는 도움말, 프롬프트, 선택된 디버그 정보, 최근 이벤트처럼 UI 구독이 필요한 저빈도 상태만 저장한다.
- 에이전트의 매 프레임 transform을 React state에 넣지 않는다.

### 6.2 시뮬레이션 시간

- 고정 step: `0.1 s`
- 한 프레임에서 누적 step은 최대 5회
- 탭 비활성화 등으로 시간이 크게 건너뛴 경우 초과 누적 시간을 버려 spiral-of-death를 막음
- NPC utility 재평가: `1.0 s` 간격
- 현재 행동 최소 유지 시간: `2.0 s`
- 같은 행동 재선택 cooldown: `3.0 s`

## 7. Minibot 표현

### 7.1 구조

각 Minibot은 다음 Three.js 프리미티브로 구성한다.

- `CapsuleGeometry`: 몸통
- `SphereGeometry`: 머리
- `BoxGeometry`: 바이저
- 작은 `SphereGeometry`: 안테나
- 바닥 selection/interaction ring
- NPC별 역할 색상 accent

플레이어는 청록 계열, NPC는 역할별 파스텔 변형을 사용한다. 머리와 몸통은 공통 geometry/material을 공유해 불필요한 할당을 줄인다.

### 7.2 절차 애니메이션

| 상태 | 표현 |
|---|---|
| `Idle` | 느린 호흡형 상하 이동 |
| `Walk` | 보행 bob, 좌우 tilt, 이동 방향 회전 |
| `Work` | 전방 기울임과 짧은 고개 움직임 |
| `Talk` | 상대를 바라보고 작은 좌우 sway |
| `Cheer` | 1회 또는 2회 짧은 hop |
| `Complain` | 몸통 처짐과 좌우 흔들림 |
| `Refuse` | 빠른 좌우 head shake |
| `Gossip` | 상대에게 기울고 작은 말풍선 |
| `Quit` | accent 감소 후 출구 anchor로 이동 |

시각 상태와 게임 상태는 `MinibotMotionState` 문자열 union으로 연결한다. 애니메이션은 world position을 변경하지 않는다.

## 8. 플레이어 제어와 충돌

### 8.1 이동

- 키 상태를 매 프레임 읽고 카메라의 평면 forward/right 기준으로 이동 벡터 생성
- 대각선 속도는 정규화
- Minibot은 이동 방향을 향해 최대 `720°/s`로 회전
- 시뮬레이션에 `MovePlayerCommand`를 전달

### 8.2 충돌 처리

- 플레이어/NPC는 XZ 평면 원형 충돌체
- 가구와 벽은 사전 계산된 AABB footprint
- 원하는 이동량에 대해 원-대-AABB 충돌을 검사하고 최대 3회 iterative slide resolution
- 월드 경계 밖 이동 차단
- 플레이어와 NPC는 서로 통과하지 않음
- 작은 시작 겹침은 최소 이동 벡터로 분리
- 프레임 시간 급증에도 가구를 관통하지 않도록 이동 거리를 반지름의 75% 이하 sub-step으로 분할

## 9. NPC Utility AI

### 9.1 NPC 구성

| ID | 역할 | 우선 성향 |
|---|---|---|
| `npc-dev-01` | 개발 | PC 업무 |
| `npc-admin-01` | 관리 | 출력·문서 |
| `npc-support-01` | 지원 | 대화·커피 |
| `npc-general-01` | 일반 | 균형 행동 |

각 NPC는 다음 정규화 상태를 가진다.

```ts
energy: number        // 0..1, 높을수록 활력 있음
stress: number        // 0..1
socialNeed: number    // 0..1
workBacklog: number   // 0..1
coffeeNeed: number    // 0..1
pendingPrint: boolean
```

초기값은 고정 seed에서 생성하되 각 역할별 범위가 정의돼 같은 seed에서 항상 같은 시작 상태가 나온다.

### 9.2 Utility 점수

모든 값은 계산 후 `0..1`로 clamp하며, 역할 affinity는 `0..0.12`, 시드 기반 jitter는 `[-0.03, 0.03]`이다.

```text
Work   = 0.55*workBacklog + 0.25*energy - 0.20*stress + affinity.work
Coffee = 0.50*coffeeNeed + 0.25*(1-energy) + 0.15*stress + affinity.coffee
Rest   = 0.60*(1-energy) + 0.35*stress + affinity.rest
Talk   = 0.65*socialNeed + 0.20*nearbyAvailable + affinity.talk
Print  = 0.45*workBacklog + 0.35*pendingPrint + affinity.print
Idle   = 0.08
```

경로가 없거나 사용 가능한 anchor가 없는 행동의 점수는 선택 후보에서 제거한다. 현재 행동 최소 유지 시간과 cooldown을 만족한 뒤 가장 높은 행동을 선택한다.

### 9.3 필요도 변화

기본 초당 변화:

```text
energy      -0.004
socialNeed  +0.010
workBacklog +0.012
coffeeNeed  +0.008
stress      -0.002 when Idle/Rest, otherwise unchanged
```

행동 완료 효과:

| 행동 | 지속 | 효과 |
|---|---:|---|
| Work | 5.0 s | backlog -0.45, energy -0.18, stress +0.12, 25% 확률로 pendingPrint=true |
| Print | 3.0 s | pendingPrint=false, backlog -0.15, stress +0.03 |
| Coffee | 2.5 s | coffeeNeed -0.65, energy +0.18, stress -0.08 |
| Rest | 4.0 s | energy +0.30, stress -0.25 |
| Talk | 3.0 s | 양쪽 socialNeed -0.55, stress -0.08 |
| Idle | 1.5–3.0 s | 추가 효과 없음 |

값은 모두 `0..1`로 clamp한다.

### 9.4 상태 머신

```text
Idle
  -> Planning
  -> Navigate
  -> Work | Print | Coffee | Rest | Talk
  -> Idle

Any non-Quit state
  -> ReactToPlayer
  -> previous recoverable state or Idle

Any state
  -> Quit
  -> Navigate(exit)
  -> Despawned
```

NPC가 `Work` 또는 `Print` 중일 때 플레이어가 대화를 요청하면 행동을 중단하지 않고 `지금 처리 중이에요.` 말풍선만 표시한다. `Idle`, `Navigate`, `Rest` 상태는 대화로 전환할 수 있다.

## 10. 경로 탐색과 예약

### 10.1 A*

- 8방향 이웃
- 대각선 corner-cut 금지
- octile distance heuristic
- 정적 장애물 셀 + 동적 예약 셀
- 시작/끝 셀은 에이전트 반경을 고려해 유효성 검사
- 계산된 경로는 line-of-sight 단축을 거쳐 불필요한 지그재그를 제거

### 10.2 상호작용 anchor

각 상호작용 오브젝트는 다음 데이터를 가진다.

```ts
interface InteractionAnchor {
  id: string
  kind: 'desk' | 'printer' | 'coffee' | 'sofa' | 'exit'
  position: [number, number, number]
  facing: number
  capacity: number
  compatibleActions: NpcAction[]
}
```

- 모든 기본 anchor capacity는 1
- Talk는 두 에이전트 사이에 생성하는 임시 pair reservation 사용
- 예약에는 owner, 만료 시각, 목적 행동이 포함됨
- 목적지까지 도달하지 못한 채 8초가 지나면 예약 자동 해제
- 도착 후 행동 완료 시 즉시 해제

### 10.3 막힘 복구

- 1.2초 동안 목표 방향 진행량이 임계치 미만이면 재탐색
- 동일 목적지에서 3회 연속 실패하면 예약을 해제하고 해당 행동을 5초 cooldown
- 대체 anchor가 있으면 가장 낮은 경로 비용의 anchor 선택
- 대체가 없으면 Idle로 복귀하고 디버그 이벤트 기록

## 11. 상호작용 시스템

플레이어의 상호작용 후보는 다음 순서로 결정한다.

1. 거리 `<= 1.25`
2. 플레이어 전방 120도 cone 안에 있는 대상 우선
3. 명시적 interactable 우선, NPC는 그 다음
4. 같은 우선순위에서는 거리 최소 대상

| 대상 | 프롬프트 | 플레이어 표현 |
|---|---|---|
| PC/책상 | `E · 업무 시작` | 4초 Work, 이동하면 취소 |
| 프린터 | `E · 문서 출력` | 2.5초 Print/Work |
| 커피 머신 | `E · 커피 마시기` | 2.5초 Coffee 후 Cheer |
| 소파 | `E · 잠깐 쉬기` | 4초 Rest |
| NPC | `E · 대화하기` | 사용 가능 시 양쪽 Talk |

상호작용 중 movement command가 들어오면 플레이어 행동과 anchor 예약을 취소한다. 완료/취소는 이벤트 로그와 HUD에 반영한다.

## 12. 데이터 모델과 이벤트

### 12.1 주요 타입

```ts
type ActorId = 'player-p1' | `npc-${string}`

type MinibotMotionState =
  | 'Idle' | 'Walk' | 'Work' | 'Talk' | 'Cheer'
  | 'Complain' | 'Refuse' | 'Gossip' | 'Quit'

type NpcAction = 'Work' | 'Print' | 'Coffee' | 'Rest' | 'Talk' | 'Idle'
```

`AgentRuntime`은 identity, role, position, heading, radius, motion state, current action, target, path, needs, cooldown을 가진다.

### 12.2 이벤트

```ts
type OfficeEvent =
  | { type: 'agent.action.started'; actorId: ActorId; action: string }
  | { type: 'agent.action.completed'; actorId: ActorId; action: string }
  | { type: 'agent.action.cancelled'; actorId: ActorId; action: string }
  | { type: 'agent.path.replanned'; actorId: ActorId; reason: string }
  | { type: 'interaction.busy'; actorId: ActorId; targetId: string }
  | { type: 'reservation.expired'; anchorId: string; actorId: ActorId }
```

이벤트에는 시뮬레이션 tick과 monotonic timestamp를 추가한다. 최근 100개만 메모리에 유지한다.

## 13. React/R3F 구성

```text
web/office-sim/
  package.json
  package-lock.json
  index.html
  THIRD_PARTY_NOTICES.md
  public/
    assets/office/
      asset-manifest.json
      *.vox
  src/
    app/
      App.tsx
      SimulationProvider.tsx
    scene/
      OfficeCanvas.tsx
      IsometricCamera.tsx
      OfficeEnvironment.tsx
      OfficeAsset.tsx
      Lighting.tsx
    entities/minibot/
      MinibotVisual.tsx
      PlayerMinibot.tsx
      NpcMinibot.tsx
      proceduralMotion.ts
    simulation/
      SimulationWorld.ts
      fixedStepClock.ts
      types.ts
      seed.ts
      npc/
        utilityAI.ts
        npcStateMachine.ts
        needs.ts
      navigation/
        NavigationGrid.ts
        aStar.ts
        pathSmoothing.ts
        reservations.ts
      collision/
        circleAabb.ts
        movementResolver.ts
      interactions/
        interactionRegistry.ts
        interactionResolver.ts
    world/
      officeLayout.ts
      assetManifest.ts
    input/
      useKeyboardInput.ts
    state/
      uiStore.ts
    ui/
      Hud.tsx
      InteractionPrompt.tsx
      NpcStatusPanel.tsx
      EventFeed.tsx
      Attribution.tsx
      DebugPanel.tsx
    test/
      fixtures.ts
      browserTestApi.ts
```

상태 머신은 별도 XState 의존성 대신 TypeScript discriminated union과 순수 transition 함수로 구현한다.

## 14. 렌더링과 성능

- R3F `Canvas`의 DPR 상한: `1.5`
- 한 개 ambient light와 한 개 directional light
- directional shadow map: `1024 × 1024`
- 후처리 라이브러리 없음
- repeated chair/PC는 geometry와 material 공유
- `.vox` 결과는 URL별로 캐시하고 clone
- `Suspense` 로딩 화면 사용
- UI는 Canvas 밖 DOM으로 렌더링
- 목표: 데스크톱 브라우저 60 FPS, 통합 GPU 30 FPS 이상
- 초기 범위: 플레이어 1 + NPC 4, 선별 에셋 13종, 배치 인스턴스 25개 이하

## 15. 오류 처리

- 에셋 로드 실패: 해당 footprint를 유지한 단색 box fallback과 경고 아이콘 표시
- 매니페스트와 실제 파일 불일치: 개발 모드에서는 시작 실패, 프로덕션 빌드에서는 fallback
- 유효하지 않은 spawn: 가장 가까운 walkable cell로 보정하고 이벤트 기록
- 경로 없음: 대상 예약 해제, 행동 cooldown, Idle 복귀
- 시뮬레이션 예외: ErrorBoundary에서 정적 사무실과 재시작 버튼 표시
- 저장소나 네트워크 없이 동작해야 하므로 런타임 fetch는 로컬 정적 에셋에만 허용

## 16. 테스트 전략

### 16.1 Vitest 단위 테스트

- A* 최단 경로, 대각선 corner-cut 방지, 경로 없음
- path smoothing이 장애물을 통과하지 않는지
- circle-AABB slide와 월드 경계
- utility 점수와 역할 affinity
- 같은 seed에서 동일 행동 시퀀스
- anchor 예약, 만료, 해제, capacity
- stuck recovery 3회 실패 처리
- 상호작용 후보 거리/방향/우선순위
- NPC busy 대화 규칙
- 필요도 변화와 clamp

### 16.2 React 컴포넌트 테스트

- HUD 프롬프트 상태
- 최근 이벤트 최대 세 개 표시
- 저작자/라이선스 표시
- 에셋 fallback UI

### 16.3 Playwright 브라우저 테스트

테스트 모드에서 `window.__OFFICE_DEBUG__` 읽기 전용 스냅샷을 제공한다.

- 앱과 Canvas가 오류 없이 로드됨
- `W` 입력 후 플레이어 위치가 예상 방향으로 변화
- 벽 방향 장기 입력에도 경계 밖으로 나가지 않음
- 커피 머신 근처에서 `E` 후 player action이 Coffee로 전환
- NPC 네 대가 15초 안에 적어도 한 번 자율 행동 시작
- 두 NPC가 같은 capacity 1 anchor를 동시에 소유하지 않음
- 고정 카메라 transform이 입력 후에도 변하지 않음

## 17. 완료 기준

1. `npm install`, `npm run dev`, `npm run build`가 `web/office-sim`에서 동작한다.
2. 고정 아이소메트릭 카메라에서 사무실 전체가 보인다.
3. 플레이어 Minibot이 WASD/방향키로 이동하고 가구와 벽을 통과하지 않는다.
4. `E`로 PC, 프린터, 커피 머신, 소파, NPC와 상호작용한다.
5. NPC 네 대가 Utility AI로 행동을 선택한다.
6. NPC가 A* 경로를 따라 이동하며 장애물을 통과하지 않는다.
7. capacity 1 anchor는 동시에 한 에이전트만 점유한다.
8. NPC 막힘이 발생해도 재탐색 또는 행동 포기로 복구한다.
9. Minibot 상태가 절차 애니메이션으로 구분된다.
10. 서버와 LLM 없이 동일 seed에서 재현 가능하다.
11. Vitest와 Playwright 필수 테스트가 통과한다.
12. 앱과 `THIRD_PARTY_NOTICES.md`에 MariaIsMe 및 CC BY 4.0 표기가 있다.

## 18. 향후 Argus 연결 경계

독립형 프로토타입에는 연결하지 않지만 다음 필드는 Argus와 호환 가능한 이름으로 유지한다.

- `actorId`
- `companyId`
- `role`
- `mood`
- `currentTask`
- `motionState`

향후 연결 시 `SimulationWorld`의 command/event adapter만 교체하고, R3F 장면과 Minibot 표현은 유지하는 방향이다. Python이 권위 상태를 공급할 때 로컬 Utility AI를 끄는 `authority: 'local' | 'remote'` 설정을 추가할 수 있다. 이 설정은 현재 프로토타입 범위에는 포함하지 않는다.

## 19. 명시적 위험과 대응

| 위험 | 대응 |
|---|---|
| `.vox` 축/피벗 차이 | 공통 바운딩 박스 정규화와 per-asset 회전 메타데이터 |
| 직접 로딩 시 draw call 증가 | 선별 에셋 제한, URL 캐시, geometry/material 공유 |
| NPC가 좁은 통로에서 교착 | anchor 예약, dynamic occupancy, stuck replan, cooldown |
| React 리렌더로 프레임 저하 | transform을 React state에서 제외, 저빈도 UI만 Zustand 구독 |
| 행동이 반복적으로 보임 | 역할 affinity, needs 변화, deterministic jitter, cooldown |
| 에셋 라이선스 누락 | visible attribution, notice 파일, asset manifest를 완료 기준으로 검사 |
