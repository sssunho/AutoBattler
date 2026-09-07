# Event Balance Profile 계약

## 문서 지위

이 문서는 EventBlueprint의 의미적 Balance Key를 Unity Event Schema가 요구하는 정확한 정수로 변환하는 `EventBalanceProfile`의 공식 계약이다.

EventBalanceProfile은 이벤트 콘텐츠가 아니라 게임 밸런스 데이터다. 서사 생성 단계가 값을 임의로 만들거나 수정할 수 없다.

문서 우선순위는 다음과 같다.

```text
Docs/plan.md의 확정된 게임 규칙
→ Docs/EventSystem.md의 State 범위와 런타임 계약
→ Docs/DesignStatus.md의 결정 상태
→ Docs/EventBlueprint.md의 의미적 Key 계약
→ Docs/EventBalanceProfile.md의 수치 매핑 계약
→ 승인된 EventBalanceProfile JSON
→ 생성된 Event JSON
```

- EventSystem의 State 범위를 변경하지 않는다.
- EventBlueprint의 Key 의미를 재해석하지 않는다.
- Profile 값은 게임 밸런스 담당자가 승인한다.
- EventGenerator는 Profile 값을 그대로 적용하며 임의 조정하지 않는다.

---

## 목적

EventBalanceProfile은 다음을 확정한다.

- 의미적 Threshold Key별 정확한 조건 정수
- 의미적 Magnitude Key별 정확한 절대 변화량
- Profile ID, Version과 승인 상태
- EventBlueprint Schema 호환 Version
- Missing Key와 범위 초과 처리 정책
- 값 사이의 순서와 Domain 제약
- Profile 변경 시 최종 Event 재생성 기준

EventBalanceProfile은 다음을 결정하지 않는다.

- Event 또는 Arc의 내용과 수량
- 어떤 Event가 어떤 Balance Key를 사용할지
- Choice, Condition 또는 Effect 구조
- Fame, Reputation과 Money 변화의 서사적 이유
- Event 표시 문장
- Battle, Promotion, Arc Transition과 Run Transition
- Unity State의 기본 범위

---

## 책임 경계

```text
EventContentPlan
→ 수치 조건과 변화의 의미 및 방향

EventBlueprint
→ Threshold 또는 Magnitude Key 선택

EventBalanceProfile
→ 선택된 Key의 정확한 정수

EventGenerator
→ Key와 Direction을 최종 Condition 및 Effect 숫자로 변환
```

Profile 숫자가 바뀌어도 EventContentPlan과 EventBlueprint의 의미가 유지되는 한 두 산출물을 다시 생성하지 않는다.

---

## 소유권

Profile 값의 소유자는 실제 게임의 밸런스 설계다.

- LLM은 명시적인 요청 없이 값을 제안하거나 채우지 않는다.
- 형식 생성만 요청받았다면 모든 미결정 값은 `null`인 Draft를 만든다.
- 값 제안을 요청받아도 자동으로 Approved 처리하지 않는다.
- Approved Profile의 값 변경은 새로운 `profileVersion`을 요구한다.
- EventGenerator와 BlueprintGenerator는 Profile을 수정할 수 없다.

---

## 저장 위치

계약 문서는 다음 위치에 있다.

```text
Docs/EventBalanceProfile.md
```

실제 Profile JSON의 기본 위치는 다음과 같다.

```text
Data/EventBalanceProfiles/{profileId}.json
```

프로토타입에서 Profile을 하나만 사용하더라도 ID와 Version을 유지한다.

---

## 상태

```text
Draft
Validated
Approved
Superseded
```

- `Draft`: 값이 비어 있거나 조정 중이다. `null`을 허용한다.
- `Validated`: 모든 값과 규칙이 검증됐지만 게임 밸런스 승인을 기다린다.
- `Approved`: 게임 밸런스 담당자가 최종 Event 생성에 사용할 수 있도록 승인했다.
- `Superseded`: 새 Version으로 대체됐다.

