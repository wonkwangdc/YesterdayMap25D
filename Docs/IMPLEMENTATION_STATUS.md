# 구현 현황

최종 갱신: 2026-08-07

## 완료

- Unity 6000.3.20f1 프로젝트 구성
- `MainMenu → Prologue → Scavenge → Shelter` 기본 흐름과 추가 탐사 씬 `Exploration`의 Build Settings 연결
- Unity Primitive 기반 캐릭터·시설·환경 Prefab 10종
- Perspective 추적 카메라와 Inspector 조절 가능한 줌
- 마우스 목적지 이동, 선택, 자동 시설 상호작용, E키 상호작용
- Scavenge 제한 시간, 식량·식수·구급상자 4개 수집 한도와 자원 전달
- Scavenge 집을 Meshy FBX 환경으로 교체하고 Base Color·Normal 텍스처 및 전용 Material 적용
- 집 크기 15 × 11.4m 정렬, 현관 방향 보정, 정적 MeshCollider와 방별 수집 물자 재배치
- Scavenge 집의 X/Z 이동 면적을 3배인 약 45 × 34m로 확장하고 아이템·현관·조명 위치 동기화
- 기존 플레이어 루트에 Urban Survivalist 스키닝 모델과 걷기 Animator 적용
- 수집 물자를 2.5D 일러스트로 표시하고 하단 4칸 운반 UI에 획득 순서대로 노출
- 다섯 번째 수집 시도를 차단하고 `양손이 가득 찼습니다.` 안내
- Scavenge 화면 왼쪽 위에 `timer.png` 일러스트를 사용한 60초 원형 카운트다운과 남은 초 숫자 표시
- 씬 전체를 다시 만들지 않고 시계 UI만 복구하는 `Yesterday Map/Repair Scavenge Clock UI (Preserve Scene)` 메뉴
- Scavenge의 기존 `BunkerEntrance` 상호작용·Collider와 화살표는 유지하고 집 모델에 포함된 입구 외형 사용
- Shelter의 기존 맵 렌더링을 숨기고 충돌 구조만 확장해 유지한 채 `MeshyBunkerEntrance.prefab`을 약 42 × 31m 새 벙커 전체 외형으로 적용
- `MeshyBunkerDoor.prefab` Iron Vault 문을 Shelter의 `ExitDoor` 외형으로 사용하고 기존 `DoorObject` 상호작용·Collider 유지
- Shelter 오른쪽 아래 공간에 Meshy 산업용 배수펌프 오브젝트 임시 배치
- `CharacterStats`의 체력·배고픔·갈증·정신력 4종 독립 판정과 감염 시스템 제거
- `ResourceManager`의 식량·식수·약품·부품·배터리·연료
- Bed, WaterPurifier, Generator, Radio, Storage, Workbench, ExitDoor, Barricade
- 행동력 4, 3종 배급, 하루·야간 처리, 0~5 벙커 위험도
- 편의점·병원·주택가·경찰서·통신소의 5개 지도 클릭 영역, 장소 데이터, 4단계 별 위험도 표시
- 한국어 HUD, 창고·탐사·작업·엔딩 UI
- 낡은 금속·종이 테마 일시정지 메뉴, 어두운 적혈색이 스며드는 버튼 Hover·클릭 피드백, `저장하기`·`불러오기` 전용 시안을 사용하는 5슬롯 선택 화면, 슬롯별 저장 일차·시각 표시, MainMenu 최신 슬롯 이어하기
- 제공된 16:9 시안을 사용하는 설정 화면, 마스터 볼륨·마우스 감도 슬라이더, 전체화면·창모드·테두리 없는 창모드, 해상도·이동 키 프리셋·화면 깜빡임 감소 설정
- UI·아이템 사용·수면·엔딩 임시 SFX와 AudioListener 자동 보완
- Scavenge 비·번개·천둥 프리팹 및 오디오 동기화
- 날짜 진행과 범용 생존·게임 오버 엔딩 호출 구조
- 문 앞 T 이벤트를 1분기 선택, 2분기 노선 검증, 3분기 단서·지원과 최종 선택까지 연결
- 1분기 CSV 26개 전체를 실제 일정 후보로 사용하며, 회차마다 벙커 내부 이벤트 4개와 구조·합류 점수 이벤트 7개를 중복 없이 배정
- 2분기 CSV의 탐사 후속 사건은 당일 탐사 완료 후에만 문 앞 T 이벤트로 진행 가능
- 2분기 두 노선이 모두 끊기면 벙커 내부 이벤트 12개를 계열 점수 없이 날짜마다 순환
- 구조신호 엔딩 `마지막 주파수`, `문을 열어준 밤`
- 합류 엔딩 `낯선 사람들과`, `붉은 완장`
- 굶주림·탈수·치명상 실패 엔딩 `마지막 생존 기록`
- 체력 0 다음 아침 빈사, 빈사 재수면 경고·사망, 정신력 0 탐사 차단
- 시설별 `InteractionPoint` 접근 지점 지원
- 시설 클릭 시 접근 지점으로 이동한 뒤 시설을 바라보고 상호작용 처리
- Shelter 관리자 테스트 패널: 통조림·물·구급상자 실제 소비, 정신력 회복, 물자 보충, 전체 회복
- 발전기 저전력 시 벙커 조명·환경광 동기 점멸, 전력 0% 암전, 점멸 속도 조정과 깜빡임 감소 접근성 옵션
- Shelter 수집 축구공의 E키 차기와 물리 이동

