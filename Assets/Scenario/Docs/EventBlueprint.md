# Event Blueprint 계약

## 문서 지위

이 문서는 승인된 EventContentPlan을 최종 Event 제작에 필요한 구체적인 청사진으로 변환한 `EventBlueprint`의 공식 계약이다.

EventBlueprint는 개발 단계의 제작 데이터이며 Unity 런타임 Event Schema가 아니다.

문서 우선순위는 다음과 같다.

```text
Docs/plan.md의 확정된 게임 규칙
→ Docs/EventSystem.md의 런타임 및 콘텐츠 계약
→ Docs/DesignStatus.md의 결정 상태
→ 승인된 GenerationRequest
→ Docs/EventContentPlan.md와 승인된 EventContentPlan
→ Docs/EventBlueprint.md의 청사진 계약
→ 생성 단계별 책임 문서
→ 개별 Blueprint와 생성된 JSON
```

- 모든 EventBlueprint는 승인된 EventContentPlan을 변경 불가능한 `planRef`로 참조한다.
- EventBlueprint는 상위 계획의 Scope, Event 수, Arc 구조, 분기 의미 또는 State 변화 방향을 바꿀 수 없다.
- EventBlueprint는 최종 문장을 작성하지 않고 문장 작성 지침만 작성한다.
- EventBlueprint는 정확한 밸런스 숫자를 생성하지 않는다.
- EventBlueprint는 `EventSystem.md`가 지원하지 않는 필드를 추가하지 않는다.

---

## 목적

EventBlueprint는 다음 내용을 확정한다.

- EventContentPlan과 Event 슬롯의 정확한 참조
- 최종 Event ID와 Choice ID
- Event 및 Choice Condition의 런타임 구조
- 수치 Condition에 사용할 의미적 Threshold Key
- Tag Effect의 정확한 구조
- Fame, Reputation과 Money 변화의 방향, 의미적 Magnitude Key와 이유
- Arc Transition과 Run Transition의 정확한 구조
- Choice 또는 Automatic Resolution의 정확한 형태
- Result별 Battle과 Promotion boolean
- 최종 제목, 설명, 선택지와 결과 문장을 위한 작성 지침
- 전투가 필요한 Result의 수동 Battle Binding 요구사항
- Epilogue Fragment의 조건 구조와 문장 작성 지침
- 최종 Event 생성 전에 필요한 검증 결과

EventBlueprint는 다음 내용을 확정하지 않는다.

- 정확한 Fame, Reputation, Money와 ResolvedEventCount 수치
- 최종 제목, 설명, 선택지, 결과와 Epilogue 문장
- 실제 Battle ID, 적 구성, 전장과 난이도
- 승급 대상 용병
- 범용 승급을 예약할 정확한 Travel Turn
- RunEventPool의 런타임 선택, Weight 또는 Priority
- EventContentPlan에 없는 새 Event, Arc, Choice 방향 또는 결말

---

## 책임 경계

EventBlueprint가 소유하는 것은 구현 구조, 의미적 Balance Key와 문장 작성 지침이다.

정확한 숫자는 EventBalanceProfile이, 최종 표시 문장과 Event JSON은 후속 생성 산출물이, 실제 Battle Binding은 수동 제작 데이터가 소유한다.

하위 산출물은 Blueprint의 구조와 Balance Key를 변경할 수 없다.

---

## 원본 참조

EventBlueprint는 승인된 GenerationRequest와 EventContentPlan을 `requestRef`와 `planRef`로 참조한다.

Location, Tag, Event, Arc와 Choice ID는 해당 제작 데이터에서 유효해야 한다. EventBalanceProfile 참조는 선택 사항이며 부재 시 후속 수치 완결 가능 상태로 판단한다.

---

## 저장 위치

기본 저장 위치는 다음과 같다.

```text
Authoring/Blueprints/{requestId}/v{requestVersion}/Blueprint.md
Authoring/Blueprints/{requestId}/v{requestVersion}/Blueprint.json
```

