# Prototype Standalone Pipeline Test — Generation Report

## 입력 참조

- Request: `prototype_standalone_pipeline_test` Version 1 — `Approved`
- Plan: `prototype_standalone_pipeline_test_plan` Version 1 — `Approved`
- Blueprint: `prototype_standalone_pipeline_test_blueprint` Version 1 — `Approved`
- Balance Profile: `prototype_event_balance` Version 1 — `Approved`

동일한 승인 Version만 사용해 생성했으며 서로 다른 Version의 산출물을 혼합하지 않았다.

## 생성 결과

- 출력 수준: `RuntimeReady`
- 생성 Event: 2
- 생성 ArcCatalog 항목: 0
- 생성 Epilogue Fragment: 0
- Pending Battle Binding: 0
- Runtime 통합: 수행하지 않음

`RuntimeReady`는 생성된 Event 배열 자체가 Runtime Schema와 배포 전 검증 조건을 만족한다는 뜻이다. 사용자 요청에 따라 실제 Runtime 경로에는 복사하거나 병합하지 않았다.

## 생성 Event

| Event ID | Location | Choice | 핵심 변화 |
|---|---|---:|---|
| `riverside_lost_payroll` | 3 — RiversideTown | 2 | 반환 시 Fame·Reputation 증가, 착복 시 Money 증가·Reputation 감소 |
| `crossroad_injured_courier` | 14 — CrossroadInn | 2 | 치료비 지출 또는 조건 없는 무료 응급처치 |

## Balance 변환

| Key | Usage | 적용값 |
|---|---|---:|
| `Fame.High` | Threshold | 8 |
| `Reputation.Low` | Threshold | -1 |
| `Reputation.High` | Threshold | 2 |
| `Money.MinorCost` | Threshold | 40 |
| `Fame.Minor` | Magnitude | 1 |
| `Reputation.Minor` | Magnitude | 1 |
| `Money.StandardReward` | Magnitude | 15 |
| `Money.MinorCost` | Magnitude | 40 |

최종 구조는 다음과 같이 변환됐다.

- `riverside_lost_payroll` 발생 범위: Fame 최대 8, Reputation -1부터 2
- `return_payroll`: Fame +1, Reputation +1
- `take_payroll`: Reputation -1, Money +15
- `pay_treatment_cost`: Money가 40 이상일 때 선택 가능, Money -40
- `offer_basic_aid`: 조건 및 State 변화 없음

## 범위 안전성

- 반환 결과의 Fame은 발생 가능한 최대 상태에서도 상한을 넘지 않는다.
- 반환 결과의 Reputation은 발생 가능한 최대 상태에서도 상한을 넘지 않는다.
- 착복 결과의 Reputation은 발생 가능한 최소 상태에서도 하한을 넘지 않는다.
- 유료 치료는 지출액 이상의 Money를 요구하므로 적용 후 음수가 되지 않는다.
- 유료 치료가 비활성화돼도 무료 응급처치가 항상 남는다.

## 문장 생성

- Event 설명은 선택 이전 상황과 갈등만 제시하고 결과를 미리 확정하지 않는다.
- 유료 Choice에는 Profile에서 해석한 정확한 비용인 40 골드를 표시한다.
- Result 문장은 실제 수치 변화의 원인을 드러내되 내부 ID, Key나 State 필드명을 노출하지 않는다.
- 5명 용병단 전체를 주체로 삼고 특정 용병의 이름, 직업, 성격이나 인연을 만들지 않았다.
- Blueprint에 없는 Tag, 보상, 후속 의뢰나 장기 관계를 추가하지 않았다.

## Risk와 미결정 사항

- Warning — `EVT-004`: Fame과 Reputation의 기능적 비중은 전체 런 플레이테스트 이후 변경될 수 있다.
- Note — `EVT-003`: RunState 초기값이 미확정이므로 첫 Event가 런 시작부터 후보가 된다고 보장하지 않는다.
- Warning — Threshold granularity: 첫 Event의 범위는 안전하지만 허용된 의미적 Key 간격 때문에 이론상 최대 후보 범위보다 보수적이다.
- Note — Runtime integration excluded: 요청에 따라 Authoring 경로에만 저장했다.

Blocking Risk는 없다.

## Validation

다음 검사를 통과했다.

- 승인 입력 ID와 Version 일치
- EventSystem Choice Event Schema
- Blueprint와 Event·Choice 수 및 구조 일치
- 모든 Balance Key의 Profile 값 변환
- Money 조건과 지출액 일치
- Fame, Reputation과 Money 범위 안전성
- 모든 발생 가능 State에서 최소 하나의 Choice 선택 가능
- Event 및 Choice ID 고유성
- Location ID 유효성
- Standalone Arc 및 Run Transition 규칙
- 전투와 승급 제외 및 Battle Binding 불필요
- Writing Guidance와 forbidden claim 준수
- 기존 Runtime Event ID 비충돌

Runtime 통합 검증은 요청 범위가 아니므로 수행하지 않았다.

## 생성 파일

- `Authoring/Generated/prototype_standalone_pipeline_test/v1/Events.json`
- `Authoring/Generated/prototype_standalone_pipeline_test/v1/ArcCatalogEntries.json`
- `Authoring/Generated/prototype_standalone_pipeline_test/v1/EpilogueFragments.json`
- `Authoring/Generated/prototype_standalone_pipeline_test/v1/GenerationReport.json`
- `Authoring/Generated/prototype_standalone_pipeline_test/v1/GenerationReport.md`
