# 정적 이벤트 시스템 명세

## 1. 목적

이 시스템은 개발 단계에서 LLM이 정적 이벤트 콘텐츠를 생성하고, Unity가 런타임에서 그 콘텐츠를 조합·실행할 수 있도록 하기 위한 **콘텐츠 데이터 명세와 출력 계약**이다.

런타임에는 LLM이 개입하지 않는다.

이 문서는 다음을 정의한다.

- 이벤트 생성기가 어떤 종류의 콘텐츠를 생성해야 하는가
- 생성된 콘텐츠가 어떤 구조와 의미를 가져야 하는가
- 콘텐츠 사이의 참조 관계
- 반드시 만족해야 하는 구조적 검증 규칙
- 생성기가 출력해야 하는 파일의 논리적 구성

이 문서는 다음을 정의하지 않는다.

- Fame, Reputation, Money 등의 실제 내부 수치
- 의미 레벨을 실제 수치로 변환하는 방식
- 전투 난이도, 보상량, 확률 등 밸런스 수치
- Unity 내부 C# 자료구조
- Unity의 구체적인 State Machine 또는 enum
- 이벤트 후보 선택 알고리즘의 세부 구현
- 데이터의 메모리 저장 방식과 역직렬화 방식

이벤트 생성기는 수치를 직접 설계하지 않는다. State 조건과 변화는 `낮음`, `높음`, `조금 증가`, `크게 감소`처럼 **게임적으로 의미가 있는 단계**로 표현하며, 실제 수치 해석은 Unity 또는 별도의 게임 설정이 담당한다.

---

## 2. 상위 진행 구조

한 런의 이벤트 콘텐츠는 다음 단위로 구성된다.

```text
Opening Event
↓
Main Arc
├─ Main Arc Node
│  └─ Main Arc Event
├─ Main Arc Transition
│  └─ Route Set
└─ Finale Node

Main Arc Node 사이 이동
↓
Travel Graph
↓
Segment
├─ Sub Arc: 0~1개
└─ 나머지 Travel Node: Standalone Event
```

Main Arc는 런 전체의 주요 갈등과 대분기, Finale를 담당한다.

Main Arc 주요 Location 사이의 이동 과정은 Travel Graph가 담당한다.

Sub Arc와 Standalone Event는 Main Arc 주요 Location 사이의 Travel Node에서 발생한다.

---

## 3. 책임 분리

### 3.1 이벤트 생성기

이벤트 생성기는 다음 콘텐츠를 생성한다.

- Opening Event
- Main Arc
- Main Arc Node
- Main Arc Event
- Main Arc Transition
- 각 Transition의 Route Set
- Sub Arc 콘텐츠
- Standalone Event 콘텐츠
- Epilogue 콘텐츠
- Event의 제목과 본문
- Choice와 Result
- Tag 변화
- Fame, Reputation, Money 등의 의미적 조건과 변화
- 전투가 발생한다는 사실
- 승급 기회가 발생한다는 사실

Main Arc의 Route Set을 생성할 때는 반드시 World Graph에 실제로 존재하는 지리적 연결만 사용한다.

이벤트 생성기는 다음을 결정하지 않는다.

- 이번 런에서 실제로 사용할 Main Arc
- 플레이어가 선택할 Segment
- 특정 Segment에 Sub Arc를 배치할지 여부
- 실제 전투 구성, 난이도와 보상 수치
- 실제 Battle ID
- 실제 승급 대상
- State의 내부 숫자 값
- 의미 레벨을 숫자로 변환하는 규칙
- Unity 내부 실행 순서와 자료구조

### 3.2 Unity

Unity는 생성된 콘텐츠를 이용해 다음 의미를 구현해야 한다.

