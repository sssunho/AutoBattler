# 정적 이벤트 시스템 명세

개정일: 2026-10-17

이 문서는 `plan.md`와 `World_Premise.md`의 현재 원정 구조를 기준으로 한다.

축복의 상세 규칙은 [BlessingSystem.md](BlessingSystem.md)를 따른다. 이 문서는 이벤트가 축복 조건에 쓰이는 태그·상태를 제공하고, 처리 완료 후 축복 시스템으로 연결하는 책임 경계를 정의한다.

---

## 1. 목적

이 시스템은 한 차례의 원정에서 모험가 파티가 실제로 지나온 장소, 겪은 사건과 선택을 정적인 이벤트 데이터로 구성하고 처리한다.

런 전체를 이끄는 필수 Main Arc는 사용하지 않는다.

플레이어가 선택한 목표와 실제 이동 경로가 원정의 큰 흐름을 만들고, 이벤트 시스템은 그 여정에서 발생하는 선택적 Arc, 독립 사건, 성장 사건과 목표 장소의 발견을 담당한다.

이벤트 데이터는 개발 단계에서 정적으로 생성한다. 런타임에는 LLM이 개입하지 않는다.

이벤트끼리는 다음 Event ID를 직접 지정하지 않는다. 이벤트 간 연결과 분기는 현재 State, Tags와 Arc Stage를 통해 느슨하게 표현한다.

---

## 2. 책임 분리

### 이벤트 생성기

이벤트 생성기는 다음 내용을 만든다.

- 이벤트의 제목과 본문
- 발생 가능한 Location과 State 조건
- 선택지 또는 자동 결과
- 선택 후 결과 문구
- 전투가 발생한다는 사실
- Tags, Fame, Reputation, Money의 변화
- 축복 조건에 쓰이는 태그의 추가를 포함한 이벤트 결과
- 해당 이벤트가 속한 Arc의 진행 변화
- 새로운 Goal을 공개하는 효과
- Opening, Introduction, Normal, Goal 등 이벤트의 역할

이벤트 생성기는 다음 내용을 결정하지 않는다.

- 런의 총 원정 시간
- Location 사이의 이동 시간과 시간 차감
- 흰밀물 종료 판정
- 이벤트 덱 구성과 출현 보장 시점
- 첫 방문·재방문 여부와 새로운 진입 경로 판정
- 목표 노드 선정과 초기 Goal 공개 개수
- 신별 축복 효과·티어와 축복 조건 사용 기록의 처리
- 실제 전투의 적 구성, 전장, 난이도와 보상
- Location 이동 처리
- 런 종료 화면과 통계 처리

### Unity 런타임

Unity는 다음 작업을 담당한다.

- 시작 Location 결정
- 이번 런의 초기 Goal 선정과 공개
- 이번 런에 포함할 Arc와 Standalone Event 선택
- 이벤트 덱 구성
- Opening Event 하나 선택
- Location 이동과 이동 시간 차감
- 방문 여부와 진입 경로 기록
- 새로운 탐험 계기 판정
- 현재 State를 기준으로 이벤트와 선택지 사용 가능 여부 판정
- 후보 이벤트 중 실제 발생 이벤트 선택
- Goal Event 우선 처리
- 전투 결과에 연결된 전투 데이터 호출
- 이벤트 결과 적용
- 관련 이벤트와 결과 처리 완료 후 축복 조건 판정 단계로 연결
- 축복 시스템을 통한 신별 축복·티어와 조건 사용 기록 관리
- Arc와 RunPhase 관리
- 목표 이벤트 성공, 전투 패배, 흰밀물에 따른 런 종료
- 실제 사건과 선택 결과의 연대기 기록

### 수동 전투 제작

프로토타입에서는 이벤트 생성 후 사람이 각 전투 결과에 맞는 전투를 직접 제작한다.

이벤트 생성기는 전투가 발생한다는 사실만 정의한다. 실제 전투는 안정적인 `Event ID + Choice ID` 조합에 수동으로 연결한다.

---

## 3. 참조 데이터

이벤트 시스템은 다음 데이터를 참조한다.

```text
Data/Facts.csv
Data/Knowledges.csv
Data/Traits.csv
Data/Locations.csv
Events/Generated/ArcCatalog.json
Events/Generated/GoalCatalog.json
```

- 이벤트가 사용하는 Tag는 Facts, Knowledges, Traits 중 하나에 정의되어야 한다.
- `CurrentLocation`과 이벤트의 `locations`에는 Locations에 존재하는 ID만 사용할 수 있다.
- Location 사이의 이동 가능 여부와 이동 시간은 Locations 또는 별도의 월드 그래프 데이터가 담당한다.
- 이동 시간은 이벤트 데이터에 작성하지 않는다.

---

## 4. RunState와 런타임 메타데이터

이벤트 시스템이 사용하는 주요 런 상태는 다음과 같다.

```text
RunState
{
    Fame;
    Reputation;
    Money;
    CurrentLocation;
    Tags;
    RunPhase;
    ArcStates;
    ResolvedEventCount;
    RevealedGoalIds;
}
```

Unity는 이와 별도로 원정 진행을 위해 다음 런타임 메타데이터를 관리한다.

```text
ExplorationRuntimeState
{
    VisitedLocations;
    UsedEntryEdges;
    SeenEligibleEventIdsByLocation;
    RemainingExpeditionTime;
}
```

`ExplorationRuntimeState`는 이벤트 생성기가 직접 변경하는 State가 아니다.

축복 시스템은 이와 별도로 신별 축복·티어와 원정 단위 조건 사용 기록을 관리한다. 조건에 쓰이는 Tag와 그 조건의 사용 여부는 서로 다른 정보다. 저장 필드와 데이터 형식은 아직 정하지 않으며, 개별 Event JSON에 축복 지급이나 조건 사용 기록 필드를 임의로 추가하지 않는다. 상세 규칙은 [BlessingSystem.md](BlessingSystem.md)를 따른다.

### Fame

```text
int
0 ~ 10
```

모험가 파티가 세상에 얼마나 알려져 있는지를 나타낸다.

### Reputation

```text
int
-3 ~ 3
```

모험가 파티의 전반적인 사회적 평판을 나타낸다. 세력별 평판과 NPC별 호감도는 사용하지 않는다.

### Money

```text
int
0 이상
```

