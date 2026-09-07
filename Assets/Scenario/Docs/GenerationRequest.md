# Generation Request 계약

## 문서 지위

이 문서는 사용자의 자연어 이벤트 생성 요청을 제작 파이프라인이 공유할 구조화된 요청으로 변환하기 위한 공식 계약이다.

`GenerationRequest`는 개발 단계의 제작 데이터이며 Unity 런타임 Event Schema가 아니다.

문서 우선순위는 다음과 같다.

```text
Docs/plan.md의 확정된 게임 규칙
→ Docs/EventSystem.md의 런타임 및 콘텐츠 계약
→ Docs/DesignStatus.md의 결정 상태
→ Docs/GenerationRequest.md의 제작 요청 계약
→ 생성 단계별 책임 문서
→ 개별 계획, 청사진과 생성된 JSON
```

- 이 문서는 GenerationRequest의 필드와 의미를 정의한다.
- 파이프라인 오케스트레이터는 사용자 요청을 해석해 GenerationRequest 인스턴스를 생성하고 관리한다.
- 사용자는 JSON을 직접 작성할 필요가 없다.
- 하위 생성 단계는 승인된 GenerationRequest를 읽기 전용으로 사용한다.
- GenerationRequest는 `Docs/EventSystem.md`에 없는 런타임 기능을 요청하거나 허용할 수 없다.
- `Docs/DesignStatus.md`의 Open 또는 Deferred 항목을 확정된 요구사항으로 바꿀 수 없다.

---

## 목적

GenerationRequest는 다음 문제를 방지한다.

- 사용자가 요청하지 않은 콘텐츠 종류를 생성하는 문제
- Main Arc 하나를 요청했는데 Opening, Sub Arc와 승급 이벤트까지 자동으로 추가하는 문제
- 사용자의 명시적 요구와 생성기가 추론한 가정을 구분하지 못하는 문제
- 단계마다 요청 범위를 다르게 해석하는 문제
- 승인된 요청이 변경됐는데 이전 계획이나 청사진을 계속 사용하는 문제

GenerationRequest는 생성할 내용과 생성하지 않을 내용의 경계를 정한다.

---

## 소유와 책임

### 사용자

- 만들고 싶은 콘텐츠와 목적을 자연어로 전달한다.
- 결과를 실질적으로 바꾸는 가정과 범위 변경을 승인한다.
- 필요하면 승인된 요청의 새 Version을 요청한다.

### 파이프라인 오케스트레이터

- 사용자 요청을 이 문서의 Schema로 정규화한다.
- 적절한 Scope를 선택한다.
- 명시적 요구와 추론한 가정을 분리한다.
- `plan.md`, `EventSystem.md`와 `DesignStatus.md`를 기준으로 요청을 검증한다.
- 결과를 실질적으로 바꾸지 않는 세부사항만 합리적으로 추론한다.
- 중요한 정보가 없으면 사용자 확인을 요청한다.
- 승인, Version과 하위 산출물의 유효 상태를 관리한다.

### 콘텐츠 플래너

- 승인된 GenerationRequest 범위 안에서만 콘텐츠를 계획한다.
- 요청한 콘텐츠 단위의 내부 완결성을 위해 필요한 슬롯 수를 정할 수 있다.
- 다른 Scope의 콘텐츠를 자동으로 추가하지 않는다.
- Scope 확장이 필요하면 요청을 직접 수정하지 않고 변경 제안을 반환한다.

### 청사진 생성기, 이벤트 생성기와 검증기

- 승인된 GenerationRequest를 읽기 전용으로 사용한다.
- 상위 단계에서 확정된 Scope와 제약을 변경하지 않는다.
- 요청 범위를 벗어난 산출물을 만들지 않는다.
- 요청과 산출물이 일치하는지 검증한다.

---

## 저장 위치

GenerationRequest 인스턴스는 다음 경로를 사용한다.

```text
Authoring/Requests/{requestId}.json
```

향후 기계 검증이 필요하면 다음 JSON Schema를 추가할 수 있다.

```text
Authoring/Schemas/GenerationRequest.schema.json
```

현재 계약의 의미는 이 문서를 기준으로 한다.

---

## Scope

```text
SingleEvent
Opening
MainArc
SubArc
StandaloneSet
PromotionSet
RunPackage
```

Scope는 생성 범위의 상한이다. 요청한 콘텐츠의 내부 완결성에 필요한 슬롯은 만들 수 있지만 다른 Scope의 콘텐츠를 자동으로 추가할 수 없다.

