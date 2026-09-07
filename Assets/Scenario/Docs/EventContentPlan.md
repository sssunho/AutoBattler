# Event Content Plan 계약

## 문서 지위

이 문서는 승인된 GenerationRequest를 이벤트 콘텐츠 구조로 확장한 `EventContentPlan`의 공식 계약이다.

`EventContentPlan`은 개발 단계의 제작 데이터이며 Unity 런타임 Event Schema가 아니다.

문서 우선순위는 다음과 같다.

```text
Docs/plan.md의 확정된 게임 규칙
→ Docs/EventSystem.md의 런타임 및 콘텐츠 계약
→ Docs/DesignStatus.md의 결정 상태
→ Docs/GenerationRequest.md와 승인된 GenerationRequest
→ Docs/EventContentPlan.md의 계획 산출물 계약
→ 생성 단계별 책임 문서
→ 개별 계획, 청사진과 생성된 JSON
```

- 콘텐츠 플래너는 이 계약에 맞춰 EventContentPlan을 작성한다.
- 청사진 생성기는 승인된 EventContentPlan을 읽기 전용 구조 입력으로 사용한다.
- EventContentPlan은 승인된 GenerationRequest의 Scope와 제약을 확장하거나 완화할 수 없다.
- EventContentPlan은 `EventSystem.md`에 없는 Event Type, State, Condition 또는 Effect를 제안할 수 없다.
- `DesignStatus.md`의 Open 또는 Deferred 항목을 임의로 결정하지 않는다.
- 요청 범위에 포함되지 않은 콘텐츠 종류를 유용하다는 이유만으로 추가하지 않는다.

---

## 목적

EventContentPlan은 다음 내용을 확정한다.

- 요청한 콘텐츠 단위와 계획된 Event 슬롯
- Main Arc 또는 Sub Arc의 구조
- Opening, Standalone과 Promotion Event의 콘텐츠 방향
- Event가 발생할 Location
- Choice 또는 Automatic Resolution
- 결과별 전투 및 승급 발생 여부
- 결과별 Arc와 RunPhase 전환 방향
- Tags, Fame, Reputation과 Money의 조건 및 변화 의도
- Main Arc Finale과 Sub Arc 후일담의 방향
- 신규 Tag 제안과 생산 및 소비 관계
- 요청 범위에 적용되는 구조 검증 결과

EventContentPlan은 다음 내용을 확정하지 않는다.

- 정확한 Event Conditions 수치
- 정확한 Choice Conditions 수치
- 최종 Choice ID
- Fame, Reputation과 Money의 정확한 변화량
- 조건과 변화량에 사용할 의미적 Threshold 및 Magnitude Key
- 실제 Battle ID, 적 구성, 전장과 난이도
- 승급 대상 용병
- 범용 승급을 예약할 정확한 Travel Turn
- 플레이어에게 표시할 최종 제목, 설명, 선택지와 결과 문장
- 최종 Epilogue Fragment 조건과 문장
- Unity 런타임의 RunEventPool 구성

정확한 밸런스 수치는 콘텐츠 계획이나 청사진이 소유하지 않는다. 청사진은 조건의 의미적 Threshold와 변화량의 의미적 Magnitude를 선택하고, 최종 Event 생성 단계가 별도의 EventBalanceProfile에서 정확한 값을 조회해 적용한다.

---

## 입력

EventContentPlan은 다음 입력을 전제로 한다.

```text
Docs/plan.md
Docs/EventSystem.md
Docs/DesignStatus.md
Docs/GenerationRequest.md
승인된 Authoring/Requests/{requestId}.json
Location 데이터
Facts, Knowledges와 Traits 데이터
기존 Event ID와 Arc ID
```

Location과 Tag 데이터가 없으면 유효한 Location 및 State 참조를 확정할 수 없다. 이 경우 계획을 승인 가능한 상태로 만들지 않고 Risk를 기록한다.

---

## 저장 위치

EventContentPlan은 사람이 검토할 Markdown과 하위 단계가 읽을 JSON을 함께 사용한다.

```text
Authoring/Plans/{requestId}/v{requestVersion}/ContentPlan.md
Authoring/Plans/{requestId}/v{requestVersion}/ContentPlan.json
```

- 두 파일은 같은 계획을 표현해야 한다.
- 구조적 사실은 승인된 `ContentPlan.json`을 기준으로 한다.
- `ContentPlan.md`는 전체 방향, 선택 이유, 분기 설명과 Risk를 사람이 검토하기 위한 문서다.
- 두 파일이 충돌하면 승인할 수 없다.

---

## Plan 상태

```text
Draft
ReadyForReview
Approved
Superseded
```

- `Draft`: 작성 또는 수정 중이다.
- `ReadyForReview`: 계약과 로컬 검증을 통과해 사용자 검토를 기다린다.
- `Approved`: 사용자 또는 승인된 자동 진행 흐름이 계획을 확정했다.
- `Superseded`: 같은 GenerationRequest의 새 Plan Version으로 대체됐다.