모험가 파티가 보유한 골드다. Tags와 분리된 독립 State다.

현재 확정된 사용처는 주점에서의 모험가 영입이며, 추가 사용처는 별도 기획을 따른다.

### CurrentLocation

```text
int
```

현재 모험가 파티가 위치한 Location ID다.

### Tags

```text
string[]
```

파티 단위의 서사적 State다.

- Fact: 과거에 어떤 일이 있었는가
- Knowledge: 파티가 무엇을 알고 있는가
- Trait: 경험을 통해 파티가 어떤 성향을 얻었는가

세 분류는 런타임에서 모두 boolean Tag로 취급한다.

### ResolvedEventCount

```text
int
0 이상
```

전투 승리를 포함해 결과 처리가 끝난 이벤트 수다.

전투 패배로 게임오버가 된 이벤트는 결과가 완료되지 않았으므로 증가시키지 않는다.

### RevealedGoalIds

```text
string[]
```

현재 플레이어에게 공개된 Goal ID 목록이다.

- 런 시작 시 Unity가 초기 Goal을 넣는다.
- 이벤트 Result의 `revealGoalIds` 효과로 Goal을 추가할 수 있다.
- 이미 공개된 Goal을 다시 공개하는 것은 상태 변화가 없는 no-op으로 처리할 수 있다.

### VisitedLocations

해당 런에서 한 번 이상 도착한 Location ID 집합이다.

이 값은 첫 방문 판정에 사용하며 이벤트 조건으로 직접 노출하지 않는다.

### UsedEntryEdges

해당 런에서 특정 Location으로 진입할 때 이미 사용한 방향성 있는 진입 연결을 기록한다.

예:

```text
A -> B
```

를 사용했다면 `B`에 `A`에서 진입한 기록이 생긴다.

반대 방향인:

```text
B -> A
```

는 `A`에 대한 별도의 진입 기록이다.

### SeenEligibleEventIdsByLocation

각 Location에서 이전 탐험 기회에 이미 발생 가능 후보로 확인된 정규 이벤트 ID들을 기록한다.

이 메타데이터는 같은 State 변화 하나를 이용해 해당 장소를 반복 왕복하며 새 이벤트를 하나씩 소진하는 것을 막기 위해 사용한다.

---

## 5. RunPhase

```text
Opening
Normal
Ended
```

- `Opening`: 시작 Location에서 필수 Opening Event를 처리하는 단계
- `Normal`: 일반 탐험, Arc, Standalone Event와 Goal 탐색이 이루어지는 단계
- `Ended`: 원정 결과가 확정되어 더 이상 정규 이벤트를 진행하지 않는 단계

`FinaleReady`는 사용하지 않는다.

Goal이 공개되거나 Goal Location을 향하고 있다는 이유만으로 별도 Phase로 전환하지 않는다.

한 번에 하나의 RunPhase만 활성화한다.

---

## 6. ArcState

```text
ArcState
{
    Stage;
    Status;
}
```

### Status

```text
Pending
Active
Completed
```

- `Pending`: 이번 런에 포함됐지만 아직 도입 이벤트를 경험하지 않은 Arc
- `Active`: 현재 진행 중인 Arc
- `Completed`: 유리하거나 불리한 것과 관계없이 하나의 결말에 도달한 Arc

`Completed`는 성공만 의미하지 않는다. 목표 실패, 불리한 결말, 거절로 사건군이 끝난 경우도 모두 `Completed`다. 구체적인 결말은 Tags와 State로 구분한다.

Arc는 `Pending`으로 시작하며, Introduction Event 결과로 `Active` 또는 `Completed`가 된다.

`Completed`는 종결 상태이며 다시 활성화하지 않는다.

원정이 종료될 때 `Pending`이나 `Active`인 Arc를 `Abandoned` 또는 `Completed`로 강제 변경하지 않는다. 당시 상태 그대로 연대기에 기록한다.

Stage는 Arc의 정적 진행 단계 이름이다. Event ID를 의미하지 않는다.

---

## 7. 런의 이벤트 단위

한 런은 다음 콘텐츠로 구성한다.

```text
한 런
├─ Opening Event: 정확히 1개
├─ Arc: 0개 이상
├─ 일반 Standalone Event: 0개 이상
└─ Goal Event: 공개된 Goal마다 대응하는 특수 이벤트
```

### Opening Event

Opening Event는 Arc와 분리된 특수 이벤트다.

- `arcId`와 `arcStage`는 `null`이다.
- `goalId`는 `null`이다.
- `eventType`은 `Opening`이다.
- 모든 런에 정확히 하나 포함한다.
- 시작 Location에서 다른 후보와 경쟁하지 않고 반드시 발생한다.
- Arc State를 시작하거나 변경하지 않는다.
- 결과 처리 완료 후 Unity가 `RunPhase`를 `Normal`로 전환하고 월드맵, 초기 Goal과 남은 원정 시간을 공개한다.
- Opening과 시작 Location 내부 사건에는 원정 시간을 차감하지 않는다.

### Arc

Arc는 시작, 진행과 결말을 가진 선택적 퀘스트 라인이다.

- 한 런에 0개 이상 포함할 수 있다.
- Main/Sub 구분을 사용하지 않는다.
- 여러 이벤트로 구성되지만 고정된 단일 체인일 필요는 없다.
- 현재 Stage와 State에 따라 일부 중간 이벤트를 건너뛸 수 있다.
- 결말의 좋고 나쁨과 관계없이 종결되면 `Completed`다.
- Arc 완료는 원정 성공이나 종료 조건이 아니다.
- Goal 공개에 관여할 수 있지만 모든 Arc를 Goal에 종속시키지 않는다.

### Standalone Event

일반 Standalone Event는 어떤 Arc에도 속하지 않는 독립 사건이다.

- `arcId`와 `arcStage`는 `null`이다.
- Tags, Fame, Reputation과 Money를 변경할 수 있다.
- Goal을 공개할 수 있다.
- 다른 Arc의 Stage나 Status를 직접 변경하지 않는다.

### Goal Event

Goal Event는 목표 장소의 실제 발견을 처리하는 특수 이벤트다.

