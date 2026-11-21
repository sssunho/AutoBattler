# RaggedBanner 아트 기준 (Art Baseline v1)

- 상태: **v1 (look) 동결.** 시각 방향의 기준이며 제작 파이프라인(프리셋·도구)으로의 고정은 v1.1 과제다.
- 기준 씬: `Assets/Scenes/ForestOverlookLeafLighting.unity` (연구 씬, Battle 씬 미연결)
- 기준일: 2026-11-22
- 색 값은 [ColorPalette.md](ColorPalette.md)에서 관리한다. 이 문서는 규칙·수치·결정 근거를 다룬다.

## 1. 기준 화면

| 파일 | 내용 |
| --- | --- |
| [Baseline_v1/main_fov20.png](Baseline_v1/main_fov20.png) | 메인 카메라 (FOV 20, PPV 포함 게임 뷰) |
| [Baseline_v1/shadow_close.png](Baseline_v1/shadow_close.png) | 캐릭터와 지면 그림자 근접 |
| [Baseline_v1/meadow_backlit.png](Baseline_v1/meadow_backlit.png) | 역광 풀밭·바위 (PPV 미적용 카메라 렌더) |

## 2. 한 줄 정의

> **Synty 에셋을 그대로 쓰면서, 따뜻한 햇빛과 단단한 단색 그림자로 칠한 과슈 일러스트.**

- Synty 특유의 각진 면·로우폴리 실루엣은 결함이 아니라 재료다. "Synty스럽지 않게"는 목표가 아니다.
- 판단 기준은 빛·그림자 색의 통일, 명도 덩어리와 분리, 단단한 형태, 대기 원근이다.

## 3. 핵심 규칙

### 3.1 잎 규칙 (모든 재질의 기준)
나뭇잎 셰이더(`RaggedBanner/Painted Tree Leaves Three Tone`)의 표현을 다른 재질이 따른다.

1. **또렷한 명도 단계.** 빛 받는 정도 = `saturate(N·L) × 그림자 감쇠`를 경계값으로 잘라 2~3단으로 칠한다. 전이폭은 0.06 + 화면 미분(fwidth) 안티앨리어싱.
2. **드리운 그림자도 같은 단계 안에서 칠한다.** 그림자를 별도 효과로 얹지 않는다.
3. **단계 색은 고유색에 곱하는 틴트.** 밝은 면은 흰색(또는 그에 가까운 값), 그림자 면은 차갑고 채도 낮은 색.
4. **디테일은 그라데이션이 아니라 단단한 가장자리의 형태로.**

### 3.2 재질별 적용

| 재질 | 단계 | 밝은 면 경계 | 비고 |
| --- | --- | --- | --- |
| 나뭇잎 | 3단 | 0.45 | 기준. 셰이더(ASE 생성) 수정 금지 |
| 바위 (`Stone_*`) | 3단 + 면 흔들림 | **0.62** | 순광에서 면이 한 색으로 뭉치지 않도록 올림. 밝은 면 안 형태 명암 0.1 |
| 지면 (`HeadlandTurf`, `ClearingEdge`) | **2단 (단색 그림자)** | 0.55 | 중간 색 = 그림자 색 |
| 풀 (`Grass*`) | **2단 (단색 그림자)** | 0.55 | 중간 색 = 그림자 색. 그림자 드리우지 않음 |
| 캠프 소품·캐릭터 (`Painted Character`) | 3단 | 0.45 | 밝은 면 안 형태 명암 0.09 |
| 줄기·가지 | 기존 부드러운 단계 | — | 미정 (로드맵 5단계) |
| 원경 지형 | 2톤(그림자 감쇠만) | — | 3단 명암은 효과가 미미해 채택하지 않음 |

### 3.3 팔레트 계열
- **녹색 계열** (잎·풀·지면): 그림자 틴트는 청록, 중간 틴트는 채도 낮은 회녹.
- **무채색 계열** (바위·캠프 소품·캐릭터·흙): 그림자 틴트는 회청. 따뜻한 햇빛을 곱했을 때 중성~회청이 되도록 청색 쪽으로 보정한 값.
- 흙 길은 **채도 낮고 약간 뿌연 색**을 유지한다. 따뜻한 황토색이나 보라 그림자 같은 강한 보색 대비는 채택하지 않는다(사용자 A/B 결정).

## 4. 조명·대기 수치 (기준 씬)

