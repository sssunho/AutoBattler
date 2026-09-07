# Prototype Standalone Pipeline Test — EventContentPlan v1

## 요청과 상태

- Request: `prototype_standalone_pipeline_test` Version 1
- Scope: `StandaloneSet`
- 요청/계획 Event 수: 2 / 2
- Plan: `prototype_standalone_pipeline_test_plan` Version 1
- 상태: `Approved`
- 진행 방식: Interactive — 사용자 승인에 따라 Blueprint 생성 입력으로 확정했다.

## 콘텐츠 방향

서로 독립된 두 Standalone Event로 파이프라인의 수치 변환과 선택 가능성 검증에 집중한다.

- 첫 사건은 공개적인 선행과 목격된 개인적 이익을 대비해 Fame, Reputation과 Money의 서로 다른 변화 방향을 검증한다.
- 두 번째 사건은 작은 비용을 지불할 수 있을 때만 활성화되는 선택과 조건 없이 항상 가능한 무료 선택을 함께 둔다.
- 두 사건 모두 Tag, 전투, 승급, Arc 전환과 Run 전환을 사용하지 않는다.
- 정확한 조건 경계와 변화량은 이 단계에서 정하지 않는다.

## Event 슬롯

| ID | Location | 사건 기능 | 핵심 검증 |
|---|---|---|---|
| `riverside_lost_payroll` | 3 — RiversideTown | Independent | 선행의 Fame·Reputation 증가와 개인적 이익의 Money 증가·Reputation 감소 |
| `crossroad_injured_courier` | 14 — CrossroadInn | Independent | 작은 비용 조건과 조건 없는 무료 기본 선택 |

### riverside_lost_payroll

RiversideTown의 붐비는 하역장에서 사고로 사라진 노동자 임금 상자를 용병단이 발견한다. 주변 상인과 인부들이 발견 사실을 목격했기 때문에 반환과 착복이 공개적인 사회적 평가로 이어진다.

- `return_payroll`: 상자를 온전히 돌려준다. Fame과 Reputation이 증가하는 방향이며 Money 변화는 없다.
- `take_payroll`: 회수 대가라는 명목으로 돈을 챙긴다. Money를 얻고 Reputation이 감소하는 방향이며 Fame 변화는 없다.
- Event 범위 조건 의도: 두 결과의 Fame 및 Reputation 변화가 허용 범위를 벗어나지 않는 상태에서만 후보가 된다.
- 두 Choice에는 별도 조건이 없다.

### crossroad_injured_courier

CrossroadInn에 부상 운송인이 쓰러져 도착하고, 여관 주인이 인근 치료사를 부를 비용을 누가 부담할지 묻는다.

- `pay_treatment_cost`: 작은 비용을 낼 수 있을 때 치료사를 즉시 부른다. Money를 소비한다.
- `offer_basic_aid`: 용병단의 휴대품으로 기본 응급처치만 한다. 조건과 비용, 수치 변화가 없다.
- 유료 선택이 비활성화되어도 무료 선택이 항상 남으므로 Event가 해결 불가능해지지 않는다.

## State Network

| State | 생산/변화 | 소비/검증 |
|---|---|---|
| Fame | `riverside_lost_payroll/return_payroll` | 공개적인 선행의 인지도 증가 |
| Reputation | `return_payroll`, `take_payroll` | 책임감과 탐욕이 반대 방향의 평가를 만드는지 검증 |
| Money | `riverside_lost_payroll/take_payroll` | `crossroad_injured_courier/pay_treatment_cost`에서 지불 가능 조건과 소비 검증 |

새로운 Tag 제안은 없으며 기존 Tag도 사용하지 않는다. 두 사건의 State 관계는 파이프라인 기능 검증을 위한 것이며 사건 사이의 직접적인 서사 연결이나 발생 순서를 뜻하지 않는다.

## Requirement Coverage

| 요구사항 | 상태 | 반영 내용 |
|---|---|---|
| Standalone Event 2개 | Covered | 독립 Normal Event 두 슬롯 |
| Interactive 승인 | Covered | `ReadyForReview`에서 중단 |
| 선행 대 개인적 이익 | Covered | 반환과 착복 Choice |
| Fame/Reputation/Money 변환 | Covered | 반환: Fame·Reputation 증가, 착복: Money 증가·Reputation 감소 |
| 작은 비용과 무료 기본 선택 | Covered | 유료 치료와 무조건 가능한 무료 응급처치 |
| 전투와 승급 제외 | Covered | 모든 Result가 `false` |
| 기존 Tag 또는 Tag 없음 | Covered | Tag를 전혀 사용하지 않음 |
| `prototype_event_balance` v1 | Covered | 승인 상태 확인, 최종 생성에서 수치 해석 예정 |
| Runtime 미통합 | Covered | 요청별 Authoring 경로에만 계획 저장 |

## Risk와 미결정 사항

- Warning — `EVT-004`: Fame과 Reputation의 기능적 비중은 프로토타입 규칙이며 전체 런 플레이테스트 뒤 바뀔 수 있다.
- Warning — `EVT-003`: RunState 초기값이 아직 확정되지 않아 `riverside_lost_payroll`이 런 시작 상태부터 범위 조건을 만족한다고 보장할 수 없다. 이는 계획 승인을 막지 않지만 실제 파이프라인 테스트 상태를 선택할 때 확인해야 한다.
- Note: 정확한 수치 경계와 변화량은 Blueprint의 의미적 Key 선택과 승인된 `prototype_event_balance` Version 1을 통해 후속 단계에서 결정한다.

Blocking Risk는 없다.

## Validation

다음 검사를 모두 통과했다.

- 승인된 Request ID, Version과 Scope 일치
- 요청 수량과 슬롯 수 일치
- Location ID 3과 14의 존재 및 사건 전제 적합성
- Standalone Event의 Arc/Run Transition 부재
- Tag Policy 준수
- 전투 및 승급 제외
- 유료 Choice 비활성화 시 조건 없는 무료 Choice 존재
- 요청한 수치 변화 방향과 비용 조건 의도 반영
- 승인된 Balance Profile 존재
- Runtime 데이터 미변경

## 저장 경로

- 구조 데이터: `Authoring/Plans/prototype_standalone_pipeline_test/v1/ContentPlan.json`
- 검토 문서: `Authoring/Plans/prototype_standalone_pipeline_test/v1/ContentPlan.md`

이 계획은 승인됐으며 EventBlueprint 두 개의 상위 입력으로 사용됐다.
