# 애니메이션 직접 재생 이관 및 이벤트 툴 구현 계획

- 작성일: 2026-11-08
- 문서 성격: 구현 범위·데이터 계약·구현 순서·검증 기준을 관리하는 계획
- 현재 상태: 캐릭터별 키 목록 구현·기존 데이터 이관·카메라 변경·컴파일·자동 검증·독립 코드 리뷰 완료. 화면 사용 검증은 사용자 담당.
- A1–A5 기록 상태: A0 승인 완료 / A1·A2 완료 / A3 이관 구현·관련 검증 완료(기존 회귀 실패 별도 기록) / A4 구현 완료 / A5 Scene 배치·자동 검증·독립 리뷰·문서 정리 완료, 최종 사용자 사용 검증 대기
- A5 승인 범위: 사용자가 전용 툴 Scene 배치와 실제 화면 조작 검증을 포함해 A5 진행을 승인했다. `Assets/Scenes/AnimationEventTool.unity`를 구성하고 검증 중 발견한 재임포트 알림 결함을 수정했다. 이후 사용자가 사용 검증을 직접 맡겠다고 요청하여 추가 화면 자동 조작을 중단했다.
- 채택 방향: Animator 컴포넌트와 Avatar는 유지하고, Animator Controller 없이 Playables로 클립을 직접 재생한다.
- 관련 명세: [전투 시스템](../BattleSystem.md), [기존 구현 로드맵](../ImplementationRoadmap.md)

## 현재 적용 계약: 캐릭터별 키 목록 (2026-11-08)

사용자는 카메라를 위에서 사선으로 변경하고 캐릭터별 키 목록을 구현하는 설계를 승인했다. 기본 공격 키는 추가 답변으로 `attack1`을 확정했다. 아래 기존 A1–A5 내용은 이관 이력이며 데이터 소유·툴 목록·저장은 이 절의 계약이 우선한다. 새 설계는 독립·비판적 리뷰 모두 Pass 후 구현에 착수했다.

- 프리팹의 `HexCharacterView._animationProfile` 참조는 유지한다. Profile은 `CharacterAnimationEntry[]`를 직접 소유하고 각 항목에 문자열 key, clip, authored FPS·length, events를 저장한다.
- 동일 클립의 복수 할당을 허용하고 이벤트는 깊은 복사한다. 키는 trim 후 비어 있거나 캐릭터 안에서 중복되면 거부한다. 비교는 ordinal, 대소문자를 구분한다.
- 툴의 기본 목록은 선택 캐릭터의 항목만 표시한다. 별도의 클립 불러오기에서 실제 클립/FBX 서브에셋을 골라 새 항목을 추가한다. 키 변경·항목 삭제·선택 항목 이벤트 편집을 지원한다.
- 작업본 단위는 Profile 전체다. 항목 이동으로 편집을 버리지 않으며, 캐릭터 이동에 저장/버리기/취소를 적용한다. 선택 항목과 이벤트는 참조로 추적하고 재로드는 고유 키로 복원한다.
- 작성 목록 검증과 게임 실행 준비 검증을 분리한다. 빈 목록·부분 목록은 작성/저장 가능하다. 게임은 정확한 `idle`, `move`, `attack1`을 초기화 시 기존 3슬롯에 연결한다. `attack1`은 nonloop이며 추가 키의 게임 실행은 이번 범위에 없다.
- 기존 3개 Profile의 GUID와 프리팹 참조를 유지하고 기존 정의의 클립·작성 메타데이터·이벤트를 그대로 옮긴다. 기존 Definition 에셋은 삭제하지 않으며 활성 데이터에서 참조하지 않는다. 신규 목록이 있으면 동일 여부를 확인하고 불일치는 덮어쓰지 않는다.
- 캐릭터마다 독립 Profile을 사용한다. 미연결·공유 Profile은 명시적인 전용 목록 만들기/복사로 처리한다. 새 Profile 저장·재확인 후 선택한 프리팹 연결만 저장한다. 상속된 연결을 가진 하위 variant가 있으면 쓰기 전에 경로를 표시하고 하위 캐릭터를 먼저 분리하도록 제한한다.
- 소스 기준은 프리팹·Profile·원본 및 작업본의 모든 클립과 의존 에셋이다. 삭제했지만 미저장인 항목의 의존성도 유지한다. 클립 추가는 기존 기준 검증 뒤 의존성만 확장하며 기존 변경을 재승인하지 않는다.
- 저장은 Profile 한 파일만 허용하고 전체 데이터를 다시 읽어 대조한다. 다른 원본의 파일·meta·메모리는 저장 후 독립적으로 검증한다. 쓰기 전 실패는 원본 메모리/dirty 복원, 쓰기 후 미확정은 작업본·복구 경로 보존 및 추가 쓰기 차단을 적용한다.
- 프리뷰 자원은 Graph → 복제 캐릭터 → 복제 클립 순으로 정리한다. 빈 목록/미연결 상태도 기본 자세로 스폰하며 타임라인만 비활성화한다.
- 카메라 변경은 `AnimationEventTool.unity`의 지정 Camera만 대상으로 한다. 컴파일·자동 테스트·독립 코드 리뷰를 수행하고 화면 사용 검증은 사용자가 맡는다.

### 구현/검증 기록

2026-11-08 구현 완료. 승인 설계와 책임 경계를 변경하지 않았다.

| 검증 | 결과 |
|---|---|
| Unity 6000.3.24f1 컴파일 | 영향받는 코드·의존 어셈블리·테스트 포함, 컴파일 오류 0 |
| AnimationData | 90/90 성공 |
| AnimationPlayback | 51/51 성공 |
| AnimationEventTool | 113/113 성공 |
| Combat | 1,056개 중 1,017 성공·39 실패 |
| 독립 코드 리뷰 | 수정 후 Pass |
| 실제 화면 조작 | 사용자 요청에 따라 이번 변경에서는 수행하지 않음 |

전체 관련 실행(job `05120aeea18d49408b6fad18d840aece`)은 1,310개 중 1,270 성공·40 실패였다. 신규 데이터 테스트 1건은 저장되지 않은 임시 클립을 EditorJsonUtility로 왕복 복사하는 fixture 문제였고, 기존 Definition 테스트와 같은 EditorUtility.CopySerialized 검증으로 수정했다. 재컴파일 후 Data 전체 90건을 다시 실행(job `433afe5b93db4522b6f234db4040c138`)하여 모두 성공했다. 따라서 최종 관련 애니메이션 테스트 결과는 254/254이며, 이를 한 번의 전체 재실행 결과로 표현하지 않는다.

전투 실패 39건은 이전 A5 실패 42건에 모두 포함된다. 38건은 실패 메시지도 동일하고, CompositeDamageUsesColoredNumbersFromOneOriginThenSeparates는 기대 화면 좌표만 달라졌으며 실제값 (0,0)의 실패는 같다. 기존 overlay 메시지 3건은 이번 실행에서 재현되지 않았다. 전투 전체 회귀는 성공한 상태가 아니며 이 변경에서 해당 실패를 수정하지 않았다.

리뷰에서 발견해 수정한 중간 위험 결함은 삭제된 Profile의 이름 표시 예외와 저장 재확인 이후 대상 파일만 디스크에서 바뀌었을 때 성공 처리되는 경로다. 삭제된 원본은 복구 경로로 표시하고, 일반 저장·전용 목록 생성 모두 검증한 파일 지문을 최종 확인한다. 디스크만 변경하는 회귀 테스트도 통과했다.

기존 3개 Profile은 이관 전 manifest와 클립 GUID/local ID·작성 FPS/길이·전체 이벤트를 대조하고 저장 후 다시 읽어 확인했다. Profile GUID·meta와 프리팹 참조는 유지했다. Battle 씬, Combat 프리팹, 기존 9개 Definition의 파일 지문도 유지됐다. 새로 저장된 Profile마다 `idle`, `move`, `attack1` 3개 항목이 있으며 게임 준비 검증을 통과한다.

툴 Scene diff는 PreviewCamera의 위치와 회전뿐이다. 위치 약 `(2.25, 4.082, 2.25)`, 회전 `(45, 225, 0)`, 대상 중심 `(0, 0.9, 0)`, 거리 `4.5`, FOV `40`이다. 검증 후 툴 Scene은 Play 종료·미저장 Scene 변경 없음 상태다. 테스트용 생성 에셋은 정리됐다.

검증 산출물은 `tmp/character-animation-keys-validation/`의 `legacy-manifest.json`, `migration-report.json`, `first-editmode.xml`, `data-recheck.xml`, `verification-summary.json`에 보존했다. 상세 사용 흐름은 [툴 사용 안내](AnimationEventToolGuide.md)를 따른다.

## 1. 목표와 범위

### 1.1 이번 구현 계획에 포함

1. 현재 턴제 전투의 Idle·Move·Attack 재생을 Controller state 방식에서 클립 직접 재생으로 이관한다.
2. 기존 이동·공격 진행, 공격 완료 시 피해 확정, 취소·재시작·표시 실패 처리 계약을 보존한다.
3. 캐릭터 프리팹에 직접 재생용 애니메이션 데이터를 연결한다.
4. 전용 Scene을 Play Mode에서 사용하는 애니메이션 이벤트 툴을 작성한다.
5. 사용자가 `Assets/Prefabs/Combat` 하위 프리팹을 선택해 캐릭터 한 개를 스폰하고, 클립 프레임을 이동하며 이벤트를 작성·저장한다.
6. 이벤트 종류는 공격 판정 하나이며 편집 파라미터는 발생 프레임과 근접/투사체 구분뿐이다.

### 1.2 포함하지 않는 기능