- Main Arc 선택과 진행
- Main Arc Node의 구조적 연결 해석
- Route Set을 이용한 Travel Graph 구성
- 플레이어의 Segment 선택과 이동
- Segment에 Sub Arc를 배치할지 판단
- 모든 Travel Node에 유효한 Event가 존재하도록 콘텐츠 배치
- 현재 State와 Tags에 따른 Event/Choice 조건 판정
- 의미적 State 조건과 Effect를 실제 게임 값으로 해석
- Battle Binding 실행
- 승급 대상 처리
- Result 적용
- Chronicle 기록
- Finale 완료, 전투 패배, 콘텐츠 오류에 따른 런 종료 처리

구체적인 코드 구조와 런타임 처리 방식은 이 문서의 범위가 아니다.

### 3.3 수동 전투 제작

프로토타입에서는 이벤트 생성 후 사람이 실제 전투를 제작한다.

이벤트 생성기는 Result에 전투가 발생한다는 사실만 기록한다.

실제 전투 데이터는 안정적인 `Event ID + Choice ID` 조합을 통해 별도의 Binding 데이터와 연결한다.

---

## 4. 참조 데이터

이벤트 생성기는 최소한 다음 참조 데이터를 사용한다.

```text
Data/Facts.csv
Data/Knowledges.csv
Data/Traits.csv
Data/Locations.csv
```

### 4.1 Locations

`Locations.csv`는 World Graph의 고정 지리를 정의한다.

- 모든 Location은 고유한 ID를 가진다.
- Location 사이의 연결은 실제 지리적 이동 가능성을 뜻한다.
- Main Arc Node는 반드시 존재하는 Location을 참조한다.
- Route의 모든 이동은 World Graph에 실제로 존재하는 연결만 사용한다.
- 이벤트 생성기는 World Graph에 존재하지 않는 연결을 새로 만들 수 없다.

### 4.2 Tags

Tag는 다음 의미를 가진다.

- `Fact`: 과거에 어떤 일이 있었는가
- `Knowledge`: 용병단이 무엇을 알고 있는가
- `Trait`: 경험을 통해 용병단이 어떤 성향을 얻었는가

이벤트가 사용하는 Tag는 참조 데이터에 정의되어 있어야 한다.

Tags는 Main Arc Node 사이의 구조적 연결을 결정하지 않는다.

Tags는 주로 다음에 사용한다.

- 과거 선택과 사건 기록
- Event 또는 Choice 조건
- 같은 사건의 서사적 변주
- Sub Arc / Standalone Event 적합성
- Finale 및 Epilogue의 세부 결과 구분

---

## 5. State 표현 원칙

이벤트 생성기는 State의 실제 숫자를 출력하지 않는다.

예를 들어 Fame, Reputation, Money와 같은 State가 내부적으로 어떤 숫자 범위를 사용하는지는 Unity 또는 별도의 게임 설정이 결정한다.

이벤트 데이터는 State를 **의미 단계**로만 다룬다.

예:

```text
Fame이 낮음
Fame이 높음
Reputation이 나쁨
Money가 충분함
```

Effect 역시 의미 변화로 표현한다.

예:

```text
Fame 조금 증가
Reputation 크게 감소
Money 적게 소비
```

정확한 의미 단계 이름과 실제 수치 변환 규칙은 EventSystem이 소유하지 않는다. 생성 시 사용할 유효한 의미 단계와 변화 강도는 별도의 State Definition 또는 게임 설정에서 제공한다.

---

## 6. 출력 파일 계약

이벤트 생성기는 생성 결과를 파일로 출력해야 한다.

현재 논리적 출력 구조는 다음과 같다.

```text
Events/
├─ Generated/
│  ├─ MainArcs/
│  │  └─ <MainArcId>.json
│  ├─ SubArcs/
│  │  └─ <SubArcId>.json
│  ├─ StandaloneEvents.json
│  ├─ OpeningEvents.json
│  └─ EpilogueFragments.json
└─ Bindings/
   └─ BattleBindings.json
```

### Generated

LLM 이벤트 생성기가 생성하는 콘텐츠다.

### Bindings