청사진 생성기는 `Approved` 상태의 EventContentPlan만 사용할 수 있다.

---

## 기본 Schema

```json
{
  "schemaVersion": 1,
  "planId": "orc_invasion_main_arc_plan",
  "planVersion": 1,
  "status": "Draft",

  "requestRef": {
    "requestId": "orc_invasion_main_arc",
    "requestVersion": 1,
    "scope": "MainArc"
  },

  "scopeSummary": {
    "requestedUnitCount": 1,
    "requestedEventCount": null,
    "plannedUnitIds": ["orc_invasion"],
    "plannedEventCount": 5
  },

  "summary": {
    "premise": "오크 침공의 배후를 추적하고 침공의 결말을 결정한다.",
    "tone": ["dark_fantasy", "heroic"],
    "contentDirection": "용병단이 피해 지역을 지나며 침공의 원인과 대응 방식을 선택한다."
  },

  "arcs": [],
  "slots": [],
  "epiloguePlans": [],
  "runPackagePlan": null,
  "stateNetwork": [],
  "newTagProposals": [],
  "requirementCoverage": [],
  "risks": [],
  "validationSummary": []
}
```

Scope에 필요하지 않은 배열은 빈 배열을, 필요하지 않은 단일 객체는 `null`을 사용한다. 정의되지 않은 임의 필드를 추가하지 않는다.

---

## 공통 필드

### schemaVersion

EventContentPlan Schema의 Version이다.

현재 Version은 `1`이다.

### planId

하나의 EventContentPlan을 식별하는 고유하고 안정적인 문자열 ID다.

다른 계획과 중복될 수 없다.

### planVersion

같은 GenerationRequest Version 안에서 계획이 수정된 횟수를 나타내는 1 이상의 정수다.

- 계획의 Arc 구조, 슬롯, 분기, Location 또는 결과 방향이 바뀌면 증가시킨다.
- GenerationRequest의 Scope나 핵심 제약이 바뀌면 Plan Version만 올리지 않고 GenerationRequest Version을 먼저 올린다.

### status

현재 계획의 승인 상태다.

### requestRef

```json
{
  "requestId": "orc_invasion_main_arc",
  "requestVersion": 1,
  "scope": "MainArc"
}
```

- 승인된 GenerationRequest와 정확히 일치해야 한다.
- EventContentPlan은 `scope`를 변경할 수 없다.
- 하위 산출물은 `requestId`, `requestVersion`, `scope`, `planId`와 `planVersion`을 기록해야 한다.

### scopeSummary

```json
{
  "requestedUnitCount": 1,
  "requestedEventCount": null,
  "plannedUnitIds": ["orc_invasion"],
  "plannedEventCount": 5
}
```

- `requestedUnitCount`: GenerationRequest의 `targetCount`와 같다.
- `requestedEventCount`: GenerationRequest의 같은 필드와 같다.
- `plannedUnitIds`: Scope가 요청한 콘텐츠 단위의 ID 목록이다.
- `plannedEventCount`: `slots`의 실제 수다.

MainArc와 SubArc는 하나의 콘텐츠 단위가 여러 Event 슬롯을 가질 수 있다.

### summary

계획 전체를 사람이 검토하기 위한 압축 설명이다.

- `premise`: 요청 전제를 계획 수준으로 정리한다.
- `tone`: GenerationRequest와 일치해야 한다.
- `contentDirection`: 계획이 어떤 경험과 선택을 만들 것인지 설명한다.

새로운 필수 요구사항을 추가하지 않는다.

---

## Arc Plan Schema

`arcs`는 이번 요청에서 새로 계획하는 Main Arc와 Sub Arc 정의다.

```json
{
  "id": "orc_invasion",
  "name": "잿빛 깃발 아래",
  "arcType": "Main",
  "purpose": "오크 침공의 원인을 추적하고 침공의 결말을 결정한다.",
  "initialStage": "Introduction",
  "stages": [
    "Introduction",
    "Investigation",
    "Escalation",
    "FinaleReady"
  ],
  "introductionSlotIds": ["orc_invasion_intro"],
  "introductionGuidanceIntent": "Opening 종료 후 폐허가 된 변경 마을을 조사 목표로 안내한다.",
  "finaleStage": "FinaleReady",
  "finaleLocation": 20,
  "finaleSlotIds": ["orc_invasion_finale"],
  "branchAxes": [
    {
      "key": "response_to_invasion",
      "decisionSlotIds": ["orc_invasion_response"],
      "options": [
        {
          "key": "defend_settlements",
          "stateId": "OrcWarDefender"
        },
        {
          "key": "hunt_leadership",
          "stateId": "OrcWarHunter"
        }
      ]
    }
  ],
  "finaleCoveragePlan": {
    "coverageDirection": "두 대응 노선을 조건부 선택으로 반영하고 항상 조건 없는 기본 선택을 제공한다.",
    "requiredStateAxes": ["response_to_invasion"]
  }
}
```