- 이벤트 프레임을 실제 피해·근접 충돌·투사체 생성/이동/명중에 연결하는 런타임 이벤트 실행기.
- 공격 피해 시점을 애니메이션 완료에서 이벤트 프레임으로 바꾸는 전투 규칙 변경.
- Animator state별 이벤트 관리, 직업별 Controller 또는 Override Controller 신규 제작.
- 구형 `Animation` 컴포넌트로의 전환, BlendTree·다중 레이어·IK·루트 모션을 관리하는 범용 애니메이션 시스템.
- 새 피격·사망·스킬 애니메이션 기능, 애니메이션/리깅/텍스처 콘텐츠 제작.
- 별도 AutoBattler 프로토타입의 재생 코드 이관.
- 배포용 빌드와 측정 근거 없는 성능 최적화.

직접 재생 이관은 게임의 재생 코드를 바꾸는 작업이다. 이벤트 툴의 이번 완료 조건은 유효한 데이터 생성까지이며, 이 두 작업을 했다고 공격 판정 이벤트의 게임 연결이 완료된 것으로 보고하지 않는다.

## 2. 확인한 현재 구현

| 위치 | 현재 동작 | 이관 시 유지/변경 사항 |
|---|---|---|
| `Assets/Scripts/CombatView/HexCharacterView.cs` | Idle/Walk/Attack state 이름·hash, `HasState`, `Play`, `CrossFadeInFixedTime`, `normalizedTime` 사용 | state 의존을 직접 재생기로 교체. 모델·방향·이동·overlay 책임 유지 |
| `Assets/Scripts/CombatView/BattleRunner.cs` | scaled `Time.deltaTime` 사용. busy일 때 행동 표시 진행 | idle 포함 모든 활성 view의 재생 시간을 한 번씩 진행할 위치 추가 |
| `Assets/Scripts/CombatView/BattleRunner.Presentation.cs` | 공격 재생 완료 또는 표시 실패 시 `CompleteAttack` | 피해 확정·중복 방지·simulation 교체 방어 유지 |
| `BattleStartController`, `BattleMapPreparation`, `BattleRestartSnapshot` | 직업에서 프리팹을 선택하고 기존 초기화/재시작 수행 | 별도 직업 애니메이션 레지스트리를 추가하지 않고 프리팹의 데이터 참조 사용 |
| `Assets/Prefabs/Combat/Warrior.prefab`, `Archer.prefab`, `Rogue.prefab` | Warrior와 variant 두 개가 같은 Controller와 기본 클립 공유 | 현재 클립을 그대로 명시적 데이터로 옮겨 최초 시각 결과 보존 |
| `Assets/Scripts/BattleMapTool` | Play Mode Game 뷰의 IMGUI, 작업본, 명시적 Save, 외부 변경 검사 | UI·저장 관례만 참고. 맵 도구를 범용 도구로 리팩터링하지 않음 |

현재 기본 클립은 `HumanM@CombatIdle2H01`, `HumanM@Run01_Forward`, `HumanM@Attack2H01`이다. 이번 이관에서 직업별 동작을 임의로 다른 클립으로 교체하지 않는다. 이후 사용자가 정의 에셋의 클립을 교체할 수 있다.

현재 코드에서 특히 보존할 계약:

- 초기화 시 유효한 재생 구성을 요구하고, 일부 view 초기화 실패 시 앞서 생성한 view를 정리한 후 재시도가 가능해야 한다.
- 이미 발행된 공격에서 애니메이션 시작/표시가 실패해도 승인된 공격 결과를 취소하지 않는다.
- 일반 취소는 미발행 공격에 적용된다. 이미 `Attacking`인 공격과 확정된 반응·이동 처리의 정책을 바꾸지 않는다.
- 공격자/대상 비활성화, `DisableVersion`, Runner의 `_presentationVersion`, `_isCompletingAttack`, simulation 동일성 검사와 피해 배치의 일회성 처리를 보존한다.
- Runner 종료는 진행 중 확정 결과를 정리한 다음 view를 해제한다. 애니메이션 자원 정리가 이 순서를 뒤집으면 안 된다.

## 3. 데이터 소유와 책임 경계

### 3.1 직접 재생 데이터

| 형식 | 역할과 필드 |
|---|---|
| `AnimationDefinition` ScriptableObject | 실제 `AnimationClip` 참조, 작성 기준 FPS·길이, 이벤트 키 배열 |
| `AnimationEventKey` 직렬화 항목 | 정수 프레임, 이벤트 종류 `AttackHit`, 공격 종류 `Melee`/`Projectile` |
| `CharacterAnimationProfile` ScriptableObject | 현재 게임에 필요한 Idle·Move·Attack의 `AnimationDefinition` 참조 |
| 기존 `HexCharacterView`의 직렬화 필드 | 해당 프리팹이 사용할 `CharacterAnimationProfile` 참조 |

- 데이터는 `Assets/GameData/Animations` 아래에 관리하고 Profile과 Definition을 구분한다. 정확한 에셋 이름은 직업과 역할을 드러내도록 정한다.
- Definition 에셋이 이벤트의 소유자다. 동일 클립을 쓰더라도 별도 Definition이면 서로 다른 이벤트를 가질 수 있다.
- 같은 Definition을 여러 Profile이 참조하면 클립과 이벤트도 공유한다. 초기 이관에서는 직업별 Definition을 분리하여 의도하지 않은 공동 편집을 피한다.
- 최초 이관의 이벤트 배열은 빈 값이다. 실제 판정 프레임을 추정해서 넣지 않는다. 이벤트가 없어도 기존 게임 애니메이션은 정상 재생한다.
- Definition을 이름이나 클립 이름으로 자동 합치지 않는다. Unity 에셋 참조가 식별 기준이며 FBX의 클립 서브에셋도 정확히 구분한다.
- 앞서 검토한 `프리팹+클립당 트랙 하나`와 `Controller+state별 트랙`은 이 계획에서 사용하지 않는다. 직접 재생에서 명시적으로 참조하는 Definition으로 소유권을 통일한다.
- Profile의 역할은 현재 세 가지로 한정한다. 범용 행동 ID 레지스트리나 임의 파라미터 사전은 추가하지 않는다.
- `Projectile`은 이벤트 분류다. 발사 시점/생성 위치/명중 시점/피해 계산의 의미를 이번 데이터에 추가하지 않는다.

### 3.2 코드 경계

| 모듈 제안 | 책임 | 의존 |
|---|---|---|
| `Assets/Scripts/AnimationData` | Definition/Profile/키·enum, 데이터 검증, 프레임↔시간 계산 | UnityEngine, 게임 규칙 의존 없음 |
| `Assets/Scripts/AnimationPlayback` | `CharacterAnimationPlayer`, PlayableGraph·클립 노드·시간·혼합·완료 상태 | AnimationData, Unity Playables |
| 기존 CombatView | player 소유, 행동을 재생 요청으로 변환, 전투 표시의 결과 처리 | AnimationPlayback 및 기존 전투 모듈 |
| `Assets/Scripts/AnimationEventTool` | Controller/Gui, 프리뷰 세션, 에셋 검색·로드·저장 | AnimationData, 필요한 Unity API. Editor 코드는 `UNITY_EDITOR` 경계 |

데이터·재생기·도구는 별도 asmdef로 경계를 둔다. 전투 simulation은 새 재생기나 도구에 의존하지 않는다. 재생기에는 피해 callback, 스킬 실행, 전투 취소 정책을 넣지 않는다. 구현에 필요한 내부 연결만 추가하며 요청과 무관한 public API를 늘리지 않는다.

## 4. 직접 재생기의 계약

### 4.1 갱신과 시간

- 모든 Unity 객체와 Playables 호출은 메인 스레드에서 동기 실행한다.
- View의 독립 `Update`와 Runner가 동시에 재생 시간을 진행하지 않는다. 전투에서는 `BattleRunner`가 유일한 tick 소유자다.
- 기존 Runner의 구성/피해/파괴 view 정리 후, busy 분기 전에 살아 있는 바인딩 view 전체를 한 번 tick한다. 그 다음 현재 행동의 공격 완료를 검사하거나 새 행동을 시작한다.
- tick 중에는 외부 완료 callback을 호출하지 않는다. 피해/전투 교체는 기존 Presentation 경로에서 처리하므로 view 열거 중 컬렉션 변경을 만들지 않는다.
- 게임은 `Time.deltaTime`을 사용한다. 유효한 `Animator.speed`를 재생기의 시간 배율로 명시적으로 한 번 적용하여 기존 속도 설정의 의미를 유지한다. 0 이하/NaN/Infinity 속도는 유효한 공격 재생이 아니며 기존 표시 실패 경로로 처리한다. `deltaTime == 0`은 시간 정지로 처리한다.
- PlayableGraph는 Manual 모드로 운용한다. 클립 로컬 시간을 명시적으로 설정하고 `Evaluate(0)`로 자세를 반영하여 자동 시간 진행과 중복되지 않게 한다.
- 새 공격 명령은 해당 프레임에 `t=0` 자세를 즉시 평가한다. 이미 소비한 delta를 새 공격에 다시 더하지 않는다. 다음 Runner tick부터 시간이 진행된다.
- Idle/Move는 반복하고 Attack은 한 번 재생한 뒤 마지막 자세를 유지한다. Attack 완료를 읽고 Idle로 전환하는 결정은 기존 Presentation이 한다.
- 공격 로컬 시간이 클립 길이에 도달하면 완료다. 큰 delta에서도 길이로 제한하며 클립 끝을 넘어 반복하지 않는다.

### 4.2 단순한 혼합