## 검증 결과

- Unity 배치 컴파일 성공, Console 오류 0개
- 기존 Windows x64 개발 빌드 성공
- 2026-08-05 Windows x64 비개발 빌드와 독립 실행 스모크 테스트 성공
- Build Settings의 다섯 씬과 `MainMenu → Prologue → Scavenge → Shelter` 순차 전환 확인
- 한도윤 1명, Shelter 시설 8종 확인
- 목적지 이동, 시설 사용, 정수 작업, 하루 종료, 탐사, 엔딩 자동 테스트 통과
- 런타임 NullReferenceException 및 MissingReferenceException 0개
- 2026-08-03 BranchOne 캠페인 EditMode 테스트 222개 통과
- 2026-08-03 Shelter Play Mode에서 2일차 문 이벤트 표시·선택·일기 기록과 Console 오류 0개 확인
- 2026-08-03 Shelter 관리자 패널의 자원 차감·4종 상태 회복을 Play Mode에서 확인하고 BranchOne EditMode 테스트 238개 통과
- 2026-08-03 1분기 일정 500개 시드에서 26개 전체 후보 노출, 회차별 4개 내부 이벤트·7개 점수 이벤트 구성 검증
- 2026-08-03 Play Mode에서 2분기 탐사 전 이벤트 차단·탐사 후 해제와 모든 노선 실패 후 30일차 내부 이벤트 선택 처리 확인
- 2026-07-24 Unity 6000.3.20f1 배치 컴파일 재확인, C# 오류 0개
- 2026-07-27 Scavenge 현관에서 중앙 복도까지 런타임 이동, 새 집 MeshCollider 및 Console 오류 0개 확인
- 2026-07-27 식량·식수·구급상자 4개 수집 후 하단 슬롯 표시, 다섯 번째 수집 거부 및 안내 문구 확인
- 2026-07-27 3배 확장 맵 현관 진입 이동, 3D 한도윤 모델의 `IsMoving` 걷기/정지 전환, Console 오류 0개 확인
- 2026-07-28 Scavenge Play Mode에서 원형 시계와 남은 초 감소 확인, Console 오류·경고 0개
- 2026-07-29 새 나무·황동 타이머 PNG를 투명 Sprite로 적용하고 60초 제한, 원형 진행 영역, 남은 초 표시 및 Console 오류·경고 0개 확인
- 2026-07-29 새 벙커·배수펌프 FBX에 Base Color·Normal 전용 Material을 적용하고 Scavenge/Shelter 런타임 로드 및 Console 오류·경고 0개 확인
- 2026-07-29 Shelter 전체 맵을 Meshy 벙커 외형으로 교체하고 Scavenge/Shelter 두 씬의 Play Mode 및 Console 오류·경고 0개 확인
- 2026-07-29 벙커를 집과 비슷한 크기로 확대하고 Iron Vault 문을 Scavenge 집에서 Shelter 출입구로 이동
- 현재 빌드와 어디에서도 참조되지 않던 `Scenes/Map` 테스트 씬·백업 씬·미리보기 파일 제거
- 새 벙커 원본 FBX가 GitHub의 일반 파일 제한을 넘으므로 벙커·문·배수펌프 FBX를 Git LFS 대상으로 지정