- `eventType`은 `Goal`이다.
- `goalId`를 정확히 하나 가진다.
- `arcId`와 `arcStage`는 `null`이다.
- 일반 이벤트 덱에 포함하지 않는다.
- 공개된 Goal의 Location에 도착하면 일반 이벤트보다 우선하여 발생한다.
- 일반 이벤트 후보 추첨과 경쟁하지 않는다.
- 전투를 포함할 수 있지만 모든 Goal Event가 전투를 가져야 하는 것은 아니다.
- Goal Event의 Result 처리가 끝까지 성공적으로 완료되면 해당 원정은 성공한다.
- 전투 패배 등으로 Result 처리가 중단되면 Goal 성공으로 처리하지 않는다.

현재 명세에서 Goal Event는 전투 패배가 아닌 별도의 비전투 실패·후퇴 결과를 지원하지 않는다. 그러한 분기가 필요해지면 Goal 결과 스키마를 별도로 확장한다.

---

## 8. Arc Catalog

`Events/Generated/ArcCatalog.json`은 제작된 Arc의 정적 정보를 보관한다.

```json
{
  "arcs": [
    {
      "id": "refugee_caravan",
      "initialStage": "Introduction"
    }
  ]
}
```

- `id`는 모든 Arc에서 고유해야 한다.
- `initialStage`는 해당 Arc의 초기 Stage다.
- Catalog는 다음 또는 이전 Event ID를 지정하지 않는다.
- Finale Stage와 Finale Location은 사용하지 않는다.

---

## 9. Goal Catalog

`Events/Generated/GoalCatalog.json`은 원정 목표 후보의 정적 정보를 보관한다.

```json
{
  "goals": [
    {
      "id": "silent_crater",
      "locationId": 120,
      "eventId": "goal_silent_crater"
    },
    {
      "id": "glass_marsh",
      "locationId": 184,
      "eventId": "goal_glass_marsh"
    }
  ]
}
```

- Goal ID는 전체 Goal에서 고유해야 한다.
- `locationId`는 Locations에 존재해야 한다.
- `eventId`는 정확히 하나의 `Goal` Event를 가리켜야 한다.
- 하나의 Goal은 하나의 Goal Event와 연결한다.
- 이번 런의 초기 Goal 선정과 공개 개수는 Unity 런타임이 담당한다.
- 진행 중 이벤트가 `revealGoalIds`를 통해 추가 Goal을 공개할 수 있다.

미공개 Goal Location을 먼저 방문했을 때의 처리는 현재 Plan에서 미정이므로 이 명세에서도 확정하지 않는다.

---

## 10. 런 시작

Unity는 런을 시작할 때 다음 작업을 수행한다.

1. 시작 Location을 결정한다.
2. 이번 런의 초기 Goal들을 선정하고 `RevealedGoalIds`에 등록한다.
3. 이번 런에 포함할 Arc와 일반 Standalone Event를 선택한다.
4. Opening Event 하나를 선택한다.
5. 일반 이벤트 덱을 구성한다.
6. `RunPhase`를 `Opening`으로 설정한다.
7. 선택된 Arc를 `Pending`과 초기 Stage로 설정한다.
8. `ResolvedEventCount`를 0으로 설정한다.
9. 방문·진입 경로·후보 기록 메타데이터를 초기화한다.
10. 신별 축복·티어와 축복 조건 사용 기록을 이번 원정의 초기 상태로 설정한다.
11. 시작 Location에서 Opening Event를 발생시킨다.

시작 Location은 Locations에 존재해야 한다.

Opening Event가 정상적으로 끝나면 `RunPhase`를 `Normal`로 전환하고 월드맵, 초기 Goal과 남은 원정 시간을 공개한다.

Opening Event에서 전투 패배가 발생하면 즉시 게임오버로 종료할 수 있다.

시작 Location의 Opening은 해당 장소의 첫 방문 콘텐츠 역할을 하므로, Opening 직후 같은 Location에서 추가 정규 첫 방문 이벤트를 강제로 발생시키지 않는다.

---

## 11. Location 이동과 도착 처리

원정 시간은 Location 사이 이동에서만 소모한다.

이벤트 시스템은 시간 수치 자체를 변경하지 않으며, Unity의 이동 시스템이 이동 시간 차감과 도착을 처리한다.

기본 흐름은 다음과 같다.

```text
이동 시작
→ 이동 시간 차감
→ 목적지 Location 도착
→ Goal Event 우선 여부 판정
→ 정규 탐험 기회 판정
→ 발생 이벤트가 있으면 처리
→ 원정 결과가 아직 미확정이면 시간 소진 판정
→ 계속 또는 흰밀물 종료
```

- 노드 내부의 이벤트, 전투, 조사, 대화, 거래, 휴식과 회복은 원정 시간을 소비하지 않는다.
- 같은 길을 다시 이동하거나 역행해도 이동 시간은 다시 소비한다.
- 이벤트 Effect는 `CurrentLocation`을 직접 변경하지 않는다.
- 원정 시간은 이벤트 조건이나 Result Effect로 변경하지 않는다.

### 도착 후 종료 판정 순서

이동으로 남은 시간이 0 이하가 되었더라도 목적지 도착 직후 즉시 원정을 끝내지 않는다.

1. 목적지 도착 처리를 시작한다.
2. Goal Event 또는 정규 이벤트가 있다면 해당 이벤트의 결과 처리를 끝낸다.
3. 전투 패배가 발생했다면 즉시 게임오버가 확정된다.
4. Goal Event를 성공적으로 완료했다면 원정 성공이 확정된다.
5. 아직 원정 결과가 확정되지 않았다면 마지막에 남은 시간을 확인한다.
6. 시간이 소진되었다면 흰밀물에 따른 목표 미달성 종료를 적용한다.
7. 시간이 남아 있다면 다음 이동을 허용한다.

따라서 마지막 이동으로 시간이 소진되어도 해당 Location의 이벤트는 정상적으로 경험할 수 있다.

---

## 12. 탐험 기회와 재방문 규칙

정규 이벤트 발생 여부는 단순히 Location에 도착했다는 이유만으로 매번 다시 추첨하지 않는다.

정규 이벤트를 판정하는 **새로운 탐험 계기**는 다음 세 종류다.

```text
FirstVisit
NewEntryEdge
NewCondition
```

### FirstVisit

해당 런에서 처음 방문한 일반 Location이다.

- 정규 이벤트가 반드시 1개 발생해야 한다.
- 발생 가능한 후보가 0개라면 콘텐츠 구성 오류다.
- 후보가 여러 개라면 그중 하나만 발생한다.
- 한 번의 첫 방문에서 이벤트를 연속으로 여러 개 발생시키지 않는다.

