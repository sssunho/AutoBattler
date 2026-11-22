# RaggedBanner 아트 기준 (Art Baseline v1)

- 상태: **v1 (look) 동결, 스터디 종료.** 사용한 셰이더·머티리얼은 `Assets/Art/PainterlyEnvironment/` 프로덕션 폴더로 정리했다(§9).
- 기준 씬: `Assets/Scenes/Studies/ForestOverlookLeafLighting.unity` (스터디 씬, Battle 씬 미연결)
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
| 바위 (`M_Rock_*`) | 3단 + 면 흔들림 | **0.62** | 순광에서 면이 한 색으로 뭉치지 않도록 올림. 밝은 면 안 형태 명암 0.1 |
| 지면 (`M_Ground_Turf`, `M_ClearingEdge`) | **2단 (단색 그림자)** | 0.55 | 중간 색 = 그림자 색 |
| 풀 (`M_Grass_*`) | **2단 (단색 그림자)** | 0.55 | 중간 색 = 그림자 색. 그림자 드리우지 않음 |
| 캠프 소품·캐릭터 (`Painted Character`) | 3단 | 0.45 | 밝은 면 안 형태 명암 0.09 |
| 줄기·가지 | 기존 부드러운 단계 | — | 미정 (로드맵 5단계) |
| 원경 지형 | 2톤(그림자 감쇠만) | — | 3단 명암은 효과가 미미해 채택하지 않음 |
| 동굴 바닥 (`M_Ground_CaveFloor`) | 주광 2단 (단색 그림자), **추가 광원은 단계 없음** | 0.62 | `M_Rock_Wall` Variant. 평면에서 추가 광원 단계가 동심원 고리로 보여 `_AdditionalLightStepBlend = 0`(사용자 결정) |

### 3.3 팔레트 계열
- **녹색 계열** (잎·풀·지면): 그림자 틴트는 청록, 중간 틴트는 채도 낮은 회녹.
- **무채색 계열** (바위·캠프 소품·캐릭터·흙): 그림자 틴트는 회청. 따뜻한 햇빛을 곱했을 때 중성~회청이 되도록 청색 쪽으로 보정한 값.
- 흙 길은 **채도 낮고 약간 뿌연 색**을 유지한다. 따뜻한 황토색이나 보라 그림자 같은 강한 보색 대비는 채택하지 않는다(사용자 A/B 결정).

## 4. 조명·대기 수치 (기준 씬)