- 현재 요구에 맞춰 Idle·Move·Attack의 클립 노드 세 개와 고정 크기 가중치 상태를 사용한다. 바인딩 시 생성하고 매 프레임 다시 만들지 않는다.
- 기존과 같이 공격 시작은 0프레임으로 즉시 전환한다. Idle↔Move와 공격 종료 후 Idle은 기본 0.1초 혼합을 유지한다.
- 혼합 도중 새 Idle/Move 요청이 오면 현재 세 가중치를 출발점으로 새 목표 가중치에 보간한다. 같은 역할의 반복 요청은 시간을 계속 0으로 되돌리지 않는다.
- Attack 재시작은 새로운 재생으로 취급하고 시간과 완료 상태를 초기화한다. 이전 Attack의 완료 상태가 새 요청을 완료시키지 않도록 요청 세대/상태를 구분한다.
- Attack 이외 노드가 혼합에 남아 있어도 공격 완료 기준은 현재 Attack 요청의 로컬 시간 하나다. 혼합 종료 시점으로 피해 시점을 바꾸지 않는다.
- 다중 행동 큐, 임의 레이어와 합성 포즈 캡처 기능은 만들지 않는다.

### 4.3 준비·실패·해제

- `HasAnimator`의 Controller 필수 조건을 Animator·클립·Profile·재생 자원 검증으로 대체한다. model/facing/이동 속도 검사는 유지한다.
- Legacy 클립은 지원하지 않는다. Humanoid 클립은 유효한 Humanoid Avatar가 필요하다. 기존 단위 테스트의 non-Humanoid Transform 클립은 동일 재생 핵심부에서 사용할 수 있게 하되 실제 Humanoid 프리팹 검증을 별도로 유지한다.
- Attack은 유한한 양수 길이/FPS와 비반복 클립을 요구한다. Profile의 필수 역할이 빠지면 초기화에 실패한다.
- Animator Controller와 재생기가 동시에 Animator를 제어하지 않게 한다. `applyRootMotion=false`, `fireEvents=false`로 두고 실제 이동은 기존 View가 담당한다.
- player는 외부에서 넘긴 클립·Profile을 소유하지 않고 수정/파괴하지 않는다. 자신이 생성한 graph와 노드만 해제한다.
- 종료는 완료/중단/잘못된 재생을 구분한다. 단, 애니메이션 중단을 게임 공격 취소로 자동 번역하지 않는다. 표시 실패 시 결과를 확정하는 기존 Runner 정책이 우선이다.
- `OnDisable`, `Unbind`, 파괴, 초기화 도중 실패에서 graph를 안전하게 정리한다. 기존 `SnapToCharacter/SetIdle`가 Dispose 도중 graph를 다시 생성하지 않게 정리 전용 경로를 둔다.
- `OnEnable`에서 이미 바인딩된 view가 돌아오면 유효성 재검사 후 Idle로 초기화한다. 이전 공격을 복구하거나 오래된 완료 상태를 되살리지 않는다.
- 설정하는 Animator의 culling 등 기존 상태는 소유 범위와 복원 순서를 명시한다. 공격은 화면 밖에서도 완료 시간을 잃지 않아야 한다.
- 플레이 도중 Profile 교체를 자동 추적하는 기능은 추가하지 않는다. 재바인딩을 기준으로 새 데이터를 사용하고, 참조 소실/자원 무효화는 진행 중 표시 실패로 처리한다.

## 5. 이벤트 툴의 사용자 흐름

```text
Combat 프리팹 선택 → 캐릭터 스폰
    → Profile의 Idle/Move/Attack 정의 선택 또는 기존 Definition 열기/새 정의 만들기
    → 실제 클립 선택·프레임 이동 → 이벤트 추가/편집 → 명시적 저장
```

- 기본 목록은 선택 프리팹 Profile의 Definition이다. 기존 Definition을 따로 열거나 새 Definition을 만들어 아직 Profile에 배정되지 않은 클립도 작성할 수 있다.
- 클립 선택 목록은 `Assets/Art/Character/Animation`의 실제 AnimationClip 서브에셋을 포함한다. 동일 이름은 출처 경로로 구분한다.
- 새 Definition 저장과 Profile 역할 배정은 별개다. 새 정의는 저장만으로 게임에 자동 연결되지 않으며 Profile 배정은 Inspector에서 명시적으로 수행한다. 툴은 현재 배정 상태와 공유 편집 여부를 표시한다.
- 프리팹 한 개와 클립 한 개만 미리 본다. 현재 Combat 프리팹의 Humanoid 구성을 우선 지원하며 누락/호환되지 않는 Avatar·클립은 오류를 표시한다.
- 화면은 상단 프리팹/정의/클립 선택, 중앙 캐릭터, 우측 선택 키의 속성, 하단 타임라인으로 구성한다. 기존 BattleMapTool처럼 IMGUI를 사용한다.

```text
[프리팹] [애니메이션 정의] [클립] [스폰] [저장]

       캐릭터 미리보기             선택 키 속성
                                  이벤트: 공격 판정
                                  프레임: [18]
                                  공격 종류: [근접 / 투사체]

              ◆       ◆        ◆       ← 슬라이더 위쪽의 키
0 ────────────●────────────────── 마지막 프레임
[이전 프레임] [재생/정지] [다음 프레임] [이벤트 추가] [삭제]
```

- 슬라이더는 정수 프레임에 맞춰 이동한다. 키를 클릭하면 그 프레임으로 이동하고 재생을 정지한다.
- 이벤트 추가는 현재 프레임에 키 하나를 만들고 선택한다. 키의 프레임 숫자를 바꾸면 위치도 바뀐다.
- 같은 프레임에 여러 키를 허용한다. 마커를 위로 쌓고 스크롤 가능한 키 목록을 제공하여 개별 선택을 보장한다. 정렬 후에도 작업 항목 참조로 선택을 유지한다.
- 같은 프레임 키의 저장 순서는 기존 순서를 유지하고 새 키는 뒤에 놓는다. 공격 종류를 바꿔도 별도 위치/크기/투사체 파라미터를 만들지 않는다.
- 툴의 재생은 unscaled time 1배속이며 끝에서 멈춘다. 스크럽·재생 모두 피해나 Unity AnimationEvent를 실행하지 않는다.

## 6. 프레임·프리뷰·저장 계약

### 6.1 프레임 기준

- 임포트된 클립의 시작을 0프레임으로 본다. 원본 FBX take 전체의 시작 프레임과 혼동하지 않는다.
- `time = min(frame / fps, clip.length)`이며 fps는 클립의 FPS다. 별도 사용자 지정 FPS로 이벤트 의미를 바꾸지 않는다.
- 마지막 프레임은 `floor(length * fps + 0.0001)`로 정하고 0부터 마지막 프레임까지 포함한다. 허용 오차는 계산 오차를 위한 값이며 프레임을 재배치하는 보정이 아니다.
- 길이/FPS는 유한한 양수여야 한다. 길이가 짧아 마지막 프레임이 0이면 프레임 0만 허용하고 슬라이더의 0으로 나누기를 피한다.
- 프레임 정수가 유일한 이벤트 시간 원본이다. 초는 계산해서 표시하며 별도 저장하지 않는다. 29.97 등 소수 FPS도 검증한다.

### 6.2 프리뷰 자원

- 비활성 부모 아래 프리팹을 복제하고, 활성화 전에 게임 MonoBehaviour와 Collider를 비활성화하고 Controller를 분리한다. 원본 프리팹을 변경하지 않는다.
- 현재 Combat 프리팹의 `HexCharacterView`는 명시적 바인딩 없이 전투를 시작하지 않지만, 프리뷰는 이 동작에 기대어 게임 재생기를 함께 실행하지 않는다.
- 프리뷰의 전용 graph가 Animator를 단독 제어한다. 위치·방향을 고정하고 root motion과 내장 AnimationEvent 실행을 차단한다.
- 루프 클립도 끝 자세를 볼 수 있도록 메모리 클립 사본의 loop 설정을 끄며 원본/importer는 수정하지 않는다.
- prefab/clip 교체, 비활성화, Play 종료, 실패 시 graph → 스폰 인스턴스 → 임시 클립 사본 순서로 정리한다. 중복 정리에도 안전해야 한다.
- 툴의 모델/자원 소유와 게임의 바인딩·이동 정책을 결합하지 않는다. 프레임 계산·데이터 검증은 공통 코드를 사용한다.

### 6.3 저장과 변경 감지