### SingleEvent

이벤트 슬롯 하나를 계획하고 생성한다.

- `targetCount`는 항상 `1`이다.
- 기존 Arc에 속한 이벤트라면 `arcContext`가 필요하다.
- Arc의 전체 도달 가능성이나 완결성을 보장하지 않는다.
- 전체 Arc 정보가 없어서 검증할 수 없는 사항은 Risk로 보고한다.

### Opening

Opening Event 하나를 계획하고 생성한다.

- `targetCount`는 항상 `1`이다.
- `requestedEventCount`도 `1`이다.
- `arcType`은 `Standalone`이다.
- `eventType`은 `Opening`이다.
- Main Arc, Sub Arc 또는 보장용 승급 이벤트를 추가하지 않는다.

### MainArc

Main Arc 단위를 계획하고 생성한다.

- `targetCount`는 생성할 Main Arc 수다.
- Main Arc 하나는 Introduction, 진행, 필요한 분기, FinaleReady 전환과 Finale에 필요한 여러 이벤트 슬롯을 가질 수 있다.
- `requestedEventCount`가 `null`이면 플래너가 Arc의 내부 완결성에 필요한 슬롯 수를 정한다.
- Opening, 별도 Sub Arc, 일반 Standalone 또는 보장용 승급 세트를 자동으로 추가하지 않는다.
- Main Arc의 모든 주요 경로가 정상 Finale에 도달할 수 있어야 한다.

### SubArc

Sub Arc 단위를 계획하고 생성한다.

- `targetCount`는 생성할 Sub Arc 수다.
- Sub Arc 하나는 Introduction, 진행과 하나 이상의 `Completed` 종결 방향에 필요한 여러 이벤트 슬롯을 가질 수 있다.
- 미완료 상태에서 Main Arc Finale에 진입하면 Unity가 `Abandoned`로 처리할 수 있는 후일담 방향을 계획한다.
- Main Arc, Opening 또는 다른 독립 콘텐츠를 자동으로 추가하지 않는다.

### StandaloneSet

지정한 수의 일반 Standalone Event를 계획하고 생성한다.

- `targetCount`는 Standalone Event 수다.
- 각 이벤트는 `arcId`와 `arcStage`에 `null`을 사용한다.
- `eventType`은 `Normal`이다.
- 다른 Arc의 Stage나 Status를 직접 변경하지 않는다.

### PromotionSet

지정한 수의 범용 승급 Event를 계획하고 생성한다.

- `targetCount`는 승급 Event 수다.
- 현재 프로토타입에서는 제한형 승급을 만들지 않는다.
- `promotionUsage`가 `GuaranteedCandidate`라면 `EventSystem.md`의 보장용 승급 후보 조건을 모두 만족해야 한다.
- Main Arc, Opening 또는 다른 콘텐츠를 자동으로 추가하지 않는다.

### RunPackage

하나의 전체 런 콘텐츠 구성을 계획하거나 기존 콘텐츠 모듈의 런 구성을 검증한다.

- `targetCount`는 항상 `1`이다.
- 이 Scope에서만 Opening 하나, Main Arc 하나와 보장용 범용 승급 Event 세 개라는 전체 런 불변 조건을 함께 검증한다.
- Sub Arc와 일반 Standalone의 수는 사용자 요구 또는 승인된 요청에 따른다.
- 기존 모듈을 조립하는 요청과 누락 콘텐츠까지 새로 생성하는 요청을 구분해야 한다.

---

## 기본 Schema

```json
{
  "schemaVersion": 1,
  "requestId": "orc_invasion_main_arc",
  "requestVersion": 1,
  "status": "Draft",

  "scope": "MainArc",
  "targetCount": 1,
  "requestedEventCount": null,

  "requestSummary": "오크 침공을 주제로 Main Arc 하나를 생성한다.",
  "premise": "오크 침공의 배후를 추적한다.",
  "tone": ["dark_fantasy", "heroic"],

  "arcContext": null,
  "promotionUsage": null,
  "runPackageMode": null,

  "requiredContent": [],
  "forbiddenContent": [],

  "locationPolicy": {
    "mode": "PlannerSelect",
    "allowedLocationIds": [],
    "requiredLocationIds": [],
    "forbiddenLocationIds": []
  },

  "tagPolicy": {
    "mode": "AllowProposal"
  },

  "approvalMode": "Interactive",

  "explicitRequirements": [
    "오크 침공을 주제로 한다."
  ],
  "inferredAssumptions": [
    "Main Arc의 정확한 이벤트 수는 콘텐츠 플래너가 결정한다."
  ],
  "designStatusRefs": [],
  "supersedesRequestVersion": null
}
```