- 하나의 EventContentPlan에는 EventBlueprint 묶음 하나가 대응한다.
- JSON이 기계 판독용 정본이다.
- Markdown은 사용자 검토용 표현이다.
- 묶음 안의 각 `eventBlueprints` 항목은 개별 Event JSON 하나에 대응한다.

---

## Blueprint 상태

```text
Draft
ReadyForReview
Approved
Superseded
```

- `Draft`: 작성 중이거나 Blocking Risk 또는 Failed Validation이 있다.
- `ReadyForReview`: Blueprint 로컬 검증을 통과해 검토를 기다린다.
- `Approved`: 사용자 또는 승인된 자동 진행 흐름이 청사진을 확정했다.
- `Superseded`: 같은 Plan Version의 새 Blueprint Version으로 대체됐다.

미완성 Battle Binding은 Blueprint 승인 자체를 차단하지 않는다. 수동 Binding 요구사항으로 기록하고 런타임 배포 가능 검증에서 차단한다.

EventBalanceProfile 부재도 Blueprint 승인 자체를 차단하지 않는다. 대신 수치가 완결된 최종 Event JSON 생성을 차단한다.

---

## 기본 Schema

```json
{
  "schemaVersion": 1,
  "blueprintId": "orc_invasion_main_arc_blueprint",
  "blueprintVersion": 1,
  "status": "Draft",

  "requestRef": {
    "requestId": "orc_invasion_main_arc",
    "requestVersion": 1,
    "scope": "MainArc"
  },

  "planRef": {
    "planId": "orc_invasion_main_arc_plan",
    "planVersion": 1
  },

  "balanceProfileRef": null,
  "arcBlueprints": [],
  "eventBlueprints": [],
  "epilogueBlueprints": [],
  "requiredBalanceKeys": [],
  "manualBattleBindings": [],
  "risks": [],
  "validationSummary": []
}
```

정의되지 않은 임의 필드를 추가하지 않는다. 사용하지 않는 배열은 빈 배열을, 사용하지 않는 객체는 `null`을 사용한다.

---

## 공통 필드

### schemaVersion

현재 EventBlueprint Schema Version은 `1`이다.

### blueprintId

하나의 Blueprint 묶음을 식별하는 고유하고 안정적인 ID다.

### blueprintVersion

같은 EventContentPlan Version 안에서 Blueprint가 수정된 횟수를 나타내는 1 이상의 정수다.

- Choice ID, Condition 구조, Balance Key, Transition 구조 또는 작성 지침의 의미가 바뀌면 증가시킨다.
- 상위 EventContentPlan이 바뀌면 기존 Blueprint Version만 올리지 않고 새 Plan 참조로 다시 생성한다.

### requestRef와 planRef

승인된 GenerationRequest 및 EventContentPlan과 정확히 일치해야 한다.

Blueprint는 Scope를 변경할 수 없다.

### balanceProfileRef

```json
{
  "profileId": "prototype_event_balance",
  "profileVersion": 1
}
```

- Blueprint 작성 시 Profile이 없으면 `null`일 수 있다.
- 값이 있으면 모든 `requiredBalanceKeys`가 해당 Profile에 존재해야 한다.
- Profile Version이 바뀌어도 Blueprint의 의미적 Key가 유지된다면 Blueprint Version을 올릴 필요가 없다.
- 최종 Event JSON은 사용한 Profile ID와 Version을 제작 메타데이터에 기록해야 한다.

---

## 의미적 Balance Key

Blueprint는 숫자 대신 아래 Key를 사용한다. Key의 정확한 숫자는 EventBalanceProfile이 소유한다.

### 변화량 Magnitude Key

```text
Fame.Minor
Fame.Moderate
Fame.Major

Reputation.Minor
Reputation.Moderate
Reputation.Major

Money.MinorReward
Money.StandardReward
Money.MajorReward
Money.MinorCost
Money.StandardCost
Money.MajorCost
```

### 조건 Threshold Key

```text
Fame.Low
Fame.Moderate
Fame.High

Reputation.Low
Reputation.Moderate
Reputation.High

Money.MinorCost
Money.StandardCost
Money.MajorCost

ResolvedEventCount.Early
ResolvedEventCount.Mid
ResolvedEventCount.Late
```

