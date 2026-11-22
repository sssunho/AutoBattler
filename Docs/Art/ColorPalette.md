# RaggedBanner 색상표 (Color Palette v1)

- 기준: [ArtBaseline.md](ArtBaseline.md) v1, 기준일 2026-11-22
- 견본 이미지: [Baseline_v1/palette.png](Baseline_v1/palette.png) — 행 순서: 빛·대기 / 녹색 단계 / 무채색 단계 / 식물 고유색 / 흙·바위·붓 터치. 각 행은 아래 표의 순서를 따른다(대기 행의 천정색·원경 틴트, 식물 행의 공터 이끼는 생략).
- 모든 값은 머티리얼 Inspector에 표시되는 **sRGB hex**다. Unity가 선형 공간으로 변환해 계산하므로, 셰이더 상수(선형 값)와 숫자가 다를 수 있다.
- `(HDR …)` 표기는 1을 넘는 성분이 있어 hex로 정확히 표현되지 않는 값이다. 이 경우 괄호 안의 float 값이 기준이다.
- 색을 바꿀 때는 **이 표를 먼저 고치고** 머티리얼에 반영한다. 표와 머티리얼이 다르면 머티리얼을 표에 맞춘다.

## 1. 색 토큰

토큰은 역할 이름이다. 같은 토큰을 쓰는 머티리얼은 같은 값을 가져야 한다.

### 1.1 빛과 대기

| 토큰 | 값 | 사용처 |
| --- | --- | --- |
| `light.sun` | #FFE9BF (강도 2.0) | 태양 `Sun through the canopy` |
| `light.ambient.sky` | #B8C9E0 | RenderSettings 앰비언트 (Trilight) |
| `light.ambient.equator` | #A8B0A3 | 〃 |
| `light.ambient.ground` | #57614A | 〃 |
| `atmos.fog` | #669CC9 | RenderSettings 안개 (Linear 67~176m) |
| `atmos.sky.horizon` | #8CC0EC | `M_Sky_Meadow._Bottom_Color` |
| `atmos.sky.zenith` | #084494 | `M_Sky_Meadow._Top_Color` (메인 시점에서 보이지 않음) |
| `atmos.camera.bg` | #73A3C7 | Main Camera 배경색 |
| `atmos.vista.shade` | #C2D1E0 | `M_Vista_Terrain._FlatShadeTint` |
| `atmos.vista.tint` | #FFFFFF (HDR 1.040, 1.065, 1.170) | `M_Vista_Terrain._Color_Tint` |

### 1.2 단계 틴트 — 녹색 계열 (잎·풀·지면)

| 토큰 | 값 | 사용처 |
| --- | --- | --- |
| `band.leaf.lit` | #FFFFFF | 나뭇잎 `_BandLitColor` |
| `band.leaf.mid` | #9CAD91 | `M_Tree_MeadowLeaf._BandMidColor` |
| `band.leaf.shade` | #577570 | `M_Tree_MeadowLeaf._BandShadeColor` |
| `band.leaf.birch.mid` | #879D7E | `M_Tree_BirchLeaf._BandMidColor` |
| `band.leaf.birch.shade` | #3D655F | `M_Tree_BirchLeaf._BandShadeColor` |
| `band.ground.lit` | #FFFFFF | `M_Ground_Turf`, `M_ClearingEdge` `_BandLitColor` |
| `band.ground.shade` | #8AA89E | 〃 `_BandShadeColor` = `_BandMidColor` (단색 그림자) |
| `band.grass.lit` | #F0F0F0 | `M_Grass_*._GrassBandLitColor` |
| `band.grass.shade` | #73948A | `M_Grass_*._GrassBandShadeColor` = `_GrassBandMidColor` (단색 그림자) |

### 1.3 단계 틴트 — 무채색 계열 (바위·흙·소품·캐릭터)

| 토큰 | 값 | 사용처 |
| --- | --- | --- |
| `band.neutral.lit` | #FFFFF7 (HDR 1.040, 1.020, 0.970) | `M_Prop_CampAtlas`(와 Variant `M_Prop_DungeonAtlas`·`M_Prop_DungeonRealmsAtlas`·`M_Prop_GenericAtlas`), `Synty_DungeonRealms_01A_Painted` `_BandLitColor` |
| `band.neutral.lit.canvas` | #F0F0E8 | `M_Prop_CampCanvas._BandLitColor` (천이 하얗게 날지 않도록) |
| `band.neutral.mid` | #A8B8CC | 소품·캐릭터 `_BandMidColor`, 바위 `_RockBandMidColor` |
| `band.neutral.shade` | #5C7594 | 소품·캐릭터 `_BandShadeColor` |
| `band.rock.shade` | #788FA8 | `M_Rock_*._RockBandShadeColor` (역광 대비 완화로 소품보다 밝음) |
| `band.path.shade` | #91A8C2 | `M_ClearingEdge`, `M_Ground_Turf` `_PathBandShadeColor` = `_PathBandMidColor` |