생성된 Event와 실제 Unity 전투 데이터를 연결하는 수동 제작 데이터다.

`BattleBindings.json`은 이벤트 생성기가 생성하지 않는다.

파일 분리 방식은 콘텐츠 책임을 명확히 하기 위한 데이터 계약이다. Unity가 이 파일들을 어떤 Asset 또는 C# 객체로 변환하는지는 이 문서에서 정하지 않는다.

---

# Main Arc

## 7. Main Arc 정의

Main Arc는 한 런의 주요 갈등과 대분기, Finale를 정의하는 **Main Arc Node들의 DAG**다.

Main Arc는 다음 개념을 가진다.

```text
Main Arc
├─ startNodeId
├─ Nodes
│  ├─ Main Arc Node
│  └─ Finale Node
└─ Transitions
```

Main Arc의 실제 진행은 Node ID의 명시적 연결로 표현한다.

Tags를 이용해 "현재 조건을 만족하는 다음 Main Arc Event"를 검색하는 방식은 사용하지 않는다.

---

## 8. Main Arc Node

각 Main Arc Node는 다음 의미를 가진다.

```text
Main Arc Node
= Main Arc의 특정 진행 시점
+ Location 1개
+ Main Arc Event 1개
```

한 Node는 정확히 하나의 Location과 대응한다.

반대로 같은 Location을 여러 서로 다른 Main Arc Node가 사용할 수 있다.

예:

```text
Castle_FirstVisit
→ OrcCamp
→ Castle_Return
```

`Castle_FirstVisit`과 `Castle_Return`은 같은 Location을 사용하지만 서로 다른 Main Arc Node다.

같은 Main Arc Node로 다시 돌아가는 것은 허용하지 않는다.

### Finale Node

Finale Node는 Main Arc의 정상적인 종결을 나타내는 terminal node다.

- Main Arc는 하나 이상의 Finale Node를 가질 수 있다.
- 서로 다른 분기는 서로 다른 Finale Node에서 끝날 수 있다.
- Finale Node는 다음 Main Arc Node로 이어지지 않는다.
- Finale Event의 정상적인 Result 처리가 완료되면 런은 `Completed`로 종료된다.
- `Completed`는 반드시 플레이어에게 유리한 결말을 의미하지 않는다.

---

## 9. Main Arc Transition

Main Arc Transition은 한 Main Arc Node에서 다른 Main Arc Node로 이어지는 구조적 연결이다.

Transition은 최소한 다음 관계를 표현해야 한다.

```text
현재 Main Arc Node
+ 현재 Event의 Choice ID
→ 다음 Main Arc Node ID
+ 해당 이동 구간의 Route Set
```

Choice가 Automatic인 경우에도 안정적인 예약 Choice ID를 사용할 수 있다.

Main Arc Transition은 Tags에 의해 간접적으로 검색되지 않는다.

Choice 조건에는 Tags와 State가 사용될 수 있지만, 실제로 어느 Node로 진행하는지는 Transition의 명시적인 Node ID 연결로 결정한다.

---

## 10. Main Arc 대분기

Main Arc는 선택에 따라 이후 서사와 Finale가 완전히 달라지는 대분기를 허용한다.

예:

```text
                    → Kingdom Front → Orc Capital → Finale: 오크 멸망
Start → Castle ────┤
                    → Orc Camp → Human Capital → Finale: 왕국 멸망
                    → Borderlands → Independent Finale: 양측 배신
```

분기된 경로는 이후 다시 합류할 수도 있고, 서로 다른 Finale까지 독립적으로 이어질 수도 있다.

Main Arc의 모든 정상 진행은 구조적으로 유효해야 하며, 게임 기획이 요구하는 진행 길이와 승급 기회 보장 조건을 만족해야 한다.

이 문서는 그 최소 개수 자체를 하드코딩하지 않는다. 필요한 기준은 게임 기획 또는 생성 설정에서 제공한다.

---

# Route / Travel