EventGenerator는 `Approved` 상태의 Profile만 수치가 완결된 Event JSON 생성에 사용할 수 있다.

---

## 기본 Schema

아래 예시는 값이 아직 결정되지 않은 Draft Template이다.

```json
{
  "schemaVersion": 1,
  "profileId": "prototype_event_balance",
  "profileVersion": 1,
  "status": "Draft",
  "eventBlueprintSchemaVersion": 1,

  "stateDomains": {
    "Fame": {
      "min": 0,
      "max": 10
    },
    "Reputation": {
      "min": -3,
      "max": 3
    },
    "Money": {
      "min": 0,
      "max": null
    },
    "ResolvedEventCount": {
      "min": 0,
      "max": null
    }
  },

  "thresholds": {
    "Fame.Low": null,
    "Fame.Moderate": null,
    "Fame.High": null,

    "Reputation.Low": null,
    "Reputation.Moderate": null,
    "Reputation.High": null,

    "Money.MinorCost": null,
    "Money.StandardCost": null,
    "Money.MajorCost": null,

    "ResolvedEventCount.Early": null,
    "ResolvedEventCount.Mid": null,
    "ResolvedEventCount.Late": null
  },

  "magnitudes": {
    "Fame.Minor": null,
    "Fame.Moderate": null,
    "Fame.Major": null,

    "Reputation.Minor": null,
    "Reputation.Moderate": null,
    "Reputation.Major": null,

    "Money.MinorReward": null,
    "Money.StandardReward": null,
    "Money.MajorReward": null,
    "Money.MinorCost": null,
    "Money.StandardCost": null,
    "Money.MajorCost": null
  },

  "resolutionPolicy": {
    "missingKey": "Error",
    "nullValue": "Error",
    "outOfRangeResult": "Error",
    "rounding": "None"
  },

  "changeSummary": "초기 Draft Template",
  "supersedesProfileVersion": null,
  "validationSummary": []
}
```

정의되지 않은 임의 필드나 Key를 추가하지 않는다.

---

## 공통 필드

### schemaVersion

EventBalanceProfile 데이터 구조의 Version이다. 현재 값은 `1`이다.

Key 목록, 필드 구조 또는 해석 규칙이 바뀌면 Schema Version을 올린다.

### profileId

Profile 계열을 식별하는 안정적인 ID다.

- 영문 소문자, 숫자와 밑줄 조합을 권장한다.
- 같은 밸런스 계열의 값 조정에는 동일한 ID를 유지한다.
- 난이도나 게임 모드에 따라 실제로 다른 매핑이 필요할 때만 별도 ID를 만든다.

### profileVersion

같은 profileId에서 승인 대상 값이 변경된 횟수를 나타내는 1 이상의 정수다.

다음 변경은 Version을 올린다.

- Threshold 값 변경
- Magnitude 값 변경
- Resolution Policy 변경
- State Domain 변경

설명 문장이나 오탈자만 바뀌고 데이터 의미가 같다면 올리지 않아도 된다.

### eventBlueprintSchemaVersion

이 Profile이 지원하는 EventBlueprint Schema Version이다.

Profile과 Blueprint의 Schema Version이 다르면 Key 변환을 수행하지 않는다.

### stateDomains

EventSystem이 정의한 수치 State 범위를 복제해 Profile 자체 검증에 사용한다.

- Fame은 `0 ~ 10`이다.
- Reputation은 `-3 ~ 3`이다.
- Money와 ResolvedEventCount는 0 이상이며 현재 최대값은 없다.
- Profile이 임의로 범위를 넓히거나 줄일 수 없다.

### changeSummary

현재 Version에서 바뀐 수치와 목적을 사람이 검토할 수 있도록 요약한다.

### supersedesProfileVersion

이 Version이 직접 대체하는 이전 Version이다. 첫 Version이면 null이다.

---

## Threshold 값

Threshold 값은 Event 또는 Epilogue Condition의 Min과 Max에 직접 들어가는 정수다.