### 1.4 고유색 (albedo)

| 토큰 | 값 | 사용처 |
| --- | --- | --- |
| `albedo.leaf.meadow` | #274B1B | `M_Tree_MeadowLeaf._BaseAlbedoColor` |
| `albedo.leaf.birch` | #264613 | `M_Tree_BirchLeaf._BaseAlbedoColor` |
| `albedo.grass.low` | #8A9E60 | `M_Grass_Short/Medium._LowColor` |
| `albedo.grass.mid` | #92A561 | `M_Grass_Short/Medium._MidColor`, `_HighColor`; `M_Grass_Tall._HighColor` |
| `albedo.grass.tall.low` | #8B9D64 | `M_Grass_Tall._LowColor` |
| `albedo.grass.tall.mid` | #7A9557 | `M_Grass_Tall._MidColor` |
| `albedo.turf` | #889C5A | `M_Ground_Turf._BaseColor` |
| `albedo.turf.clearing` | #7A9747 | `M_ClearingEdge._BaseColor` |
| `albedo.path` | #979A87 | `M_ClearingEdge._PathColor` (채도 낮은 흙 길) |
| `albedo.path.headland` | #B58F69 | `M_Ground_Turf._PathColor` (마스크 경로) |
| `albedo.moss` | #73804F / #657142 | `M_Ground_Turf` / `M_ClearingEdge` `_MossColor` |
| `albedo.rock.bluff` | #929D9E | `M_Rock_BluffGranite._BaseColor` |
| `albedo.rock.wall` | #99A3A8 | `M_Rock_Wall._BaseColor` |
| `albedo.rock.mossy` | #99A699 | `M_Rock_Mossy._BaseColor` |
| `albedo.rock.field` | #9DA5AA | `M_Rock_Fieldstone._BaseColor` |

### 1.5 붓 터치 (지면 셰이더)

| 토큰 | 값 | 사용처 |
| --- | --- | --- |
| `brush.warm` | #FFF2D1 / #FFE9C4 | `M_Ground_Turf` / `M_ClearingEdge` `_WarmTint` |
| `brush.cool` | #DEF5FF / #D6EAFF | 〃 `_CoolTint` |

## 2. 후처리 색 보정

색상표 값은 **후처리 전** 값이다. 화면에는 아래 보정이 더해진다.

| 효과 | 값 | 영향 |
| --- | --- | --- |
| White Balance | Temperature +4 | 전체가 약간 따뜻해짐 |
| Color Adjustments | Post Exposure 0.3, Contrast 8 (Saturation 사용 안 함) | 밝아짐, 채도는 머티리얼 값 그대로 |
| Shadows | (0.92, 0.92, 1.0) | 어두운 영역이 약간 청색 |
| Midtones | (1.0, 0.98, 0.94) | 중간톤이 약간 따뜻함 |
| Tonemapping | Neutral | 밝은 영역 압축 |

## 3. 화면 측정 기준 (PPV 포함 게임 뷰, 메인 시점)

색을 바꾼 뒤 아래 범위를 크게 벗어나면 의도한 변화인지 확인한다. 측정값은 선형 휘도 기준이다.

| 항목 | 기준값 | 메모 |
| --- | --- | --- |
| 화면 평균 밝기 | 약 0.28~0.30 | |
| 밝은 흙 / 그늘 밝기 비 | 약 2.1~2.3 | 너무 높으면 그림자가 무거움, 너무 낮으면 들뜸 |
| 오른쪽 풀밭 채도 | 약 0.47 | 0.53 이상이면 형광처럼 튐. 2026-11-22 PPV 조정 전 측정값이므로 재측정 필요 |
| 그늘 흙 색 | 중성~약간 따뜻한 회색 | 회청이 강하면 차갑고 무거움 |

## 4. 변경 이력

| 날짜 | 변경 |
| --- | --- |
| 2026-11-22 | v1 작성. 사용자 조정값(바위 그림자 #788FA8, 바위 중간 #A8B8CC, 흙 그림자 #91A8C2) 반영 |
| 2026-11-22 | 스터디 자산 프로덕션 정리에 맞춰 사용처의 머티리얼 이름을 새 이름(`M_*`)으로 갱신. 색 값 변경 없음. 이름 대응표는 [ArtBaseline.md](ArtBaseline.md) §9.1 |
| 2026-11-22 | 화면이 노랗게 보여 PPV 조정(사용자): White Balance Temperature 8→4, Color Adjustments Saturation 10→사용 안 함. 같은 화면 비교에서 평균 채도 약 14%, R−B 차 약 32% 감소, 평균 휘도 변화 없음 |