## 11. Route

Route는 서로 연결된 두 Main Arc Node의 Location 사이를 잇는 World Graph 위의 하나의 완주 가능한 이동 경로다.

Route는 다음 규칙을 가진다.

- World Graph의 실제 연결만 사용한다.
- source Main Arc Node의 Location에서 시작한다.
- target Main Arc Node의 Location에 도달한다.
- 같은 Location 또는 Travel Node를 같은 Route 안에서 다시 방문하지 않는다.
- Route 자체는 순환하지 않는다.

Route는 플레이어가 그대로 선택해 끝까지 따라가는 고정 코스가 아니다.

Route의 목적은 Travel Graph를 만들기 위한 샘플을 제공하는 것이다.

---

## 12. Route Set

하나의 Main Arc Transition은 해당 두 Location 사이를 잇는 Route Set을 가진다.

World Graph가 복잡하면 두 Location 사이에 가능한 Route가 매우 많을 수 있다.

이벤트 생성기는 가능한 모든 Route를 저장하지 않고 일부 유효 Route를 미리 선정해 Route Set으로 출력한다.

현재 생성 기준은 **유효 Route 중 무작위 선정**이다.

Route의 위험도, 보상, 테마, 길이 등을 평가하는 정교한 선택 기준은 현재 EventSystem의 필수 규칙이 아니다.

---

## 13. Travel Graph

Travel Graph는 Route Set에 포함된 샘플 Route들을 겹쳐 만든 해당 Main Arc 구간의 실제 이동 선택 구조다.

플레이어는 샘플 Route 하나를 처음부터 선택하지 않는다.

Travel Graph의 이동 선택 지점에서 진행 방향을 선택하고, 그 선택들의 누적 결과가 실제로 지나온 `Actual Route`가 된다.

Actual Route는 Route Set의 개별 샘플 Route와 정확히 일치하지 않아도 된다.

Travel Graph는 진행 방향상 순환하지 않는 DAG여야 한다.

또한 다음 구조적 조건을 만족해야 한다.

- source Main Arc Location에서 target Main Arc Location에 도달할 수 있다.
- 플레이 가능한 경로에 진행 불가능한 dead end가 존재하지 않는다.
- World Graph에 존재하지 않는 이동을 포함하지 않는다.

개별 Route가 비순환이더라도 여러 Route를 합친 결과에 cycle이 생길 수 있으므로, 생성된 Route Set은 합성된 Travel Graph까지 검증할 수 있어야 한다.

---

## 14. Segment

Segment는 Travel Graph에서 플레이어가 하나의 이동 방향을 선택한 뒤, **다음 이동 선택이 발생하기 전까지 실제로 진행하게 되는 연속 Travel Node 구간**이다.

Segment는 정적으로 미리 분할된 그래프 조각이 아니다.

플레이어의 실제 이동 선택에 따라 런타임에 확정되는 진행 단위다.

합류점 자체는 Segment의 경계가 아니다.

합류 후 새로운 이동 선택이 없다면 같은 Segment가 계속된다.

예:

```text
         ┌→ 4 ─┐
1 → 2 → 3      → 6 → 7 → 8
         └→ 5 ─┘         ├→ 9
                         └→ 10
```

3에서 4 방향을 선택했다면 다음 이동 선택 지점인 8까지가 하나의 Segment가 될 수 있다.

```text
4 → 6 → 7 → 8
```

3에서 5 방향을 선택했다면 다음과 같다.

```text
5 → 6 → 7 → 8
```

---

# Travel Event

## 15. Travel Node 콘텐츠 원칙

Main Arc Node 사이의 모든 Travel Node에는 반드시 하나의 Event가 존재해야 한다.

빈 Travel Node는 허용하지 않는다.

Segment가 실제로 확정된 뒤 Unity는 해당 Segment에 Sub Arc를 배치할지 판단한다.