각 일반 Location에는 첫 방문을 보장할 수 있도록 최소 하나의 조건 없는 Standalone 후보를 준비한다.

첫 방문 보장용 후보는 가능한 한 해당 Location만을 `locations`로 가져, 다른 장소에서 먼저 소비되어 첫 방문 보장이 깨지지 않게 한다.

Opening Location은 Opening Event가 첫 방문 콘텐츠를 담당한다.

공개된 Goal Location은 Goal Event가 첫 방문 콘텐츠를 담당한다.

### NewEntryEdge

이미 방문한 Location에 이전에 사용하지 않은 방향의 진입 경로로 들어온 경우다.

예:

```text
Village -> Forest   // Forest에 Village에서 첫 진입
River -> Forest     // Forest에 River에서 첫 진입: 새로운 진입 경로
Village -> Forest   // 이미 사용한 진입 경로: 새 계기 아님
```

- 새 진입 경로가 생기면 정규 이벤트 기회를 최대 1회 얻는다.
- 현재 후보가 없다면 이벤트 없이 이동을 계속한다.
- 새 진입 경로 하나를 이용해 같은 후보들을 반복해서 소진할 수 없다.

### NewCondition

이미 방문한 Location에서 새로운 State, Tag 또는 Arc Stage 변화 때문에 이전에는 발생 가능하지 않았던 이벤트가 새롭게 활성화된 경우다.

예:

```text
신전 첫 방문
→ 열리지 않는 문을 확인

폐광에서 Knowledge 획득
→ 신전의 특정 이벤트 조건 충족

신전 재방문
→ 새롭게 활성화된 이벤트 발생 가능
```

Unity는 Location별 `SeenEligibleEventIdsByLocation`을 이용해 어떤 이벤트가 과거 탐험 기회에서 이미 후보였는지 기록한다.

현재 후보 중 과거에 해당 Location에서 후보가 된 적 없는 이벤트가 있을 때 `NewCondition`이 성립한다.

- `NewCondition`만으로 발생한 탐험 기회에서는 새롭게 활성화된 후보들 가운데 최대 1개만 발생한다.
- 같은 State 변화로 여러 이벤트가 동시에 열려도 이후 변화 없이 반복 왕복해 나머지를 하나씩 소진할 수 없다.
- 탐험 기회가 처리될 때 현재 후보 집합을 `SeenEligibleEventIdsByLocation`에 기록한다.

### 여러 계기가 동시에 성립한 경우

예를 들어 새로운 진입 경로로 들어오는 동시에 새로운 조건 이벤트도 열려 있을 수 있다.

이 경우에도 해당 Location 도착에서 정규 이벤트는 최대 1개만 발생한다.

`NewEntryEdge`가 성립하면 현재 발생 가능한 전체 정규 후보 중 하나를 선택할 수 있다.

`NewCondition`만 성립한 경우에는 새롭게 활성화된 후보 중 하나를 선택한다.

### 일반 재방문

다음 조건을 모두 만족하는 재방문에서는 정규 이벤트가 발생하지 않는다.

- 이미 방문한 Location이다.
- 사용한 적 있는 진입 경로로 들어왔다.
- 새롭게 활성화된 이벤트가 없다.

따라서 같은 두 Location을 반복 왕복해서 일반 이벤트만 소진하는 플레이는 지원하지 않는다.

### 이벤트 결과 직후 연속 발생 금지

한 이벤트의 결과로 새로운 Tag나 Arc Stage가 생겨 같은 Location에서 다른 이벤트가 즉시 활성화되더라도 그 자리에서 연속 발생시키지 않는다.

새롭게 활성화된 사건은 이후 해당 Location을 다시 방문했을 때 `NewCondition` 규칙으로 판정한다.

축복 판정·획득은 정규 이벤트를 추가 추첨하는 절차가 아니라 별도의 후처리 단계다. 이벤트 처리 완료 후의 축복 연결은 20절과 [BlessingSystem.md](BlessingSystem.md)를 따른다.

---

## 13. 정규 이벤트 후보 판정

Goal Event와 Opening Event를 제외한 정규 이벤트는 일반 이벤트 덱에서 판정한다.

### Arc Event

Arc 이벤트는 다음을 모두 만족해야 한다.

- 현재 Location이 `conditions.locations`에 포함된다.
- 현재 Arc Stage와 `arcStage`가 일치한다.
- Arc가 종결 상태가 아니다.
- `eventType == Introduction`이면 해당 Arc가 `Pending`이다.
- 그 밖의 Arc 이벤트라면 해당 Arc가 `Active`다.
- 이벤트의 모든 State 조건을 만족한다.
- Choice 이벤트라면 선택 가능한 Choice가 최소 하나 존재한다.
- Battle Result가 가능한 선택지에는 유효한 전투 연결이 존재한다.

### Standalone Event

Standalone Event는 다음을 모두 만족해야 한다.

- 현재 Location이 `conditions.locations`에 포함된다.
- 이벤트의 모든 State 조건을 만족한다.
- Choice 이벤트라면 선택 가능한 Choice가 최소 하나 존재한다.
- Battle Result가 가능한 선택지에는 유효한 전투 연결이 존재한다.

### 후보 선택

- 한 탐험 기회에서 정규 이벤트는 최대 1개 발생한다.
- 후보가 여러 개라면 가중치나 우선순위를 사용하지 않고 하나를 무작위로 선택한다.
- Goal Event와 Opening Event는 이 무작위 후보 선택에 참여하지 않는다.

---

## 14. 이벤트 덱

Unity는 다음 콘텐츠로 일반 이벤트 덱을 구성한다.

- 선택된 Arc의 모든 Introduction/Normal Event
- 이번 런에 선택된 일반 Standalone Event

다음 이벤트는 일반 이벤트 덱에 포함하지 않는다.

- Opening Event
- Goal Event

덱은 다음 규칙을 따른다.