### Key 사용 규칙

- 목록에 없는 Key는 유효하지 않다.
- `requiredBalanceKeys`에는 Blueprint에서 실제로 사용한 Key를 중복 없이 기록한다.
- Magnitude Key는 변화량의 절대 크기를 뜻한다. `direction`이 최종 부호를 결정한다.
- Money의 Reward Key는 `Gain`, Cost Key는 `Spend`와 함께 사용한다.
- Threshold Key는 `min` 또는 `max` 경계에 배치한다.
- 동일한 Key의 숫자가 조정되어도 Blueprint의 서사적 의미는 유지된다.
- 이 Key 목록을 바꾸는 것은 EventBlueprint Schema 변경이다.

---

## Arc Blueprint

`arcBlueprints`는 EventContentPlan이 새로 정의한 Arc만 포함한다.

```json
{
  "id": "orc_invasion",
  "arcType": "Main",
  "initialStage": "Introduction",
  "finaleStage": "FinaleReady",
  "finaleLocation": 20,
  "writingGuidance": {
    "nameDirection": "잿빛 깃발과 침공의 압박을 드러내는 짧은 이름",
    "chronicleTheme": "용병단이 침공의 원인과 대응 방식을 선택한 과정",
    "forbiddenClaims": [
      "런 시작부터 용병단이 완성된 영웅 집단이었다고 단정하지 않는다."
    ]
  }
}
```

- ID, Arc Type, Stage와 Finale Location은 EventContentPlan을 그대로 따른다.
- `writingGuidance`는 최종 Arc 이름이나 문장을 포함하지 않는다.
- 기존 Arc를 대상으로 하는 SingleEvent라면 Arc 정의를 복제하지 않는다.

---

## Event Blueprint Schema

```json
{
  "id": "orc_scout_encounter",
  "sourceSlotId": "orc_scout_encounter",
  "arcId": "orc_invasion",
  "arcType": "Main",
  "eventType": "Normal",
  "arcStage": "Investigation",
  "systemRoles": [],
  "locations": [4],

  "writingGuidance": {
    "titleDirection": "불탄 농장과 정찰대 발견을 압축한 제목",
    "descriptionBeats": [
      "용병단이 불타버린 농장의 흔적을 조사한다.",
      "침공 경로를 정찰하던 오크 부대를 발견한다.",
      "즉시 대응할지 판단해야 하는 상황으로 끝낸다."
    ],
    "toneNotes": ["긴박함", "현장 조사"],
    "continuityFacts": ["침공 경로는 아직 확정적으로 알지 못한다."],
    "forbiddenClaims": ["특정 용병을 고정 주인공으로 지정하지 않는다."]
  },

  "conditions": {
    "requiredTags": [],
    "forbiddenTags": [],
    "fame": null,
    "reputation": null,
    "money": null,
    "resolvedEventCount": null
  },

  "resolution": {
    "type": "Choice",
    "choices": []
  },

  "endingContribution": null
}
```

---

## Event Blueprint 공통 필드

### ID와 상위 참조

- `id`는 EventContentPlan 슬롯의 Event ID와 같으며 최종 Event까지 유지한다.
- `sourceSlotId`는 반드시 대응하는 슬롯 ID다.
- 새 Event ID를 만들거나 슬롯을 합치고 나누지 않는다.

### 구조 필드

다음 필드는 EventContentPlan과 정확히 일치해야 한다.

```text
arcId
arcType
eventType
arcStage
systemRoles
locations
Resolution Type
battle
promotion
Arc Transition 방향
Run Transition 방향
```

### writingGuidance

모든 문자열은 최종 문장이 아니라 작성 지침이다.

- `titleDirection`: 제목이 전달해야 할 이미지와 초점
- `descriptionBeats`: 설명 문장이 반드시 포함해야 할 정보의 순서
- `toneNotes`: 어조와 감정의 제한
- `continuityFacts`: 문장 작성 시 보존해야 할 현재 사실
- `forbiddenClaims`: 서술이 새로 확정해서는 안 되는 사실

