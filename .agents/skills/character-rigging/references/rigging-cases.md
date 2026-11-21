# 리깅 실패·보정 사례

문제 부위와 관련된 사례만 참고한다. 경로는 프로젝트 루트 기준이다. 기록과 스크립트는 당시 모델의 증거이며 새 모델의 직접 검사나 일반 보정 알고리즘을 대신하지 않는다.

## MedievalKnight 턱·목

높이만으로 Neck–Head 전이를 정해 아래턱이 목에 과도하게 묶였다. 가중치 누락과 합계에는 문제가 없고 기본 자세도 정상이었지만 큰 머리 회전에서 아래턱이 목에 붙잡혀 늘어났다.

하악을 Head에 묶은 첫 후보에서는 실제 목 경계에 가중치 급차가 새로 생겼다. 인접 피부 정점의 전이를 고르게 하고 실제 Neck 시작 부근까지 연결한 뒤 턱 형태를 유지하면서 경계 변형을 완화했다. 최종 124정점 수정은 이 캐릭터에 한정된다. 큰 각도의 목 피부 변형은 남았으며 Unity의 지정 Bow 클립 실제 재생은 검증하지 않았다.

- 작업·수치 기록: `Art/Characters/MedievalKnight_JawFix_20261101/validation.json`, `neck_transition_checks.json`, `unity_import_checks.json`
- 수정 방법 참고: `Art/Characters/MedievalKnight_JawFix_20261101/fix_jaw_weights.py`
- 근접 비교: `Captures/CharacterReview/KnightJawFix_20261101/yaw_left_40_BeforeAfter.png`, `Yaw40_FrontSide_BeforeAfter.png`, `Weights_BeforeAfter.png`, `pitch_up_30_BeforeAfter.png`, `pitch_down_30_BeforeAfter.png`

해당 스크립트의 캐릭터 이름·정점 번호·좌표·피부색 마스크를 다른 모델에 그대로 실행하지 않는다. 턱과 목의 실제 표면을 확인하고, 턱 개선 후 목 경계까지 검사하는 판단 절차를 참고한다.

## FatBandit 어깨·팔·엄지

높이·좌우 좌표 분류로 어깨에 Neck 약 76~87%, 팔 안쪽에 Chest·Spine 약 47~49%, 엄지 부근에 Chest 최대 약 11%가 섞였다. 정규화·누락 검사로 잡히지 않았고 팔 내림에서 평탄화와 손 변형이 드러났다.

- 수치 기록: `Art/Characters/FatBandit_20260924/weights_fixed_checks.json`
- 비교 이미지: `Captures/CharacterReview/FatBanditWeights_20260924/BeforeArmInner.png`, `AfterArmInner.png`, `BeforeHandClose.png`, `AfterHandClose.png`
- 방법 참고: `Art/Characters/FatBandit_20260924/fix_upper_limb_weights.py`

부위 마스크·국소 전이·보존 검사 방법을 참고하되 대상 이름·정점 번호·치수는 재사용하지 않는다. FatBandit 수정본을 새로운 기준 리그로 자동 승격하지 않는다.
