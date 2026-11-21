---
name: unity-prop-import
description: "RaggedBanner에 AI 생성 프랍 ZIP을 임포트할 때 사용한다. 모델·텍스처 이름 정리, PaintedCharacter 재질 생성과 모델 재질 연결을 수행한다. 경로와 이름은 사용자 지정이 우선이며, 생략하면 Props 경로와 내용에 맞는 이름을 사용한다. 이미지 생성·메쉬 수정·씬 배치는 포함하지 않는다."
---

# Unity prop import

## 기본값과 범위

프로젝트 루트는 현재 RaggedBanner 작업 폴더다. 아래 에셋 경로는 프로젝트 루트 기준이다.

- 입력: 사용자가 지정한 로컬 ZIP.
- 대상 경로: 사용자가 지정하면 그 경로를 사용하고, 생략하면 `Assets/Art/BG/Props`를 사용한다. 대상은 현재 프로젝트의 `Assets` 안이어야 한다.
- 기본 이름: 사용자가 지정한 이름을 우선한다. 생략하면 ZIP 이름과 내용으로 의미 있는 영문 PascalCase 이름을 정한다. `OakBarrel`, `WoodenCrate`는 예시이며 모든 프랍에 고정해서 쓰지 않는다. 생성 서비스의 UUID와 `3d model` 같은 접미사는 이름에서 제외한다.
- 출력: `<대상>/<이름>.fbx`, `<대상>/Textures/T_<이름>_BaseColor.<원본 확장자>`, `<대상>/Materials/M_<이름>.mat`과 각 Unity 메타 파일.
- 기본 셰이더: `RaggedBanner/Painted Character`. 원본 컬러맵을 `_BaseMap`에 연결하고 `_BaseColor`는 흰색으로 둔다.
- 메쉬, UV, 원본 텍스처 픽셀은 변경하지 않는다. 이 스킬은 모델을 생성하거나 단순화하지 않는다. 현재 작업의 승인 범위와 프로젝트 AGENTS 지침을 따르고 이미 승인된 설계는 재확인하지 않는다.

## 실행 환경과 기준 에셋

이 컴퓨터에서 확인한 Python은 다음이다.

`C:/Users/Doggie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe`

먼저 이 경로의 실행 가능 여부를 확인한다. 없으면 사용 가능한 런타임 도구나 설치된 Python을 확인하고 실제 사용할 실행 파일을 찾는다. WindowsApps의 `python.exe` 별칭만 보고 Python이 없다고 결론 내리지 않는다. 찾지 못하면 사용자에게 알린다. 스크립트 의존성은 Python 표준 라이브러리와 Pillow다.

스크립트가 읽는 기준 에셋:

- `Assets/Art/BG/Props/OakBarrel.fbx.meta`: 정적 모델 임포트 설정.
- `Assets/Art/BG/Props/Textures/T_OakBarrel_BaseColor.jpg.meta`: sRGB 컬러 텍스처 설정.
- `Assets/Art/BG/Props/Materials/M_OakBarrel.mat` 및 `.meta`: 부드러운 명암과 약한 반사 설정.
- `Assets/Art/PainterlyEnvironment/Character/PaintedCharacter.shader` 및 `.meta`: 실제 셰이더와 GUID.

기준 파일은 읽기만 한다. 새 에셋에는 새 GUID를 만들고 실제 원본 FBX의 재질 슬롯 이름으로 리맵한다. 기준 파일이 없어졌거나 다른 셰이더로 바뀌었다면 다른 에셋을 무작정 대입하지 말고 원인을 확인한다.

## 절차

1. ZIP 구성, 모델 수, 재질 슬롯, 컬러맵을 조사하고 최종 이름과 저장 위치를 정한다. 사용자가 이름을 생략하면 의미 있는 이름을 스스로 결정해 `--name`으로 전달해도 된다.
2. [scripts/import_prop.py](scripts/import_prop.py)를 `--apply` 없이 실행한다. 이는 읽기 전용 계획이며 출력 경로와 충돌을 보여 준다. 프로젝트 파일을 만들지 않는다.
3. 현재 승인된 범위에 해당하면 같은 인수에 `--apply`를 붙여 실행한다. 원본 ZIP과 기존 파일은 유지한다. 같은 이름이 이미 있으면 기존 에셋을 임의로 덮어쓰지 않는다.
4. 반환된 원본 보존·파일 참조 검증 결과를 확인한다. 실행 실패 시 실제 남은 파일을 확인하고 실패를 완료로 보고하지 않는다.
5. Unity MCP가 있으면 연결된 프로젝트가 현재 프로젝트인지 먼저 확인한다. 해당 텍스처·재질·모델의 임포트와 저장된 재질 리맵, BaseMap, 셰이더를 확인한다. 메모리에서만 바뀐 재질을 저장 완료로 취급하지 않는다.
6. Unity가 닫혀 있거나 MCP를 사용할 수 없으면 파일 저장과 정적 검증까지만 완료했다고 보고한다. Unity 실행, 코드 실행 도구를 통한 컴파일, 빌드, 씬 배치는 별도 요청 없이 수행하지 않는다.

현재 자동 처리 범위는 정적 binary FBX 하나, 연결된 재질 슬롯 하나, JPG 또는 PNG 컬러맵이다. 다른 모델 형식, 스킨·애니메이션, 여러 모델·재질은 이 스크립트로 임의 변환하거나 하나로 합치지 않는다. 해당 구조를 보고하고 필요한 추가 처리 범위를 정한다.

여러 이미지가 있으면 컬러맵 파일을 확인해 `--base-color`에 정확한 ZIP 내부 경로를 전달한다. 노멀·metallic·roughness 맵을 컬러맵으로 연결하지 않는다. PaintedCharacter의 기본 재질은 노멀맵을 비활성화하며 metallic/roughness 텍스처 슬롯은 없다. 원본에 추가 맵이 있으면 사용 여부를 보고하고, 요청 없는 텍스처 가공은 하지 않는다.

## 호출 예시

프로젝트 루트에서 실행한다. `$propPython`은 위에서 확인한 실제 Python 실행 파일이다.

```powershell
$propPython = 'C:/Users/Doggie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
& $propPython -X utf8 '.agents/skills/unity-prop-import/scripts/import_prop.py' 'C:/Users/Doggie/Downloads/wooden crate.zip'
& $propPython -X utf8 '.agents/skills/unity-prop-import/scripts/import_prop.py' 'C:/Users/Doggie/Downloads/wooden crate.zip' --apply
```

이름이나 경로를 지정한 요청이면 같은 인수를 계획과 적용 단계 모두에 전달한다.

```powershell
& $propPython -X utf8 '.agents/skills/unity-prop-import/scripts/import_prop.py' 'C:/Users/Doggie/Downloads/prop.zip' --name SupplyCrate --destination 'Assets/Art/BG/Storage' --apply
```

`--project-root`로 다른 작업 디렉터리에서도 프로젝트 루트를 지정할 수 있다. `--base-color` 값은 디스크 경로가 아닌 ZIP 내부 파일 경로다.

## 결과 보고

모델·텍스처·재질의 최종 경로와 이름, 원본 보존과 재질 참조 검증 결과를 간단히 보고한다. Unity에서 실제 임포트·표시를 확인했는지와 파일 설정만 확인했는지를 구분한다. 미해결 문제나 사용하지 않은 추가 텍스처가 있으면 함께 알린다.

