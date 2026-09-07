# 설계 상태

이 문서는 아직 확정되지 않았거나 임시로 사용하는 결정과 의도적으로 보류한 사항을 기록한다.

`Docs/plan.md`와 `Docs/EventSystem.md`가 확정 규칙의 원본이다. 이 문서는 원본 규칙을 대체하지 않는다.

## 사용 규칙

- `Open`과 `Deferred` 항목은 게임 또는 생성기의 확정 요구사항이 아니다.
- `Provisional` 항목은 현재 프로토타입에서 사용할 수 있지만 플레이테스트나 후속 설계로 변경될 수 있다.
- 생성기는 Open 또는 Deferred 항목을 임의로 결정하거나 이를 위한 새 필드를 추가하지 않는다.
- 미결정 사항이 생성 작업을 막으면 임의로 가정하지 않고 해당 ID를 보고한다.
- 결정이 확정되면 관련 `plan.md` 또는 `EventSystem.md`를 먼저 수정한 뒤 이 문서의 상태를 `Resolved`로 변경한다.
- `Resolved` 항목의 상세 규칙은 이 문서가 아니라 반영된 원본 문서를 기준으로 읽는다.

## 상태 정의

```text
Open
결정이 필요하다.

Provisional
현재 프로토타입에서 사용하는 임시 결정이다.

Deferred
현재 범위에서 의도적으로 보류한다.

Resolved
결정되어 원본 문서에 반영됐다.
```

# Open

## EVT-001 범용 승급 예약 Travel Turn

Scope: Unity Runtime  
Status: Open  
Required Before: 전체 런 프로토타입의 진행 템포 확정

현재 확정된 내용:

- 한 런에 총 3회의 범용 승급 기회를 예약한다.
- 예약 Turn에 도달하면 `PendingPromotionCount`를 증가시킨다.
- 같은 Location에서 Main Arc 이벤트가 가능하면 Main Arc를 우선한다.
- 발생하지 못한 승급 기회는 다음 Location 도착으로 이월한다.
- 이벤트 생성기는 예약 Turn을 이벤트 조건으로 작성하지 않는다.

미결정 사항:

- 세 승급 기회를 예약할 정확한 Travel Turn
- 플레이테스트 결과에 따른 Turn 조정 기준

영향받는 문서 및 시스템:

- `Docs/plan.md`
- `Docs/EventSystem.md`
- Unity 런 설정

## EVT-002 NoProgressPossible 종료 분류

Scope: Game Design / Unity Runtime  
Status: Open  
Required Before: 결과 화면, 통계와 저장 기록 구현

현재 확정된 내용:

- 정상 성공이나 전투 패배와 다른 진행 불능 안전장치다.
- 정규 후일담을 조립하지 않는다.
- 현재 Location의 후보가 0개라는 이유만으로 발동하지 않는다.

미결정 사항:

- 실패로 기록할지 여부
- 별도의 무효 런 또는 시간 초과 결과로 기록할지 여부
- 결과 화면에 사용할 최종 명칭

영향받는 문서 및 시스템:

- `Docs/plan.md`
- `Docs/EventSystem.md`
- Unity 결과 화면과 통계

## EVT-003 RunState 초기값

Scope: Game Design / Unity Runtime  
Status: Open  
Required Before: 이벤트가 포함된 전체 런 프로토타입 시작

현재 확정된 내용:

- 런 시작 시 Fame, Reputation, Money와 Tags를 초기화한다.
- 초기값은 이벤트 생성기가 결정하지 않는다.

미결정 사항:

- 초기 Fame
- 초기 Reputation
- 초기 Money
- 런 시작 시 기본으로 보유할 Tags

영향받는 문서 및 시스템:

- `Docs/EventSystem.md`
- Unity 런 설정

## GAME-001 최대 파티 이후 영입 처리

Scope: Game Design  
Status: Open  
Required Before: 원정 중 반복 주점과 추가 영입 구현

현재 확정된 내용:

- 파티 최대 인원은 5명이다.
- 현재 기획에서는 벤치를 사용하지 않는다.

미결정 사항:

- 파티가 가득 찬 상태에서 새로운 용병 영입을 허용할지 여부
- 허용한다면 기존 용병 교체, 해고 또는 영입 거절 중 어떤 방식을 사용할지

영향받는 문서 및 시스템:

- `Docs/plan.md`
- 주점과 파티 관리 UI

# Provisional

## GROW-002 승급 대상 선택 방식

Scope: Game Design / Unity Runtime  
Status: Provisional  
Review After: 승급 이벤트 플레이테스트

현재 프로토타입 규칙:

- 승급 이벤트가 발생하면 현재 한 단계 승급 가능한 용병을 표시한다.
- 플레이어가 그중 한 명을 직접 선택한다.
- Result를 선택한 뒤 승급 대상 선택은 취소할 수 없다.

