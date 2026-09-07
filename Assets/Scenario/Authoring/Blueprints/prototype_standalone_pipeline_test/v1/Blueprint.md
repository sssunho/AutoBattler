# Prototype Standalone Pipeline Test — EventBlueprint v1

## 참조와 상태

- Request: `prototype_standalone_pipeline_test` Version 1 — `StandaloneSet`
- Plan: `prototype_standalone_pipeline_test_plan` Version 1 — `Approved`
- Blueprint: `prototype_standalone_pipeline_test_blueprint` Version 1
- Balance Profile: `prototype_event_balance` Version 1 — `Approved`
- 상태: `Approved`
- Event 수: 2
- Arc 및 Epilogue 수: 0

Interactive 흐름에 따른 사용자 승인으로 최종 Event JSON 생성 입력이 확정됐다.

## Event Blueprint 요약

### riverside_lost_payroll

- Location: 3 — RiversideTown
- Event Condition:
  - Fame 최대 경계: `Fame.High`
  - Reputation 범위: `Reputation.Low`부터 `Reputation.High`
- `return_payroll`
  - 조건 없음
  - Fame 증가: `Fame.Minor`
  - Reputation 증가: `Reputation.Minor`
  - 작성 방향: 임금을 온전히 반환하고 목격자들이 이름과 책임감 있는 행동을 각각 전하고 평가하게 한다.
- `take_payroll`
  - 조건 없음
  - Money 획득: `Money.StandardReward`
  - Reputation 감소: `Reputation.Minor`
  - 작성 방향: 회수 대가 명목으로 임금 일부를 챙기고 현장의 인부와 상인들이 이를 탐욕스럽게 평가하게 한다.

Fame과 Reputation 조건은 결과 변화가 Profile의 State Domain을 벗어나지 않도록 보수적인 의미적 경계를 제공한다. 허용된 Threshold Key의 간격 때문에 실제 후보 범위는 수학적으로 가능한 최대 범위보다 좁을 수 있다.

### crossroad_injured_courier

- Location: 14 — CrossroadInn
- Event Condition: 없음
- `pay_treatment_cost`
  - Money 조건: `Money.MinorCost`
  - Money 지출: `Money.MinorCost`
  - 작성 방향: 지출 사실을 숨기지 않고 치료사를 불러 전문 처치를 마련한다.
- `offer_basic_aid`
  - 조건 없음
  - State 변화 없음
  - 작성 방향: 휴대품으로 위급한 상처만 안정시키며 전문 치료와 동일한 결과로 과장하지 않는다.

유료 Choice의 조건과 지출 Magnitude가 같은 Key를 사용한다. Money가 부족해 유료 Choice가 비활성화되더라도 무료 기본 Choice는 항상 선택 가능하다.

## Writing Guidance 원칙

- 최종 제목, 설명, Choice 또는 Result 문장은 아직 작성하지 않는다.
- 5명으로 이루어진 용병단 전체를 행동 주체로 삼고 특정 용병을 고정하지 않는다.
- 첫 사건은 공개된 도덕적 선택을 다루되 용병단을 선택 전에 영웅이나 도둑으로 규정하지 않는다.
- 두 번째 사건은 작은 자원 선택에 집중하며 운송인을 후속 의뢰나 중요 인물로 확장하지 않는다.
- 실제 Effects에 없는 보상, Tag, 관계나 장기 결과를 문장에 추가하지 않는다.
- 정확한 밸런스 수치는 최종 Event 생성 단계에서만 Profile로 변환한다.

## requiredBalanceKeys

| Key | Usage | Consumer |
|---|---|---|
| `Fame.High` | Threshold | `riverside_lost_payroll` |
| `Reputation.Low` | Threshold | `riverside_lost_payroll` |
| `Reputation.High` | Threshold | `riverside_lost_payroll` |
| `Money.MinorCost` | Threshold | `crossroad_injured_courier/pay_treatment_cost` |
| `Fame.Minor` | Magnitude | `riverside_lost_payroll/return_payroll` |
| `Reputation.Minor` | Magnitude | `return_payroll`, `take_payroll` |
| `Money.StandardReward` | Magnitude | `riverside_lost_payroll/take_payroll` |
| `Money.MinorCost` | Magnitude | `crossroad_injured_courier/pay_treatment_cost` |

연결된 Balance Profile에서 모든 Key의 존재와 Domain을 확인했다.

## Battle과 Promotion

- 모든 Result의 `battle`과 `promotion`은 `false`다.
- `manualBattleBindings`는 비어 있다.
- 전투 및 승급 관련 수동 작업은 없다.

## Risk

- Warning — `EVT-004`: Fame과 Reputation의 기능적 비중은 전체 런 플레이테스트 이후 변경될 수 있다.
- Warning — `EVT-003`: RunState 초기값이 미확정이므로 첫 사건이 런 시작부터 후보가 된다고 보장할 수 없다.
- Warning — Threshold granularity: 의미적 Key만으로 범위 안전성을 표현하므로 첫 사건의 실제 후보 범위가 보수적으로 좁아질 수 있다.

Blocking Risk는 없다.

## Validation

다음 검사를 모두 통과했다.

- 승인된 Request와 Plan 참조 일치
- 두 Event 슬롯의 일대일 변환과 Scope 보존
- Location, Choice 수, Effect 방향, battle, promotion 및 Transition 구조 보존
- Choice ID 고유성과 sourcePlanKey 대응
- Event 및 Choice Condition 구조 유효성
- 조건 없는 무료 기본 Choice 존재
- Threshold 및 Magnitude Key 허용 목록·Domain·Direction 일치
- Balance Profile의 requiredBalanceKeys 완전성
- Money 비용 조건과 지출 Magnitude 호환성
- 정확한 밸런스 숫자 부재
- 모든 Event, Choice와 Result의 Writing Guidance 완전성
- Runtime 데이터 미변경

## 다음 단계 가능 여부

- Blueprint 승인 가능: 가능
- 수치가 완결된 Event JSON 생성: 가능
- Unity 배포 가능: 이번 요청의 범위가 아니며 Runtime 통합도 금지됨

## 저장 경로

- 구조 데이터: `Authoring/Blueprints/prototype_standalone_pipeline_test/v1/Blueprint.json`
- 검토 문서: `Authoring/Blueprints/prototype_standalone_pipeline_test/v1/Blueprint.md`