- 하나의 Segment에는 Sub Arc가 `0~1개` 배치될 수 있다.
- Sub Arc가 배치되지 않은 Travel Node는 모두 Standalone Event가 담당한다.
- Sub Arc가 일부 Travel Node만 사용한다면 나머지는 Standalone Event가 담당한다.

어떤 Event를 어느 시점에 실제로 선택해 배치하는지는 Unity의 구현 책임이다.

EventSystem은 모든 Travel Node가 결과적으로 유효한 Event 하나를 갖는다는 사실만 요구한다.

---

## 16. Sub Arc

Sub Arc는 하나의 Segment 안에서 시작하고 완결되는 짧은 선택적 사건선이다.

현재 원칙은 다음과 같다.

- 한 Segment에는 Sub Arc가 없거나 하나만 존재한다.
- 하나의 Sub Arc는 같은 Segment 안에서 완결되어야 한다.
- Sub Arc의 Event가 Segment의 연속된 모든 Node에서 발생할 필요는 없다.
- Sub Arc Event 사이에 Standalone Event가 발생할 수 있다.
- 여러 Segment 또는 다음 Main Arc 구간까지 이어지는 Long Sub Arc는 현재 사용하지 않는다.

Sub Arc가 특정 Segment에 완전히 배치 가능한지를 Unity가 판단할 수 있도록 **배치 호환성 정보가 필요하다.**

다만 그 호환성을 Location ID 목록, Location 속성, 별도 배치 태그 등 어떤 방식으로 표현할지는 아직 확정하지 않았다.

따라서 Sub Arc의 최종 배치 메타데이터 Schema는 별도 결정 사항으로 남긴다.

Sub Arc의 서사 이벤트 자체는 공통 Event 구조를 사용한다.

---

## 17. Standalone Event

Standalone Event는 Main Arc나 Sub Arc에 속하지 않는 독립적인 단일 사건이다.

Standalone Event는 다음 용도로 사용한다.

- Travel Node의 독립 사건
- Tags와 State에 따른 월드 변주
- 전투, 승급 또는 일반 보상 기회

Standalone Event는 Main Arc의 다음 Node ID를 직접 변경하지 않는다.

Standalone Event가 어떤 Location 또는 Travel Node에 적합한지 Unity가 판단할 수 있도록 배치 조건을 가져야 한다.

정확한 Location 호환 Schema는 Sub Arc와 함께 별도 배치 규칙에서 확정한다.

---

# Event 공통 데이터

## 18. Event 기본 구조

Main Arc Event, Sub Arc Event, Standalone Event, Opening Event는 동일한 핵심 Event 개념을 공유한다.

개념적 구조는 다음과 같다.

```json
{
  "id": "event_unique_id",
  "title": "이벤트 제목",
  "description": "플레이어에게 표시할 내용",
  "conditions": {
    "requiredTags": [],
    "forbiddenTags": [],
    "stateConditions": []
  },
  "resolution": {
    "type": "Choice",
    "choices": []
  }
}
```

### id

모든 Event에서 안정적이고 고유한 문자열 ID를 사용한다.

이 ID는 Battle Binding과 Chronicle 등 외부 참조의 기준이 된다.

### title / description

플레이어에게 표시할 사건의 제목과 본문이다.

### conditions

Event 또는 Choice가 사용 가능한지를 판정하기 위한 의미적 조건이다.

Main Arc Event는 현재 Main Arc Node가 해당 Event의 발생 자체를 보장하므로, Event-level 조건으로 Main Arc Event 자체가 사라지도록 만들지 않는 것을 원칙으로 한다.

---

## 19. Conditions

Conditions는 다음 종류의 의미를 표현할 수 있다.

### Tag 조건

```text
필요한 Tag
금지된 Tag
```

### State 조건

숫자 Threshold를 직접 출력하지 않는다.

예:

```text
Fame이 높음
Reputation이 낮음
Money가 충분함
```

JSON 표현은 의미 수준을 유지한다.

예:

```json
{
  "state": "Fame",
  "comparison": "AtLeast",
  "level": "High"
}
```

여기서 `High`가 실제 어떤 숫자를 의미하는지는 EventSystem이 정하지 않는다.

유효한 State 이름, 의미 레벨, 비교 방식은 별도의 State Definition에서 제공한다.

모든 조건은 별도 명시가 없는 한 AND 관계로 해석한다.

복잡한 OR 조건과 임의 수식은 현재 기본 EventSystem 범위에 포함하지 않는다.

---

## 20. Resolution

Event의 Resolution은 다음 두 형태를 지원한다.

```text
Choice
Automatic
```

### Choice

플레이어가 하나의 Choice를 선택한다.

각 Choice는 안정적인 문자열 ID를 가진다.

### Automatic

플레이어 선택 없이 하나의 Result를 실행한다.

Battle Binding 등 안정적인 외부 참조가 필요할 경우 예약 Choice ID를 사용한다.

---

## 21. Choice

개념적 Choice 구조는 다음과 같다.

```json
{
  "id": "choice_id",
  "text": "선택지 문구",
  "conditions": {
    "requiredTags": [],
    "forbiddenTags": [],
    "stateConditions": []
  },
  "result": {}
}
```

Choice 조건 역시 실제 숫자가 아닌 의미적 State 조건을 사용한다.

Choice Event가 실제로 발생했을 때 플레이어가 아무 Choice도 선택할 수 없는 상태가 되지 않도록 콘텐츠를 작성해야 한다.

Main Arc Event에서 구조적 진행을 만드는 Choice는 해당 Main Arc Node의 Transition과 Choice ID로 연결된다.

---

## 22. Result

각 Choice 또는 Automatic Resolution은 하나의 결정적인 Result를 가진다.

개념적 구조는 다음과 같다.

```json
{
  "text": "결과 설명",
  "battle": false,
  "promotion": false,
  "effects": {
    "addTags": [],
    "removeTags": [],
    "stateEffects": []
  }
}
```

### 결정성

같은 Event에서 같은 Choice를 선택하면 같은 Result가 실행된다.

Result 내부의 확률 분기나 랜덤 Effect는 현재 지원하지 않는다.

### battle

해당 Result에 전투가 포함되는지를 의미한다.

실제 전투 구성은 Event 데이터가 결정하지 않는다.

### promotion

해당 Result가 용병 승급 기회를 제공하는지를 의미한다.

실제 대상과 승급 가능 조건의 세부 구현은 Unity가 담당한다.

---

## 23. Effects

### Tag Effect

```text
Tag 추가
Tag 제거
```

같은 Result에서 같은 Tag를 동시에 추가하고 제거해서는 안 된다.

### State Effect

State 변화는 숫자가 아닌 의미로 표현한다.

예:

```json
{
  "state": "Reputation",
  "direction": "Decrease",
  "magnitude": "Large"
}
```

또는:

```json
{
  "state": "Money",
  "direction": "Decrease",
  "magnitude": "Small"
}
```

`Large`, `Small` 등의 실제 수치 해석은 별도 State Definition 또는 Unity 설정이 담당한다.

EventSystem은 구체적인 증가량과 감소량을 정의하지 않는다.

---

## 24. Battle Result 처리 원칙

Battle Result에는 다음 의미적 순서만 보장한다.

```text
Choice 또는 Automatic Result 확정
→ 전투 실행
→ 전투 승리
   → Result의 성공 Effect와 후속 처리
→ 전투 패배
   → 성공 Effect를 적용하지 않음
   → Run Failed
```

Unity가 내부적으로 Chronicle, State 객체, UI 등을 어떤 순서로 갱신하는지는 EventSystem에서 정의하지 않는다.

---

## 25. Promotion Result 처리 원칙

`promotion == true`는 승급 기회가 발생했다는 뜻이다.

Event 데이터는 실제 승급 대상의 개별 ID를 지정하지 않는다.