- 편집은 원본 SO와 분리된 작업본에서 수행하고 명시적 Save에만 디스크에 반영한다. 저장된 `.asset`은 Play 종료 후 유지된다.
- 미저장 작업본은 Play 종료 시 소실된다. dirty 표시와 종료 전 저장 안내를 제공하며 자동 저장을 약속하지 않는다.
- 프리팹/정의/클립 변경 시 미저장 내용은 저장·버리기·취소로 처리한다. 저장 실패 시 전환을 중단하고 작업본과 dirty를 유지한다.
- 기존 Definition은 불러온 snapshot과 저장 직전 값을 비교해 외부 수정 덮어쓰기를 막는다. 신규 이름 충돌은 고유 경로로 처리하되 기존 에셋을 덮어쓰지 않는다.
- 유효 참조·프레임 범위·enum·정렬 순서를 검사하고, 디스크 재로드로 저장 결과를 확인한다. 디스크 쓰기 전 실패라면 원본 메모리 상태를 복원하고 작업본·dirty·기존 snapshot을 유지한다.
- 쓰기를 시도한 뒤 재로드/검증이 실패하면 디스크가 변경되지 않았다고 보고하지 않는다. 저장 결과 미확정 상태로 전환하고 작업본을 보존한 채 추가 저장을 막는다. 기존 snapshot을 임의로 성공 기준으로 갱신하지 않고, 실제 파일을 다시 읽어 상태를 확인한 뒤 명시적인 재로드/편집 재개로 복구한다. 파일시스템 오류까지 자동 복구/원자적 저장을 보장하지 않는다.
- 프리뷰 생성 시 선택 prefab/Profile/Definition/원본 clip의 필요한 의존성 hash를 기록한다. 프로젝트 변경 통지와 Save 직전에 재검사하여 FPS·길이가 같은 재임포트도 감지한다.
- 소스 변경 시 즉시 재생을 멈추고 프리뷰를 무효화하여 편집·저장을 막는다. graph와 임시 자원은 정리하지만 작업본과 디스크 원본은 보존한다.
- 원본 다시 불러오기는 dirty가 있으면 버리기·취소를 명시적으로 선택한 뒤 수행한다. 성공적으로 최신 프리뷰를 만든 경우에만 source snapshot을 갱신한다. 실패/취소하면 차단 상태를 유지한다.
- Definition 저장은 이를 참조하는 Profile/prefab의 전이적 dependency hash도 바꿀 수 있다. 따라서 자기 Save를 전체 hash 불변으로 판별하지 않는다. 저장 직전 검사 통과 → 대상 Definition만 동기 저장 → 저장 내용이 작업본과 일치하는지 확인 → 소스 검증 → 새 기준값 확정 순서로 처리한다.
- 이때 prefab와 base prefab, Profile 자체의 저장 내용 snapshot 및 원본 clip/모델/Avatar 등의 독립적인 소스 검사는 유지한다. 대상 Definition의 예상 변경 외에 소스가 바뀌었으면 세션을 무효화한다. 자기 Save 검증이 성공한 경우에만 상위 dependency hash를 새 기준으로 갱신하며, 임의의 프로젝트 변경 통지에서 현재 값을 새 기준으로 받아들이지 않는다.
- 클립 교체 또는 저장된 작성 FPS/길이와 현재 값의 불일치는 자동 프레임 보정으로 처리하지 않는다. 사용자가 ‘현재 클립 기준으로 다시 작성’을 확인하면 작업본의 키를 비우고 기준을 갱신한다. 명시적인 Save 전까지 기존 에셋은 보존하며, 쓰기 시도 이후 오류는 앞의 저장 결과 미확정 규칙을 따른다.
- source hash는 이 편집 세션의 오래된 프리뷰 저장 방지를 위한 검사다. 장기간의 원본 변경 이력·자동 키 이전 시스템으로 확장하지 않는다.
- Editor 통지 구독은 ToolController가 소유하고 `OnDisable`에서 해제한다. 검색/의존성 검사는 초기화·명시적 새로고침·변경 통지·저장 시 수행하며 매 프레임 실행하지 않는다.

## 7. 구현 단계와 완료 기준

| 단계 | 상태 | 선행 | 산출물과 완료 기준 |
|---|---|---|---|
| A0 범위·기준선 확정 | 완료·사용자 승인 | 없음 | 이 문서 검토/승인, 현재 실제 프리팹와 회귀 테스트 목록 확정. 구현 허가와 Scene 배치 허가를 구분 |
| A1 데이터·검증 | 완료 | A0 승인 | AnimationDefinition/Profile/키, 공통 프레임 계산, 데이터 테스트. 빈 이벤트·동일 클립의 독립 정의·소수 FPS·잘못된 참조 검증 |
| A2 직접 재생 핵심부 | 완료 | A1 | 고정 역할 player와 수동 시간 진행, 혼합·완료·중단·재초기화·해제 검증. 기존 게임 경로를 아직 제거하지 않음 |
| A3 턴제 연결·프리팹·테스트 이관 | 구현·관련 검증 완료, 기존 회귀 실패 42개 기록 | A2 | HexCharacterView/Runner 연결, 실제 3프리팹와 초기 데이터, controller fixture 교체. 직접 재생·실제 에셋 및 Battle Play 검증 완료. 전체 회귀 결과와 제한은 10절 참조 |
| A4 이벤트 툴 | 구현 완료 | A1, A2 | 프리팹/정의/클립 선택, 프리뷰, 슬라이더 위 키, 두 파라미터 편집, 저장/재로드/충돌 차단, Scene 구성 안내. A5에서 실제 화면 검증과 재임포트 알림 보완 수행 |
| A5 통합 검증·문서 정리 | Scene·자동 검증·독립 리뷰·정리 완료, 사용자 사용 검증 대기 | A3, A4 | 관련 테스트 1,245개 중 1,203 성공·기존 실패 42개. 전용 Scene 및 주요 화면 흐름 검증, 재임포트 결함 수정 완료. 최종 사용 검증은 사용자 담당 |

### A3 이관 순서

1. 기존 Controller에서 실제 연결 클립·loop·speed를 확인하고 Profile/Definition에 명시적으로 옮긴다. 임의로 새 클립을 고르지 않는다.
2. 기존 View 컴포넌트에 Profile 필드를 추가한다. 불필요한 새 Scene 컴포넌트나 직업 레지스트리는 추가하지 않는다.
3. HexCharacterView의 준비/시작/완료/Idle/Move/수명 경로와 Runner의 전체 view tick을 연결한다. Presentation의 simulation 처리와 callback 재진입 방어는 유지한다.
4. Warrior와 Archer/Rogue variant 각각에 의도한 Profile을 연결한다. base prefab 변경이 variant에 미치는 상속 결과를 확인한다.
5. 같은 단계에서 controller 전제 테스트 fixture와 실제 프리팹 의존 테스트를 함께 이관한다. Profile 없이 Controller로 몰래 대체하는 영구 fallback은 남기지 않는다.
6. 직접 재생 경로 검증 후 대상 프리팹의 Controller 참조와 사용하지 않는 state 필드를 제거한다. 기존 Controller 에셋은 다른 참조가 있을 수 있으므로 이번 계획에서 삭제하지 않는다.
7. 원본 FBX·Avatar·material·모델과 관계없는 게임 로직은 변경하지 않는다. 변경 내용을 단계별로 되돌릴 수 있도록 코드·데이터·프리팹의 연결 단위를 함께 기록한다.

### A4 Scene 배치 경계

도구 Scene에는 Camera, Directional Light, PreviewRoot, ToolController가 필요하다. 사용자는 Scene 하나를 툴로 사용하지만, 이번 문서 작성 요청을 Scene 생성·배치 실행 허가로 해석하지 않는다. 기본 구현 산출물은 필요한 스크립트와 연결 지침이며 Scene 배치는 사용자가 수행한다. 사용자가 별도로 Scene 생성·배치를 명시적으로 요청하면 해당 범위에서 구성한다. 실제 배치와 조작을 확인하지 못했다면 A4/A5를 전체 완료로 표기하지 않고 기능 검증 대기로 남긴다.

2026-11-08 A5 진행 요청에서 사용자가 전용 툴 Scene 배치·실제 화면 조작 검증을 명시적으로 승인했다. 이 승인에 따라 `Assets/Scenes/AnimationEventTool.unity`를 구성한다.

### 병렬 작업 경계

- 메인이 데이터 계약·단계 승인·통합을 책임진다.
- A1 이후 재생 핵심부와 도구의 데이터 편집/저장 부분은 파일 소유권을 나눠 병렬 구현할 수 있다. 공통 데이터 변경은 담당자를 한 명으로 고정한다.
- HexCharacterView·BattleRunner·공용 테스트 fixture·Combat 프리팹은 연결된 이관 단위로 한 담당자가 조정한다.
- 모든 담당자는 다른 작업자의 변경을 되돌리지 않는다. 실제 Unity 상태 조작과 프리팹 저장은 동시에 수행하지 않는다.

## 8. 검증 계획

### 8.1 데이터와 직접 재생

- 빈 이벤트, 근접/투사체 enum, 프레임 0/끝/범위 밖, 마지막 프레임 0, 29.97/30/60 FPS, NaN/Infinity와 길이 0.
- 같은 clip을 참조하는 별도 Definition의 이벤트 독립성, 같은 Definition 공유의 일관성.
- 공격 시작 `t=0`, 중간 미완료, 정확한 끝/큰 delta 완료, 재시작 시 이전 완료 무효화, `delta=0` 일시정지.
- idle 상태에서도 시간 진행, 이동 중 재생, 동일 프레임 이중 tick 없음, 혼합 중 새 요청, 동일 역할 반복 요청.
- 공격 완료 시 정확히 한 번 피해 확정, 표시 실패의 fallback, 종료/재바인딩/중복 Dispose, 화면 밖 공격 완료.
- non-Humanoid 테스트 클립의 시간/혼합 검사와 실제 Humanoid 프리팹의 자세 확인을 구분한다.

### 8.2 기존 테스트 이관과 회귀

| 대상 | 해야 할 일 |
|---|---|
| `BattleRunnerTests`, `BattleMapRunnerTests`의 AnimatorFixture | 임시 Controller/state 생성을 직접 재생 데이터 fixture로 교체. `Animator.Update`로 시간 진행하던 검사를 실제 Runner tick 순서로 수정 |
| `BattlePresentationTestUtility` | reflection 기반 Presentation 호출과 새 시간 진행을 일관되게 연결. 테스트만 다른 시간을 소유하지 않도록 수정 |
| `BattleOverwatchInputTests`, `BattlePioneerInputTests`, `BattleStealthInputTests`, `BattleStealthRangeTests` | 클립 없는 Controller와 내부 필드 주입으로 IsReady를 만족시키던 fixture를 유효 재생 구성으로 교체 |
| `BattleRestartTests` | Missing.Attack state 실패 주입을 필수 Definition/clip 누락으로 교체. 재시작·실패 복구 단언 유지 |
| `BattleMapSceneTests` 및 실제 prefab을 읽는 Assassin/Pioneer/Movement/Spirit/Range 테스트 | 이관된 실제 Warrior/Rogue/Archer Profile 연결을 사용한 초기화 확인 |
| 기존 순수 simulation의 `BattleAttackTests` 등 | 게임 규칙 불변 회귀 실행. 새 player 때문에 순수 로직 테스트를 재설계하지 않음 |

반드시 보존할 시나리오: 공격 전/후 취소 구분, 공격자·대상 disable→reenable, 접근 중 disable, Runner disable, 대상 제거, 치명타 cleanup, pull/leap·반응 공격, 부분 초기화 rollback 후 정상 재시도, 완료 callback에서 전투 교체 시 새 전투를 같은 호출에서 진행하지 않음.