- 실제로 정상 처리된 정규 이벤트는 결과 처리 완료 후 덱에서 제거한다.
- 전투 패배로 게임오버가 된 이벤트는 이후 진행이 없으므로 제거 여부가 결과에 영향을 주지 않는다.
- 조건을 충족하지 못한 이벤트는 덱에서 제거하지 않는다.
- 여러 Location을 가진 이벤트도 한 번 발생하면 덱 전체에서 제거한다.
- 한 런에서 같은 정규 이벤트를 반복하지 않는다.
- `Completed` Arc의 남은 이벤트는 후보에서 제외한다.
- 반복 방지만을 위한 Tag를 만들지 않는다.

**일반 이벤트 덱 고갈은 원정 종료 조건이 아니다.**

사건이 남지 않아도 플레이어는 월드맵을 이동하고 Goal을 향할 수 있다.

단, 아직 첫 방문하지 않은 일반 Location에 반드시 이벤트가 발생해야 하는 구조와 덱 구성이 모순되지 않도록 사전 검증한다.

---

## 15. Goal Event와 원정 종료

### Goal Event 발생

현재 Location이 `RevealedGoalIds`에 포함된 Goal의 `locationId`와 일치하면 해당 Goal Event를 일반 이벤트보다 먼저 처리한다.

Goal Event는 일반 이벤트 덱에서 추첨하지 않는다.

Goal Event 처리 중에는 같은 도착에서 정규 이벤트를 추가로 발생시키지 않는다.

### Goal 성공

Goal Event의 Result가 전투 패배 없이 끝까지 정상 처리되면:

1. 해당 Goal Event와 결과를 연대기에 기록한다.
2. 원정 결과를 `GoalSuccess`로 확정한다.
3. `RunPhase`를 `Ended`로 변경한다.
4. 정규 원정을 종료한다.

Goal Event 성공 이후의 선택적 자유 탐사는 현재 EventSystem의 기본 범위에 포함하지 않는다.

### 전투 패배

어떤 이벤트에서든 전투에 패배하면:

1. 다른 Result Effect를 적용하지 않는다.
2. 해당 이벤트와 선택, 전투 패배 사실을 연대기에 기록할 수 있다.
3. 원정 결과를 `BattleDefeat`로 확정한다.
4. `RunPhase`를 `Ended`로 변경한다.

### 흰밀물 종료

Location 도착 이벤트 처리 이후에도 원정 결과가 확정되지 않았고 남은 원정 시간이 소진되었다면:

1. 원정 결과를 `TimeExpired`로 확정한다.
2. `RunPhase`를 `Ended`로 변경한다.
3. 흰밀물에 따른 목표 미달성 원정으로 종료한다.

이때 `Pending` 또는 `Active` Arc의 Status를 변경하지 않는다.

### 정상 종료 결과

```text
GoalSuccess
BattleDefeat
TimeExpired
```

이벤트 덱 고갈은 종료 결과가 아니다.

---

## 16. Event Schema

모든 이벤트는 JSON으로 정의한다.

### Choice 이벤트 예시

```json
{
  "id": "refugee_caravan_request",
  "title": "길가의 행렬",
  "description": "짐을 잔뜩 실은 행렬이 길을 막고 있다.",
  "arcId": "refugee_caravan",
  "goalId": null,
  "eventType": "Introduction",
  "arcStage": "Introduction",

  "conditions": {
    "requiredTags": [],
    "forbiddenTags": [],
    "fameMin": null,
    "fameMax": null,
    "reputationMin": null,
    "reputationMax": null,
    "moneyMin": null,
    "moneyMax": null,
    "resolvedEventCountMin": null,
    "resolvedEventCountMax": null,
    "locations": [12]
  },

  "resolution": {
    "type": "Choice",
    "choices": [
      {
        "id": "help",
        "text": "행렬을 돕는다.",
        "conditions": {
          "requiredTags": [],
          "forbiddenTags": [],
          "moneyMin": null
        },
        "result": {
          "text": "모험가들은 행렬과 동행하기로 했다.",
          "battle": false,
          "effects": {
            "addTags": ["helped_refugees"],
            "removeTags": [],
            "fameChange": 0,
            "reputationChange": 1,
            "moneyChange": 0,
            "revealGoalIds": [],
            "arcTransition": {
              "arcId": "refugee_caravan",
              "stage": "RoadTrouble",
              "status": "Active"
            }
          }
        }
      }
    ]
  }
}
```

### Automatic Standalone 이벤트 예시

```json
{
  "id": "old_watchtower_view",
  "title": "무너진 감시탑",
  "description": "오래된 감시탑 너머로 낯선 빛이 보인다.",
  "arcId": null,
  "goalId": null,
  "eventType": "Normal",
  "arcStage": null,

  "conditions": {
    "requiredTags": [],
    "forbiddenTags": [],
    "fameMin": null,
    "fameMax": null,
    "reputationMin": null,
    "reputationMax": null,
    "moneyMin": null,
    "moneyMax": null,
    "resolvedEventCountMin": null,
    "resolvedEventCountMax": null,
    "locations": [31]
  },

  "resolution": {
    "type": "Automatic",
    "result": {
      "text": "북동쪽 산허리에 전에 없던 빛줄기가 보였다.",
      "battle": false,
      "effects": {
        "addTags": [],
        "removeTags": [],
        "fameChange": 0,
        "reputationChange": 0,
        "moneyChange": 0,
        "revealGoalIds": ["silent_crater"],
        "arcTransition": null
      }
    }
  }
}
```

### Goal Event 예시

```json
{
  "id": "goal_silent_crater",
  "title": "소리가 사라지는 분지",
  "description": "고개를 넘자 모든 소리가 끊긴 거대한 분지가 모습을 드러낸다.",
  "arcId": null,
  "goalId": "silent_crater",
  "eventType": "Goal",
  "arcStage": null,

  "conditions": {
    "requiredTags": [],
    "forbiddenTags": [],
    "fameMin": null,
    "fameMax": null,
    "reputationMin": null,
    "reputationMax": null,
    "moneyMin": null,
    "moneyMax": null,
    "resolvedEventCountMin": null,
    "resolvedEventCountMax": null,
    "locations": [120]
  },

  "resolution": {
    "type": "Automatic",
    "result": {
      "text": "모험가들은 아무 기록에도 없던 분지의 모습을 처음으로 남겼다.",
      "battle": false,
      "effects": {
        "addTags": ["discovered_silent_crater"],
        "removeTags": [],
        "fameChange": 1,
        "reputationChange": 0,
        "moneyChange": 0,
        "revealGoalIds": [],
        "arcTransition": null
      }
    }
  }
}
```