검토할 사항:

- 대상 선택 과정이 이벤트 흐름을 지나치게 끊는지
- 자동 추천 또는 사전 선택이 필요한지

영향받는 문서 및 시스템:

- `Docs/plan.md`
- `Docs/EventSystem.md`
- 승급 대상 선택 UI

## EVT-004 Fame과 Reputation의 기능적 비중

Scope: Game Design / Event Content  
Status: Provisional  
Review After: 첫 전체 런 이벤트 플레이테스트

현재 프로토타입 규칙:

- Fame은 용병단이 세상에 알려진 정도를 나타낸다.
- Reputation은 용병단의 전반적인 사회적 평가를 나타낸다.
- 두 값은 이벤트 조건, 결과와 후일담의 보조 서사 State로 사용한다.
- 세력별 평판과 NPC별 호감도는 지원하지 않는다.

검토할 사항:

- 두 수치가 실제 선택에 충분한 차이를 만드는지
- 후일담 전용으로 축소할지
- 둘 중 하나를 제거하거나 다른 State로 대체할지

영향받는 문서 및 시스템:

- `Docs/EventSystem.md`
- 이벤트 생성 파이프라인
- 후일담 조립

# Deferred

## GROW-001 아이템과 축복 상세 규칙

Scope: Game Design  
Status: Deferred  
Resume After: 승급 중심 성장과 오토배틀 재미 검증

현재 확정된 내용:

- 용병단의 성장 축은 승급, 아이템과 축복으로 구성할 수 있다.
- 현재 프로토타입에서는 승급을 먼저 구현한다.

보류한 내용:

- 아이템 획득, 장착과 교체 규칙
- 축복 획득과 중복 규칙
- 이벤트 Result와 두 시스템의 연결 방식

## EVT-005 승급 대상의 연대기 기록

Scope: Unity Runtime / Chronicle  
Status: Deferred  
Resume Before: 개별 용병의 성장 기록을 정규 후일담에 반영

현재 상태:

- 정적 이벤트는 Event ID, Choice ID와 Result를 기록한다.
- 승급 대상 용병과 승급 전후 티어를 정규 연대기에 포함하는 형식은 정의하지 않았다.

검토할 내용:

- `PromotionTargetMercenaryId`
- 승급 전 티어와 승급 후 티어
- T4 도달을 후일담에 반영할지 여부

## ECO-001 추가 골드 사용처

Scope: Game Design  
Status: Deferred  
Resume After: 주점 영입과 기본 런 경제 플레이테스트

현재 확정된 내용:

- 골드의 확정된 사용처는 주점의 용병 영입이다.

보류한 내용:

- 파티 완성 이후의 골드 사용처
- 이벤트 선택지 비용 외의 소비처
- 상점 또는 기타 관리 기능 도입 여부

# Resolved

## EVT-006 프로토타입 승급 범위

Scope: Game Design / Event Content  
Status: Resolved  
Applied To: `Docs/plan.md`, `Docs/EventSystem.md`

- 현재 프로토타입의 모든 Promotion Result는 범용 승급이다.
- 직업, 계열, 티어 또는 개별 용병을 제한하는 승급 이벤트는 지원하지 않는다.

## EVT-007 범용 승급 보장 방식

Scope: Unity Runtime  
Status: Resolved  
Applied To: `Docs/plan.md`, `Docs/EventSystem.md`

- 서로 다른 범용 승급 이벤트 3개를 보장용으로 지정한다.
- 설정된 Travel Turn마다 승급 기회를 예약한다.
- Main Arc 이벤트가 겹치면 Main Arc를 우선하고 승급 기회는 이월한다.
- 정확한 Travel Turn 값만 `EVT-001`로 남긴다.

## EVT-008 Main Arc Introduction 안내

Scope: Game Design / Unity Runtime  
Status: Resolved  
Applied To: `Docs/plan.md`, `Docs/EventSystem.md`

- Opening 종료 후 Main Arc Introduction이 발생 가능한 Location 하나를 안내한다.
- 이동과 Introduction 발생 자체는 강제하지 않는다.

## EVT-009 동적 이벤트 후보 구성

Scope: Unity Runtime  
Status: Resolved  
Applied To: `Docs/plan.md`, `Docs/EventSystem.md`

- 런 시작 시 선택한 전체 콘텐츠는 `RunEventPool`로 유지한다.
- 이벤트 결과 적용 후 현재 State 기준 사용 가능 풀을 다시 계산한다.
- Location 도착 시 실제 실행 가능한 이벤트만 `CurrentCandidatePool`로 구성한다.
- 이미 완료한 Event ID는 다시 후보가 되지 않는다.
- 현재 Location의 후보가 0개인 것은 정상이며 바로 이동한다.