### 8.3 도구와 저장

- 세 Combat 프리팹 선택/교체, 최초 프레임 표시, 임의 순서 역스크럽과 끝 포즈, 원본/게임 스크립트 비활성 상태.
- 키 추가/선택/프레임 변경/종류 변경/삭제, 같은 프레임 복수 키, 정렬 뒤 선택 유지.
- 신규·기존 Definition 저장/재로드, Profile 미배정 정의 표시, 공유 정의 편집 표시, 미저장 작업본의 원본 비반영.
- Save 후 Play 종료·재진입 데이터 유지, dirty 전환 저장/버리기/취소, 이름 충돌, 외부 수정 충돌, 실패 시 작업본 유지. 쓰기 전 실패와 쓰기 후 재로드/검증 실패를 나누어 주입하고, 후자의 추가 저장 차단·실제 파일 재확인을 검증한다.
- 같은 FPS/길이를 유지한 원본 재임포트, Profile/clip 참조 변경, 저장 직전 변경 감지, 자기 Save의 통지, 재로드 실패/취소 후 차단 유지.
- prefab/clip 반복 교체와 Play 재진입에서 임시 graph·instance·clip·Editor 구독이 누적되지 않는지 확인한다.

### 8.4 실행 권한과 완료 판정

- 코드 수정 후에는 현행 `AGENTS.md`에 따라 영향받는 코드·의존 어셈블리·테스트 코드를 컴파일한다. 오래된 문서의 컴파일 제한과 충돌하면 현행 지침을 따른다.
- 최소 영향 어셈블리: 새 AnimationData/AnimationPlayback/AnimationEventTool, `RaggedBanner.CombatView`, `RaggedBanner.Combat.Tests`, 새 관련 테스트 어셈블리.
- 데이터 검증은 EditMode에서, 실제 Playables/Humanoid 동작은 적합한 실행 환경에서 검증한다. 별도 컴파일과 Unity Editor 컴파일을 구분하여 기록한다.
- 기존 실제 Scene의 Play 검증은 작업 승인과 프로젝트 지침이 허용한 범위에서 수행한다. 검증을 위해 Scene을 임의 생성/저장하거나 배포 빌드를 만들지 않는다.
- 기능 구현 후 구현에 참여하지 않은 리뷰어의 코드 리뷰를 받고, 확인된 범위 내 결함 수정과 영향 경로 재검증을 진행한다.
- 컴파일만 통과한 결과를 애니메이션·툴 조작·회귀 테스트 성공으로 보고하지 않는다. 미배치 Scene이나 실행 환경 문제는 미검증 항목과 이유로 남긴다.

## 9. 설계 선택과 trade-off

- 직접 재생은 현재 게임이 행동 전환을 코드로 결정하고 전환 수가 적다는 점에 맞춘 선택이다. Controller 기능 전체를 복제하지 않는다.
- 세 역할의 고정 노드와 가중치 처리는 현재 요구를 위한 제한된 구조다. 임의 스킬 목록이나 다중 레이어 지원이 필요해지면 그때 별도 변경 설계를 검토한다.
- 별도 Definition은 같은 clip을 서로 다른 공격 의미로 사용하는 경우를 명확히 구분한다. 에셋 수는 늘지만 prefab/state 이름에 이벤트 소유가 묶이지 않는다.
- IMGUI는 기존 도구의 관례와 필요한 입력 규모에 맞춘 선택이다. 별도 EditorWindow나 UI 프레임워크를 추가하지 않는다.
- 실제 이벤트 실행을 미루면 현재 피해 시점을 보존한 채 재생기와 데이터 작성기를 각각 검증할 수 있다. 타격 프레임으로 피해 시점을 바꾸는 작업은 별도 설계·승인이 필요하다.
- 성능 우위는 주장하지 않는다. 매 프레임 검색·LINQ·새 graph 생성·불필요한 할당을 피하되, 성능 변경이 필요하면 동일 조건으로 측정한다.

## 10. 리뷰 및 진행 기록

### 2026-11-08 계획 작성

- 실제 수행: 기존 코드·프리팹·Controller·클립 메타·테스트·문서와 Unity 6.3 공식 문서의 읽기 전용 조사.
- 변경 범위: 이 계획 문서 한 개.
- 독립 리뷰: Pass. 요구 범위·데이터 소유·기존 전투/테스트/프리팹 연결을 대조했으며 확인된 높음·중간·낮음 결함 없음.
- 비판적 리뷰: Pass. tick 순서·표시 실패·취소·재진입·자원 해제를 대조했으며 확인된 높음·중간 결함 없음.
- 통합 판정: Pass. 두 리뷰와 저장 계약 보완 부분의 후속 검토를 모두 완료했다. 코드 구현·실행 성공을 뜻하지 않는다.
- 채택한 보완 대안: 6.3의 자기 Save에 따른 상위 dependency hash 변화와 독립 소스 변경을 분리했다. Definition 저장만으로 프리뷰를 무효화하는 오탐을 줄이며, snapshot 관리가 소폭 늘어난다.
- 채택한 보완 대안: 6.3/8.3에서 쓰기 전 실패와 쓰기 후 검증 실패를 구분했다. 후자는 디스크 원본 보존을 단정하지 않고 저장 결과 미확정·추가 저장 차단·파일 재확인으로 처리하며 실패 주입 검증을 추가했다.
- 보류한 제안: 기존 두 AnimatorFixture의 공통 helper 통합은 구현의 필수 조건이 아니다. A3에서 새 테스트 데이터 계약이 정해진 뒤 필요한 중복만 검토하고 무관한 테스트 리팩터링으로 확대하지 않는다.
- 검증 한계: 실제 자세·혼합·역스크럽·끝점·저장 지속성·실패 복구는 구현 후 실행 검증이 필요하다. 핵심 설계 판단을 막는 미확정 사항이나 남은 확인된 결함은 없다.
- 미실행: 코드 구현, 에셋/Scene 변경, 컴파일, 테스트, Play 검증, 배포 빌드.
- 다음 작업: 사용자의 구현 진행 승인 후 A1 착수. 현재 요청으로 승인된 문서 작성은 완료했다.

각 단계 갱신 시 상태, 승인 범위, 실제 변경 파일, 리뷰 지적 처리, 수행한 검증, 미실행 검증과 이유, 미해결 문제 및 다음 작업을 기록한다. 체크되지 않은 기능을 완료로 옮기지 않는다.

### 2026-11-08 A0 승인 및 A1 구현

- 사용자 결정: 이 문서를 승인하고 다음 단계 진행을 요청했다. 승인된 설계를 반복 제안하지 않고 A1을 구현했다.
- 완료 범위: `AnimationDefinition`, `CharacterAnimationProfile`, `AnimationEventKey`, `AnimationEventKind`, `AnimationAttackKind`, `AnimationFrameUtility`와 데이터/테스트 asmdef.
- 코드 경로: `Assets/Scripts/AnimationData`의 C# 6개·asmdef 1개 및 Unity 생성 meta.
- 테스트 경로: `Assets/Tests/Editor/AnimationData/AnimationDefinitionTests.cs`, `AnimationFrameUtilityTests.cs`, `RaggedBanner.AnimationData.Tests.asmdef` 및 Unity 생성 meta.
- 검증 계약: 빈/동일 프레임 이벤트 허용, 잘못된 참조·Legacy·FPS/길이·프레임·enum·정렬 거부, 작성 기준과 원본의 불일치 거부, Definition별 이벤트 독립성과 명시적 공유, Attack 비반복을 확인한다. 검증은 입력이나 원본을 수정하지 않는다.
- 프레임 역변환: `TryGetFrame`은 입력 시간보다 뒤의 샘플을 선택하지 않는다. 마지막 프레임 계산의 허용 오차로 만들어진 후보가 미래 샘플이면 한 프레임 낮춘다. 정확한 프레임·길이로 제한된 끝점·소수 FPS 왕복은 유지한다.
- 자체 점검: 승인 범위, 직렬화 필드/API, 데이터 의존성, 모든 제어문 중괄호·비공개 필드 접두사·주석/로그 미추가, asmdef 구성을 확인했다.
- 독립 코드 리뷰: 최초 중간 지적 1건(TryGetFrame이 경계 직전 시간에 다음 프레임을 반환)을 수정하고 영향 경로 재검토 후 Pass. 남은 확인된 결함 없음.
- 테스트 보완: 메모리 클립 참조의 검증은 `EditorUtility.CopySerialized`를 통한 Unity 직렬화 복사로 명확히 했고, 길이 0 클립은 0시점 단일 키를 만들어 전제를 검증하도록 수정했다. 실제 디스크 저장·재로드는 A4 검증 범위로 유지한다.
- 실제 컴파일: Unity 6000.3.24f1 Editor에서 소스를 갱신하고 컴파일했다. `RaggedBanner.AnimationData.dll`과 `RaggedBanner.AnimationData.Tests.dll` 생성 및 컴파일 오류 0건을 확인했다. 별도 외부 빌드가 아니다.
- 실제 테스트: 새 데이터 테스트 어셈블리의 EditMode 테스트 75개 전체 성공. 최종 Unity MCP job `2641719af2a044fd8e27d6f87f70959d`의 succeeded와 completed 75/75를 확인하고, Unity가 저장한 `TestResults.xml`에서도 Passed 75 / Failed 0 / Skipped 0 / Inconclusive 0을 확인했다.
- 실행 검증의 한계: 이번 테스트는 데이터·시간 계산·메모리 직렬화 검증이다. 아직 작성하지 않은 Playables 재생, 실제 Humanoid 자세, 전투 연결, 툴 조작, 디스크 저장과 Play 종료 후 유지, 전체 전투 회귀 테스트는 실행하지 않았다.
- 설계 변경: 승인된 요구·범위·책임 경계 변경 없음. A1에 필요한 구체 API와 오류 검증을 확정했다.
- 다음 작업: A2 직접 재생 핵심부. 프리팹·기존 전투 코드·Scene·실제 애니메이션 데이터 에셋은 이번 단계에서 변경하지 않았다.