---

## 17. Event 기본 필드

### id

모든 이벤트에서 고유하고 변경되지 않는 문자열 ID다.

### arcId

- Arc 이벤트는 Catalog에 존재하는 `arcId`를 사용한다.
- Opening, Standalone, Goal Event는 `null`이다.
- Main/Sub 타입 구분은 사용하지 않는다.

### goalId

- Goal Event만 유효한 Goal ID를 가진다.
- Opening, Arc, Standalone Event는 `null`이다.

### eventType

```text
Opening
Introduction
Normal
Goal
```

- `Opening`: 시작 Location의 특수 이벤트
- `Introduction`: Pending Arc의 도입 이벤트
- `Normal`: Active Arc 진행 이벤트 또는 일반 Standalone Event
- `Goal`: Goal Location의 발견을 처리하는 특수 이벤트

`Finale`은 사용하지 않는다.

### arcStage

Arc 이벤트가 요구하는 현재 Stage다.

같은 Stage에 여러 이벤트가 존재할 수 있으며 Stage는 다음 Event ID를 뜻하지 않는다.

Opening, Standalone, Goal Event는 `null`이다.

---

## 18. Conditions

모든 조건은 AND 관계다. `null` 또는 빈 배열은 해당 조건을 검사하지 않는다는 뜻이다.

### 이벤트 조건

```text
requiredTags
forbiddenTags
fameMin / fameMax
reputationMin / reputationMax
moneyMin / moneyMax
resolvedEventCountMin / resolvedEventCountMax
locations
```

- `requiredTags`: 모두 현재 Tags에 존재해야 한다.
- `forbiddenTags`: 하나라도 현재 Tags에 존재하면 안 된다.
- `fameMin / fameMax`: Fame 범위
- `reputationMin / reputationMax`: Reputation 범위
- `moneyMin / moneyMax`: Money 범위
- `resolvedEventCountMin / resolvedEventCountMax`: 해결한 이벤트 수 범위
- `locations`: 발생 가능한 Location ID 목록

`locations`는 비어 있을 수 없다.

Goal Event는 Goal Catalog의 Location과 일치하는 Location을 가져야 한다.

Opening Event는 시작 Location에서 강제로 발생하므로 일반 후보 선택과 경쟁하지 않는다.

Goal Event 역시 공개된 Goal Location에서 강제로 발생하므로 일반 후보 선택과 경쟁하지 않는다.

### 선택지 조건

프로토타입의 선택지 조건은 다음만 지원한다.

```text
requiredTags
forbiddenTags
moneyMin
```

선택지 조건은 이벤트 발생 여부에 직접 영향을 주지 않는다.

Choice 이벤트가 발생 가능한 모든 State에는 선택 가능한 선택지가 최소 하나 있어야 한다.

같은 조건에서 같은 선택지를 고르면 항상 같은 결과가 발생한다. 확률형 결과는 지원하지 않는다.

---

## 19. Result

각 선택지 또는 Automatic 이벤트는 하나의 확정된 Result를 가진다.

```json
{
  "text": "결과 설명",
  "battle": false,
  "effects": {
    "addTags": [],
    "removeTags": [],
    "fameChange": 0,
    "reputationChange": 0,
    "moneyChange": 0,
    "revealGoalIds": [],
    "arcTransition": null
  }
}
```

- `battle`: 결과에 전투가 포함되는가
- 하나의 Result에는 전투를 최대 한 번만 포함한다.
- Result 내부에는 무작위 분기를 넣지 않는다.
- Result는 원정 시간을 직접 변경하지 않는다.
- Result는 RunPhase를 직접 변경하지 않는다.

`runTransition`은 사용하지 않는다.

Opening 완료, Goal 성공, 전투 패배와 흰밀물 종료는 Unity 런타임 규칙으로 처리한다.

---

## 20. Result 처리 순서

Unity는 일반적인 Result를 다음 순서로 처리한다.

1. 선택한 Choice ID를 확정한다. Automatic이면 예약 ID `auto`를 사용한다.
2. `battle == true`라면 연결된 전투를 실행한다.
3. 전투에서 패배하면 즉시 `BattleDefeat`를 확정하고 이후 Result Effect를 적용하지 않는다.
4. 전투가 없거나 승리하면 Tags, Fame, Reputation과 Money 변화를 적용한다.
5. `arcTransition`을 적용한다.
6. `revealGoalIds`를 적용한다.
7. `ResolvedEventCount`를 1 증가시킨다.
8. 정규 이벤트라면 이벤트 덱에서 제거한다.
9. 이벤트, Choice ID와 결과를 연대기에 기록한다.
10. Opening Event라면 정상 완료 후 `RunPhase = Normal`로 전환한다.
11. Goal Event라면 정상 완료 후 `GoalSuccess`를 확정하고 `RunPhase = Ended`로 전환한다.
12. 원정 결과가 아직 확정되지 않았다면 도착 처리의 마지막 단계에서 원정 시간 소진 여부를 검사한다.

전투가 있는 Result의 모든 비용과 보상도 전투 승리 후 적용한다.

프로토타입에서는 전투 전에 지불하거나 적용하는 Effect를 지원하지 않는다.

Goal Event는 일반 이벤트 덱에 없으므로 성공 후 덱 제거를 수행하지 않는다.

### 축복 후처리 단계와의 연결

축복과 연결된 이벤트는 축복 조건에 필요한 태그·상태를 기존 Result로 반영한다. Result에서 축복을 직접 지급하거나 티어를 상승시키지 않으며, 태그 하나를 적용한 직후마다 축복을 검사하지 않는다.

원정이 계속되는 일반적인 경우의 연결은 다음과 같다.

```text
관련 이벤트와 Result 처리 완료
→ 반영된 태그·상태를 기준으로 미사용 축복 조건 검사
→ 조건을 만족하면 별도의 축복 획득 단계 실행
→ 축복 시스템에서 획득·티어 상승과 해당 조건의 사용 완료 기록 처리
```

조건별 원정당 1회 제한과 동시 발생 처리는 [BlessingSystem.md](BlessingSystem.md)를 따른다. 이 단계는 다른 정규 이벤트의 연속 발생을 허용한다는 뜻이 아니다.