### Main Arc 규칙

- `arcType`은 `Main`이다.
- `initialStage`는 유효한 Stage다.
- Introduction 슬롯이 하나 이상 존재한다.
- Introduction은 Opening과 별개이며 시작 Location에서 강제되지 않는다.
- `introductionGuidanceIntent`는 Unity가 안내할 Introduction Location의 의미를 설명한다.
- `finaleStage`, `finaleLocation`과 Finale 슬롯이 존재한다.
- 모든 주요 분기가 `FinaleReady`와 정상 Finale에 도달할 수 있다.
- EmergencyFinale을 계획하지 않는다.
- 불리한 정상 결말도 Main Arc를 `Completed`로 처리하고 구체적인 결과를 Tags로 구분한다.

### Sub Arc 규칙

- `arcType`은 `Sub`다.
- `initialStage`와 Introduction 슬롯이 존재한다.
- `finaleStage`, `finaleLocation`, `finaleSlotIds`와 `finaleCoveragePlan`은 `null` 또는 빈 배열을 사용한다.
- 긍정적·부정적 종결 모두 `Completed`를 사용한다.
- `Abandoned`를 Event Result로 계획하지 않는다.
- Main Arc Finale 진입까지 완료되지 않은 경우를 위한 `Abandoned` 후일담 방향을 계획할 수 있다.

### 기존 Arc를 대상으로 한 SingleEvent

기존 Arc의 Event 하나만 계획한다면 새 Arc 정의를 `arcs`에 복제하지 않는다.

- GenerationRequest의 `arcContext`를 사용한다.
- Risk에 전체 Arc 구조를 검증하지 못했다는 사실을 기록한다.
- 기존 Arc의 Stage나 목적을 임의로 변경하지 않는다.

---

## Event Slot Schema

```json
{
  "id": "orc_scout_encounter",
  "arcId": "orc_invasion",
  "arcType": "Main",
  "eventType": "Normal",
  "arcStage": "Investigation",
  "narrativeFunction": "Escalation",
  "systemRoles": [],
  "premise": "불타버린 농장에서 침공 경로를 정찰하던 오크 부대를 발견한다.",
  "locations": [4],

  "eventConditionIntents": {
    "requiredTags": [],
    "forbiddenTags": [],
    "fame": null,
    "reputation": null,
    "money": null,
    "resolvedEventCount": null
  },

  "resolutionPlan": {
    "type": "Choice",
    "choices": [
      {
        "planKey": "attack_scouts",
        "intent": "정찰대를 공격한다.",
        "choiceConditionIntents": {
          "requiredTags": [],
          "forbiddenTags": [],
          "money": null
        },
        "resultPlan": {
          "outcomeDirection": "전투에서 승리하고 침공 경로를 알아낸다.",
          "battle": true,
          "promotion": false,
          "effectIntents": {
            "addTags": ["OrcRouteDiscovered"],
            "removeTags": [],
            "fame": "Increase",
            "reputation": "None",
            "money": "None"
          },
          "arcTransitionPlan": {
            "stage": "Escalation",
            "status": "Active"
          },
          "runTransitionPlan": null
        }
      }
    ]
  },

  "endingContribution": null
}
```

---

## Event Slot 공통 필드

### id

계획부터 청사진과 최종 Event까지 유지되는 고유 Event ID다.

- 기존 Event ID와 중복될 수 없다.
- 다음 또는 이전 Event ID를 의미하지 않는다.
- Event 연결은 Arc Stage와 State를 사용한다.

### arcId와 arcType

`arcType`은 다음 중 하나다.

```text
Main
Sub
Standalone
```

- Main과 Sub는 새로 계획한 `arcs` 또는 GenerationRequest의 기존 `arcContext`를 참조한다.
- Standalone은 `arcId`와 `arcStage`가 `null`이다.

### eventType

```text
Opening
Introduction
Normal
Finale
```

- `Opening`: 특수 Standalone Event에만 사용한다.
- `Introduction`: Main 또는 Sub Arc 도입에 사용한다.
- `Normal`: Arc 진행 또는 일반 Standalone에 사용한다.
- `Finale`: Main Arc에만 사용한다.
- EmergencyFinale은 지원하지 않는다.

### arcStage

Arc Event가 요구하는 현재 Stage다.

Stage는 다음 Event ID가 아니라 의미 있는 진행 상태다.

### narrativeFunction

제작 단계에서 슬롯이 담당하는 서사적 기능을 설명하는 문자열이다.

권장 값은 다음과 같다.

```text
Introduction
Development
Escalation
TurningPoint
Resolution
Independent
Growth
```

런타임 필드가 아니며 `eventType`, `arcStage` 또는 전환을 대신하지 않는다.

### systemRoles

현재 허용 값은 다음과 같다.

```text
GuaranteedPromotionCandidate
```