Unity는 게임의 승급 규칙에 따라 실제 가능한 대상을 처리한다.

Main Arc의 승급 기회 보장 횟수와 Main Arc 최소 길이는 게임 기획 또는 생성 설정에서 제공하며, EventSystem 자체에 숫자로 하드코딩하지 않는다.

---

# Battle Binding

## 26. Battle Binding

프로토타입에서 Battle Result는 사람이 제작한 실제 전투와 별도로 연결한다.

논리적 Binding 형식은 다음과 같다.

```json
[
  {
    "eventId": "orc_scout_encounter",
    "choiceId": "attack",
    "battleId": "OrcScout_01"
  }
]
```

규칙:

- `eventId`는 존재하는 Event를 참조한다.
- `choiceId`는 해당 Event의 실제 Choice 또는 Automatic 예약 ID를 참조한다.
- Battle Result에는 유효한 Binding이 존재해야 한다.
- 실제 `battleId`는 Unity의 전투 데이터에 존재해야 한다.
- 이벤트 생성기는 `battleId`를 결정하거나 `BattleBindings.json`을 생성하지 않는다.

---

# Opening / Chronicle / Epilogue

## 27. Opening Event

Opening은 Main Arc와 분리된 특수 Event다.

게임 시작부의 사건과 첫 전투 등을 담당할 수 있다.

Opening의 구체적인 런타임 Phase 구현은 EventSystem에서 정하지 않는다.

Opening이 완료된 뒤 Main Arc 진행이 시작될 수 있어야 한다.

Opening Event는 공통 Event 구조를 사용한다.

---

## 28. Chronicle

Unity는 플레이어가 실제로 경험한 사건과 선택을 기록할 수 있어야 한다.

Chronicle에는 최소한 다음 정보를 식별할 수 있어야 한다.

- 발생한 Event
- 선택한 Choice
- 적용된 Result
- 전투 패배 등 런 종료에 직접 영향을 준 결과

정확한 저장 자료구조는 Unity가 결정한다.

---

## 29. Epilogue

Epilogue 콘텐츠는 런 종료 시 실제로 도달한 Finale와 누적된 Tags/State, 완료한 사건을 이용해 후일담을 구성하기 위한 정적 텍스트 콘텐츠다.

Main Arc는 여러 Finale Node를 가질 수 있으므로 Epilogue는 단일 고정 결말을 전제로 하지 않는다.

`Completed`는 반드시 좋은 결말을 의미하지 않는다.

이벤트 생성기는 조건과 텍스트를 가진 Epilogue Fragment를 생성할 수 있다.

정확한 Fragment 분류 방식과 조립 알고리즘은 Unity 구현으로 남긴다.

---

# Validation

## 30. 공통 검증

생성 콘텐츠는 최소한 다음을 검증해야 한다.

- Event ID가 고유하다.
- 같은 Event 안의 Choice ID가 중복되지 않는다.
- 참조하는 Tag가 존재한다.
- 참조하는 Location이 존재한다.
- 의미적 State 조건과 Effect가 허용된 State Definition을 사용한다.
- Choice Event가 발생 가능한 상태에서 선택 가능한 Choice가 존재한다.
- Result는 결정적이다.
- Battle Result에 필요한 Battle Binding을 나중에 연결할 수 있는 안정적인 Event/Choice ID가 존재한다.

---

## 31. Main Arc 검증

Main Arc는 최소한 다음을 만족해야 한다.

- Main Arc Node ID가 Arc 내부에서 고유하다.
- `startNodeId`가 존재한다.
- 모든 Node가 유효한 Location을 참조한다.
- 모든 Normal Transition이 존재하는 source/target Node를 참조한다.
- Transition의 `choiceId`가 source Node의 Main Arc Event에 존재한다.
- Main Arc Node Graph는 DAG다.
- Finale Node는 구조적으로 terminal이다.
- 모든 정상 진행 경로가 어떤 Finale Node에 도달할 수 있다.
- 게임 기획 또는 생성 설정이 요구하는 Main Arc 길이 조건을 만족한다.
- 게임 기획 또는 생성 설정이 요구하는 승급 기회 조건을 모든 유효 경로에서 만족한다.