| 항목 | 값 | 근거 |
| --- | --- | --- |
| 태양 방향 | (53, 100, 0) 측광 | 메인 카메라에서 그림자 형태가 보이도록. 순광(53,51)은 그림자가 숨음 |
| 태양 색 / 강도 | #FFE9BF / 2.0 | |
| 그림자 | Soft, Soft Shadow Quality **Low** | Hard는 픽셀 계단이 딱딱, 파이프라인 기본 Soft는 가는 그림자가 녹음 |
| 그림자 강도 | **1.0** | 0.74에서는 드리운 그림자가 중간 단계로 칠해져 형태가 흐려짐 |
| 그림자 바이어스 | 광원 오버라이드: Depth 0.05, **Normal 0** | Normal 0.5에서는 가는 다리·팔 그림자가 사라져 캐릭터가 떠 보임 |
| 앰비언트 | Trilight (하늘 #B8C9E0 / 지평 #A8B0A3 / 지면 #57614A) | 프로젝트 셰이더는 `_PainterlyAmbientColor`(현재 0)만 사용 |
| 안개 | Linear #669CC9, **67~176m** | 카메라 거리 이동만큼 함께 이동 |
| 하늘 | `Sky_PaintedMeadow.mat` 지평선 #8CC0EC | 메인 카메라에서는 지평선 색만 보임 |
| 풀 그림자 드리우기 | **Off** (렌더러 설정) | 풀밭 얼룩 제거, 약 2ms 절감(사용자 측정) |

### 4.1 카메라

| 항목 | 값 |
| --- | --- |
| FOV | **20** |
| 기울기 | 30° |
| 위치 | (1.85, 31.20, −48.08) — FOV 45 대비 시선 방향으로 약 36m 후퇴 |
| Far | 420 |

FOV를 낮추면 같은 화면 크기를 유지하도록 카메라를 뒤로 빼고(`거리 × tan(45/2)/tan(FOV/2)`), 안개 시작·종료도 같은 거리만큼 민다.

### 4.2 후처리 (`Assets/Art/BG/PP/Test.asset`)

| 효과 | 값 |
| --- | --- |
| Tonemapping | Neutral |
| White Balance | Temperature +8 |
| Color Adjustments | Post Exposure 0.3, Contrast 8, Saturation 10 |
| Shadows/Midtones/Highlights | Shadows (0.92, 0.92, 1.0), Midtones (1.0, 0.98, 0.94) |
| Vignette | 강도 0.195, 부드러움 0.366 |

## 5. 셰이더 기능 요약 (이번 작업에서 추가)

| 셰이더 | 추가 기능 | 켜는 방법 |
| --- | --- | --- |
| `Rock/PaintedEnvironment.shader` | Rock 모드 3단 명암, 면 흔들림(`_RockFacetJitter`) | `_SurfaceKind = Rock`이면 항상 |
| `Grass/PaintedGrass.shader` + `PaintedGrassLighting.hlsl` | 3단 명암 (`_GrassBandLighting`) | 옵트인 토글 (기본 0) |
| `Studies/ForestOverlook/OverlookStudy.shader` | 공터 경계 단단함(`_ClearingEdgeSharpness`), 흙 전용 틴트(`_PathBandBlend`) | 기본 0. 현재 경계 0(번진 경계 선호), 흙 틴트 1 |

- 지면 셰이더의 3단 명암은 원래 있던 기능을 `_FlatGroundLighting = 0`으로 켠 것이다.
- 원경 지형 3단 명암과 그림자 탈색 기능은 시험 후 제거했다.

## 6. 하지 않는 것

- Synty 형태를 숨기려는 메시 수정이나 별도 보정
- 강한 보색 대비(주황 햇빛·보라 그림자)와 채도 높은 흙 길
- 그림자를 문턱값으로만 넓히기(`_LitThreshold` 0.68 등). 그림자가 부풀고 뭉툭해짐
- 나뭇잎 셰이더 직접 수정 (ASE 생성 코드)
- 근경 31m 안의 안개

## 7. 알려진 한계·미결정

| 항목 | 상태 |
| --- | --- |
| 캐릭터 가독성 | FOV 20에서 작고 흙과 색이 비슷함. 진영색·외곽선 등 미결정 (**최우선**) |
| 앞쪽 수관 | 화면의 약 35~40%. 전장 가장자리를 가릴 수 있음 |
| 하늘 | FOV 20 구도에서 보이지 않음 |
| 줄기·가지 | 잎 규칙 미적용 (로드맵 5단계) |
| 그림자 해상도 | 카메라가 멀어져 메인 시점 그림자 가장자리가 약간 거침. 파이프라인 그림자 거리(150m) 조정은 보류 |
| Synty 애니메이션 | Kevin Iglesias Humanoid 리타게팅 동작은 확인, 관통 등 세부 품질 미검증 |
| 전투 화면 | Battle 씬 카메라·헥스 오버레이·UI와 함께 검증하지 않음 |

## 8. 재현성·관리 (v1.1 과제)

1. **씬에만 있는 값**을 프리셋으로 묶는다: 태양(각도·강도·그림자 설정), 안개, 카메라, 풀 렌더러 그림자 끄기.
2. 풀 프리팹의 기본 Cast Shadows를 Off로 바꾼다(새로 배치하는 풀이 다시 그림자를 드리우지 않도록).
3. PPV 프로필 이름을 `Test.asset`에서 정식 이름으로 바꾼다.
4. **공유 머티리얼 주의:** 풀·`HeadlandTurf`·캠프 소품 머티리얼은 `ForestOverlookLowFov.unity`도 사용한다.
5. **버전 관리:** `Assets/Art/PainterlyEnvironment/`, `Assets/Art/Character/Synty/`, `Assets/Art/BG/PP/`, `Assets/Scenes/`가 git에 추적되지 않는다. 기준 동결 시 커밋이 필요하다.

## 9. 관련 자료

- 단계별 작업 캡처와 원복 백업: `Captures/RockMaterial_20261121/`
- 색상표: [ColorPalette.md](ColorPalette.md)