- 보장용 범용 승급 후보가 아니면 빈 배열을 사용한다.
- Authoring 전용 분류이며 최종 Event JSON 필드가 아니다.
- 해당 역할을 가진 슬롯은 EventSystem의 보장용 승급 후보 조건을 모두 만족해야 한다.

### premise

사건의 압축된 전제다.

Location, 갈등, 선택 또는 자동 결과의 의미를 판단할 수 있어야 한다. 최종 표시 문장을 작성하지 않는다.

### locations

Event가 발생할 수 있는 정확한 Location ID 목록이다.

- 모든 ID는 Location 데이터에 존재해야 한다.
- 빈 배열을 사용할 수 없다.
- 여러 장소에서 동일하게 성립하는 사건만 여러 ID를 사용한다.
- 모든 Location에 전용 Event를 강제하지 않는다.

---

## Condition Intents

EventContentPlan은 조건의 의미를 계획하지만 정확한 수치 경계는 정하지 않는다.

### eventConditionIntents

```json
{
  "requiredTags": [],
  "forbiddenTags": [],
  "fame": null,
  "reputation": null,
  "money": null,
  "resolvedEventCount": null
}
```

- `requiredTags`, `forbiddenTags`: 기존 또는 승인 대기 중인 신규 Tag ID다.
- `fame`: `높은 Fame에서만`과 같은 조건 의도 또는 `null`이다.
- `reputation`: `부정적인 Reputation에서만`과 같은 조건 의도 또는 `null`이다.
- `money`: `의뢰 비용을 낼 수 있을 때`와 같은 조건 의도 또는 `null`이다.
- `resolvedEventCount`: `충분한 일반 사건 이후`와 같은 진행량 의도 또는 `null`이다.

청사진 단계는 조건을 `min` 또는 `max` 중 어느 경계로 표현할지와 의미적 Threshold Key를 결정한다. 정확한 `Min`과 `Max` 숫자는 EventBalanceProfile이 소유하며 최종 Event 생성 단계에서 변환한다.

### choiceConditionIntents

```json
{
  "requiredTags": [],
  "forbiddenTags": [],
  "money": null
}
```

EventSystem이 지원하는 Choice 조건만 계획한다.

- Fame, Reputation, Location 또는 ResolvedEventCount를 Choice 조건으로 계획하지 않는다.
- Choice Event가 발생 가능한 모든 State에는 선택 가능한 방향이 최소 하나 있어야 한다.

---

## Resolution Plan

`resolutionPlan.type`은 다음 중 하나다.

```text
Choice
Automatic
```

확률형 Resolution은 지원하지 않는다.

### Choice

```json
{
  "type": "Choice",
  "choices": [
    {
      "planKey": "accept_contract",
      "intent": "의뢰를 받아들인다.",
      "choiceConditionIntents": {
        "requiredTags": [],
        "forbiddenTags": [],
        "money": null
      },
      "resultPlan": {}
    }
  ]
}
```

- `choices`는 하나 이상이어야 한다.
- `planKey`는 같은 슬롯 안에서 고유한 Authoring 참조 Key다.
- `planKey`는 최종 Choice ID가 아니다.
- `intent`는 플레이어 행동의 의미이며 최종 선택지 문장이 아니다.
- 서로 다른 Choice가 사실상 같은 결과가 되지 않아야 한다.

### Automatic

```json
{
  "type": "Automatic",
  "resultPlan": {
    "outcomeDirection": "용병단이 습격에 휘말려 전투에 돌입한다.",
    "battle": true,
    "promotion": false,
    "effectIntents": {
      "addTags": [],
      "removeTags": [],
      "fame": "None",
      "reputation": "None",
      "money": "None"
    },
    "arcTransitionPlan": null,
    "runTransitionPlan": null
  }
}
```

- Automatic은 Choice 방향을 갖지 않는다.
- 최종 런타임 기록에서 사용하는 예약 Choice ID `auto`는 청사진 및 최종 변환 단계가 처리한다.

---

## Result Plan

```json
{
  "outcomeDirection": "전투에서 승리하고 침공 경로를 알아낸다.",
  "battle": true,
  "promotion": false,
  "effectIntents": {
    "addTags": ["OrcRouteDiscovered"],
    "removeTags": [],
    "fame": "Increase",
    "reputation": "None",
    "money": "None"
  },
  "arcTransitionPlan": {
    "stage": "Escalation",
    "status": "Active"
  },
  "runTransitionPlan": null
}
```

### outcomeDirection

전투 승리 이후를 포함해 Result가 실제로 만드는 결과의 의미다.

최종 결과 문장을 작성하지 않는다.

### battle

정확한 boolean 구조 결정이다.

- `true`면 최종 Event에 수동 Battle Binding이 필요하다.
- 실제 Battle ID, 적 구성, 전장과 난이도는 계획하지 않는다.
- 하나의 Result에는 전투를 최대 한 번만 계획한다.
- 전투 패배 이후 결과나 분기를 계획하지 않는다.

### promotion