### 2026-11-08 A2 직접 재생 핵심부 구현

- 사용자 결정: A2 구현 진행을 명시적으로 요청했다. 승인된 설계의 범위·책임 경계를 유지했다.
- 완료 범위: `Assets/Scripts/AnimationPlayback`의 `CharacterAnimationPlayer`, `AnimationPlaybackStatus`, `RaggedBanner.AnimationPlayback.asmdef`와 Unity 생성 meta. A1 데이터 모듈만 참조하며 기존 전투 코드·프리팹·Scene·실제 데이터 에셋은 변경하지 않았다.
- 재생 계약: 고정 클립 노드 3개와 mixer를 초기화 시 생성한다. Manual graph에 명시적인 클립 시간을 설정하고 `Evaluate(0)`로 자세를 반영한다. Idle/Move는 반복, Attack은 즉시 0시점부터 시작해 끝에서 멈춘다. `deltaTime * Animator.speed`를 한 번 적용하며 0 delta는 정지한다. Idle/Move 전환과 Attack 이후 전환은 현재 가중치에서 0.1초 혼합하고, 동일 역할 요청은 시간·혼합을 재시작하지 않는다.
- 구체 API: `TryInitialize`, `IsReady`, `TryPlayIdle`, `TryPlayMove`, `TryPlayAttack(out ulong requestId)`, `TryAdvance(float deltaTime)`, `GetAttackStatus(requestId)`, `Dispose`. 공격 상태는 `Invalid`, `Playing`, `Completed`, `Interrupted`로 구분한다. 새 공격은 새 요청 ID를 받으며 이전 요청은 현재 공격 완료로 읽히지 않는다. 최신 완료 상태는 읽어도 소비되지 않고 Idle 전환·Dispose 후에도 유지되며, 재초기화·새 공격에서는 이전 요청이 중단 상태가 된다.
- 수명 계약: 초기화는 이전 세션을 해제하고 유효한 Profile·Animator·Avatar를 검사한다. 실패한 tick/요청은 진행 중 공격을 Invalid로 만들고 graph를 정리한다. 명시적 Dispose는 진행 중 공격을 Interrupted로 만들며 반복 호출 가능하다. Profile/Definition/Clip은 빌린 참조이고 파괴하지 않는다. Profile 슬롯 교체는 재초기화 후 적용한다.
- Animator 소유권: Controller를 분리하고 root motion·내장 이벤트를 끄며 AlwaysAnimate를 설정한다. 해제 때 자신이 설정한 값이 그대로인 항목만 원래 값으로 복원하여 외부 변경을 덮어쓰지 않는다. 같은 Animator를 여러 `CharacterAnimationPlayer`가 동시에 소유하지 않도록 등록/해제를 관리한다. 호출자는 Animator를 배타적으로 넘기고 Dispose를 보장해야 하며, 다른 임의 Playables 시스템의 소유권을 자동 중재하지 않는다. View의 OnDisable/OnEnable/Unbind 연결은 A3 범위다.
- 실행 중 발견·수정: Unity의 `hasBoundPlayables`는 Controller 제거·graph 파괴 후에도 true가 남아 정상 재초기화를 거부했다. 이 값을 소유권/해제 판정에 쓰지 않고 이 재생기의 실제 바인딩을 등록·해제하도록 수정했다. 테스트도 sticky 값 대신 다른 player의 재바인딩 성공으로 해제를 검증한다.
- 독립 리뷰 지적·수정: 파괴된 Avatar가 Unity의 null 동등 비교에서 놓칠 수 있는 중간 결함을 수정했다. 바인딩 당시 Avatar 존재 여부와 Humanoid 요구 여부를 기록하고 실행 중 필수 Avatar 소실·유효성을 재검사한다. 임시 복제 Avatar 파괴와 참조 제거 회귀 테스트를 추가했다.
- 독립 코드 리뷰 최종 판정: Pass. 구현에 참여하지 않은 리뷰어가 런타임·A1 의존 계약·Generic/Humanoid 테스트와 수정 경로를 직접 재검토했으며, 남은 확인된 결함은 없다. 리뷰어의 검증은 정적 검토이고 위 실행 검증은 메인이 수행했다.
- 테스트 경로: `Assets/Tests/Editor/AnimationPlayback/CharacterAnimationPlayerTests.cs`, `CharacterAnimationPlayerHumanoidTests.cs`, `RaggedBanner.AnimationPlayback.Tests.asmdef` 및 Unity 생성 meta. Generic Transform의 실제 포즈로 시간·속도·혼합·반복·재시작·중단·실패·설정 복원·해제·소스 소실을 검사한다.
- 실제 컴파일: Unity 6000.3.24f1 Editor의 전체 소스/에셋 refresh 후 AnimationData·AnimationPlayback 및 두 테스트 어셈블리를 컴파일했으며 컴파일 오류 0건을 확인했다. 별도 외부 빌드가 아니다.
- 실제 테스트: A1 데이터 75개 + A2 Generic 44개 + A2 Humanoid 6개, EditMode 총 125개 전체 성공. 최종 Unity MCP job `70679a9f0c4c4cd692999584b5d9185a`와 Unity 저장 `TestResults.xml` 모두 Passed 125 / Failed 0 / Skipped 0을 확인했다.
- 실제 Humanoid 검증: Warrior·Archer·Rogue를 임시 복제하고 기존 3개 FBX 클립을 연결하여 본 회전 변화, 공격 중간/완료, 마지막 자세 유지, 루트 위치·회전 유지, Controller 복원·재초기화를 확인했다. 원본 프리팹·클립·Avatar는 수정하지 않았고 임시 객체와 graph는 테스트 종료 시 정리했다.
- 검증 한계: 위 검증은 Editor에서 Playables를 실제 평가한 EditMode 테스트다. Game 뷰에서 사람이 보는 Play Mode 시각 검증이나 전투 통합 테스트 성공을 뜻하지 않는다. 내장 이벤트는 `fireEvents=false` 설정·복원만 검사했으며 Play Mode의 실제 이벤트 dispatch를 양성 대조로 검증하지 않았다. 실제 전투 연결·피해 확정·Runner tick 순서와 전체 전투 회귀는 A3에서, 툴·저장은 A4에서 검증한다.
- 다음 작업: A3 턴제 연결·프리팹·테스트 이관. 현재 게임은 기존 Controller 경로를 계속 사용한다.

### 2026-11-08 A3 전투·프리팹·테스트 이관

- 사용자 결정: A3 진행을 명시적으로 요청했다. 새 설계나 기능 확장 없이 승인된 이관을 수행했다.
- 런타임 변경: `HexCharacterView.cs`, `BattleRunner.cs`, `RaggedBanner.CombatView.asmdef`. View에 `_animationProfile`과 소유 player를 연결하고 Controller state/hash/normalizedTime 의존을 제거했다. 초기 바인딩은 세 역할의 유효 Profile을 요구하며 Controller fallback은 없다.
- 시간·제어 흐름: Runner의 구성 검사·피해 처리·파괴 View 정리 다음, busy 분기 전에 활성 바인딩 View 전체를 한 번 tick한다. 이후 기존 Presentation이 공격 시작·완료를 처리한다. 새 공격은 0시점으로 평가하고 같은 delta를 다시 적용하지 않는다. Presentation·Damage·순수 simulation 코드는 변경하지 않았다.
- 수명: View disable/unbind/destroy는 재생 요청을 하지 않는 해제 경로로 graph를 정리한다. bound re-enable은 재검사 후 새 Idle 세션을 만든다. Runner 컴포넌트 disable은 기존 논리 배틀/바인딩 유지 정책을 보존하고, 실제 shutdown/destroy의 ReleaseBattle에서 확정 결과 처리 후 Unbind한다. 표시 실패를 발행된 공격의 취소로 바꾸지 않는다.
- 데이터: `Assets/GameData/Animations/Profiles/{Warrior,Archer,Rogue}AnimationProfile.asset` 3개와 `Definitions/{직업}{Idle,Move,Attack}.asset` 9개를 Unity API로 생성했다. 직업별 Definition은 독립적이며 최초 이벤트는 모두 빈 배열이다. 기존 클립 서브에셋을 정확히 참조한다: Idle `HumanM@CombatIdle2H01`(30 FPS, 약 1.3333334초), Move `HumanM@Run01_Forward`(30 FPS, 0.6초), Attack `HumanM@Attack2H01`(30 FPS, 약 1.6000001초). Idle/Move loop·Attack non-loop를 유지한다.
- 프리팹: Warrior의 Profile 연결과 root Animator Controller 제거, Archer/Rogue variant의 직업별 Profile override를 적용했다. 모델·Avatar·재질·scale은 유지했다. 제거된 하위 Animator에 남아 있던 Warrior의 Controller override도 정리했다. Camp의 FBX 직접 인스턴스가 기존 Controller를 참조하므로 Controller 에셋은 삭제하지 않았다.
- 테스트 변경: `Assets/Tests/Editor/TurnBasedCombat`의 기존 11개 파일과 새 `BattleAnimationFixture.cs`, `BattleAnimationAssetTests.cs`. 공통 fixture는 유효한 Definition/Profile과 실제 TryBind를 사용한다. `BattleRunnerTests`, `BattleMapRunnerTests`, Overwatch/Pioneer/Stealth 입력·StealthRange, Restart, RunnerRange/RangeController, Presentation helper, 테스트 asmdef를 이관했다. Animator.Update·state 필드 주입 대신 Runner tick → Presentation 순서를 사용한다.
- 추가 회귀 5개: 전체 Idle View tick, 새 공격 0시점·Animator.speed 1회 적용, Unbind 반복 호출·소유권 해제·재바인딩, 실제 Runner.Update의 Idle 평가·disabled 정지·새 공격 시작, 세 프리팹의 독립 Profile/Definition·원본 클립·빈 이벤트·Controller 제거를 검증한다. 5개 모두 통과했다.
- 통합 중 수정: 재활성화 대상 테스트 View에 Profile이 없는 fixture 4건을 실제 바인딩 fixture로 교체했다. 로그 기대만 추가해 실패를 감추지 않았다.
- 독립 코드 리뷰: Pass. 구현에 참여하지 않은 리뷰어가 런타임·관련 호출부·데이터·프리팹·이관 테스트를 직접 조사했다. 낮음 지적 1건인 테스트 helper의 disabled Runner tick을 기존 수명 가드로 차단하고 실제 Runner.Update 회귀를 보완했다. 재검토 후 남은 확인된 A3 결함은 없다. 리뷰어의 검증은 읽기 전용 정적 검토이며 다음 실행 검증은 메인이 수행했다.
- 실제 컴파일: Unity 6000.3.24f1 Editor에서 전체 에셋/소스를 refresh하고 CombatView 및 관련 테스트를 현행 의존 어셈블리와 함께 컴파일했다. 컴파일 오류 0건. 실행 파일 빌드는 생성하지 않았다.