```text
Blueprint minThresholdKey
→ Profile thresholds 값
→ 최종 Condition Min

Blueprint maxThresholdKey
→ Profile thresholds 값
→ 최종 Condition Max
```

### Fame

```text
Fame.Low
Fame.Moderate
Fame.High
```

- 모든 값은 0 이상 10 이하다.
- `Low < Moderate < High`여야 한다.
- Low는 낮은 인지도 범위의 경계, Moderate는 지역 인지도 경계, High는 높은 인지도 경계를 표현해야 한다.

### Reputation

```text
Reputation.Low
Reputation.Moderate
Reputation.High
```

- 모든 값은 -3 이상 3 이하다.
- `Low < Moderate < High`여야 한다.
- Low는 불신 또는 악평, Moderate는 중립이나 일정 신뢰, High는 강한 신뢰의 경계 역할을 해야 한다.

### Money

```text
Money.MinorCost
Money.StandardCost
Money.MajorCost
```

- 모든 값은 0보다 큰 정수다.
- `MinorCost < StandardCost < MajorCost`여야 한다.
- Choice가 비용을 지불할 수 있는지 확인하는 `moneyMin`으로 사용한다.

### ResolvedEventCount

```text
ResolvedEventCount.Early
ResolvedEventCount.Mid
ResolvedEventCount.Late
```

- 모든 값은 0 이상의 정수다.
- `Early < Mid < Late`여야 한다.
- Main Arc Finale 페이싱을 보조하는 경계이며 승급 예약 Turn을 의미하지 않는다.

---

## Magnitude 값

Magnitude 값은 방향을 적용하기 전의 양수 절대값이다.

### Fame과 Reputation

```text
Minor
Moderate
Major
```

- 모든 값은 0보다 큰 정수다.
- `Minor <= Moderate <= Major`여야 한다.
- 같은 값이 두 등급에 반복되면 Validated는 가능하지만 의미 구분이 약하다는 Warning을 기록한다.
- 각 값은 해당 State 전체 범위를 초과할 수 없다.

Direction 적용은 다음과 같다.

```text
Increase → +Magnitude
Decrease → -Magnitude
```

### Money Reward

```text
Money.MinorReward
Money.StandardReward
Money.MajorReward
```

- 모든 값은 0보다 큰 정수다.
- `MinorReward <= StandardReward <= MajorReward`여야 한다.
- 같은 값이 반복되면 Warning을 기록한다.
- `Gain` 방향과 함께 사용한다.

### Money Cost

```text
Money.MinorCost
Money.StandardCost
Money.MajorCost
```

- 모든 값은 0보다 큰 정수다.
- `MinorCost < StandardCost < MajorCost`여야 한다.
- `Spend` 방향과 함께 사용한다.
- 동일한 Cost Key의 Threshold 값과 Magnitude 값은 같아야 한다.

Money 적용 방향은 다음과 같다.

```text
Gain → +Magnitude
Spend → -Magnitude
```

---

## 동일 문자열 Key와 Usage

Threshold와 Magnitude는 별도 Map이다. 같은 문자열이 두 Map에 존재할 수 있으며 `requiredBalanceKeys.usage`가 어느 값을 사용할지 결정한다.

예:

```text
Fame.Moderate + Threshold
→ thresholds["Fame.Moderate"]

Fame.Moderate + Magnitude
→ magnitudes["Fame.Moderate"]
```

Money Cost는 지불 가능 조건과 실제 지출이 정확히 일치해야 하므로 양쪽 Map에서 같은 숫자를 사용한다.

하나의 Blueprint에서 같은 Key가 Threshold와 Magnitude로 모두 사용되면 `requiredBalanceKeys`에는 `(key, usage)` 조합별로 별도 항목이 있어야 한다.

---

## resolutionPolicy

```json
{
  "missingKey": "Error",
  "nullValue": "Error",
  "outOfRangeResult": "Error",
  "rounding": "None"
}
```

현재 지원 값은 위 조합 하나뿐이다.