정확한 boolean 구조 결정이다.

- 현재 프로토타입의 모든 Promotion은 범용 승급이다.
- 특정 직업, 계열, 티어 또는 개별 용병을 전제로 하지 않는다.
- `battle`과 동시에 `true`일 수 있다.
- 전투가 함께 있으면 승리한 뒤 승급을 제공한다.

### effectIntents

```json
{
  "addTags": [],
  "removeTags": [],
  "fame": "None",
  "reputation": "None",
  "money": "None"
}
```

수치 Intent는 다음 값을 사용한다.

```text
fame: None | Increase | Decrease
reputation: None | Increase | Decrease
money: None | Gain | Spend
```

- 청사진 단계는 변화 방향을 보존하고 의미적 Magnitude Key와 변화 이유를 정한다.
- 정확한 수치는 EventBalanceProfile이 소유하며 최종 Event 생성 단계에서 기계적으로 변환한다.
- 결과 설명으로 정당화할 수 없는 수치 변화를 계획하지 않는다.
- 같은 Result에서 같은 Tag를 추가하고 제거하지 않는다.
- Fact를 제거하려면 해당 사실이 더 이상 참이 아닌 이유가 명확해야 한다.

### arcTransitionPlan

```json
{
  "stage": "Escalation",
  "status": "Active"
}
```

- Arc State를 바꾸지 않으면 `null`이다.
- 현재 슬롯이 속한 Arc만 변경할 수 있다.
- Standalone은 항상 `null`이다.
- Main Arc는 `Active` 또는 `Completed`만 사용한다.
- Sub Arc는 `Active` 또는 `Completed`만 사용한다.
- 부정적인 종결도 `Completed`를 사용하고 결과 Tag로 구분한다.
- `Abandoned`는 계획하지 않는다. Main Arc Finale 실제 진입 시 Unity가 적용한다.

### runTransitionPlan

```text
null
Normal
FinaleReady
Ended
```

- Opening의 모든 Result는 `Normal`이다.
- Main Arc 진행 Result만 `FinaleReady`를 사용할 수 있다.
- Main Arc Finale의 모든 Result는 `Ended`다.
- 일반 Main Arc, Main Arc Introduction, Sub Arc와 Standalone은 `null`이다.

---

## Event 조합 규칙

| arcType | eventType | 허용 여부 | 핵심 규칙 |
|---|---|---:|---|
| Standalone | Opening | 허용 | Arc 필드 null, 모든 Result가 Normal 전환 |
| Standalone | Normal | 허용 | Arc 및 Run 전환 없음 |
| Main | Introduction | 허용 | Opening과 분리, Run 전환 없음 |
| Main | Normal | 허용 | Active 진행 또는 FinaleReady 전환 |
| Main | Finale | 허용 | Completed 및 Ended 전환 |
| Sub | Introduction | 허용 | Pending에서 Active 또는 Completed로 전환 |
| Sub | Normal | 허용 | Active 유지 또는 Completed 종결 |
| Standalone | Introduction | 금지 | Arc 도입이 아님 |
| Standalone | Finale | 금지 | Main Arc 전용 |
| Sub | Opening | 금지 | Opening은 특수 Standalone |
| Sub | Finale | 금지 | Main Arc 전용 |
| Main | Opening | 금지 | Opening은 Main Arc와 분리 |

---

## 보장용 범용 승급 후보

`systemRoles`에 `GuaranteedPromotionCandidate`가 있는 슬롯은 다음 조건을 모두 만족한다.

- `arcType`은 `Standalone`이다.
- `eventType`은 `Normal`이다.
- `arcId`와 `arcStage`는 `null`이다.
- Choice와 Automatic을 포함한 모든 Result의 `promotion`이 `true`다.
- 모든 Result의 `arcTransitionPlan`과 `runTransitionPlan`이 `null`이다.
- Event 및 Choice Condition Intent에서 Tag, Fame, Reputation, Money와 ResolvedEventCount 조건을 사용하지 않는다.
- 모든 Normal 이동 Location을 `locations`에 포함한다.
- 특정 용병, 직업, 계열 또는 티어를 전제로 하지 않는다.

GenerationRequest가 `PromotionSet + GuaranteedCandidate`이거나 `RunPackage`에서 부족한 보장 후보 생성을 명시한 경우에만 이 역할의 슬롯을 의무적으로 계획한다.

다른 Scope의 요청에 보장용 승급 슬롯을 자동으로 추가하지 않는다.

---

## Epilogue Plan

```json
{
  "key": "orc_villages_saved",
  "category": "SubArc",
  "arcId": "orc_refugees",
  "arcStatus": "Completed",
  "conditionIntent": {
    "requiredTags": ["RefugeesRescued"],
    "forbiddenTags": [],
    "fame": null,
    "reputation": null
  },
  "textDirection": "피난민들이 살아남아 변경 마을을 다시 세웠다는 사실을 남긴다."
}
```

### category