최종 제목이나 설명을 그대로 작성하지 않는다.

---

## Event Conditions

```json
{
  "requiredTags": ["OrcTracksFound"],
  "forbiddenTags": [],
  "fame": {
    "minThresholdKey": "Fame.Moderate",
    "maxThresholdKey": null
  },
  "reputation": null,
  "money": null,
  "resolvedEventCount": {
    "minThresholdKey": "ResolvedEventCount.Mid",
    "maxThresholdKey": null
  }
}
```

- Tag 배열은 승인된 실제 Tag ID만 사용한다.
- 수치 조건이 없으면 해당 객체는 `null`이다.
- 수치 조건이 있으면 `minThresholdKey`와 `maxThresholdKey` 중 하나 이상이 존재해야 한다.
- 두 Key가 모두 있으면 Profile에서 변환한 Min이 Max보다 클 수 없다.
- EventContentPlan의 Condition Intent를 강화하거나 완화하지 않는다.
- Exact numeric Min과 Max를 기록하지 않는다.

Threshold Key는 최종 Event Schema의 다음 필드에 대응한다.

```text
fame.minThresholdKey → fameMin
fame.maxThresholdKey → fameMax
reputation.minThresholdKey → reputationMin
reputation.maxThresholdKey → reputationMax
money.minThresholdKey → moneyMin
money.maxThresholdKey → moneyMax
resolvedEventCount.minThresholdKey → resolvedEventCountMin
resolvedEventCount.maxThresholdKey → resolvedEventCountMax
```

`locations`는 Conditions 안에 중복하지 않고 Event Blueprint 공통 필드에서 관리하며 최종 Event Schema의 `conditions.locations`에 대응한다.

---

## Resolution Blueprint

