# 기준 리그와 Humanoid

경로는 RaggedBanner 프로젝트 루트 기준이다. 구조 기준과 현재 캐릭터의 가중치 편집본을 구분한다.

## 기준 파일

- 구조 기준: `Art/Characters/MedievalKnight_Fingers_20260925/MedievalKnight_Fingers_Rigged.blend`
- Unity 기준 결과: `Assets/Art/Character/Base/MedievalKnight_Rigged.fbx`
- Armature: `MedievalKnight_20260924_Rig`
- 손가락 구조 기록: `Art/Characters/MedievalKnight_Fingers_20260925/README.md`, `finger_checks.json`, `finger_pose_checks.json`
- 2026-11-01 턱 보정 후 편집본 후보: `Art/Characters/MedievalKnight_JawFix_20261101/MedievalKnight_JawFix.blend`. 이 기록 시점의 결과이며, 다음 작업에서도 현재 Unity FBX와 메시·골격·가중치가 일치하는지 먼저 확인한다.
- 최신 보정 기록: `Art/Characters/MedievalKnight_JawFix_20261101/validation.json`, `neck_transition_checks.json`, `unity_import_checks.json`. 마지막 파일은 재임포트·Humanoid 인식·머티리얼·GUID 검사 기록이며 지정 Bow 클립의 실제 재생 검증 기록은 아니다.

`Art/Characters/MedievalKnight`의 이전 리그나 `MedievalKnight_20260924`의 손가락 없는 25개 뼈 버전과 혼동하지 않는다. 실제 뼈·부모·rest pose·로컬 축·bone roll·변형 설정을 파일에서 읽는다. 문서와 파일이 다르면 차이를 보고하며 기준을 임의로 바꾸지 않는다. 구조 기준의 오래된 가중치로 이후 손가락·턱 보정을 되돌리지 않는다.

## 55개 뼈 구조

- Root, Hips, Spine, Chest, UpperChest, Neck, Head와 양쪽 Shoulder, UpperArm, LowerArm, Hand, UpperLeg, LowerLeg, Foot, Toes, Pauldron의 25개 뼈에 양손 손가락 30개를 더한다.
- 좌우 접미사는 `.L`/`.R`, Root는 비변형 뼈이며 기본 FK 리그다. IK는 기본 범위에 없다.
- Thumb, Index, Middle, Ring, Little마다 01·02·03 관절을 둔다. 예: `Thumb.01.L → Thumb.02.L → Thumb.03.L`. 01은 같은 쪽 Hand, 02·03은 앞 관절의 자식이며 양쪽 모두 변형 뼈다.
- 신규 모델은 구조 규칙을 따르되 체형·관절 위치·길이·방향을 실제 메시로 판단한다. 다른 캐릭터의 좌표나 가중치를 그대로 복사하지 않는다.
- Pauldron이 있으면 기준 파일의 부모·추종 설정을 확인한다. FBX에서 실행되지 않는 Blender 제약은 승인된 내보내기 범위에 맞는 베이크 또는 별도 Unity 처리 필요성을 구분한다.

## 손가락 작업과 검증

- 손가락 길이·굵기·벌어짐과 실제 메시 연결로 관절 중심을 정한다. 엄지의 벌림·맞섬 방향은 다른 손가락과 별도로 확인한다.
- 손가락 수가 다르거나 벙어리장갑처럼 분리되지 않은 형상이면 먼저 보고한다. 5개 구조에 맞추려고 메시·토폴로지를 임의로 바꾸거나 변형 불가능한 리그를 완료 처리하지 않는다.
- 기존 리그에 손가락을 추가할 때 필요한 손 영역의 Hand 가중치를 재분배한다. 손 바깥 가중치, 기존 뼈 rest transform과 애니메이션 곡선을 보존한다.
- 양손 편 자세, 손가락별 독립 굽힘, 엄지 맞섬, 주먹, 임시 손잡이 쥐기를 확인한다. 손가락 사이 찢김·늘어짐, 손바닥 평탄화, 엄지 붕괴·관통과 무관한 정점 이동을 근접 검사한다.
- 손목·팔꿈치 굽힘과 사용 중인 idle에서도 확인한다. 진단용 손잡이와 포즈는 최종 결과에서 제거·복원한다. 실제 무기별 쥐기 애니메이션 제작은 별도 범위다.

## Unity Humanoid

- 임포트가 요청되면 손가락 30개의 Proximal·Intermediate·Distal 매핑을 포함한다. 예: `Left Thumb Proximal → Thumb.01.L`, `Right Little Distal → Little.03.R`. 실제 Unity HumanTrait 이름을 확인하고 기존 몸통 매핑을 보존한다.
- 애니메이션 공유는 각 모델의 Avatar 매핑과 T 자세를 확인한다. 체형 차이로 실루엣·접촉이 달라질 수 있으며, 같은 기준 구조라는 이유로 하나의 Avatar 파일을 강제로 공유하지 않는다.
- 해당 매핑에 필요한 임포터 변경만 승인 범위에서 수행한다. 임포터 설정만 다루는 작업은 프로젝트의 `unity-animation-setup` 스킬 범위와 구분한다.
- Unity에서 사용할 수 있는 조회 도구로 Avatar 유효성·인간형 여부, 손가락 매핑·스킨 본을 확인한다. `execute_code`를 통한 메모리 내 컴파일도 명시 허용 없이는 수행하지 않는다.
- 기존 FBX 덮어쓰기는 백업 후 GUID·하위 에셋 참조·재질 연결·클립을 보존한다. 재임포트 후 계층·가중치·기존 idle 보존을 확인한다. 접근 또는 검증이 불가능하면 매핑 표와 미검증 항목을 보고한다.