위 기존 종료 판정 절차에 축복 후처리를 정확히 어디에 접합할지는 아직 정하지 않았다. 전투 패배·Goal 성공·시간 소진이 있는 방문에서 축복 평가와 지급을 실행하거나 생략하는 기준, 종료 판정과의 선후 관계는 축복 문서의 미정 사항이다. 이 연결 규칙만으로 기존 원정 결과를 지연·취소하거나 종료 후 축복을 지급한다고 해석하지 않는다.

---

## 21. Effects

### addTags / removeTags

정의된 Tag를 추가하거나 제거한다.

같은 Result에서 같은 Tag를 동시에 추가하고 제거할 수 없다.

### fameChange

적용 후 Fame은 항상 `0 ~ 10`이어야 한다.

### reputationChange

적용 후 Reputation은 항상 `-3 ~ 3`이어야 한다.

### moneyChange

적용 후 Money는 0보다 작아질 수 없다.

### revealGoalIds

```json
[
  "silent_crater",
  "glass_marsh"
]
```

- 현재 Result에서 새롭게 공개할 Goal ID 목록이다.
- 모든 ID는 Goal Catalog에 존재해야 한다.
- 이미 공개된 Goal은 중복 추가하지 않는다.
- Goal 공개는 해당 장소를 방문하거나 완료한 것으로 처리하지 않는다.
- Goal을 공개해도 `RunPhase`는 `Normal`을 유지한다.
- 이벤트 생성기는 Goal Location 자체를 변경하거나 새 지리 연결을 만들지 않는다.

### arcTransition

```json
{
  "arcId": "refugee_caravan",
  "stage": "Resolution",
  "status": "Completed"
}
```

- Arc State를 변경하지 않으면 `null`이다.
- `arcId`는 현재 이벤트의 `arcId`와 일치해야 한다.
- Standalone, Opening, Goal Event는 항상 `null`이다.
- 다른 Arc의 State를 직접 변경할 수 없다.
- 사용할 수 있는 Status는 `Active` 또는 `Completed`다.
- Introduction Event는 `Pending` Arc를 `Active` 또는 `Completed`로 바꿀 수 있다.
- Active Arc는 Stage를 변경하거나 `Completed`로 종결할 수 있다.
- `Completed` Arc는 다시 활성화할 수 없다.
- 불리한 결말과 목표 실패도 사건군 자체가 끝났다면 `Completed`를 사용하고 Tags로 결과를 남긴다.

---

## 22. 전투 연결

프로토타입에서 실제 전투는 이벤트 생성 후 사람이 직접 연결한다.

`Events/Bindings/BattleBindings.json` 예시는 다음과 같다.

```json
[
  {
    "eventId": "orc_scout_encounter",
    "choiceId": "attack",
    "battleId": "OrcScout_01"
  },
  {
    "eventId": "opening_goblin_raid",
    "choiceId": "auto",
    "battleId": "GoblinOpening_01"
  }
]
```

- Choice 이벤트는 안정적인 Choice ID를 사용한다. 배열 인덱스를 연결 키로 사용하지 않는다.
- Automatic 이벤트는 예약된 Choice ID `auto`를 사용한다.
- 일반 Choice의 ID로 `auto`를 사용할 수 없다.
- `battle == true`인 모든 Result에는 정확히 하나의 유효한 연결이 있어야 한다.
- 연결된 `battleId`는 Unity에 존재하는 전투 데이터여야 한다.
- 이벤트 생성기는 `BattleBindings.json`이나 `battleId`를 생성하지 않는다.
- Unity는 연결되지 않은 Battle Result가 있는 이벤트를 유효하지 않은 콘텐츠로 처리한다.

이 구조는 프로토타입용이다. 이후 필요하면 직접 연결 대신 전투 Theme 또는 전투 Pool을 통한 자동 선택 방식으로 교체할 수 있다.

---

## 23. 연대기와 후일담

Unity는 실제로 발생한 이벤트, 선택한 Choice ID와 적용된 결과를 순서대로 기록한다.

Automatic 이벤트는 Choice ID로 `auto`를 기록한다.

전투 패배 시에도 해당 이벤트와 선택, 전투 패배 사실은 기록할 수 있지만 적용되지 않은 Result Effect는 기록하지 않는다.

원정 종료 시 다음도 기록한다.

- 종료 결과: `GoalSuccess`, `BattleDefeat`, `TimeExpired`
- 성공한 Goal ID 또는 도달한 마지막 Location
- Fame과 Reputation
- Arc별 최종 `Pending`, `Active`, `Completed` 상태
- 주요 Facts, Knowledges와 Traits

원정 종료 때문에 `Pending`이나 `Active` Arc를 다른 Status로 강제 변경하지 않는다.

후일담 조각을 사용하는 경우 `Events/Generated/EpilogueFragments.json`에서 현재 상태와 일치하는 조각을 선택할 수 있다.

예:

```json
[
  {
    "id": "legacy_respected_party",
    "category": "Legacy",
    "arcId": null,
    "arcStatus": null,
    "conditions": {
      "requiredTags": [],
      "forbiddenTags": [],
      "fameMin": 5,
      "fameMax": 10,
      "reputationMin": 1,
      "reputationMax": 3
    },
    "text": "그 원정에서 돌아온 모험가들의 이름은 여러 여행자에게 알려졌다."
  },
  {
    "id": "refugee_caravan_unfinished",
    "category": "Arc",
    "arcId": "refugee_caravan",
    "arcStatus": "Active",
    "conditions": {
      "requiredTags": [],
      "forbiddenTags": [],
      "fameMin": null,
      "fameMax": null,
      "reputationMin": null,
      "reputationMax": null
    },
    "text": "원정이 끝날 때까지 난민 행렬의 여정은 아직 계속되고 있었다."
  }
]
```

후일담은 런의 State를 변경하거나 새로운 선택지를 제공하지 않는다.

GoalSuccess의 경우 Goal Event의 마지막 Result text를 원정 결말의 중심 문장으로 사용할 수 있다.

TimeExpired와 BattleDefeat의 종료 화면 문구와 정산 차이는 Unity 런타임에서 별도로 처리한다.

---

## 24. 필수 검증 규칙

Unity에 포함하기 전에 최소한 다음 항목을 검증한다.

### ID와 참조