## 참고

기존 `Prototype.unity`는 삭제하지 않고 보존했으며 현재 Build Settings에서는 제외되어 있다. `V02ProjectBuilder`가 현재 v0.2 씬·Prefab·데이터의 기준 생성 도구다. 집 FBX가 없을 때는 Primitive 대체 구조를 생성하고, `HouseInterior_Meshy.prefab`이 있으면 해당 Prefab을 우선 사용한다.

## 다른 컴퓨터로 인계할 때

- 현재 PC의 실제 작업 프로젝트는 `C:\Projects\YesterdayMap25D`다. 과거 기록의 OneDrive 및 Desktop 경로는 이전 작업 경로다. 다른 PC에서는 GitHub 저장소 루트, 즉 `Assets`, `Packages`, `ProjectSettings`가 함께 있는 폴더를 Unity Hub로 연다.
- 작업 시작 전 `main`의 최신 변경을 Pull하고 Unity가 에셋 가져오기를 마칠 때까지 기다린다.
- 코드뿐 아니라 변경된 `.unity`, `.prefab`, `.mat`, `.asset` 및 모든 `.meta` 파일을 함께 커밋해야 맵·가구·Inspector 연결이 다른 PC에도 전달된다.
- `Scavenge.unity`는 충돌이 나기 쉬우므로 여러 PC에서 동시에 전체 씬을 수정하지 않는다. 시계가 사라졌을 때는 전체 씬 재생성 대신 위의 시계 UI 복구 메뉴를 사용한다.
- 작업 종료 시 이 문서와 `Prompts/UNITY_CODEX_PROMPT.md`도 함께 갱신해 다음 작업자가 현재 상태와 검증 결과를 알 수 있게 한다.

## Shelter collected special-item display

- `CollectedSpecialItems` in `Shelter.unity` contains rough-positioned 3D
  displays for knife, teddy bear, clothes, soccer ball, baseball bat, and guitar.
- `ShelterCollectedItemDisplay` activates each prop from the existing
  `GameSession` special-item count; no duplicate inventory system was added.
- Display copies cannot be collected and have collision disabled.
- Verified hidden state with no records and all six visible after simulated
  collection. Shelter scene validation and Console errors: 0.

## Shelter diary shortcut and shelf consumption

- The existing Shelter diary opens and closes with `Tab`; `Escape` closes an open diary.
- The existing `StorageObject` interaction trigger now covers both IronVault food/water shelves.
- `E` consumes Food/Water and restores Hunger/Thirst; `F` opens the existing storage UI.
- No duplicate UI, inventory, resource manager, or interaction script was added.
- Play Mode verified diary toggle and shelf consumption. Console errors/warnings: 0.

## 2026-08-04 문서 최신화 및 엔딩 통합 검증