### missingKey

Blueprint가 요구한 `(key, usage)`가 Profile에 없으면 Event 생성을 실패한다. 가까운 이름의 Key로 대체하지 않는다.

### nullValue

Draft의 null 값은 허용하지만 Event 생성에는 사용할 수 없다.

### outOfRangeResult

Effect 적용 결과가 EventSystem의 State 범위를 벗어날 수 있으면 Event 생성을 실패한다.

- Fame과 Reputation을 자동 Clamp하지 않는다.
- Money가 음수가 되도록 허용하지 않는다.
- EventGenerator가 임의로 Effect 값을 줄이지 않는다.
- Blueprint 또는 Event Condition 구조를 검토해 모든 가능한 Result를 범위 안으로 만들어야 한다.

### rounding

모든 값은 정수이므로 반올림하지 않는다.

---

## Key 변환 규칙

### Threshold

```text
event.conditions.fame.minThresholdKey
→ thresholds[key]
→ final conditions.fameMin

event.conditions.fame.maxThresholdKey
→ thresholds[key]
→ final conditions.fameMax
```

Reputation, Money, ResolvedEventCount와 Epilogue Condition도 같은 규칙을 사용한다.

### Magnitude

```text
fameChange Increase + Fame.Minor
→ +magnitudes["Fame.Minor"]

reputationChange Decrease + Reputation.Major
→ -magnitudes["Reputation.Major"]

moneyChange Gain + Money.StandardReward
→ +magnitudes["Money.StandardReward"]

moneyChange Spend + Money.StandardCost
→ -magnitudes["Money.StandardCost"]
```

변화 객체가 null이면 최종 변화량은 0이다.

---

## 범위 안전성

Profile 값 자체가 유효해도 Blueprint와 결합한 Result가 범위를 벗어날 수 있다.

모든 Event는 발생 가능한 State와 선택 가능한 Result의 조합에서 다음을 만족해야 한다.

```text
0 <= Fame + fameChange <= 10
-3 <= Reputation + reputationChange <= 3
0 <= Money + moneyChange
```

### Increase Result

Event가 수치 최대값에서도 발생할 수 있는데 Increase Result가 존재하면 범위 초과 가능성이 있다.

Blueprint의 Event Condition이 충분한 max 경계를 제공하지 않는다면 변환을 실패한다. EventGenerator가 조건을 자동 추가하지 않는다.

### Decrease Result

Event가 수치 최소값에서도 발생할 수 있는데 Decrease Result가 존재하면 같은 방식으로 실패한다.

### Money Spend

Spend Choice의 `moneyMinThresholdKey`와 실제 Cost Magnitude가 같은 금액을 보장해야 한다.

- 조건 값이 지출값보다 작으면 실패한다.
- 조건 값이 더 크면 데이터상 안전하지만 같은 Cost Key 사용 원칙을 위반하므로 실패한다.
- Automatic Result에서 Money를 Spend하려면 Event Condition이 지출 가능성을 보장해야 한다.

### 여러 Choice

Fame과 Reputation은 Choice Condition으로 지원되지 않는다. 하나의 Choice만 수치 범위를 벗어날 수 있어도 Event 전체 조건으로 안전성을 보장해야 한다.

범위 안전성을 확보하기 위해 다른 Choice의 의미를 바꾸거나 Result Effect를 조용히 제거하지 않는다.

---

## Validation Summary

```json
{
  "check": "ThresholdOrdering",
  "status": "Passed",
  "details": "모든 Threshold Domain이 정의된 순서를 만족한다."
}
```

`status`는 다음 값을 사용한다.

```text
Passed
Failed
NotApplicable
```

`Validated` 또는 `Approved` Profile에는 필수 검증 항목이 모두 Passed 또는 NotApplicable이어야 한다.

---

## 공통 검증 규칙

### Schema와 식별자