- Event ID가 전체 이벤트에서 고유하다.
- 같은 Event 안에서 Choice ID가 중복되지 않는다.
- Choice ID로 예약어 `auto`를 사용하지 않는다.
- 참조하는 Arc, Stage, Location, Goal과 Tag가 존재한다.
- Goal Catalog의 각 Goal이 유효한 Location과 정확히 하나의 Goal Event를 가진다.
- Goal Event의 `goalId`와 Goal Catalog의 연결이 일치한다.

### 이벤트 타입

- Opening Event의 `arcId`, `goalId`, `arcStage`는 `null`이다.
- Goal Event의 `goalId`는 유효하고 `arcId`, `arcStage`는 `null`이다.
- Standalone Normal Event의 `arcId`, `goalId`, `arcStage`는 `null`이다.
- Introduction Event는 유효한 Arc ID와 Stage를 가진다.
- Arc Normal Event는 유효한 Arc ID와 Stage를 가진다.
- `Main`, `Sub`, `Finale`, `FinaleReady` 관련 타입과 필드는 사용하지 않는다.

### 첫 방문과 재방문

- Opening Location에는 정확히 하나의 Opening Event가 존재한다.
- 각 일반 Location에는 첫 방문 시 후보 0개가 되지 않도록 조건 없는 Standalone 후보를 최소 하나 준비한다.
- 첫 방문 보장 후보는 다른 Location에서 먼저 소비되어 보장이 깨지지 않도록 구성한다.
- 한 탐험 기회에서 정규 이벤트는 최대 하나만 발생한다.
- 같은 State 변화 하나를 이용해 단순 왕복으로 여러 정규 이벤트를 순차 소진할 수 없도록 `SeenEligibleEventIdsByLocation` 갱신 규칙을 지킨다.
- 같은 진입 경로는 `NewEntryEdge`를 두 번 발생시키지 않는다.

### Result와 전투

- Choice 이벤트가 발생 가능한 모든 State에 선택 가능한 Choice가 최소 하나 있다.
- 모든 Result가 결정적이며 확률 분기를 포함하지 않는다.
- Battle Result마다 유효한 전투 연결이 정확히 하나 존재한다.
- State 변화 후 Fame, Reputation과 Money가 허용 범위를 벗어나지 않는다.
- 같은 Result에서 같은 Tag를 동시에 추가하고 제거하지 않는다.
- `revealGoalIds`의 모든 Goal이 Goal Catalog에 존재한다.

### 축복 연결

- 축복 조건에 쓰이는 Tag도 Facts, Knowledges, Traits 중 해당 참조 데이터에 정의되어 있다.
- 이벤트 Result에 축복 직접 지급·티어 상승·조건 사용 기록을 위한 임의 필드가 없다.
- 축복 시스템은 조건 사용 여부를 Tag와 별도로 기록하며, 같은 조건으로 획득 기회를 반복 제공하지 않는다.
- 문서의 설명용 태그를 실제 참조 데이터에 등록된 콘텐츠로 간주하지 않는다.

### Arc

- Arc는 `Pending`으로 시작한다.
- Introduction Event를 통해서만 `Pending -> Active/Completed` 전환이 일어난다.
- `Completed` Arc가 다시 활성화되지 않는다.
- Standalone, Opening, Goal Event는 `arcTransition`을 사용하지 않는다.
- 원정 종료 시 `Pending`과 `Active` Arc를 자동 종결하지 않는다.

### 런 진행

- Opening 정상 완료 후에만 `RunPhase = Normal`이 된다.
- Goal Event 정상 완료는 `GoalSuccess`와 `RunPhase = Ended`를 만든다.
- 전투 패배는 즉시 `BattleDefeat`와 `RunPhase = Ended`를 만든다.
- Location 도착 이벤트 처리가 끝난 뒤에만 `TimeExpired`를 판정한다.
- `GoalSuccess` 또는 `BattleDefeat`가 이미 확정된 경우 `TimeExpired`로 덮어쓰지 않는다.
- 이벤트 덱 고갈을 런 종료 사유로 사용하지 않는다.

---

## 25. 현재 지원하지 않거나 미정인 기능

현재 지원하지 않는 기능:

```text
런타임 LLM 생성
이벤트끼리 다음 Event ID 직접 지정
Main / Sub Arc 구분
Main Arc Finale
FinaleReady
이벤트 덱 고갈 종료
복잡한 조건식
OR 조건 그룹
임의 변수와 수식
선택 결과의 확률 분기
랜덤 Effect
한 Result 안의 복수 전투
전투 전 비용 또는 Effect 적용
이벤트 생성기가 지정하는 실제 Battle ID
이벤트 Effect를 통한 Location 이동
이벤트 Effect를 통한 원정 시간 변경
이벤트 조건을 통한 원정 시간 판정
NPC별 호감도
세력별 평판
개별 모험가의 서사 State
이벤트 가중치와 우선순위
원정 종료 시 Arc 강제 Abandoned 처리
Goal Event의 비전투 실패/후퇴 분기
GoalSuccess 이후 자유 탐사
```

개별 모험가의 직업별 레벨 보상과 공용 특성에 관한 기본 구조는 [plan.md](plan.md)를 따른다. 공용 특성은 파티 단위의 서사 Tag인 Trait과 별개이며, `Data/Traits.csv`에 정의된 태그를 의미하지 않는다.

현재 Plan에서 아직 미정인 항목:

- 총 원정 시간
- 경로별 이동 시간 산정 방식
- 남은 시간보다 긴 이동의 시작 허용과 처리 방식
- 회복·재정비 비용과 반복 이용 규칙
- 영입 후보·상점 재고 갱신
- Goal의 구체적인 배치·접근 조건
- 미공개 Goal Location을 먼저 방문했을 때의 처리
- GoalSuccess 이후 자유 탐사 도입 여부
- 레벨업 달성 조건, 이벤트와의 연계 및 레벨 보상 처리 계약
- 반복 원정에서 발견 기록과 축복 이외 성장의 유지·초기화

축복 관련 후속 결정은 [BlessingSystem.md](BlessingSystem.md)의 미정 사항을 따른다. 특히 원정 종료와 축복 후처리의 정확한 순서, 비선택·거절 조건의 사용 기록, 축복 획득 후 재평가·연쇄 발생 여부와 이벤트 종료 외의 검사 시점은 아직 확정하지 않았다.

이 기능이나 규칙을 확정할 때 개별 Event JSON에 임의 필드를 추가하지 않고 EventSystem 변경 사항으로 별도 반영한다.