| 항목 | 값 | 근거 |
| --- | --- | --- |
| 태양 방향 | (53, 100, 0) 측광 | 메인 카메라에서 그림자 형태가 보이도록. 순광(53,51)은 그림자가 숨음 |
| 태양 색 / 강도 | #FFE9BF / 2.0 | |
| 그림자 | Soft, Soft Shadow Quality **Low** (`UniversalAdditionalLightData`) | Hard는 픽셀 계단이 딱딱, 파이프라인 기본 Soft는 가는 그림자가 녹음 |
| 그림자 강도 | **1.0** | 0.74에서는 드리운 그림자가 중간 단계로 칠해져 형태가 흐려짐 |
| 그림자 바이어스 | Use Pipeline Settings **Off** (`UniversalAdditionalLightData`), Depth 0.05, **Normal 0** | Normal 0.5에서는 가는 다리·팔 그림자가 사라져 캐릭터가 떠 보임 |
| 앰비언트 | Trilight (하늘 #B8C9E0 / 지평 #A8B0A3 / 지면 #57614A) | 프로젝트 셰이더는 `_PainterlyAmbientColor`(현재 0)만 사용 |
| 안개 | Linear #669CC9, **67~176m** | 카메라 거리 이동만큼 함께 이동 |
| 하늘 | `M_Sky_Meadow.mat` 지평선 #8CC0EC | 메인 카메라에서는 지평선 색만 보임 |
| 풀 그림자 드리우기 | **Off** (렌더러 설정, `*_Painted` 풀 프리팹에 포함) | 풀밭 얼룩 제거, 약 2ms 절감(사용자 측정) |

조명·카메라 값은 프리셋 에셋 없이 이 표로 관리한다. Light·Camera 프리셋만으로는 URP 추가 데이터(그림자 품질·바이어스 오버라이드·후처리 토글)가 재현되지 않기 때문이다. 새 맵에서는 이 표의 값을 직접 맞춘다.

### 4.1 카메라

| 항목 | 값 |
| --- | --- |
| FOV | **20** |
| 기울기 | 30° |
| 위치 | (1.85, 31.20, −48.08) — FOV 45 대비 시선 방향으로 약 36m 후퇴 |
| Far | 420 |
| Post Processing | **On** (`UniversalAdditionalCameraData`) |

FOV를 낮추면 같은 화면 크기를 유지하도록 카메라를 뒤로 빼고(`거리 × tan(45/2)/tan(FOV/2)`), 안개 시작·종료도 같은 거리만큼 민다.

### 4.2 후처리 (`Assets/Art/BG/PP/PP_PainterlyDay.asset`)

| 효과 | 값 |
| --- | --- |
| Tonemapping | Neutral |
| White Balance | Temperature +4 |
| Color Adjustments | Post Exposure 0.3, Contrast 8 (Saturation 사용 안 함) |
| Shadows/Midtones/Highlights | Shadows (0.92, 0.92, 1.0), Midtones (1.0, 0.98, 0.94) |
| Vignette | 강도 0.195, 부드러움 0.366 |

## 5. 셰이더 기능 요약 (이번 작업에서 추가)

| 셰이더 | 추가 기능 | 켜는 방법 |
| --- | --- | --- |
| `Rock/PaintedEnvironment.shader` | Rock 모드 3단 명암, 면 흔들림(`_RockFacetJitter`) | `_SurfaceKind = Rock`이면 항상 |
| `Rock/PaintedEnvironment.shader` | 추가 광원(포인트 라이트 등) 단계 혼합 `_AdditionalLightStepBlend` | 기본 0.45(기존 동작). 0이면 단계 없는 그라데이션 |
| `Grass/PaintedGrass.shader` + `PaintedGrassLighting.hlsl` | 3단 명암 (`_GrassBandLighting`) | 옵트인 토글 (기본 0) |
| `Ground/PaintedGround.shader` (`RaggedBanner/Painted Ground`) | 공터 경계 단단함(`_ClearingEdgeSharpness`), 흙 전용 틴트(`_PathBandBlend`) | 기본 0. 현재 경계 0(번진 경계 선호), 흙 틴트 1 |
| `Vista/PaintedVistaTerrain.shader` (`RaggedBanner/Painted Vista Terrain`) | 원경 지형 2톤 | — |

- 지면 셰이더의 3단 명암은 원래 있던 기능을 `_FlatGroundLighting = 0`으로 켠 것이다.
- 원경 지형 3단 명암과 그림자 탈색 기능은 시험 후 제거했다.
- 지면 셰이더의 Clearing 레이아웃(`_GroundSurface = 2`)은 머티리얼에 맵의 월드 좌표(`_ClearingFootprint`, `_ClearingGrassEdge`)를 넣어 쓴다. 그래서 공터 머티리얼은 맵마다 따로 만들고 맵 폴더에 둔다.

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
| Synty 캐릭터 | Kevin Iglesias Humanoid 리타게팅은 크게 틀어지지 않음(사용자 확인). Animator의 **Root Motion을 켜면** 발 접지·회전이 의도대로 나온다. Root Motion을 끌 때의 처리는 확인 필요. 캐릭터 재질은 추후 결정 (테스트 자산은 `Assets/Art/Character/Synty/`) |
| 전투 화면 | Battle 씬 카메라·헥스 오버레이·UI와 함께 검증하지 않음 |

## 8. 재현성·관리

1. 조명·안개·카메라 값은 §4 표로 관리한다(프리셋 미사용).
2. **공유 머티리얼 주의:** 프로덕션 머티리얼 대부분은 두 스터디 씬(`Studies/ForestOverlookLeafLighting`, `Studies/ForestOverlookLowFov`)도 함께 쓴다. 값을 바꾸면 두 씬에 같이 반영된다.
3. **Synty 업데이트 주의:** 나무 `_Split` 메시와 `*_Painted` 프리팹의 오버라이드는 Synty 원본 FBX의 자식 구성에 의존한다. Synty 패키지를 업데이트해 자식 이름·구성이 바뀌면 오버라이드가 풀리거나 Split 메시가 맞지 않을 수 있으므로, 업데이트 후 `*_Painted` 프리팹을 확인한다.
4. 바위·꽃·덤불·소품은 Synty 프리팹에 `M_*` 머티리얼만 바꿔 쓴다. 자주 쓰게 되면 그때 `*_Painted` 프리팹으로 만든다.

## 9. 자산 위치 (프로덕션)

루트: `Assets/Art/PainterlyEnvironment/`

| 폴더 | 내용 |
| --- | --- |
| `Ground/` | `PaintedGround.shader`, `Materials/M_Ground_Turf` |
| `Rock/` | `PaintedEnvironment.shader`(바위·줄기·가지 공용), `Materials/M_Rock_*` |
| `Grass/` | `PaintedGrass.shader`, `Materials/M_Grass_Short/Medium/Tall`, **`Prefabs/SM_Env_Grass_*_Painted` 11개** |
| `Trees/` | `PaintedTreeLeaves.shader`, `Materials/M_Tree_*`(근경 잎·카드·줄기·가지), `Materials/Far/`(원경 변형), `Meshes/*_Split`, `Textures/T_MeadowPaletteAtlas`, **`Prefabs/SM_Env_Tree_*_Painted` 5개** |
| `Plants/Materials/` | `M_Flower_*` 6개, `M_Bush_*` 4개 |
| `Props/Materials/` | `M_Prop_CampAtlas`(Synty 아틀라스 소품·돌무더기), `M_Prop_CampCanvas`. 팩별 아틀라스는 `M_Prop_CampAtlas`의 Material Variant로 `_BaseMap`만 바꾼다: `M_Prop_DungeonAtlas`(PolygonDungeon), `M_Prop_DungeonRealmsAtlas`(PolygonDungeonRealms), `M_Prop_GenericAtlas`(PolygonGeneric) |
| `Vista/` | `PaintedVistaTerrain.shader`, `Materials/M_Vista_Terrain`, `M_Vista_River`(+부모 `M_Vista_RiverBase`) |
| `Sky/Materials/` | `M_Sky_Meadow` |
| `Character/` | `PaintedCharacter.shader` (변경 없음) |

- **프리팹:** `*_Painted`는 Synty 원본 프리팹의 Prefab Variant다. 머티리얼, `_Split` 메시, LOD 높이, 풀의 Cast Shadows Off만 오버라이드한다. 나무는 근경 잎 조합 기준이다.
- **맵 전용 자산:** `Assets/Scenes/SyntyForestOverlook/`에 이 맵의 지형 메시와 공터 머티리얼(`M_ClearingEdge`, `T_ClearingEdgeMask`)이 있다.
- **PPV:** `Assets/Art/BG/PP/PP_PainterlyDay.asset`
- **스터디 씬:** `Assets/Scenes/Studies/` (Build Settings 미포함). LowFov 전용 머티리얼(`Clearing.mat`, `PathMask.png`)만 `Studies/ForestOverlook/`에 남았다.

### 9.1 이름 대응표 (스터디 → 프로덕션)

| 이전 | 이후 |
| --- | --- |
| `Studies/ForestOverlook/OverlookStudy.shader` (`RaggedBanner/Studies/Forest Overlook`) | `Ground/PaintedGround.shader` (`RaggedBanner/Painted Ground`) |
| `Studies/ForestOverlook/VistaTerrain.shader` (`…/Vista Terrain`) | `Vista/PaintedVistaTerrain.shader` (`RaggedBanner/Painted Vista Terrain`) |
| `Stone_BluffGranite` / `Stone_RockWall` / `Stone_Mossy` / `Stone_Fieldstone` | `M_Rock_BluffGranite` / `M_Rock_Wall` / `M_Rock_Mossy` / `M_Rock_Fieldstone` |
| `GrassShort` / `GrassMedium` / `GrassTall` | `M_Grass_Short` / `M_Grass_Medium` / `M_Grass_Tall` |
| `HeadlandTurf` | `M_Ground_Turf` |
| `ClearingEdge`, `ClearingEdgeMask.png` | `Scenes/SyntyForestOverlook/M_ClearingEdge`, `T_ClearingEdgeMask.png` |
| `Flowers_Card01~03` / `Flowers_Flat` / `Flowers_Meadow03` / `Flowers_MeadowPetals` | `M_Flower_Card01~03` / `M_Flower_Flat` / `M_Flower_Meadow03` / `M_Flower_MeadowPetals` |
| `BushBranches` / `BushMeadow` / `BushMeadowDense` / `BushMeadowSmall` | `M_Bush_Branches` / `M_Bush_Meadow` / `M_Bush_MeadowDense` / `M_Bush_MeadowSmall` |
| `Branches_Birch` / `Branches_Meadow` | `M_Tree_Branches_Birch` / `M_Tree_Branches_Meadow` |
| `Branches_ValleyBirch` / `Branches_ValleyMeadow` | `Far/M_Tree_Branches_ValleyBirch` / `Far/M_Tree_Branches_ValleyMeadow` |
| `PaintedTrees/Materials/<이름>_Painted`, `<이름>_Trunk` 등 | `M_Tree_<이름>` (`_Painted` 제거). `Riverbank*`·`Valley*`는 `Far/` |
| `PaintedTrees/Meshes/*_Split` | `Trees/Meshes/*_Split` (이름 유지) |
| `MeadowPaletteAtlas.png` | `Trees/Textures/T_MeadowPaletteAtlas.png` |
| `CampAndStone_Palette` / `Camp_Canvas` | `M_Prop_CampAtlas` / `M_Prop_CampCanvas` |
| `ValleyTerrain` / `RiverWater` / `Scenes/SyntyForestOverlook/RiverBlue` | `M_Vista_Terrain` / `M_Vista_River` / `M_Vista_RiverBase` |
| `Sky_PaintedMeadow` | `M_Sky_Meadow` |
| `Assets/Art/BG/PP/Test.asset` | `PP_PainterlyDay.asset` |
| `Assets/Scenes/ForestOverlook*.unity` | `Assets/Scenes/Studies/ForestOverlook*.unity` |

전체 이동 목록(85건): `Captures/RockMaterial_20261121/asset_move_map.tsv`. 모든 이동은 GUID를 유지했다.

## 10. 관련 자료

- 단계별 작업 캡처: `Captures/RockMaterial_20261121/`
- 스터디 원본 상태: git 커밋 `831b3a0` (브랜치 `art/forest-overlook-baseline`)
- 색상표: [ColorPalette.md](ColorPalette.md)