| EditMode 실행 | 전체 | 성공 | 실패 | 건너뜀 |
|---|---:|---:|---:|---:|
| 이관 전 Combat 기준선 | 1,051 | 978 | 73 | 0 |
| 최종 Combat | 1,056 | 1,014 | 42 | 0 |
| 최종 AnimationData | 75 | 75 | 0 | 0 |
| 최종 AnimationPlayback | 50 | 50 | 0 | 0 |
| 최종 합계 | 1,181 | 1,139 | 42 | 0 |

- 기준선 job: `325f3b1b45bc4a45acc937302081ca20`. 최종 job: `7db102ca8c444cacb592dcf0cea21a86`. Unity job 완료 결과와 `TestResults.xml`을 확인했다. 비교 원본은 작업 폴더의 `tmp/a3-animation-validation/baseline.xml`, `final-editmode.xml`에 보관했다.
- 남은 회귀 실패: 최종 실패 42개는 모두 기준선에도 실패했던 테스트 이름이다. 32개는 메시지·stack 동일, 6개는 fixture 수정에 따른 행 번호 이동뿐, Overlay 1개는 기대 화면 좌표만 소폭 달라졌다. RangeController 3개는 유효 바인딩으로 Setup 장애가 해소되어 본문의 기존 EditMode 수명 콜백 전제 실패가 드러났다. 이 3개를 동일 위치·동일 원인의 실패라고 보고하지 않는다.
- 실패 원인 대조: RangeController 3개는 enabled=false 또는 파괴 후 OnDisable/OnDestroy의 자동 실행을 전제한 범위 메시 해제 검사다. 애니메이션을 생성하지 않는 독립 RangeMeshView/RangePresentationLayer fixture도 기준선부터 같은 수명 전제로 실패한다. 별도 읽기 전용 조사에서 A3 새 회귀 근거는 발견되지 않았고 관련 없는 Range 구현은 변경하지 않았다. 나머지는 환경 표시·범위 표시·HUD/overlay·일부 수명 callback·치명타 비율 반올림 및 Scene fixture 실패 등이다. Scene 관련 7개는 테스트 실행 환경의 unsaved untitled scene 때문에 additive 생성 Setup에서 실패한다. 전체 전투 회귀가 성공했다고 보고하지 않는다.
- 실제 Play Mode 검증: 저장된 기존 `Assets/Scenes/Battle.unity`에서 정상 전투 시작을 확인했다. 세 직업의 6개 캐릭터가 모두 Profile 연결·player Ready·Controller 없음 상태로 초기화되고 Runner의 자동 Idle tick이 진행됐다. Rogue(2)의 Warrior(4) 대상 접근·공격 요청이 실제 프레임에서 완료됐으며 Attack 로컬 시간은 클립 길이 1.600000143초에 도달하고 busy가 해제됐다. 대상 체력은 10→8로 반영됐고 후속 프레임에서도 8을 유지했다.
- 실제 재시작·종료 검증: 기존 재시작 요청 직후 player 바인딩 수 6→0, 새 전투 초기화 후 6개 Ready 및 체력 초기화를 확인했다. Play 중 Console 오류 0건. 비포커스 실행을 위해 `Application.runInBackground`만 일시적으로 true로 바꾸고 원래 false로 복원했다. 검증 후 Play를 종료했고 바인딩 0, 기존 Battle 씬 하나, dirty=false를 확인했다. 씬을 생성·배치·저장하지 않았고 Battle.unity 파일 hash도 작업 전과 같다.
- 검증 한계: 실제 Play 검증은 시작·자동 Idle·접근 공격·피해 반영·재시작의 제한된 시나리오이며 모든 스킬·시각 품질의 수동 검증을 대신하지 않는다. 기존 회귀 실패 42개는 위와 같이 남아 있다. 이벤트 키의 게임 실행·피해 시점 변경·툴·디스크 저장 흐름은 이번 단계에 포함하지 않는다.
- 다음 작업: A4 애니메이션 이벤트 툴. 게임의 세 Combat 프리팹은 이제 직접 재생 경로를 사용한다.

### 2026-11-08 A4 이벤트 툴 구현

- 승인: 사용자가 A4 구현을 명시적으로 요청했다. 승인된 책임 경계대로 Editor Play Mode 툴을 구현했고 Scene 파일·기존 전투 코드·Combat 프리팹·기존 정의 데이터는 이 단계에서 변경하지 않았다.
- 구현 위치: `Assets/Scripts/AnimationEventTool`. Controller·IMGUI, 프리팹/정의/실제 클립 서브에셋 목록, 독립 작업본, 소스 스냅샷, 저장소, 수동 프리뷰를 분리했다. 런타임 asmdef에서 Editor 조건부 코드를 사용해 Scene 컴포넌트를 연결할 수 있으며 배포 플레이어에서는 툴 구현을 제외한다.
- 편집 기능: 세 Combat 프리팹과 Profile 역할 선택, 기존/새 정의, 정수 프레임 스크럽·재생·초 표시, 슬라이더 위 키, 같은 프레임 복수 키, 선택 유지, 프레임·근접/투사체 편집 및 삭제를 구현했다. 새 정의는 Profile에 자동 배정하지 않고 공유 정의의 사용처를 표시한다.
- 프리뷰 수명: 비활성 부모 아래 복제본을 준비해 게임 동작·충돌·Controller를 비활성화하고 이벤트와 loop를 제거한 복제 클립을 별도 수동 graph로 평가한다. 루트를 고정하며 교체·중단·파괴 시 graph, 캐릭터 복제본, 클립을 정리한다. 임의 사용자 스크립트의 Awake까지 차단하는 범용 격리를 보장하지 않으며 현재 Combat 프리팹을 검증했다.
- 저장·복구: 명시적 Save 전에는 원본을 바꾸지 않는다. 소스와 의존 파일·meta·메모리 상태를 검사하고 외부 변경을 차단한다. 자기 Save에서는 대상 정의의 변경만 허용하고 다른 원본은 독립 검사한 뒤 기준을 갱신한다. 쓰기 전 실패는 원본 메모리를 복원하고, 쓰기 후 검증 실패는 작업본·복구 경로를 보존한 채 재로드 전까지 차단한다. 클립 변경·작성 메타데이터 불일치는 명시적인 키 초기화 후 다시 작성한다.
- 독립 리뷰: 구현에 참여하지 않은 리뷰어가 저장·소스 검사부터 Controller·UI·프리뷰·테스트까지 읽기 전용으로 검토했다. 누락된 Assets 파일/meta를 정상 기준으로 캡처할 위험, 신규 저장 실패 시 비영속 SO 정리 누락, 작은 화면에서 속성 패널 잘림을 수정했다. 프레임의 계산된 초 표시도 보완했다. 최종 변경 재검토까지 **Pass**, 확인된 미해결 지적 없음이다.
- 실제 컴파일: Unity 6000.3.24f1 Editor에서 현행 소스·참조로 refresh 및 컴파일했고 툴과 관련 테스트 어셈블리의 컴파일 오류는 0건이다. 실행 파일 빌드는 생성하지 않았다.

| EditMode 검증 | 전체 | 성공 | 실패 | 건너뜀 |
|---|---:|---:|---:|---:|
| 최종 A4 Document | 10 | 10 | 0 | 0 |
| 최종 A4 AssetStore | 19 | 19 | 0 | 0 |
| 최종 A4 Preview | 23 | 23 | 0 | 0 |
| 최종 A4 Controller | 9 | 9 | 0 | 0 |
| 최종 A4 합계 | 61 | 61 | 0 | 0 |
| A4 작업 중 재검증한 AnimationData | 75 | 75 | 0 | 0 |
| A4 작업 중 재검증한 AnimationPlayback | 50 | 50 | 0 | 0 |