```text
Legacy
SubArc
Additional
```

- `Legacy`: Fame과 Reputation에 따른 용병단 전체 평가 방향이다.
- `SubArc`: 지정 Sub Arc의 `Completed` 또는 `Abandoned` 결과 방향이다.
- `Additional`: 주요 Tag 결과를 보충한다.

### Scope별 요구

- SubArc 요청은 가능한 `Completed` 결과 방향과 미완료 `Abandoned` 방향을 계획한다.
- MainArc 요청은 Finale Result 자체가 중심 결말을 서술하므로 Main Arc 전용 Epilogue Category를 만들지 않는다.
- Opening, SingleEvent, StandaloneSet과 PromotionSet은 요청한 사건이 후일담에 기여할 명확한 이유가 있을 때만 Additional 방향을 제안한다.
- RunPackage는 EventSystem이 요구하는 Legacy 전체 범위와 포함된 Sub Arc 결과의 조립 가능성을 검증한다.

청사진 단계는 Fame과 Reputation 조건의 의미적 Threshold Key와 조건 구조를 결정한다. 정확한 수치 범위는 EventBalanceProfile이 소유하며 최종 Event 생성 단계에서 변환한다.

---

## State Network

`stateNetwork`는 계획 전체에서 중요한 State의 생산, 소비와 결말 사용 관계를 검토하기 위한 Authoring 정보다.

```json
{
  "stateId": "OrcRouteDiscovered",
  "stateType": "Fact",
  "purpose": "침공 경로를 알아낸 사실을 후속 Main Arc 진행에 사용한다.",
  "producerRefs": [
    "orc_scout_encounter/attack_scouts"
  ],
  "consumerRefs": [
    "orc_camp_assault"
  ],
  "endingRefs": []
}
```

`stateType`은 다음 값을 사용할 수 있다.

```text
Fact
Knowledge
Trait
Fame
Reputation
Money
ResolvedEventCount
```

- Tag State에는 실제 ID를 사용한다.
- 수치 State의 `stateId`는 `Fame`, `Reputation`, `Money` 또는 `ResolvedEventCount`를 사용한다.
- 생산 경로가 없는 Tag를 요구하지 않는다.
- 소비 또는 결말 사용 계획이 전혀 없는 신규 Tag를 만들지 않는다.
- Location, Arc Stage와 RunPhase는 이 네트워크의 State로 중복 기록하지 않는다.

---

## 신규 Tag 제안

```json
{
  "id": "OrcRouteDiscovered",
  "category": "Fact",
  "desc": "용병단이 오크 침공대가 사용하는 이동 경로를 알아냈다.",
  "reason": "후속 Main Arc 공격 경로와 선택 조건을 구분한다.",
  "producerRefs": [
    "orc_scout_encounter/attack_scouts"
  ],
  "plannedConsumerRefs": [
    "orc_camp_assault"
  ],
  "approvalStatus": "Proposed"
}
```

- GenerationRequest의 Tag Policy가 `AllowProposal`일 때만 제안할 수 있다.
- `category`는 `Fact`, `Knowledge`, `Trait` 중 하나다.
- 기존 Tag와 의미가 중복되지 않아야 한다.
- 생산 Event와 미래 사용처가 모두 존재해야 한다.
- `Proposed` Tag는 최종 Event에서 사용할 수 없다.
- 사용자 승인과 해당 Data 반영 이후 `Approved`로 변경해야 한다.
- 신규 Tag가 승인되기 전에는 EventContentPlan 전체를 승인할 수 없으며 청사진 단계로 진행하지 않는다.

---

## Requirement Coverage

GenerationRequest의 명시적 요구가 계획 어디에서 처리되는지 기록한다.

```json
{
  "requirement": "오크 침공의 원인을 선택에 따라 다르게 해석할 수 있어야 한다.",
  "coverageRefs": [
    "orc_invasion_response",
    "orc_invasion_finale"
  ],
  "status": "Covered",
  "note": "대응 노선 Tag와 Finale 선택으로 반영한다."
}
```

`status`는 다음 값을 사용한다.

```text
Covered
PartiallyCovered
NotCovered
NotApplicable
```

필수 요구가 `PartiallyCovered` 또는 `NotCovered`면 계획을 승인할 수 없다.

---

## Risks

```json
{
  "id": "RISK-001",
  "severity": "Blocking",
  "type": "MissingReferenceData",
  "description": "Finale Location ID가 Location 데이터에 존재하지 않는다.",
  "affectedRefs": ["orc_invasion"],
  "designStatusRef": null
}
```

### severity

```text
Blocking
Warning
Note
```

- `Blocking`: 계획을 승인하거나 다음 단계로 넘길 수 없다.
- `Warning`: 계획은 진행할 수 있지만 사용자 검토가 필요하다.
- `Note`: 의도적 제약이나 후속 작업을 기록한다.

DesignStatus의 Open 항목이 현재 요청을 실제로 막을 때만 `Blocking`으로 기록한다.