Scope에 사용하지 않는 선택 필드는 `null` 또는 빈 배열을 사용한다. 정의되지 않은 임의 필드를 추가하지 않는다.

---

## 공통 필드

### schemaVersion

GenerationRequest Schema의 Version이다.

현재 Version은 `1`이다.

### requestId

요청과 모든 하위 산출물을 연결하는 고유하고 안정적인 문자열 ID다.

- 같은 논리적 요청의 Version이 올라가도 `requestId`는 유지한다.
- 다른 요청과 중복될 수 없다.
- 파일 경로에 안전한 영문 소문자, 숫자와 밑줄 사용을 권장한다.

### requestVersion

같은 `requestId` 안에서 승인된 요청 내용이 변경된 횟수를 나타내는 1 이상의 정수다.

Scope, 핵심 전제, 필수 콘텐츠, 금지 콘텐츠 또는 결과 구조에 영향을 주는 제약이 바뀌면 Version을 올린다.

### status

```text
Draft
Validated
Approved
InProgress
Completed
Superseded
```

- `Draft`: 사용자 요청을 정규화하는 중이다.
- `Validated`: 문서 계약과 Schema 검사를 통과했다.
- `Approved`: 사용자가 범위와 중요한 가정을 승인했다.
- `InProgress`: 하위 생성 단계가 실행 중이다.
- `Completed`: 요청 범위의 생성과 검증이 완료됐다.
- `Superseded`: 새 Version으로 대체되어 더 이상 사용하지 않는다.

하위 생성 단계는 원칙적으로 `Approved` 또는 `InProgress` 요청만 사용한다.

### scope

생성할 콘텐츠 단위와 적용할 로컬 검증 범위를 결정한다.

하위 단계는 Scope를 변경할 수 없다.

### targetCount

Scope가 가리키는 콘텐츠 단위의 수다.

예:

```text
Opening + targetCount 1
→ Opening Event 하나

MainArc + targetCount 1
→ Main Arc 하나

SubArc + targetCount 2
→ Sub Arc 두 개

StandaloneSet + targetCount 10
→ Standalone Event 열 개
```

### requestedEventCount

최종 Event 슬롯 수에 대한 사용자의 명시적 요구다.

- 사용자가 Event 수를 지정하지 않았다면 `null`이다.
- MainArc와 SubArc에서 `null`이면 플래너가 내부 완결성에 필요한 Event 수를 정할 수 있다.
- 명시된 수량으로 요청 단위의 필수 구조를 만들 수 없다면 임의로 초과하지 않고 문제를 보고한다.

### requestSummary

사용자 요청의 목적과 범위를 한 문장으로 요약한다.

새로운 요구사항을 추가하지 않는다.

### premise

요청 콘텐츠가 다룰 중심 사건, 상황 또는 소재다.

사용자가 자유로운 제안을 요청했다면 `null`을 사용할 수 있다.

### tone

콘텐츠의 분위기와 표현 방향이다.

빈 배열이면 `plan.md`와 기존 프로젝트 톤을 따른다.

### requiredContent

반드시 포함해야 하는 소재, 선택, 결과 또는 게임플레이 요소다.

문자열 배열로 기록하며 사용자의 명시적 요구를 우선한다.

### forbiddenContent

사용하지 않아야 하는 소재, 표현, 결과 또는 게임플레이 요소다.

`requiredContent`와 충돌하면 요청을 승인하지 않고 사용자 확인을 받는다.

### explicitRequirements

사용자가 직접 명시한 요구사항이다.

원문의 의미를 축소하거나 확장하지 않는다.

### inferredAssumptions

사용자가 말하지 않았지만 파이프라인 진행을 위해 합리적으로 해석한 세부사항이다.

- 결과를 실질적으로 바꾸지 않는 가정만 기록할 수 있다.
- Scope, 핵심 결말, 이벤트 수와 같은 중요한 결정을 조용히 추론하지 않는다.
- 사용자가 가정을 수정하면 GenerationRequest를 갱신한다.

### designStatusRefs

요청과 관련된 `DesignStatus.md` 항목 ID다.