- `README.md`, `AGENTS.md`, `Prompts/UNITY_CODEX_PROMPT.md`, `Docs/NEXT_STEPS.md`를 4종 핵심 상태, 감염 미사용, 날짜 제한 없음, 다섯 씬 기준으로 갱신했다.
- BranchOne EditMode 테스트 238/238 통과.
- 실제 Play Mode에서 `MainMenu → Prologue → Scavenge → Shelter` 전환을 확인했다.
- Scavenge에서 4개 수집, 다섯 번째 차단, 1차 반입, 재파밍, 2차 반입을 확인했다.
- Shelter 진입 후 반입 자원과 기타 수집 기록 및 3D 기타 표시를 확인했다.
- 실제 Shelter의 `GameManager`, `EndingManager`, `EndingUI` 연결로 스토리 엔딩 4종과 굶주림·탈수·치명상 실패 엔딩을 확인했다.
- 7일에서 8일로 자동 엔딩 없이 진행하고, 양쪽 2분기 노선 실패 후 30일차 내부 사건 대화창이 열리는 것을 확인했다.
- 일시정지·5개 저장 슬롯·설정 패널, 5개 탐사 장소, 임시 SFX, Scavenge 날씨를 Play Mode에서 검증하고 Console 오류 0개를 확인했다.
- 2026-08-04 일시정지 첫 화면의 상시 저장 슬롯 Dropdown을 제거하고 저장/불러오기 버튼별 슬롯 선택 화면, 뒤로가기와 빈 불러오기 슬롯 비활성화를 Play Mode에서 확인. Console 오류·경고 0개
- 2026-08-04 일시정지 첫 화면 6개 버튼과 설정 닫기 버튼에 적혈색 Hover·Pressed 색과 0.18초 물듦 전환을 적용. `Time.timeScale == 0` 상태의 Hover 표시와 저장 슬롯 진입·뒤로가기를 Play Mode에서 확인, Console 오류·경고 0개
- 2026-08-04 제공된 1672 × 941 설정 시안을 실제 uGUI 설정 화면으로 적용. 슬라이더 2개, 3단계 화면 모드, 해상도 7개, 이동 키 2개와 닫기 동작을 Play Mode에서 변경·복원 검증, Console 오류·경고 0개
- 2026-08-04 설정 화면의 고정 펼침 목록 때문에 생기던 중간 공백을 제거하고 하단 컨트롤을 재배치. 화면 모드 Dropdown이 해상도처럼 클릭 시 아래로 펼쳐지며 3개 옵션을 표시하는 것을 Play Mode에서 확인, Console 오류·경고 0개
- 2026-08-04 저장·불러오기 슬롯에 확인 모달 추가. 빈 슬롯 저장, 기존 파일 덮어쓰기, 불러오기 문구를 분리하고 `예 / 아니오 / ESC` 동작을 Play Mode에서 확인. 기존 저장 파일을 변경하지 않은 상태로 Console 오류·경고 0개
- 2026-08-04 일시정지 UI 버튼 15개에 1024 × 256 투명 피 얼룩 Sprite 기반 Hover 효과 적용. Hover한 버튼 하나의 얼룩 전체가 0.6초간 동시에 서서히 진해지고 이탈 시 0.18초간 사라지는 것을 Play Mode에서 확인, Console 오류·경고 0개
- 2026-08-04 피 Hover Sprite의 강제 비율 변형 제거. 원본 4:1 비율을 유지한 Cover 배치와 버튼별 RectMask2D를 적용해 세로 눌림 없이 버튼 영역만 표시되는 것을 Play Mode에서 확인
- 2026-08-05 Scavenge 22개 및 Shelter 21개 Meshy 렌더러에 원본 에셋과 GUID를 유지하는 LOD 적용
- 2026-08-05 Scavenge 텍스처 최대 크기·mipmap·압축 및 그림자 설정 최적화
- 2026-08-05 BranchOne EditMode 238/238, Unity 컴파일 오류 0개, Windows x64 비개발 빌드와 독립 실행 스모크 테스트 성공
- 2026-08-05 탐사 장소별 로딩 화면을 `ExplorationLoading.prefab`과 `ExplorationLoadingView`로 분리. 5개 장소와 이미지를 Inspector 직접 참조로 연결하고 `ExplorationSceneController`의 런타임 UI 생성·문자열 이름 매칭 제거
- 2026-08-05 Scavenge 반입 요약·시간 종료 암전·게임 오버 UI를 `ScavengeStatusHUD.prefab`과 `ScavengeStatusView`로 분리. `ScavengeManager`는 타이머·수집·전환 규칙만 유지
- 2026-08-05 두 프리팹을 `Exploration.unity`, `Scavenge.unity`, `V02ProjectBuilder`에 연결. 두 씬 직접 Play Mode 실행 및 Unity Console 오류 0개 확인
- 2026-08-07 설정 프리팹에 `화면 깜빡임 감소` 토글 연결. Play Mode에서 발전기 저전력 조명이 일정 밝기를 유지하고 Scavenge 번개 광원이 차단되며 비·천둥 오디오는 유지되는 것을 확인. Console 오류 0개