---

## Validation Summary

```json
{
  "check": "MainArcFinaleReachability",
  "status": "Passed",
  "details": "모든 주요 분기가 FinaleReady와 Finale에 도달한다."
}
```

`status`는 다음 값을 사용한다.

```text
Passed
Failed
NotApplicable
```

`Failed`가 하나라도 있으면 Plan Status를 `ReadyForReview` 또는 `Approved`로 변경할 수 없다.

---

## Run Package Plan

`runPackagePlan`은 GenerationRequest Scope가 `RunPackage`일 때만 사용한다.

```json
{
  "mode": "AssembleExisting",
  "openingRef": {
    "refType": "ExistingEvent",
    "id": "opening_goblin_raid"
  },
  "mainArcRef": {
    "refType": "ExistingArc",
    "id": "orc_invasion"
  },
  "subArcRefs": [],
  "standaloneRefs": [],
  "guaranteedPromotionRefs": [
    {
      "refType": "ExistingEvent",
      "id": "promotion_mentor_visit"
    },
    {
      "refType": "ExistingEvent",
      "id": "promotion_ancient_memory"
    },
    {
      "refType": "ExistingEvent",
      "id": "promotion_battle_lesson"
    }
  ]
}
```

`refType`은 다음 값을 사용한다.

```text
PlannedEvent
PlannedArc
ExistingEvent
ExistingArc
ExistingModule
```

### RunPackage 규칙

- Opening 참조가 정확히 하나다.
- Main Arc 참조가 정확히 하나다.
- 서로 다른 보장용 범용 승급 Event 참조가 정확히 세 개다.
- 포함한 모든 Sub Arc와 Standalone 참조가 존재한다.
- `AssembleExisting`은 새 Event 슬롯을 만들지 않는다.
- `GenerateMissing`은 필수 구성에 부족한 콘텐츠만 `slots`와 `arcs`에 계획한다.
- `GenerateAll`은 요청한 전체 구성을 새로 계획한다.
- 정확한 승급 예약 Travel Turn은 계획하지 않는다.
- Main Arc가 정상 Finale까지 도달 가능하고 `NoProgressPossible`이 예상되지 않아야 한다.

---

## Scope별 계획 규칙

### SingleEvent

- `slots`는 정확히 하나다.
- 새 Arc 전체를 자동으로 만들지 않는다.
- 기존 Arc Event라면 GenerationRequest의 `arcContext`와 일치한다.
- 전체 Arc 도달 가능성을 검증하지 못하면 Risk를 기록한다.
- `runPackagePlan`은 `null`이다.

### Opening

- `slots`는 정확히 하나다.
- `arcs`는 비어 있다.
- 슬롯은 `Standalone + Opening` 조합이다.
- 모든 Result의 `arcTransitionPlan`은 `null`, `runTransitionPlan`은 `Normal`이다.
- Main Arc, Sub Arc와 보장용 승급 슬롯을 추가하지 않는다.
- Opening 문장은 특정 Main Arc를 전제로 계획하지 않는다.

### MainArc

- `arcs`의 Main Arc 수가 GenerationRequest의 `targetCount`와 같다.
- 각 Main Arc에 Introduction, 진행, FinaleReady 전환과 Finale가 존재한다.
- 모든 주요 분기가 정상 Finale에 도달한다.
- Opening, 별도 Sub Arc, 일반 Standalone과 보장용 승급 세트를 자동 추가하지 않는다.
- `runPackagePlan`은 `null`이다.

### SubArc

- `arcs`의 Sub Arc 수가 GenerationRequest의 `targetCount`와 같다.
- 각 Sub Arc에 Introduction과 하나 이상의 `Completed` 종결 방향이 존재한다.
- 긍정적·부정적 결과를 Tags로 구분한다.
- 미완료 상태의 `Abandoned` 후일담 방향이 존재한다.
- Main Arc와 Opening을 자동 추가하지 않는다.

### StandaloneSet

- 일반 Standalone 슬롯 수가 GenerationRequest의 `targetCount`와 같다.
- 각 슬롯은 `Standalone + Normal` 조합이다.
- Arc 전환과 Run 전환을 계획하지 않는다.
- 다른 Arc의 도입이나 진행을 직접 변경하지 않는다.

### PromotionSet

- Promotion 슬롯 수가 GenerationRequest의 `targetCount`와 같다.
- 모든 Promotion Result는 범용 승급이다.
- `promotionUsage`가 `GuaranteedCandidate`면 모든 슬롯에 해당 `systemRoles`를 사용하고 보장 후보 규칙을 적용한다.
- 다른 Scope의 콘텐츠를 추가하지 않는다.

### RunPackage

- `runPackagePlan`이 존재한다.
- GenerationRequest의 `runPackageMode`와 일치한다.
- 전체 런 불변 조건을 검증한다.
- GenerateMissing 또는 GenerateAll일 때만 필요한 새 Arc와 Event 슬롯을 계획한다.