---

## 32. Route / Travel Graph 검증

각 Main Arc Transition의 Route Set은 최소한 다음을 만족해야 한다.

- Route가 World Graph의 실제 연결만 사용한다.
- 각 Route가 source Location에서 시작해 target Location에 도달한다.
- 각 Route 내부에 동일 Node 재방문이 없다.
- 각 Route는 비순환이다.
- Route Set을 합쳐 만든 Travel Graph는 DAG다.
- Travel Graph에서 target에 도달할 수 없는 플레이 가능한 dead end가 없다.

---

## 33. Travel Event 검증

런타임에서 선택된 실제 Segment는 다음 조건을 만족해야 한다.

- Segment의 모든 Travel Node에 Event가 존재한다.
- 하나의 Segment에 배치되는 Sub Arc 수는 현재 기획의 구조적 제한을 넘지 않는다.
- Sub Arc가 배치된 경우 같은 Segment 안에서 완결 가능해야 한다.
- Sub Arc가 사용하지 않는 모든 Travel Node에는 Standalone Event가 존재한다.

구체적인 Sub Arc/Standalone 위치 호환성 검증 방식은 배치 메타데이터 Schema가 확정된 뒤 추가한다.

---

# 현재 미결정 사항

## 34. 별도 결정이 필요한 항목

다음은 현재 상위 기획만으로 확정하지 않았으며 EventSystem이 임의로 결정하지 않는다.

### Sub Arc 배치 호환성 Schema

Sub Arc Event가 어떤 Travel Node에 배치 가능한지를 표현할 구체적인 데이터 형식.

가능한 방식의 예:

- 명시적인 Location 목록
- Location 종류/속성
- 별도 배치 Tag

현재는 특정 방식을 확정하지 않는다.

### Standalone Event 배치 호환성 Schema

Standalone Event가 어떤 Travel Node에 적합한지 표현하는 구체적인 데이터 형식.

### 같은 Location의 연속 Main Arc Node

서로 다른 Main Arc Node가 같은 Location을 연속해서 사용할 때 Travel 구간을 어떻게 표현할지 아직 정하지 않는다.

### Event 본문 변주 Schema

Tags와 State에 따라 같은 Event의 본문을 달리 표현할 수 있다는 개념은 사용할 수 있지만, 별도의 `presentationVariants`와 같은 구체 Schema는 아직 정하지 않는다.

### Sub Arc 내부 구조

현재 Sub Arc는 하나의 Segment 안에서 완결된다는 것만 확정했다.

Sub Arc 내부에서 Choice에 따른 별도 구조 분기를 데이터로 어떻게 표현할지는 추후 필요할 때 결정한다.

### Epilogue 조립 방식

어떤 Fragment를 몇 개 선택하고 어떤 순서로 조립할지는 별도 설계 사항이다.

---

## 35. 현재 지원하지 않는 기능

현재 기본 EventSystem 범위에는 다음을 포함하지 않는다.

```text
런타임 LLM 콘텐츠 생성
Main Arc의 Tag 기반 다음 Node 검색
Main Arc Node cycle
Travel Graph cycle
Route 내부 Node 재방문
Travel Node의 빈 Event
여러 Segment에 걸친 Long Sub Arc
Result 내부 확률 분기
랜덤 Effect
Event 데이터가 지정하는 실제 Battle ID
NPC별 호감도
세력별 개별 Reputation
EventSystem이 직접 결정하는 밸런스 숫자
Unity 내부 자료구조 및 State Machine 강제
```

새 기능이 필요해지면 개별 Event JSON에 임의 필드를 추가하지 않고 EventSystem 변경 사항으로 별도 논의한다.