`resolution.type`은 다음 중 하나다.

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
      "id": "attack_scouts",
      "sourcePlanKey": "attack_scouts",
      "writingGuidance": {
        "actionDirection": "정찰대를 즉시 공격한다는 능동적 행동",
        "clarityRequirements": ["전투가 발생한다는 사실을 숨기지 않는다."],
        "forbiddenImplications": ["전투 승리를 선택 전에 확정하지 않는다."]
      },
      "conditions": {
        "requiredTags": [],
        "forbiddenTags": [],
        "moneyMinThresholdKey": null
      },
      "result": {}
    }
  ]
}
```

- `id`는 같은 Event 안에서 고유한 최종 Choice ID다.
- `auto`는 일반 Choice ID로 사용할 수 없다.
- `sourcePlanKey`는 EventContentPlan의 Choice `planKey`와 일치한다.
- Choice를 추가, 삭제 또는 의미가 다른 방향으로 바꾸지 않는다.
- Choice Condition은 Tags와 `moneyMinThresholdKey`만 사용한다.
- 조건부 Choice만 남아 Event가 해결 불가능해지지 않도록 조건 없는 기본 Choice를 제공한다.

Choice의 `writingGuidance`는 최종 선택지 문장을 쓰지 않는다.

### Automatic

```json
{
  "type": "Automatic",
  "result": {}
}
```

- Choice 배열을 함께 사용하지 않는다.
- Automatic의 최종 연대기 기록용 Choice ID는 예약 값 `auto`다.
- Automatic Result에 전투가 있으면 수동 Battle Binding의 `choiceId`도 `auto`다.

---

## Result Blueprint

```json
{
  "writingGuidance": {
    "outcomeBeats": [
      "전투에서 승리한 뒤 생존한 정찰대의 지도에서 침공 경로를 알아낸다.",
      "이 정보가 다음 조사 방향을 연다."
    ],
    "chronicleFocus": "용병단이 위험을 감수해 중요한 정보를 획득한 사실",
    "toneNotes": ["짧은 승리", "더 큰 위협의 암시"],
    "forbiddenClaims": ["전투 패배 이후의 결과를 서술하지 않는다."]
  },
  "battle": true,
  "promotion": false,
  "effects": {
    "addTags": ["OrcRouteDiscovered"],
    "removeTags": [],
    "fameChange": {
      "direction": "Increase",
      "magnitudeKey": "Fame.Minor",
      "reason": "농장 생존자들이 용병단의 승리를 주변에 알린다."
    },
    "reputationChange": null,
    "moneyChange": null,
    "arcTransition": {
      "arcId": "orc_invasion",
      "stage": "Escalation",
      "status": "Active"
    },
    "runTransition": null
  }
}
```

### Result writingGuidance

- `outcomeBeats`: 선택 또는 Automatic 결과와 전투 승리 후 상태를 순서대로 설명하는 지침
- `chronicleFocus`: 연대기에 남겨야 할 핵심 행위나 결과
- `toneNotes`: 결과 문장의 감정과 강도
- `forbiddenClaims`: 실제 Effect와 State를 넘어 새 사실을 만들지 못하게 하는 제한

최종 결과 문장을 작성하지 않는다.

### battle

- EventContentPlan의 boolean을 그대로 사용한다.
- `true`면 대응하는 `manualBattleBindings` 항목이 정확히 하나 있어야 한다.
- Result의 작성 지침과 Effects는 전투 승리 후를 뜻한다.
- 전투 패배 후 결과나 분기를 만들지 않는다.

### promotion

- EventContentPlan의 boolean을 그대로 사용한다.
- 모든 Promotion은 범용 승급이다.
- 특정 용병, 직업, 계열 또는 티어를 문장 지침이나 조건에 넣지 않는다.
- battle과 함께 true라면 승리 후 승급을 제공한다.

---

## Effects Blueprint

### Tags

- `addTags`와 `removeTags`는 최종 Event에 사용할 정확한 Tag 배열이다.
- EventContentPlan의 생산 및 소비 방향과 일치해야 한다.
- 같은 Result에서 같은 Tag를 추가하고 제거할 수 없다.
- 승인되지 않은 Proposed Tag를 사용할 수 없다.

### Fame과 Reputation

```json
{
  "direction": "Increase",
  "magnitudeKey": "Reputation.Moderate",
  "reason": "보상을 포기하고 주민과의 약속을 지킨다."
}
```

`direction`은 다음 값을 사용한다.

```text
Increase
Decrease
```

- 변화가 없으면 전체 객체를 `null`로 사용한다.
- `magnitudeKey`는 같은 State Domain의 Key여야 한다.
- `reason`은 변화가 사회적으로 알려지거나 평가되는 이유를 설명하는 제작 지침이다.
- Profile의 Magnitude 값은 양수이며 Direction이 최종 부호를 결정한다.

### Money

```json
{
  "direction": "Spend",
  "magnitudeKey": "Money.StandardCost",
  "reason": "부상자의 치료비를 부담한다."
}
```

`direction`은 다음 값을 사용한다.

```text
Gain
Spend
```

- Gain은 Reward Key, Spend는 Cost Key와 함께 사용한다.
- 변화가 없으면 `null`이다.
- Spend Result를 선택할 수 있다면 같은 Choice의 `moneyMinThresholdKey`가 지출 Magnitude와 호환되어야 한다.
- Spend는 최종 `moneyChange`의 음수 방향에 대응한다.

### Arc Transition

```json
{
  "arcId": "orc_invasion",
  "stage": "Escalation",
  "status": "Active"
}
```

- EventContentPlan의 방향을 정확한 런타임 구조로 표현한다.
- 현재 Event가 속한 Arc만 변경할 수 있다.
- Standalone은 `null`이다.
- Main과 Sub Arc는 `Active` 또는 `Completed`만 사용한다.
- Event Result가 `Abandoned`를 만들지 않는다.

### Run Transition

```text
null
Normal
FinaleReady
Ended
```

- Opening의 모든 Result는 `Normal`이다.
- Main Arc 진행 Result만 `FinaleReady`를 사용할 수 있다.
- Main Arc Finale의 모든 Result는 `Ended`다.
- 그 외 Result는 `null`이다.

---

## Finale Blueprint

Main Arc Finale Blueprint는 EventContentPlan의 Finale State Coverage를 실제 Choice 조건 구조로 구체화한다.

- 모든 주요 분기 State 조합이 하나 이상의 선택 가능한 Choice를 가져야 한다.
- 조건 없는 기본 Choice를 하나 이상 제공한다.
- 기본 Choice는 특정 결말 Tag가 없다는 이유만으로 오류가 발생하지 않게 한다.
- 서로 겹치는 조건부 Choice가 의도치 않은 중복 결말을 만들지 않게 한다.
- 모든 Result는 Main Arc를 `Completed`, RunPhase를 `Ended`로 전환한다.
- 불리한 정상 결말도 `Completed`를 사용하고 결과 Tags로 구분한다.
- 전투가 있으면 Effects와 Ended 전환은 승리 후에만 적용된다.

정확한 Fame 또는 Reputation 조건이 필요하면 숫자가 아니라 Threshold Key를 사용한다.

---

## 보장용 범용 승급 후보

`GuaranteedPromotionCandidate` 역할을 가진 Event는 다음을 모두 만족해야 한다.

- `Standalone + Normal` 조합이다.
- 모든 Result의 `promotion`이 true다.
- 모든 Event 및 Choice Condition이 비어 있다.
- 모든 Normal 이동 Location을 포함한다.
- Arc와 Run Transition이 없다.
- 모든 파티 구성에서 자연스러운 작성 지침을 사용한다.
- `requiredBalanceKeys`를 요구하는 수치 조건을 만들지 않는다.

승급 서술은 특정 용병이 선택되기 전에 누구에게 적용될지 단정하지 않는다.

---

## 수동 Battle Binding

```json
{
  "eventId": "orc_scout_encounter",
  "choiceId": "attack_scouts",
  "bindingStatus": "Pending",
  "battleId": null,
  "encounterDirection": "소수 오크 정찰대를 빠르게 제압하지만 본대의 위협을 암시하는 전투",
  "requiredOutcomeContinuity": [
    "승리 후 정찰대가 지닌 지도를 확보할 수 있어야 한다."
  ]
}
```

`bindingStatus`는 다음 값을 사용한다.

```text
Pending
Bound
```

- 아직 수동 연결되지 않은 요구사항은 `Pending`과 `battleId: null`이다.
- 사람이 실제 전투를 연결한 뒤에만 `Bound`와 유효한 `battleId`를 기록할 수 있다.
- Pending 상태에서도 Event JSON 초안 생성은 허용한다.
- Pending 상태의 Event와 BattleBindings는 Unity 런타임 배포 가능 상태가 아니다.
- Choice Result는 정확한 Choice ID를, Automatic Result는 `auto`를 연결 키로 사용한다.

`BattleBindings.json`은 EventBlueprint 산출물에 포함되지 않는다.

---

## Epilogue Blueprint

```json
{
  "id": "refugee_caravan_rescued",
  "sourcePlanKey": "refugee_caravan_rescued",
  "category": "SubArc",
  "arcId": "refugee_caravan",
  "arcStatus": "Completed",
  "conditions": {
    "requiredTags": ["RefugeesRescued"],
    "forbiddenTags": [],
    "fame": null,
    "reputation": null
  },
  "writingGuidance": {
    "factBeats": [
      "구조된 피난민들이 변경 마을에 정착한다.",
      "용병단의 선택이 그들의 생존에 직접 영향을 주었다는 사실을 남긴다."
    ],
    "toneNotes": ["담담한 희망"],
    "forbiddenClaims": ["게임에서 발생하지 않은 추가 전투를 만들지 않는다."]
  }
}
```

- `id`는 Epilogue Fragment의 최종 ID다.
- `sourcePlanKey`는 EventContentPlan의 Epilogue Plan Key와 일치한다.
- Category, Arc ID와 Arc Status는 상위 계획을 따른다.
- Fame과 Reputation 조건은 Event Conditions와 같은 Threshold 구조를 사용한다.
- 정확한 수치와 최종 문장을 작성하지 않는다.
- Sub Arc의 `Completed` 및 `Abandoned` 방향을 혼동하지 않는다.
- Main Arc Finale Result 자체를 별도의 Main Arc Epilogue Category로 복제하지 않는다.

---

## requiredBalanceKeys

```json
[
  {
    "key": "Fame.Minor",
    "usage": "Magnitude",
    "consumerRefs": [
      "orc_scout_encounter/attack_scouts"
    ]
  },
  {
    "key": "ResolvedEventCount.Mid",
    "usage": "Threshold",
    "consumerRefs": [
      "orc_invasion_finale"
    ]
  }
]
```

`usage`는 다음 중 하나다.

```text
Magnitude
Threshold
```

- Blueprint에서 사용하는 모든 `(key, usage)` 조합이 정확히 한 번씩 목록에 존재해야 한다.
- 같은 Key를 Threshold와 Magnitude로 모두 사용하면 Usage별 항목을 따로 만든다.
- `consumerRefs`는 해당 `(key, usage)` 조합을 사용하는 Event, Choice Result 또는 Epilogue를 가리킨다.
- Profile이 연결되어 있다면 Key 존재 여부와 Domain을 검증한다.
- Profile이 없다면 Key는 계약의 허용 목록에 속하는지만 검증한다.

---

## Risk

```json
{
  "id": "BP-RISK-001",
  "severity": "Warning",
  "type": "PendingBattleBinding",
  "description": "오크 정찰대 전투가 아직 연결되지 않았다.",
  "affectedRefs": ["orc_scout_encounter/attack_scouts"]
}
```

`severity`는 다음 값을 사용한다.

```text
Blocking
Warning
Note
```

- Pending Battle Binding은 기본적으로 Warning이다.
- 상위 계획 불일치, 미지원 기능, 잘못된 ID 또는 해결 불가능한 Choice 구조는 Blocking이다.
- Profile 부재는 Blueprint 자체에는 Note이며 최종 Event 생성에는 Blocking이다.
- 필요한 Balance Key가 연결된 Profile에 없으면 최종 Event 생성 Blocking이다.

---

## Validation Summary

```json
{
  "check": "BalanceKeyValidity",
  "status": "Passed",
  "details": "사용된 모든 Key가 계약의 허용 목록에 존재한다. Profile 값 검증은 아직 수행하지 않았다."
}
```

`status`는 다음 값을 사용한다.

```text
Passed
Failed
NotApplicable
NotRun
```

- Profile이 없을 때 Profile 값 검증은 `NotRun`이다.
- Battle Binding 배포 검증은 Pending 항목이 있으면 `Failed`지만 Blueprint 구조 검증과 구분한다.
- Blueprint의 `ReadyForReview` 여부는 Blueprint 구조 검증만으로 판단한다.
- 수치 완결 Event 생성과 런타임 배포 가능 여부는 별도 검증 결과로 판단한다.

---

## 공통 검증 규칙

### 상위 산출물 일치

- `requestRef`와 `planRef`가 승인된 입력과 일치한다.
- Event와 Arc 수가 EventContentPlan과 일치한다.
- 모든 Event에 대응하는 `sourceSlotId`가 있다.
- Scope, Event ID, Arc 구조, Location과 Resolution Type을 변경하지 않는다.
- Choice 방향을 추가하거나 삭제하지 않는다.

### ID

- Blueprint ID와 Version 조합이 고유하다.
- Event 및 Arc ID가 상위 계획과 같다.
- Choice ID가 같은 Event 안에서 고유하고 안정적이다.
- 일반 Choice ID로 `auto`를 사용하지 않는다.
- Epilogue ID가 중복되지 않는다.

### Conditions

- 승인된 Tag ID만 사용한다.
- Event Conditions는 EventSystem이 지원하는 필드만 사용한다.
- Choice Conditions는 Tags와 Money Min만 사용한다.
- 수치 조건에 정확한 숫자가 없다.
- 모든 Threshold Key가 허용 목록에 있다.
- 조건부 Choice 때문에 해결 불가능한 Event가 없다.

### Results와 Effects

- 모든 Result의 battle과 promotion이 boolean이다.
- Add와 Remove에 같은 Tag가 없다.
- 수치 변화가 없으면 객체가 null이다.
- 수치 변화가 있으면 Direction, Magnitude Key와 Reason이 모두 있다.
- Money Direction과 Reward 또는 Cost Key가 일치한다.
- Fame과 Reputation 변화 이유를 사건 내용으로 설명할 수 있다.
- 제한형 승급을 만들지 않는다.
- 전투 패배 이후 결과를 만들지 않는다.

### Arc와 Run

- Standalone은 Arc Transition이 없다.
- Sub Arc와 Standalone은 Run Transition이 없다.
- Event Result가 `Abandoned` 또는 `Failed`를 만들지 않는다.
- Opening의 모든 Result가 `Normal`로 전환한다.
- Main Arc Finale의 모든 Result가 Main Arc `Completed`와 RunPhase `Ended`를 만든다.
- 모든 주요 Finale State에 선택 가능한 경로가 있다.

### Writing Guidance

- 최종 제목, 설명, 선택지, 결과 또는 Epilogue 문장을 직접 작성하지 않는다.
- 각 Event에 Title Direction과 Description Beats가 있다.
- 각 Choice에 Action Direction이 있다.
- 각 Result에 Outcome Beats와 Chronicle Focus가 있다.
- 지침이 실제 Conditions와 Effects를 과장하거나 모순되지 않는다.
- 특정 용병을 고정 주인공으로 만들지 않는다.
- 범용 승급 지침이 특정 직업이나 티어를 전제로 하지 않는다.

### Balance

- Blueprint 안에 밸런스 숫자가 없다.
- 모든 사용 Key가 `requiredBalanceKeys`에 있다.
- Profile이 연결되어 있으면 모든 Key가 Profile에 존재한다.
- Profile 값 적용 후 Fame, Reputation과 Money가 EventSystem 범위를 위반할 가능성을 검증한다.
- 각 Key는 Profile 값으로 단일하게 변환될 수 있어야 한다.

### Battle Binding

- battle true인 모든 Result에 수동 Binding 항목이 정확히 하나 있다.
- battle false인 Result에는 Binding 항목이 없다.
- Event ID와 Choice ID 또는 `auto`가 실제 Result를 가리킨다.
- `battleId`는 수동으로 연결된 값이 아니면 null이다.
- Pending Binding이 배포 가능 콘텐츠에 포함되지 않는다.

Failed인 Blueprint 구조 검증이 하나라도 있거나 Blocking Risk가 남아 있으면 `ReadyForReview` 또는 `Approved`로 만들 수 없다.

---

## 후속 단계 준비 상태

EventBlueprint는 다음 두 준비 상태를 구분한다.

### 수치 완결 가능

- 수치 Key를 사용하지 않거나 유효한 `balanceProfileRef`가 존재한다.
- 사용하는 모든 Key가 Profile에 정의되어 있다.
- 수치 Key가 있지만 Profile이 없으면 수치가 완결된 Event JSON을 만들 수 없다.

### Unity 배포 가능

- 수치 완결 조건을 만족한다.
- 모든 battle true Result의 Binding 상태가 `Bound`다.
- Pending Binding이 하나라도 있으면 Event JSON 초안은 허용되지만 Unity 배포 가능 상태는 아니다.

Blueprint 승인 상태는 이 두 상태와 별개다. 승인된 Blueprint도 Profile 또는 Battle Binding 상태에 따라 후속 산출물이 제한될 수 있다.

---

## 최종 목적

EventBlueprint의 목적은 LLM이 밸런스 숫자나 최종 문장을 한 번에 결정하게 하는 것이 아니다.

EventContentPlan의 구조를 보존하면서 서사적 의미를 검토 가능한 작성 지침과 안정적인 의미적 Balance Key로 바꾸고, 중앙 Balance Profile의 수치 변경만으로 모든 최종 Event를 일관되게 재생성할 수 있게 하는 것이 목적이다.