- 실행 기록: A4 초기 58개와 A1/A2 125개를 함께 실행한 job `95c85b85e4f348f68640ceb98d14b18e`는 **183/183** 통과했다. 이후 실제 Warrior·Archer·Rogue를 Controller의 소스 검사·프리뷰·키 선택·projectChanged까지 함께 통과시키는 3개 테스트를 추가하고 최종 A4 **61/61**을 확인했다. 단일 실행 186개로 합산 보고하지 않는다. XML은 `tmp/a4-animation-validation/editmode.xml`, `final-tool-editmode.xml`에 보관했다.
- 테스트 보정: 첫 실행의 base prefab 변경 검사는 PrefabUtility가 이름을 정규화해 문자열 교체가 실제 파일을 바꾸지 않아 실패했다. 테스트를 확실한 원본 파일 변경으로 수정한 뒤 통과했다. 이 실패를 제품 코드 결함으로 기록하지 않는다.
- 실제 Play 저장 검증: 기존 Battle 씬에서 격리된 테스트 정의에 프레임 18·Projectile 키를 저장했다. Play 종료 후 디스크 재로드와 다음 Play 진입 모두 같은 키를 유지했고 원본 Warrior Attack 정의의 이벤트 배열은 빈 값이었다. 임시 에셋 폴더를 삭제하고 Play를 종료했다. 이 검증은 저장 데이터의 수명 검사이며 툴 화면 조작 검증이 아니다.
- 검증 한계: 전용 Scene을 생성·배치하지 않았으므로 카메라 구도, IMGUI 클릭·스크롤, 확인 창 표시와 툴 Scene의 반복 Play 조작은 아직 검증하지 않았다. A3의 기존 Combat 회귀 실패 42개는 이번에 수정하거나 전체 재실행하지 않았다. A4 전체 및 A5 완료로 표기하지 않는다.
- 사용·배치 안내: [AnimationEventToolGuide.md](AnimationEventToolGuide.md). 다음 단계에 필요한 Scene 구성과 수동 확인 항목을 기록했다.

### 2026-11-08 A5 전용 Scene·통합 검증

- 승인과 인계: 사용자가 전용 Scene 배치와 실제 화면 조작 검증을 포함한 A5를 승인했다. 이후 “툴 사용은 내가 검증할게”라고 요청하여 추가 화면 자동 조작을 중단했다. 아래는 중단 전 실제 확인한 결과이며 최종 사용성 검증 완료를 대신하지 않는다.
- Scene: `Assets/Scenes/AnimationEventTool.unity`에 PreviewCamera, Directional Light, PreviewRoot, ToolController 네 루트를 구성했다. Controller의 Camera·Root를 연결하고 저장했다. 카메라는 `(0, 1.2, 4)`, 회전 `(10, 180, 0)`, FOV 40, 단색 배경이다. 빌드 설정에는 추가하지 않았다.
- 실제 화면 검증: computer-use로 세 Combat 프리팹 선택·스폰, Attack 역할 열기, 정수 슬라이더 이동·역방향 스크럽, 끝 프레임 48 정지, 키 추가·선택·동일 프레임 중복·스크롤·삭제, 프레임 변경 후 선택 유지, 근접/투사체 변경을 확인했다. 확인 창의 취소·저장 후 계속과 원본 재로드의 변경 버리기, 다른 FBX 클립 선택·명시적 키 초기화도 조작했다. 1920×1080과 1366×768에서 화면을 확인했다.
- 저장 유지: 미배정 테스트 정의 `__A5UiValidation`에 12F Melee·18F Projectile을 UI로 저장했다. Play 종료 후 디스크와 다음 Play에서 같은 키가 유지됐다. 이후 0F Melee를 추가하고 저장 후 프리팹 전환까지 확인했다. 테스트 정의는 작업 종료 시 정리했으며 기존 직업 정의에 키를 추가하지 않았다.
- 발견·수정한 결함: 기존 정의 파일의 내용만 바꾼 뒤 동기 `ImportAsset`을 수행하면 소스 스냅샷 비교는 변경을 찾지만 `EditorApplication.projectChanged`가 호출되지 않아 자동 차단이 실행되지 않았다. 기존 저장 직전 검사와 수동 목록 새로고침은 차단했으며 잘못된 덮어쓰기는 관찰하지 않았다. `AnimationEventToolAssetPostprocessor`가 임포트·삭제·이동 완료를 알리고 Controller가 다음 Update에 검사하도록 보완했다. 콜백에서는 pending 플래그만 설정하고, 자체 저장 중에는 보류해 새 snapshot 확정 이후 검사한다. 비활성화 시 구독과 예약 검사를 해제한다.
- 수정 실행 검증: 실제 파일 변경·재임포트 후 버튼을 누르지 않은 상태에서 `blocked=True`, `previewReady=False`, `draftDirty=True`를 확인했고 화면에도 키가 남아 있었다. 재로드로 복구한 다음 UI 저장 후 전환에서 `blocked=False`, 예약 검사 없음, 저장된 키 3개를 확인했다. 실제 ImportAsset을 사용하는 회귀 테스트 3개를 추가했다.
- 자원 수명: Play 종료 후 카메라 rect `(0,0,1,1)`, Scene 루트 4개·dirty=false를 확인했다. Play 중 PreviewRoot 비활성화 시 graph 무효·자식 0·작업본 보존·차단을 확인했다. ToolController 비활성화 시 카메라 영역 복원과 변경 알림 구독 해제를 확인했다. 중단된 마지막 재활성화 검사 호출은 성공 결과가 회수되지 않았으므로 그 세부 경로를 완료 검증으로 세지 않는다.
- 컴파일·독립 리뷰: Unity 6000.3.24f1 Editor의 현행 참조로 컴파일했고 오류 0건이다. 독립 리뷰어가 새 Scene의 GUID·참조·수명 경계와 재임포트 수정·테스트를 읽기 전용으로 검토했으며 최종 **Pass**, 확인된 미해결 지적 없음이다. 배포 빌드는 생성하지 않았다.

| A5 EditMode 최종 실행 | 전체 | 성공 | 실패 | 건너뜀 |
|---|---:|---:|---:|---:|
| AnimationData | 75 | 75 | 0 | 0 |
| AnimationPlayback | 50 | 50 | 0 | 0 |
| AnimationEventTool | 64 | 64 | 0 | 0 |
| Combat | 1,056 | 1,014 | 42 | 0 |
| 합계 | 1,245 | 1,203 | 42 | 0 |

- 실행 job: `8b3dd0920c48402cac5dac69d81b5f63`. XML은 `tmp/a5-animation-validation/final-editmode.xml`에 보관했다. 실행 결과는 기존 Combat 실패 때문에 실패 상태이며 전체 회귀 성공으로 보고하지 않는다.
- 기준선 비교: 별도 읽기 전용 조사자가 A3 최종 XML과 직접 대조했다. 기존 1,181개 모두 존재하며 성공/실패 상태 변경 0개, 신규 툴 64개 모두 성공이다. 실패 42개 중 41개는 fullname·실패 메시지·stack 원문이 동일하다. Overlay의 `CompositeDamageUsesColoredNumbersFromOneOriginThenSeparates` 1개는 기대 좌표만 `(-179.25, -103.57)`→`(-178.39, -103.20)`으로 달라졌으며 실제값 `(0,0)`, stack·실패 행은 같다. 신규 회귀는 확인되지 않았고 기존 실패 42개는 남아 있다.
- 기존 자산 보존: Battle 씬, Combat 프리팹, Profile, 기존 정의 총 16개 파일의 작업 전후 SHA-256이 모두 같다. 최종 씬은 AnimationEventTool이며 Play 종료·dirty=false 상태를 확인했다. 사용자 검증에 필요한 사용 안내는 [AnimationEventToolGuide.md](AnimationEventToolGuide.md)에 반영했다.
- 남은 범위: 최종 사용성·카메라 구도·개별 클립 품질과 최소 900×600 경계의 실제 조작은 사용자 검증 대상이다. 기존 Combat 실패를 해결한 상태나 A5 전체 사용 승인을 받은 상태로 표현하지 않는다. 공격 이벤트의 게임 로직 연결은 계속 범위 밖이다.

## 11. 기술 근거

- [Unity 6.3 Playables API](https://docs.unity.com/en-us/engine/6000.3/manual/animation-section/animation-mecanim/playables): Controller 없이 클립을 재생하고 동적으로 혼합할 수 있는 공식 API.
- [PlayableGraph.Evaluate](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/playables/playablegraph/evaluate): 수동 그래프 평가.
- [AnimationMixerPlayable](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/animations/animationmixerplayable): 클립 혼합.
- [AnimationClip.frameRate](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/animationclip/framerate): 클립 프레임 기준.
- [Animator.fireEvents](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/animator/fireevents): 내장 AnimationEvent 호출 제어.
- [AssetDatabase.GetAssetDependencyHash](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityeditor/assetdatabase/getassetdependencyhash): 소스·importer 등을 포함한 의존성 변경 감지.
## 툴 화면·시점 조작 변경 (2026-11-08)

사용자가 설계를 승인한 뒤 프리팹·애니메이션 목록과 이벤트 속성·저장·안내를 오른쪽 320px 스크롤 패널로 통합했다. 왼쪽 프리뷰와 하단 140px 타임라인은 같은 영역 계산을 카메라 viewport와 입력 판정에 사용한다. 900×600에서 프리뷰는 544×424px이다.

휠은 거리 1.5~10의 확대·축소, 오른쪽 드래그는 캐릭터 중심의 수평 회전과 15~80° 수직 회전으로 동작한다. 시점 초기화 버튼을 제공하며 동일 캐릭터의 항목 전환·재스폰은 시점을 유지한다. 다른 캐릭터를 성공적으로 연 뒤 기본 시점으로 돌아간다. 패널·모달 입력과 분리하고 드래그 종료·포커스 상실·리사이즈·차단·종료에서 입력 상태를 해제한다. 종료 시 캡처한 카메라의 소유한 상태만 복원한다.

데이터·게임 재생 계약과 저장된 Scene 배치는 변경하지 않았다. 설계 리뷰 두 명과 구현 후 독립 코드 리뷰는 Pass다. 실제 화면 조작·사용성 확인은 사용자 담당이며 이번에 수행하지 않았다.
검증: Unity 컴파일 오류 0건, 툴 EditMode 117/117 성공(job `ea13c83e5e0547dab662e96f24ac6ec0`). 결과 XML은 `tmp/animation-tool-layout-validation/editmode.xml`에 보존했다. Scene·Combat 프리팹·애니메이션 에셋의 파일 지문 변화는 없다. 이번 변경에서 전투 전체 테스트를 재실행하지 않았다.