- Open 항목이 있어도 현재 요청에 필요하지 않으면 작업을 계속할 수 있다.
- 작업에 반드시 필요한 Open 항목은 해결하거나 사용자에게 임시 Override 승인을 받기 전까지 Blocker다.
- Deferred 기능을 요청했다면 현재 범위 밖임을 보고한다.
- Provisional 항목은 현재 원본 문서에 정의된 규칙으로 사용하되 변경 가능성을 Risk에 남긴다.

### supersedesRequestVersion

현재 Version이 대체하는 이전 `requestVersion`이다.

첫 Version에서는 `null`이다.

---

## Scope별 선택 필드

### arcContext

기존 Arc의 단일 이벤트를 생성하거나 기존 Arc를 확장할 때 사용한다.

```json
{
  "arcId": "orc_invasion",
  "arcType": "Main",
  "arcStage": "Investigation",
  "eventType": "Normal"
}
```

- `SingleEvent`가 기존 Arc에 속하면 필수다.
- 새로운 MainArc 또는 SubArc 전체를 만드는 요청에서는 `null`일 수 있다.
- 존재하지 않는 기존 Arc ID를 임의로 만들지 않는다.

### promotionUsage

`PromotionSet`에서 사용한다.

```text
General
GuaranteedCandidate
```

- `General`: 범용 승급 Result를 제공하지만 런의 보장 슬롯 후보 조건까지 요구하지 않는다.
- `GuaranteedCandidate`: `EventSystem.md`의 보장용 범용 승급 Event 조건을 모두 만족해야 한다.

사용자가 보장용인지 명시하지 않았고 결과가 달라진다면 사용자에게 확인한다.

### runPackageMode

`RunPackage`에서 사용한다.

```text
AssembleExisting
GenerateMissing
GenerateAll
```

- `AssembleExisting`: 승인된 기존 콘텐츠만 조립하고 검증한다.
- `GenerateMissing`: 기존 콘텐츠를 우선 사용하고 필수 구성에 부족한 콘텐츠만 생성한다.
- `GenerateAll`: 요청한 런 패키지의 모든 콘텐츠를 새로 생성한다.

---

## Location Policy

```json
{
  "mode": "PlannerSelect",
  "allowedLocationIds": [],
  "requiredLocationIds": [],
  "forbiddenLocationIds": []
}
```

### mode

```text
PlannerSelect
Restricted
Fixed
```

- `PlannerSelect`: 플래너가 요청과 Location 연결성을 기준으로 선택한다.
- `Restricted`: `allowedLocationIds` 안에서만 선택한다.
- `Fixed`: `requiredLocationIds`를 반드시 사용한다.

### allowedLocationIds

사용 가능한 Location 범위다.

`Restricted`에서 비어 있을 수 없다.

### requiredLocationIds

반드시 사용해야 하는 Location 목록이다.

`Fixed`에서 비어 있을 수 없다.

### forbiddenLocationIds

사용할 수 없는 Location 목록이다.

Allowed 또는 Required와 충돌하면 요청을 승인할 수 없다.

모든 Location ID는 프로젝트의 Location 데이터에 존재해야 한다.

---

## Tag Policy

```json
{
  "mode": "AllowProposal"
}
```

```text
ExistingOnly
AllowProposal
```

- `ExistingOnly`: 기존 Facts, Knowledges와 Traits만 사용한다.
- `AllowProposal`: 플래너가 신규 Tag를 제안할 수 있다.

`AllowProposal`은 신규 Tag의 자동 승인을 의미하지 않는다. 제안된 Tag는 사용자 승인과 Data 반영 이후에만 청사진과 최종 Event에서 사용할 수 있다.

---

## Approval Mode

```text
Interactive
RunToCompletion
```

### Interactive

- 정규화된 GenerationRequest를 사용자에게 제시한다.
- 콘텐츠 계획과 중요한 구조 변경 시 승인을 기다린다.
- 대량 생성은 승인된 배치 단위로 진행한다.

### RunToCompletion

- 사용자가 요청 범위와 자동 진행을 명시적으로 승인한 경우 사용한다.
- 정상적인 단계별 승인 없이 완료까지 진행할 수 있다.
- Scope 변경, 문서 충돌, 미지원 기능과 DesignStatus Blocker는 자동으로 넘기지 않는다.

사용자가 자동 진행을 요청하지 않았다면 기본값은 `Interactive`다.

---

## 요청 정규화 규칙