---

## 공통 검증 규칙

- `schemaVersion`은 `1`이다.
- `planId`와 `planVersion` 조합이 고유하다.
- `requestRef`가 승인된 GenerationRequest와 일치한다.
- `scopeSummary.requestedUnitCount`와 GenerationRequest의 `targetCount`가 일치한다.
- `scopeSummary.requestedEventCount`와 GenerationRequest가 일치한다.
- `scopeSummary.plannedEventCount`와 실제 `slots` 수가 일치한다.
- `plannedUnitIds` 수와 Scope 단위 수가 일치한다.
- 사용자가 요청하지 않은 Scope의 콘텐츠가 없다.
- Event ID와 새 Arc ID가 기존 데이터 및 계획 안에서 중복되지 않는다.
- 모든 Location ID가 실제 데이터에 존재한다.
- 모든 Tag ID가 기존 데이터에 있거나 승인 대기 중인 신규 제안이다.
- Event Type, Arc Type과 Run Transition 조합이 EventSystem과 일치한다.
- Choice와 Automatic 구조가 서로 섞이지 않는다.
- 모든 Result의 `battle`과 `promotion`이 boolean이다.
- 전투 패배 이후 결과나 분기를 계획하지 않는다.
- 제한형 승급을 계획하지 않는다.
- Standalone은 Arc Transition을 만들지 않는다.
- Sub Arc는 Run Transition을 만들지 않는다.
- Event Result가 직접 `Abandoned`를 만들지 않는다.
- `Failed` Arc Status를 사용하지 않는다.
- EmergencyFinale을 사용하지 않는다.
- Finale의 모든 Result가 Main Arc를 `Completed`, RunPhase를 `Ended`로 전환한다.
- Opening의 모든 Result가 RunPhase를 `Normal`로 전환한다.
- Main Arc Introduction은 Opening과 분리되어 있다.
- 현재 Location에 Event가 없는 것을 오류로 취급하지 않는다.
- 모든 Location에 전용 Event를 강제하지 않는다.
- 신규 Tag에 생산과 소비 계획이 존재한다.
- GenerationRequest의 필수 요구가 모두 Covered다.
- Blocking Risk와 Failed Validation이 없다.
- Open 또는 Deferred DesignStatus 항목을 임의로 결정하지 않는다.

---

## Blueprint 단계 인계

승인된 EventContentPlan의 다음 정보는 청사진 생성기가 임의로 변경할 수 없다.

```text
requestRef
scopeSummary의 요청 범위
Arc ID, Arc Type, Stage와 Finale 구조
Event ID
arcId
arcType
eventType
arcStage
narrativeFunction의 의미
systemRoles
premise의 핵심 의미
locations
Resolution Type
Choice planKey와 의미
battle
promotion
Tag 생산 및 소비 방향
Fame, Reputation과 Money 변화 방향
Arc Transition 방향
Run Transition 방향
endingContribution
Epilogue 방향
승인된 신규 Tag
```

청사진 생성기는 다음을 상세화한다.

```text
최종 Choice ID
Event Conditions의 정확한 구조와 의미적 Threshold Key
Choice Conditions의 정확한 구조와 의미적 Threshold Key
Fame, Reputation과 Money 변화의 의미적 Magnitude Key와 이유
Tag, Arc Transition과 Run Transition의 정확한 Effects 구조
Arc Transition과 Run Transition의 런타임 구조
Finale 후보의 정확한 State 분할과 의미적 Threshold Key
Epilogue Fragment의 조건 구조와 의미적 Threshold Key
전투 Binding 필요 목록
최종 문장을 만들기 위한 작성 지침
```

청사진 생성기는 정확한 밸런스 숫자를 생성하거나 조정하지 않는다. 최종 Event 생성기는 청사진의 Key를 EventBalanceProfile의 값으로 결정적으로 변환하며 임의의 수치를 선택하지 않는다.

EventBalanceProfile이 없거나 필요한 Key가 정의되지 않았다면 청사진 작성은 가능하지만 수치가 완결된 최종 Event JSON은 생성할 수 없다.

EventContentPlan을 현재 EventSystem Schema로 구현할 수 없다면 청사진 생성기가 계획을 조용히 수정하지 않는다. 영향을 받는 Plan 참조와 이유를 보고하고 Plan 수정 단계로 되돌린다.

---

## 출력 규칙

EventContentPlan은 다음을 명확히 보여야 한다.

- 사용자가 요청한 Scope와 수량
- 실제 계획한 콘텐츠 단위와 Event 수
- 각 Event가 만드는 선택 또는 자동 결과
- 전투와 승급이 발생하는 위치
- Arc와 State의 생산 및 소비 관계
- Finale과 후일담 방향
- 신규 Tag 제안
- 미검증 사항과 Risk
- Scope별 검증 결과

EventContentPlan은 최종 Event 문장을 작성하지 않는다. 사건 전제, 행동 의도와 결과 방향만 작성한다.
