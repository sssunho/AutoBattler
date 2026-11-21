# Importer 설정과 참조 보존

Root Transform 또는 Rig·클립 설정을 실제로 변경할 때 읽는다. 아래 필드는 Unity `ModelImporterClipAnimation` 기준이다. 현재 Editor 버전과 MCP 기능에 맞는 공개 API를 확인한다. 사용 가능한 `unity_reflect`·`unity_docs`가 있다면 활용하고, 그렇지 않으면 해당 Unity 버전의 공식 문서를 확인한다.

## Root Transform 필드

| Inspector 설정 | 공개 API 필드 | 프로젝트 값 |
| --- | --- | --- |
| Rotation / Based Upon: Original | `keepOriginalOrientation` | `true` |
| Rotation / Bake Into Pose | `lockRootRotation` | `true` |
| Y / Based Upon: Original | `keepOriginalPositionY` | `true` |
| Y / Feet 사용 해제 | `heightFromFeet` | `false` |
| Y / Bake Into Pose | `lockRootHeightY` | `false` |
| XZ / Based Upon: Original | `keepOriginalPositionXZ` | `true` |
| XZ / Bake Into Pose | `lockRootPositionXZ` | `false` |

`rotationOffset`, `heightOffset`은 별도 설정이다. Original을 선택한다고 기존 offset까지 0으로 초기화하지 않는다. 이 프리셋은 Animator의 `applyRootMotion`을 변경하지 않는다. Bake 설정이 회전·높이·수평 이동의 추출 방식에 미치는 영향은 모션에 따라 확인해야 한다. [Unity ModelImporterClipAnimation API](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityeditor/modelimporterclipanimation)

`.meta`를 읽어 비교할 때 Bake 대응 직렬화 키는 `loopBlendOrientation`, `loopBlendPositionY`, `loopBlendPositionXZ`일 수 있다. 이는 `loopTime`·`loopPose`와 구분한다. 버전별 직렬화 형식을 확인하며 공개 API 이름을 YAML 키로 그대로 쓰지 않는다. 기본 수정 경로는 Unity Importer API이다.

## 클립 보존

1. `clipAnimations`에 명시된 항목이 있으면 그 배열의 모든 항목에서 요청된 필드만 변경한다.
2. 명시된 항목이 없으면 `defaultClipAnimations`의 전체 클립과 take·프레임 범위를 출발점으로 삼는다. 하나의 FBX에 클립 하나만 있다고 가정하거나 첫 항목만 복사하지 않는다.
3. 클립 이름, take, first/last frame, loopTime·loopPose·cycleOffset, mirror, additive reference pose, maskType·maskSource·transform mask, events, curves 및 offset은 요청 밖이면 유지한다.
4. 변경한 배열을 Importer에 다시 지정하고 승인된 MCP 기능으로 저장·재임포트한다. 재조회로 실제 저장과 요청 외 필드 보존을 확인한다.
5. 재임포트 전후 클립 이름과 local file ID를 비교한다. 기존 컨트롤러·프리팹의 참조가 끊겼으면 완료로 처리하지 않고 원인을 조사한다. 클립 분할·이름 변경은 참조에 영향을 줄 수 있으므로 별도 승인 범위에서 처리한다.

기존 `.anim`은 ModelImporter가 없는 독립 에셋이다. 적용 가능한 클립 설정과 원본 재임포트가 필요한 설정을 구분하여 보고하고, FBX와 동일한 설정이 가능하다고 가장하지 않는다.

## Avatar와 임포트 순서

- 애니메이션을 해석할 **원본 스켈레톤 Avatar**와 게임 캐릭터의 **대상 Avatar**는 역할이 다르다. 대상 캐릭터 Avatar를 무조건 모든 FBX의 Source Avatar로 지정하지 않는다.
- Humanoid 대상으로 기준 모델을 새로 준비하는 경우 단위·스케일을 확인하고 Humanoid / Create From This Model로 유효한 Avatar를 만든 뒤, 호환되는 모션에 Copy From Other Avatar를 적용한다. Generic·비인간형 리그에는 이 Humanoid 생성 절차를 적용하지 않는다. 기존 패키지의 유효한 모델별 Avatar는 우선 보존한다.
- 본 이름뿐 아니라 계층, 기준 자세, 실제 본 Transform 스케일의 호환성을 확인한다. `globalScale = 1`이라는 사실만으로 단위가 일치한다고 판단하지 않는다. `useFileScale`과 원본 단위도 확인하며 0.01 또는 1을 모든 공급자에 고정하지 않는다.
- 기준 모델부터 임포트를 마치고 Avatar를 다시 로드한 다음 의존 모션을 처리한다. 기준 Avatar가 바뀌었으면 의존성을 확인하고 영향을 받는 모션만 갱신한다.
- 묶음 작업이 중간에 실패하면 성공·실패·미처리 대상을 구분한다. Assets 전체 refresh나 전수 재임포트보다 정확한 대상 재임포트를 우선한다. 에셋 편집 일괄 처리 기능을 쓴다면 실패 시에도 종료·해제를 보장한다.

## 검증 기록

변경할 범위의 경로, 기존 GUID와 클립 식별자, 요청 관련 설정, Avatar·Mask 참조, 임포트 전 로그 기준점을 기록한다. 외부 파일을 복사했다면 원본→대상 해시를 추가한다. 모든 프로젝트 에셋을 조사하거나 영구 카탈로그를 만드는 작업으로 확대하지 않는다.

설정 재조회, 의존성, 최신 Rig 경고까지 확인되면 **임포트 설정 검증**을 마친 것이다. 별도로 허용된 캐릭터 재생 확인을 하지 않았다면 루프 연결·발 미끄러짐·방향·타격 타이밍의 품질을 확정하지 않는다.