- schemaVersion은 1이다.
- profileId와 profileVersion 조합이 고유하다.
- profileVersion은 1 이상의 정수다.
- eventBlueprintSchemaVersion이 지원되는 값이다.
- Superseded가 아닌 동일 ID와 Version의 Profile이 중복되지 않는다.

### Key 완전성

- EventBlueprint 계약에 정의된 Threshold Key가 모두 `thresholds`에 정확히 한 번씩 존재한다.
- EventBlueprint 계약에 정의된 Magnitude Key가 모두 `magnitudes`에 정확히 한 번씩 존재한다.
- 임의 Key가 없다.
- 대소문자와 구분 문자가 계약과 정확히 일치한다.

### 값 형식

- Draft만 null 값을 허용한다.
- Validated와 Approved의 모든 값은 정수다.
- Magnitude와 Money Cost Threshold는 0보다 크다.
- Float, 문자열 숫자, NaN과 Infinity를 허용하지 않는다.

### Threshold 순서

```text
Fame.Low < Fame.Moderate < Fame.High
Reputation.Low < Reputation.Moderate < Reputation.High
Money.MinorCost < Money.StandardCost < Money.MajorCost
ResolvedEventCount.Early < ResolvedEventCount.Mid < ResolvedEventCount.Late
```

### Magnitude 순서

```text
Fame.Minor <= Fame.Moderate <= Fame.Major
Reputation.Minor <= Reputation.Moderate <= Reputation.Major
Money.MinorReward <= Money.StandardReward <= Money.MajorReward
Money.MinorCost < Money.StandardCost < Money.MajorCost
```

### Cost 일치

각 Money Cost Key의 Threshold 값과 Magnitude 값이 정확히 같아야 한다.

### State Domain

- State Domain이 EventSystem과 일치한다.
- Threshold가 해당 Domain 범위에 있다.
- Fame과 Reputation Magnitude가 전체 Domain 폭을 초과하지 않는다.
- Profile 적용 자체가 State 범위를 변경하거나 Clamp를 도입하지 않는다.

### Resolution Policy

- 모든 Policy가 계약의 현재 지원 값과 일치한다.
- Missing 또는 null Key를 자동 대체하는 정책이 없다.
- Out-of-range 값을 Clamp하거나 무시하지 않는다.

Failed Validation이 하나라도 있으면 `Validated` 또는 `Approved`로 변경할 수 없다.

---

## Blueprint 호환 검증

EventBlueprint의 `requiredBalanceKeys`를 사용해 필요한 값만 빠르게 검증할 수 있다.

각 항목은 `(key, usage)` 조합으로 해석한다.

```text
usage: Threshold
→ thresholds[key]

usage: Magnitude
→ magnitudes[key]
```

다음을 확인한다.

- Profile Status가 Approved다.
- Blueprint의 `balanceProfileRef`가 null이 아니면 ID와 Version이 일치한다.
- EventBlueprint Schema Version이 Profile 호환 Version과 일치한다.
- 모든 `(key, usage)` 조합이 존재한다.
- 모든 값이 정수다.
- Direction과 Key Domain이 일치한다.
- Profile 적용 후 모든 Event Result가 State 범위에 남는다.

Profile 자체 검증 통과는 개별 Blueprint 범위 안전성을 보장하지 않는다. 두 데이터를 결합한 검증이 별도로 필요하다.

---

## Profile 선택 규칙

하나의 Event 생성 묶음에는 하나의 Approved Profile ID와 Version만 사용한다.

- Blueprint에 `balanceProfileRef`가 있으면 해당 Profile을 사용한다.
- 참조가 null이면 EventGenerator 실행 요청이 Profile ID와 Version을 명시해야 한다.
- 서로 다른 Profile 값을 한 Event 묶음 안에서 혼합하지 않는다.
- 사용한 Profile ID와 Version을 제작 보고서에 기록한다.
- 런타임 Event JSON Schema에는 Profile Key를 남기지 않고 변환된 정수만 기록한다.

현재 프로토타입에서는 Event별 Override를 지원하지 않는다. 특수 Event라는 이유로 Profile 값을 우회하지 않는다.