### Scope 선택

사용자의 표현이 Scope와 직접 대응하면 해당 Scope를 사용한다.

```text
"Opening 이벤트 하나 만들어줘"
→ Opening

"메인 아크로 사용할 시나리오 하나 만들어줘"
→ MainArc

"Sub Arc 두 개 만들어줘"
→ SubArc

"독립 이벤트 열 개 만들어줘"
→ StandaloneSet

"보장용 승급 이벤트 세 개 만들어줘"
→ PromotionSet + GuaranteedCandidate

"한 런 전체 이벤트를 구성해줘"
→ RunPackage
```

여러 Scope를 한 번에 요청했다면 하나의 모호한 Scope로 합치지 않는다. 다음 중 하나를 선택한다.

- `RunPackage`로 명시적으로 승인받아 통합한다.
- Scope별 하위 GenerationRequest로 분리한다.

### 수량 해석

- MainArc와 SubArc의 `targetCount`는 Arc 수다.
- StandaloneSet과 PromotionSet의 `targetCount`는 Event 수다.
- Opening과 SingleEvent의 `targetCount`는 항상 `1`이다.
- 내부 Event 수와 콘텐츠 단위 수를 혼동하지 않는다.

### 요청하지 않은 콘텐츠

요청한 Scope 밖의 콘텐츠를 유용하다는 이유만으로 추가하지 않는다.

예:

```text
MainArc 요청
→ Main Arc 내부 Event는 필요한 만큼 생성 가능
→ Opening, Sub Arc와 PromotionSet은 추가 불가
```

### 모호한 요청

결과를 실질적으로 바꾸는 모호함은 사용자에게 확인한다.

예:

- 기존 Main Arc의 Event 하나인지 새로운 Main Arc 전체인지
- 일반 승급 Event인지 보장용 승급 후보인지
- RunPackage가 기존 콘텐츠 조립인지 전체 신규 생성인지
- 특정 Location을 반드시 사용해야 하는지

결과를 실질적으로 바꾸지 않는 세부사항은 `inferredAssumptions`에 기록하고 진행할 수 있다.

---

## Scope 변경

하위 단계는 GenerationRequest Scope를 직접 확장할 수 없다.

추가 콘텐츠가 필요하다고 판단하면 다음 형식의 변경 제안을 반환한다.

```json
{
  "type": "ScopeChangeProposal",
  "requestId": "orc_invasion_main_arc",
  "currentRequestVersion": 1,
  "proposedScope": "RunPackage",
  "reason": "사용자가 Main Arc 외에 Opening과 승급 이벤트 구성도 함께 요청했다.",
  "additionalContent": ["Opening", "PromotionSet"]
}
```

사용자가 승인하면 다음 작업을 수행한다.

1. `requestVersion`을 증가시킨다.
2. 이전 Version의 Status를 `Superseded`로 변경한다.
3. 새 GenerationRequest를 검증하고 승인한다.
4. 범위 변경의 영향을 받는 하위 계획, 청사진과 생성 결과를 무효화한다.

---

## 변경과 하위 산출물 무효화

다음 변경은 `requestVersion`을 증가시킨다.

- Scope 변경
- `targetCount` 또는 명시된 `requestedEventCount` 변경
- 핵심 premise 변경
- 필수 또는 금지 콘텐츠 변경
- Location Policy의 의미 있는 변경
- Tag Policy 변경
- 기존 Arc Context 변경
- Promotion Usage 또는 Run Package Mode 변경

다음 변경은 의미가 달라지지 않으면 같은 Version에서 정리할 수 있다.

- 오탈자 수정
- 사용자 요구의 의미를 바꾸지 않는 문장 정리
- 파일 경로 표기의 정규화

새 Version이 승인되면 영향을 받는 이전 Plan, Blueprint, Generated Event와 Validation Report를 재사용하지 않는다.

---

## 공통 검증 규칙