---

## 변경과 영향 범위

### 숫자 변경

Threshold 또는 Magnitude 값이 바뀌면 다음을 수행한다.

- 같은 profileId의 새 profileVersion을 만든다.
- changeSummary에 변경 목적을 기록한다.
- 이전 Version을 Superseded 처리할지 결정한다.
- 해당 Key를 요구하는 모든 Event JSON을 다시 생성한다.
- 새 값으로 범위 안전성과 Choice 지불 가능성을 다시 검증한다.

EventContentPlan과 EventBlueprint는 Key 의미가 변하지 않았다면 다시 생성하지 않는다.

### Key 의미 변경

Key의 의미, 목록 또는 Domain이 바뀌면 단순 숫자 조정이 아니다.

- EventBlueprint Schema Version을 검토한다.
- EventBlueprintGenerator의 Key 선택 규칙을 검토한다.
- 기존 Blueprint의 의미가 유지되는지 마이그레이션한다.
- EventBalanceProfile Schema Version을 올릴 수 있다.

### 영향 분석

EventBlueprint의 `requiredBalanceKeys.consumerRefs`를 이용해 변경된 Key의 영향을 받는 Event, Choice Result와 Epilogue를 찾는다.

전체 Event를 무조건 다시 생성할 수도 있지만, 부분 재생성을 사용할 때는 모든 Consumer Ref가 최신인지 먼저 검증한다.

---

## 승인 절차

### Draft 생성

계약 Template의 모든 미결정 값을 null로 둘 수 있다. Draft는 Blueprint 작성과 Key Coverage 검토에 사용할 수 있지만 Event 수치 변환에는 사용할 수 없다.

### 값 입력

게임 밸런스 담당자가 State 범위, 예상 런 길이, Money 유입과 지출 구조를 기준으로 값을 입력한다.

Fame과 Reputation 수치만 보고 Money 값을 추론하거나, 서사적 표현만 보고 Major 값을 자동 승인하지 않는다.

### Validated

Schema, 완전성, 순서, Domain과 Policy 검증을 통과하면 Validated로 변경할 수 있다.

### Approved

게임 밸런스 담당자의 명시적 승인 이후에만 Approved로 변경한다. 승인되지 않은 LLM 제안 Profile을 최종 Event 생성에 사용하지 않는다.

---

## 금지 규칙

다음을 수행하지 않는다.

- 이벤트마다 Profile 값을 Override
- Blueprint의 서사적 이유를 보고 EventGenerator가 숫자 조정
- Missing Key를 비슷한 이름으로 대체
- null 값을 0으로 처리
- 범위 초과 Result를 자동 Clamp
- Money Spend 조건과 실제 지출 값 불일치
- Profile 숫자로 Event의 Choice 또는 Arc 구조 변경
- 정확한 승급 예약 Travel Turn을 Profile에 추가
- Battle 난이도나 전투 수치를 이 Profile에 포함
- Fame과 Reputation Domain을 Profile에서 변경

---

## 출력 및 보고

Profile 검토 시 다음을 명확히 보여야 한다.

- Profile ID, Version과 상태
- EventBlueprint Schema 호환 Version
- 모든 Threshold 값
- 모든 Magnitude 값
- 이전 Version과 달라진 값
- 순서 및 Domain 검증 결과
- 같은 값에 매핑된 의미 등급 Warning
- 영향받는 requiredBalanceKeys와 Consumer 수
- Event JSON 재생성 필요 여부

---

## 최종 목적

EventBalanceProfile의 목적은 LLM이 개별 Event마다 숫자를 임의로 정하는 것을 막고, 게임 밸런스가 승인한 하나의 수치 원본으로 모든 Event JSON을 일관되게 생성하는 것이다.

서사 구조와 Balance Key는 안정적으로 유지하면서 Profile Version만 변경해 반복적인 밸런스 테스트와 전체 또는 부분 Event 재생성을 가능하게 한다.