- `schemaVersion`은 `1`이다.
- `requestId`와 `requestVersion` 조합이 고유하다.
- `requestVersion`은 1 이상의 정수다.
- Scope가 허용된 값이다.
- `targetCount`는 1 이상의 정수다.
- Opening과 SingleEvent의 `targetCount`는 `1`이다.
- RunPackage의 `targetCount`는 `1`이다.
- `requestedEventCount`는 `null` 또는 1 이상의 정수다.
- `requiredContent`와 `forbiddenContent`가 충돌하지 않는다.
- Location Policy의 ID가 실제 데이터에 존재한다.
- Location의 Allowed, Required와 Forbidden 목록이 충돌하지 않는다.
- Scope에 사용하지 않는 선택 필드는 `null`이다.
- `explicitRequirements`가 사용자 요청의 의미를 바꾸지 않는다.
- 중요한 미확정 사항을 `inferredAssumptions`로 숨기지 않는다.
- 관련 DesignStatus 항목이 `designStatusRefs`에 포함된다.
- Deferred 기능을 현재 지원 기능처럼 요청하지 않는다.
- GenerationRequest가 `plan.md`와 `EventSystem.md`에 위배되지 않는다.
- 요청하지 않은 Scope의 콘텐츠를 결과에 추가하지 않는다.

---

## Scope별 검증 규칙

### SingleEvent

- Event 하나만 요청한다.
- 기존 Arc Event라면 유효한 `arcContext`가 존재한다.
- 전체 Arc 완결성을 보장한다고 표현하지 않는다.

### Opening

- Event 하나만 요청한다.
- Main Arc, Sub Arc와 PromotionSet을 포함하지 않는다.
- Opening의 런타임 구조는 `EventSystem.md`를 따른다.

### MainArc

- `targetCount`개의 Main Arc만 요청한다.
- Opening, Sub Arc, StandaloneSet과 PromotionSet을 자동 포함하지 않는다.
- `requestedEventCount`가 내부 완결성에 부족하면 승인 전에 문제를 보고한다.

### SubArc

- `targetCount`개의 Sub Arc만 요청한다.
- Main Arc와 Opening을 자동 포함하지 않는다.
- 부정적 결말을 Failed Status로 요청하지 않는다.

### StandaloneSet

- `targetCount`가 생성할 Standalone Event 수와 일치한다.
- 다른 Arc의 Stage와 Status를 변경하도록 요청하지 않는다.

### PromotionSet

- `promotionUsage`가 유효하다.
- 현재 프로토타입에서 제한형 승급을 요청하지 않는다.
- `GuaranteedCandidate`는 EventSystem의 보장용 승급 조건을 적용한다.

### RunPackage

- `runPackageMode`가 유효하다.
- 전체 런 구성 검증을 적용한다.
- Open 상태인 정확한 승급 예약 Turn을 파이프라인이 임의로 결정하지 않는다.

---

## 승인과 실행 흐름

```text
사용자 자연어 요청
→ GenerationRequest Draft 생성
→ Schema, plan, EventSystem과 DesignStatus 검증
→ 중요한 가정 확인
→ Validated
→ 사용자 승인 또는 명시적 자동 진행 권한 확인
→ Approved
→ 콘텐츠 계획 시작
→ InProgress
→ 요청 범위 생성과 검증 완료
→ Completed
```

사용자가 명시적으로 전체 실행을 요청했고 요청에 중요한 모호함이 없다면 Draft, Validated와 Approved 처리를 한 작업 흐름 안에서 진행할 수 있다.

---

## 하위 단계 인계 규칙

승인된 GenerationRequest는 모든 하위 산출물의 공통 입력이다.

각 Plan, Blueprint, Generated Module과 Validation Report는 다음 정보를 기록해야 한다.

```text
requestId
requestVersion
scope
```

하위 단계는 다음을 수행할 수 없다.

- Scope 변경
- Target Count 변경
- 명시된 Event Count 변경
- 필수 콘텐츠 제거
- 금지 콘텐츠 추가
- Location 또는 Tag Policy 완화
- Open 또는 Deferred 결정을 임의로 확정

구현 불가능하거나 계약과 충돌하는 요청을 발견하면 임의로 수정하지 않고 해당 요청을 Blocked 상태로 보고한다. `Blocked`는 GenerationRequest의 영구 Status가 아니라 현재 실행 결과다. 요청 자체는 수정 또는 새 Version 승인을 기다린다.

---

## 출력 규칙

GenerationRequest를 생성할 때는 다음을 함께 제시한다.

- 정규화된 Scope와 수량
- 사용자의 명시적 요구
- 파이프라인이 추가한 가정
- 관련 DesignStatus 항목
- 현재 Status
- 승인이 필요한 사항

GenerationRequest는 이벤트 내용 자체를 설계하지 않는다. Arc 구조, Event 슬롯, 선택 방향, Conditions, Effects와 최종 문장은 이후 생성 단계가 담당한다.
