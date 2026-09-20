# 《어제의 지도》 1분기·2분기·3분기 엔딩 계열 시스템 인수인계

- 문서 작성일: 2026-07-30
- 최종 갱신일: 2026-07-31
- 대상 기능 폴더: `Assets/Features/BranchOnePrototype/`
- 현재 구현 범위: 2~13일 차 1분기 테스트 흐름, 1분기 계열 판정, Signal/Join 2분기 순수 코어, Signal/Join 3분기 공통 순수 코어, 1→2→3분기 Campaign 통합, 독립 Sandbox 2종
- 실제 게임 연결 상태:
  - 1분기 문 앞 이벤트: `Shelter`에 부분 연결
  - 13일 차 1분기 판정: `Shelter` 미연결
  - 2분기·3분기 및 Campaign: `Shelter` 미연결
  - 기존 `DiaryUI`·`EndingManager`: 미연결
- 작업 담당 범위: `Assets/Features/BranchOnePrototype/` 내부의 순수 코어, Campaign, 독립 Sandbox, 자동 테스트, 이벤트 진행 규칙, 통합용 공개 API와 본 문서
- 최종 자동 테스트 결과: EditMode **214/214 통과**, 실패 0, Skip 0 — 2026-07-31 Quarter3 Campaign·Sandbox 통합 후 전체 재실행
- 최종 수동 테스트 상태: 통합 Sandbox의 대표 경로 4종 Play 검증 통과
- Build Settings 등록 여부: 두 Sandbox 모두 **미등록** (`buildIndex = -1`)

> 이 문서는 2026-07-31 현재 프로젝트의 실제 코드, Scene, asmdef, 테스트, 기존 `Shelter` 관련 코드를 기준으로 갱신했다. 이 문서에서 “1분기”는 `BranchOneFlowController`, “2분기”는 `Quarter2FlowController`, “3분기”는 `Quarter3FlowController`, “Campaign”은 세 Flow를 명시적으로 연결하는 `EndingRouteCampaignController`가 관리하는 범위를 뜻한다. 실제 Shelter에는 1분기 문 앞 이벤트의 부분 Bridge가 이미 존재하지만, 2·3분기와 Campaign의 정식 Shelter 통합은 별도 통합 담당자의 책임이다.

## 목차

1. [기능 개요](#1-기능-개요)
2. [현재 구현 완료 범위](#2-현재-구현-완료-범위)
- [작업 담당 범위](#작업-담당-범위)
3. [전체 폴더 및 파일 구조](#3-전체-폴더-및-파일-구조)
4. [핵심 아키텍처](#4-핵심-아키텍처)
5. [1분기 진행 구조](#5-1분기-진행-구조)
6. [1분기 계열 판정 규칙](#6-1분기-계열-판정-규칙)
7. [2분기 진행 구조](#7-2분기-진행-구조)
8. [2분기 계열 실패 및 전환 규칙](#8-2분기-계열-실패-및-전환-규칙)
- [3분기 공통 코어](#3분기-공통-코어)
9. [1분기·2분기·3분기 통합 구조](#9-1분기2분기3분기-통합-구조)
10. [Sandbox Scene 2개 설명](#10-sandbox-scene-2개-설명)
11. [Unity에서 직접 테스트하는 방법](#11-unity에서-직접-테스트하는-방법)
12. [자동 테스트 구성과 실행 방법](#12-자동-테스트-구성과-실행-방법)
13. [현재 구현되지 않은 내용](#13-현재-구현되지-않은-내용)
14. [실제 게임 Scene 연결 시 주의사항](#14-실제-게임-scene-연결-시-주의사항)
15. [다음 작업 권장 순서](#15-다음-작업-권장-순서)
16. [수정 금지 또는 충돌 주의 파일](#16-수정-금지-또는-충돌-주의-파일)
17. [빠른 확인용 체크리스트](#17-빠른-확인용-체크리스트)
18. [향후 작업 시 사용할 Codex 공통 프롬프트](#18-향후-작업-시-사용할-codex-공통-프롬프트)

---

## 1. 기능 개요

이 기능은 생존 중의 선택 결과를 두 엔딩 계열 점수로 누적하고, 그 결과로 후속 계열 이벤트를 진행하기 위한 독립 프로토타입이다.

- `Signal`: 구조 신호 계열
- `Join`: 다른 생존자와의 합류 계열
- 1분기: 2~12일 차 동안 일반 이벤트를 선택해 `Signal`과 `Join` 점수를 쌓는 구간
- 1분기 판정: 13일 차에 최종 점수 비율과 외부 `randomRoll`로 최초 2분기 계열을 결정하는 과정
- 2분기: 최초 판정 계열의 순서 고정 이벤트 3개를 처리하는 구간
- `Quarter2Passed`: 현재 2분기 계열을 통과한 최종 상태
- `AllBranchesFailed`: Signal과 Join 2분기를 모두 실패한 최종 상태
- 3분기: 통과한 Signal 또는 Join 계열을 이어받아 범용 `SourceId`별 단서를 수집하고, 외부 요청으로 최종 선택을 개방한 뒤 현재 계열의 선택지 1개를 확정하는 순수 코어

전체 진행은 다음과 같다.

```text
1분기 시작
→ 2~12일 차 이벤트 미리보기
→ 행동 또는 거절 확정
→ 점수와 이벤트 일기 누적
→ 13일 차 1분기 계열 판정
→ 외부의 명시적인 2분기 시작 요청
→ 최초 판정 계열의 2분기 이벤트 1
   ├─ 이벤트 3까지 완료하고 Reject 0~1회
   │  → Quarter2Passed
   └─ Reject 2회
      → 현재 계열 즉시 실패
      → RouteSwitchPending
      → 외부의 다음 단계 요청
      → 반대 계열 이벤트 1
         ├─ 통과 → Quarter2Passed
         └─ Reject 2회 → AllBranchesFailed
```

`Quarter2Passed` 이후 3분기 코어를 시작하는 Campaign 연결은 아직 없다. 3분기 코어는 독립적으로 시작·검증할 수 있으며 실제 단서 콘텐츠, 날짜별 진행 순서, 지도·탐사·UI 연결을 포함하지 않는다. `AllBranchesFailed` 이후 마지막 벙커 엔딩을 실행하는 기능도 현재 범위에 없다.

---

## 2. 현재 구현 완료 범위

### 2.1 구현 완료

#### 1분기

- Unity 비의존 1분기 순수 코어
- 날짜 설정, 런타임 상태, 점수 상태 분리
- 이벤트 정의, 다중 선택지, 이벤트 카탈로그
- 이벤트 ID·번호 조회, 날짜 범위 필터, 중복·null 방어
- 2~12일 차 하루 이벤트 1개 제한
- 이벤트 미리보기와 선택 확정의 2단계 Sandbox 흐름
- `행동을 한다`와 `행동하지 않는다` 선택지
- Signal 1 / Join 1의 Sandbox 시작 보정 점수
- 이벤트 1~5의 Signal 증가, 이벤트 6~10의 Join 증가
- 날짜별 이벤트 일기 기록과 이전 기록 보존
- 외부 `randomRoll` 기반 Signal/Join 계열 판정
- 가중 확률 판정과 높은 점수 우선 판정 옵션
- 13일 차 판정 중복 실행 방어
- 1분기 단독 Sandbox Scene과 테스트 UI

#### 2분기

- Unity 비의존 2분기 순수 코어
- Signal·Join 각각 순서가 고정된 이벤트 3개
- `Progress`와 `Reject` 선택
- 이벤트 선택 후 외부 다음 단계 요청을 기다리는 흐름
- Reject 2회가 되는 순간 현재 계열 즉시 실패
- 현재 계열 실패 후 `RouteSwitchPending`
- 외부 진행 요청 후 반대 계열 이벤트 1부터 시작
- 먼저 실패한 계열의 카운트와 실패 상태 보존
- 이벤트 3까지 완료하고 Reject가 0~1회이면 `Quarter2Passed`
- 양쪽 계열을 모두 실패하면 `AllBranchesFailed`

#### 3분기 공통 코어

- `YesterdayMap.BranchOne.Quarter3` namespace의 Unity 비의존 순수 C# 코어
- `NotStarted`, `CollectingClues`, `FinalChoiceOpen`, `Resolved` 진행 상태
- 기존 `BranchRoute.Signal`·`BranchRoute.Join` 재사용
- `ClueId`, `Route`, 범용 `SourceId`로 구성된 단서 정의와 Catalog
- 현재 계열·출처별 미획득 단서 조회, 중복 방어, 획득 목록과 출처별 진행도
- 계열별 정확히 2개를 요구하는 최종 선택 Catalog
- 외부 `OpenFinalChoice()` 호출로 최종 선택 개방
- 단서가 0개여도 최종 선택 개방·확정 가능
- `FinalChoiceOpen`에서도 단서 획득 가능, `Resolved` 이후 추가 획득·선택 차단
- 명확한 enum 상태와 실패 사유를 제공하는 단서 획득·최종 선택 결과 객체
- 실제 단서 문구·배치·개수·날짜·지도·UI를 포함하지 않는 공통 기본틀

#### Campaign 및 검증

- 1·2·3분기를 연결하는 Unity 비의존 `EndingRouteCampaignController`
- 1분기 판정 후 외부 전환 요청까지 대기하는 `Quarter1Resolved`
- 1분기 결정 계열만 2분기 시작 계열로 전달
- Campaign Controller의 2분기 조작 래퍼 API
- `Quarter2Passed`에서 외부 `AdvanceToQuarter3()` 호출을 기다리는 명시적 전환
- 2분기 통과 계열을 3분기 시작 계열로 전달
- 단서·출처 진행도·최종 선택 개방·선택을 위한 Quarter3 래퍼 API
- 래퍼 조작 직후 Campaign 상태 자동 동기화와 `Quarter3Resolved`
- 1분기부터 3분기 종료까지 확인하는 통합 Sandbox
- `AllBranchesFailed`에서 3분기 진입 차단
- EditMode 자동 테스트
- 기존 통합 Sandbox 대표 경로 4종 수동 Play 테스트 기록 유지

### 2.2 아직 구현하지 않은 범위

실제 Shelter에는 문 앞 1분기 이벤트 표시·선택·점수·Branch 내부 일기와 날짜 부분 반영까지 연결되어 있다. 날짜 완전 통합, 13일 차 판정, 2·3분기와 Campaign의 실제 게임 연결, 기존 `DiaryUI`·엔딩 시스템 연결, 실제 콘텐츠 데이터 외부화, 4분기 이후 진행, 저장 기능은 구현하지 않았다. 전체 목록은 [13. 현재 구현되지 않은 내용](#13-현재-구현되지-않은-내용)을 참고한다.

### 2.3 기본 점수에 관한 중요한 구분

`BranchPrototypeSettings`의 **기본 생성자 자체는 시작 점수 0/0**을 사용한다. Sandbox와 통합 Sandbox는 `BranchSandboxSettings.Create(...)`를 통해 `StartingScorePerRoute = 1`을 설정하기 때문에 실제 Sandbox 시작 점수가 1/1이다.

실제 `ShelterEventDialogueController.CreateFlowIfNeeded()`는 `BranchPrototypeSettings` 생성자에 시작 점수 `0`, `0`을 명시적으로 전달하므로 현재 Shelter 부분 연결도 0/0에서 시작한다. 현재 Shelter에서는 13일 차 판정을 호출하지 않으므로 즉각적인 판정 실패는 발생하지 않지만, 향후 판정만 연결하고 0/0을 유지하면 점수를 얻지 못한 플레이에서 총점 0 판정 실패가 발생할 수 있다.

기획상 정식 시작 점수는 Signal 1 / Join 1이다. 다만 실제 Shelter 설정 변경은 Shelter 통합 담당자와 협의한 뒤 수행해야 하며, BranchOnePrototype 담당자가 임의로 Shelter 코드를 변경하지 않는다. 2026-07-31 Quarter3 작업에서도 Shelter 코드와 Scene은 변경하지 않았다.

---

## 작업 담당 범위

### 이벤트 진행 시스템 담당

- `Assets/Features/BranchOnePrototype/` 순수 코어
- 1분기·2분기·3분기 진행 및 판정 규칙
- Campaign 상태 전환
- 독립 Sandbox
- 자동 테스트
- 통합 담당자가 사용할 공개 API
- 인수인계 문서 유지

### Shelter 통합 담당

- `DayCycleManager`와 Campaign 날짜 연결
- Shelter Scene 및 Prefab 연결
- Shelter UI 연결
- 기존 `EventManager`와 일정 조정
- 기존 `DiaryUI` 연결
- `EndingManager`와 `GameManager` 연결
- Save/Load 및 실제 게임 생명주기

### 공동 협의 필요

- 정식 전체 플레이 날짜
- Signal/Join 시작 점수
- 일반 이벤트와 엔딩 이벤트의 하루 배치
- 2분기·3분기 UI 노출 방식
- 계열 판정 결과의 실제 게임 전달 방식
- `AllBranchesFailed`와 마지막 벙커 연결 방식

현재 이벤트 진행 시스템 담당자는 담당자 협의 없이 Shelter 통합 영역의 코드, Scene 또는 Prefab을 수정하지 않는다.

---

## 3. 전체 폴더 및 파일 구조

아래 트리는 `.meta` 파일을 생략했다. 모든 경로는 `Assets/Features/BranchOnePrototype/` 기준이다.

```text
BranchOnePrototype/
├─ YesterdayMap.BranchOne.asmdef
├─ Runtime/
│  ├─ BranchRoute.cs
│  ├─ BranchFlowPhase.cs
│  ├─ BranchPrototypeSettings.cs
│  ├─ BranchRuntimeState.cs
│  ├─ BranchScoreState.cs
│  ├─ BranchDecisionResult.cs
│  ├─ BranchDecisionResolver.cs
│  ├─ BranchEventChoice.cs
│  ├─ BranchEventDefinition.cs
│  ├─ BranchEventCatalog.cs
│  ├─ BranchEventSelectionResult.cs
│  ├─ BranchOneFlowController.cs
│  ├─ DiaryDayRecord.cs
│  ├─ BranchDiaryRepository.cs
│  ├─ Quarter2/
│  │  ├─ Quarter2Decision.cs
│  │  ├─ Quarter2FlowPhase.cs
│  │  ├─ Quarter2EventDefinition.cs
│  │  ├─ Quarter2EventCatalog.cs
│  │  ├─ Quarter2RouteProgress.cs
│  │  ├─ Quarter2RuntimeState.cs
│  │  ├─ Quarter2SelectionResult.cs
│  │  └─ Quarter2FlowController.cs
│  ├─ Quarter3/
│  │  ├─ Quarter3FlowPhase.cs
│  │  ├─ Quarter3ClueDefinition.cs
│  │  ├─ Quarter3ClueCatalog.cs
│  │  ├─ Quarter3FinalChoiceDefinition.cs
│  │  ├─ Quarter3FinalChoiceCatalog.cs
│  │  ├─ Quarter3RuntimeState.cs
│  │  ├─ Quarter3SourceProgress.cs
│  │  ├─ Quarter3ClueAcquisitionStatus.cs
│  │  ├─ Quarter3ClueAcquisitionResult.cs
│  │  ├─ Quarter3FinalSelectionStatus.cs
│  │  ├─ Quarter3FinalSelectionResult.cs
│  │  └─ Quarter3FlowController.cs
│  └─ Campaign/
│     ├─ EndingRouteCampaignPhase.cs
│     ├─ EndingRouteCampaignState.cs
│     ├─ EndingRouteCampaignTransitionResult.cs
│     └─ EndingRouteCampaignController.cs
├─ Sandbox/
│  ├─ YesterdayMap.BranchOne.Sandbox.asmdef
│  ├─ BranchSandboxSettings.cs
│  ├─ BranchSandboxEventProvider.cs
│  ├─ BranchSandboxPresenter.cs
│  ├─ BranchSandboxView.cs
│  ├─ Quarter2SandboxEventProvider.cs
│  ├─ Quarter3SandboxDataProvider.cs
│  ├─ EndingRouteCampaignSandboxPresenter.cs
│  ├─ EndingRouteCampaignSandboxView.cs
│  └─ Editor/
│     ├─ YesterdayMap.BranchOne.Sandbox.Editor.asmdef
│     ├─ BranchOneSandboxSceneBuilder.cs
│     └─ EndingRouteCampaignSandboxSceneBuilder.cs
├─ Scenes/
│  ├─ BranchOneSandbox.unity
│  └─ EndingRouteCampaignSandbox.unity
└─ Tests/
   ├─ Editor/
   │  ├─ YesterdayMap.BranchOne.Tests.asmdef
   │  ├─ BranchEventCatalogTests.cs
   │  ├─ BranchOneCoreTests.cs
   │  ├─ BranchOneFlowControllerTests.cs
   │  ├─ BranchTestEventFactory.cs
   │  ├─ Quarter2EventCatalogTests.cs
   │  ├─ Quarter2FlowControllerTests.cs
   │  ├─ Quarter2TestEventFactory.cs
   │  ├─ Quarter3CatalogTests.cs
   │  ├─ Quarter3FlowControllerTests.cs
   │  ├─ Quarter3TestFactory.cs
   │  ├─ EndingRouteCampaignControllerTests.cs
   │  ├─ EndingRouteCampaignQuarter2ApiTests.cs
   │  ├─ EndingRouteCampaignQuarter3ApiTests.cs
   │  └─ EndingRouteCampaignTestFactory.cs
   └─ SandboxEditor/
      ├─ YesterdayMap.BranchOne.Sandbox.Tests.asmdef
      ├─ BranchSandboxTwoStepTests.cs
      ├─ Quarter2SandboxEventProviderTests.cs
      └─ Quarter3SandboxDataProviderTests.cs
```

### 3.1 1분기 주요 파일

| 파일 | 타입 또는 책임 |
|---|---|
| `Runtime/BranchRoute.cs` | `BranchRoute.None`, `Signal`, `Join` 정의 |
| `Runtime/BranchFlowPhase.cs` | `NotStarted`부터 `Finished`까지 1분기 진행 단계 정의 |
| `Runtime/BranchPrototypeSettings.cs` | 시작일, 마지막 이벤트일, 판정일, 시작 점수, 반복·진행·판정 옵션 보관 |
| `Runtime/BranchRuntimeState.cs` | 현재 날짜, 오늘 이벤트, 완료 여부, 판정 여부와 결정 계열 보관 |
| `Runtime/BranchScoreState.cs` | Signal/Join 점수, 합계·비율, 음수 및 정수 오버플로 방어, 변경 알림 |
| `Runtime/BranchEventChoice.cs` | 선택지 ID, 문구, 결과, 점수 변화, 일기 문장 |
| `Runtime/BranchEventDefinition.cs` | 이벤트 ID·번호·본문·날짜 범위·반복 여부·선택지 목록 |
| `Runtime/BranchEventCatalog.cs` | 이벤트 등록, ID·번호 조회, 날짜별 필터, 중복 방어 |
| `Runtime/BranchEventSelectionResult.cs` | 선택 성공 여부, 이벤트·선택지, 실제 적용 점수 변화 |
| `Runtime/BranchOneFlowController.cs` | 1분기 시작, 선택 확정, 날짜 진행, 판정, Reset의 단일 진입점 |
| `Runtime/BranchDecisionResolver.cs` | 점수 비율과 외부 `randomRoll`을 이용한 순수 계열 판정 |
| `Runtime/BranchDecisionResult.cs` | 최종 점수·비율·사용한 roll·계열·성공 여부 보관 |
| `Runtime/DiaryDayRecord.cs` | 날짜별 메인 일기, 이벤트 기록, 탐사 기록 |
| `Runtime/BranchDiaryRepository.cs` | 날짜 기록 생성·조회·추가·정렬·Reset |

### 3.2 2분기 주요 파일

| 파일 | 타입 또는 책임 |
|---|---|
| `Runtime/Quarter2/Quarter2Decision.cs` | `Progress`, `Reject` 선택 정의 |
| `Runtime/Quarter2/Quarter2FlowPhase.cs` | 선택 대기, 진행 대기, 계열 전환 대기, 통과, 양쪽 실패 단계 |
| `Runtime/Quarter2/Quarter2EventDefinition.cs` | 이벤트 ID, 계열, 순서, 제목, 본문 |
| `Runtime/Quarter2/Quarter2EventCatalog.cs` | 계열별 정확히 3개·1~3 순서 검증, ID 및 계열·순서 조회 |
| `Runtime/Quarter2/Quarter2RouteProgress.cs` | 계열별 Progress/Reject/완료 횟수, 시도·실패 여부 |
| `Runtime/Quarter2/Quarter2RuntimeState.cs` | 최초·현재·대기·통과 계열과 전체 2분기 Phase 보관 |
| `Runtime/Quarter2/Quarter2SelectionResult.cs` | 선택 결과, 해당 이벤트, 카운트, 다음 Phase |
| `Runtime/Quarter2/Quarter2FlowController.cs` | 2분기 시작, 선택, 다음 이벤트, 계열 전환, Reset |

### 3.3 3분기 주요 파일

| 파일 | 타입 또는 책임 |
|---|---|
| `Runtime/Quarter3/Quarter3FlowPhase.cs` | `NotStarted`, `CollectingClues`, `FinalChoiceOpen`, `Resolved` |
| `Runtime/Quarter3/Quarter3ClueDefinition.cs` | `ClueId`, 계열, 범용 `SourceId`와 유효성 검증 |
| `Runtime/Quarter3/Quarter3ClueCatalog.cs` | 외부 정의 등록, ID·계열·계열+출처 조회, 중복·null 방어 |
| `Runtime/Quarter3/Quarter3FinalChoiceDefinition.cs` | 콘텐츠 문구 없는 최종 선택 ID와 계열 |
| `Runtime/Quarter3/Quarter3FinalChoiceCatalog.cs` | 외부 선택지 등록·조회, 계열별 정확히 2개 완전성 검증 |
| `Runtime/Quarter3/Quarter3RuntimeState.cs` | 현재 Phase·계열, 읽기 전용 획득 ID, 선택 결과와 Reset |
| `Runtime/Quarter3/Quarter3SourceProgress.cs` | 현재 계열 기준 출처별 전체·획득·잔여 단서 수 |
| `Runtime/Quarter3/Quarter3ClueAcquisitionStatus.cs` | 단서 획득 성공·실패 상태 enum |
| `Runtime/Quarter3/Quarter3ClueAcquisitionResult.cs` | 요청 ID, 획득 정의·ID, 상태와 실패 사유 |
| `Runtime/Quarter3/Quarter3FinalSelectionStatus.cs` | 최종 선택 성공·실패 상태 enum |
| `Runtime/Quarter3/Quarter3FinalSelectionResult.cs` | 요청 ID, 선택 정의·ID, 상태와 실패 사유 |
| `Runtime/Quarter3/Quarter3FlowController.cs` | 3분기 시작, 단서 조회·획득, 진행도, 외부 개방, 최종 선택, Reset |

### 3.4 Campaign 주요 파일

| 파일 | 타입 또는 책임 |
|---|---|
| `Runtime/Campaign/EndingRouteCampaignPhase.cs` | 기존 Phase와 `Quarter3Running`, `Quarter3Resolved`; `AllBranchesFailed`는 별도 종료 |
| `Runtime/Campaign/EndingRouteCampaignState.cs` | 1분기 결과, 2분기 현재·통과 계열, 3분기 최종 선택 ID 사본, 양쪽 실패, 마지막 전환 결과 |
| `Runtime/Campaign/EndingRouteCampaignTransitionResult.cs` | Campaign 전환 성공 여부, 실패 사유, 메시지, Phase |
| `Runtime/Campaign/EndingRouteCampaignController.cs` | 세 하위 Controller의 명시적 전환, Q2·Q3 래퍼 조작, 자동 동기화, 전체 Reset |

### 3.5 Sandbox 주요 파일

| 파일 | 책임 |
|---|---|
| `Sandbox/BranchSandboxSettings.cs` | Sandbox용 2~13일, 시작 점수 1/1, 가중 판정 설정 생성 |
| `Sandbox/BranchSandboxEventProvider.cs` | 1분기 임시 이벤트 1~10과 ACT/DECLINE 선택지 생성 |
| `Sandbox/BranchSandboxPresenter.cs` | 1분기 단독 Scene의 입력과 코어 연결, 자동 판정, 일기 탐색 |
| `Sandbox/BranchSandboxView.cs` | 1분기 UI 표시와 버튼 이벤트 전달 |
| `Sandbox/Quarter2SandboxEventProvider.cs` | Signal·Join 임시 이벤트 각 3개 생성 |
| `Sandbox/Quarter3SandboxDataProvider.cs` | 테스트 전용 Source·단서·최종 선택 ID로 Q3 Catalog 구성 |
| `Sandbox/EndingRouteCampaignSandboxPresenter.cs` | 1분기부터 3분기 종료까지 Campaign 래퍼 API만 이용해 조정 |
| `Sandbox/EndingRouteCampaignSandboxView.cs` | Q1·Q2·Q3 Campaign Phase별 Panel, 버튼, 상태 표시 |
| `Sandbox/Editor/BranchOneSandboxSceneBuilder.cs` | 1분기 단독 Sandbox Scene 재생성 |
| `Sandbox/Editor/EndingRouteCampaignSandboxSceneBuilder.cs` | 통합 Sandbox Scene 재생성 |

---

## 4. 핵심 아키텍처

```text
Unity 비의존 순수 코어
  BranchOneFlowController
  Quarter2FlowController
  Quarter3FlowController
          ↓
Campaign 통합 관리자
  EndingRouteCampaignController (1→2→3분기 연결)
          ↓
Sandbox Presenter
  입력 해석, Controller API 호출, Refresh
          ↓
Sandbox View
  상태 표시, 버튼 이벤트 전달
          ↓
Unity Scene
  Canvas, EventSystem, Presenter GameObject
```

### 4.1 순수 코어

- `YesterdayMap.BranchOne.asmdef`의 `noEngineReferences`는 `true`다.
- `Runtime/` 코드는 `UnityEngine`을 참조하지 않는다.
- 날짜, 이벤트, 점수, 일기, 판정, 2분기 규칙과 3분기 단서·최종 선택 규칙을 C# 객체 상태로 관리한다.
- Scene, Canvas, Button, 텍스트를 직접 제어하지 않는다.
- `BranchDecisionResolver`는 `UnityEngine.Random`을 호출하지 않는다.
- `Quarter2FlowController`는 날짜를 증가시키지 않는다.
- `Quarter3FlowController`는 날짜나 실제 `SourceId` 의미를 해석하지 않고, 외부 `OpenFinalChoice()` 호출로만 최종 선택을 연다.

### 4.2 Campaign 통합 관리자

- `EndingRouteCampaignController`가 1분기 판정 결과를 감지한다.
- 2분기 시작 시 1분기 결정 계열만 전달한다.
- 2분기 선택·진행 후 Campaign 상태를 자동 동기화한다.
- `Quarter2Passed`에서는 자동 진입하지 않고 외부 `AdvanceToQuarter3()`를 기다린다.
- 3분기 시작 시 2분기 통과 계열만 전달한다.
- 단서 획득·최종 선택 래퍼 성공 직후 상태 이벤트와 `Quarter3Resolved`를 자동 반영한다.
- `AllBranchesFailed`는 3분기를 거치지 않는 별도 종료 상태다.

### 4.3 Presenter와 View

- Presenter는 버튼 입력을 받아 Controller API를 호출한다.
- View는 점수나 진행 상태를 직접 수정하지 않는다.
- 1분기 이벤트 버튼은 미리보기만 하고, 행동 버튼에서 `SelectEvent(...)`를 호출한다.
- 통합 Presenter는 정상 2분기 흐름에서 `Quarter2FlowController`를 직접 조작하지 않는다.
- 통합 Presenter가 사용하는 2분기 API는 `AdvanceToQuarter2()`, `SelectQuarter2Decision(...)`, `AdvanceQuarter2Step()`, `GetCurrentQuarter2Event()`다.
- 두 Presenter와 두 View에는 매 프레임 상태를 검사하는 `Update()`가 없다.
- 상태 이벤트 또는 버튼 처리 직후 `Render`/`RefreshView`를 호출한다.

### 4.4 asmdef와 namespace

| 어셈블리 | 역할 | 주요 참조 |
|---|---|---|
| `YesterdayMap.BranchOne` | 순수 Runtime | 참조 없음, `noEngineReferences: true` |
| `YesterdayMap.BranchOne.Sandbox` | MonoBehaviour·UGUI Sandbox | `YesterdayMap.BranchOne`, `Unity.ugui` |
| `YesterdayMap.BranchOne.Sandbox.Editor` | Scene Builder | Sandbox, `Unity.ugui`, Editor 전용 |
| `YesterdayMap.BranchOne.Tests` | 순수 코어 EditMode 테스트 | `YesterdayMap.BranchOne`, Editor 전용 |
| `YesterdayMap.BranchOne.Sandbox.Tests` | Sandbox Provider/흐름 테스트 | Runtime, Sandbox, Editor 전용 |

namespace는 다음 계열로 나뉜다.

- `YesterdayMap.BranchOne`
- `YesterdayMap.BranchOne.Flow`
- `YesterdayMap.BranchOne.Events`
- `YesterdayMap.BranchOne.Decision`
- `YesterdayMap.BranchOne.Diary`
- `YesterdayMap.BranchOne.Quarter2`
- `YesterdayMap.BranchOne.Quarter3`
- `YesterdayMap.BranchOne.Campaign`
- `YesterdayMap.BranchOne.Sandbox`
- `YesterdayMap.BranchOne.Sandbox.Editor`

기존 `Assets/_Project` 코드에는 별도 asmdef가 없으며 현재 `Assembly-CSharp`에 포함된다. 이름이 있는 asmdef 어셈블리는 `Assembly-CSharp`를 직접 참조할 수 없다. 실제 `Shelter` 타입과 기능 어셈블리 타입을 동시에 알아야 하는 Bridge는 다음 방향이 안전하다.

```text
Assembly-CSharp에 속한 Bridge
  ├─ 기존 Shelter의 Assembly-CSharp 타입 참조
  └─ autoReferenced인 YesterdayMap.BranchOne 타입 참조
```

기능 asmdef에서 기존 `DayCycleManager` 같은 `Assembly-CSharp` 타입을 직접 참조하도록 역방향 의존성을 추가하면 안 된다.

---

## 5. 1분기 진행 구조

### 5.1 날짜와 하루 진행

- 시작일: 2일 차
- 이벤트 진행일: 2~12일 차
- 판정일: 13일 차
- 하루에 이벤트 1개만 확정할 수 있다.
- 같은 이벤트는 다른 날짜에 다시 선택할 수 있다.
  - Sandbox 설정의 `AllowRepeatedEvents = true`
  - 테스트 이벤트 정의의 `Repeatable = true`
- 선택 확정 전에는 `AdvanceDay()`가 실패한다.
- 선택 확정 후 Phase가 `ReadyToAdvance`가 되어 하루 종료가 가능하다.
- 12일 차의 선택을 마치고 `AdvanceDay()`를 호출하면 13일 차 `ResolvingBranch`로 들어간다.
- 13일 차에는 이벤트를 선택할 수 없다.
- 날짜별 일기 기록은 2일 차 시작과 3~12일 차 진입 때 생성된다. 13일 차 판정용 일기 페이지는 현재 생성하지 않는다.

### 5.2 이벤트 2단계 선택

```text
WaitingForEvent
→ 이벤트 1~10 버튼 클릭
→ Presenter의 pendingEvent에 임시 보관
→ 제목·본문 미리보기
→ 행동을 한다 / 행동하지 않는다
→ BranchOneFlowController.SelectEvent(eventId, choiceId)
→ 점수와 이벤트 일기 적용
→ EventCompleted
→ 즉시 ReadyToAdvance
→ 하루 종료 버튼 활성화
```

이벤트 버튼을 눌러 미리보기만 한 상태에서는 다음 값이 변하지 않는다.

- Signal/Join 점수
- 날짜별 일기 기록 내용
- `TodaySelectedEventId`
- `IsTodayEventCompleted`
- 하루 종료 가능 여부

### 5.3 실제 Sandbox 이벤트와 Choice ID

이벤트 ID는 `sandbox-event-1`부터 `sandbox-event-10`까지다.

각 이벤트의 선택지는 다음과 같다.

| 화면 문구 | Choice ID | 결과 |
|---|---|---|
| `행동을 한다` | `EVENT_번호_ACT` | 계열 점수 적용, 행동 일기 기록 |
| `행동하지 않는다` | `EVENT_번호_DECLINE` | 점수 변화 없음, 거절 일기 기록 |

예:

- 이벤트 1 행동: `EVENT_1_ACT`
- 이벤트 1 거절: `EVENT_1_DECLINE`
- 이벤트 10 행동: `EVENT_10_ACT`
- 이벤트 10 거절: `EVENT_10_DECLINE`

거절도 이벤트를 완료한 것으로 처리하며 같은 날 다른 이벤트나 다른 선택지를 다시 선택할 수 없다.

### 5.4 일기 구조

`BranchDiaryRepository`는 날짜가 없을 때만 `DiaryDayRecord`를 만든다. 이미 존재하는 날짜를 `GetOrCreateRecord(...)`로 다시 요청해도 기록을 덮어쓰지 않는다.

`DiaryDayRecord`는 다음 세 영역을 분리한다.

- `MainDiary`
- `EventRecords`
- `ExplorationRecords`

현재 Sandbox에서 실제로 자동 추가하는 것은 선택 결과의 `EventRecords`다. 시작 보정 점수 1/1은 이벤트 선택이 아니므로 일기에 기록하지 않는다.

---

## 6. 1분기 계열 판정 규칙

### 6.1 Sandbox 점수 규칙

| 항목 | Signal | Join |
|---|---:|---:|
| 2일 차 시작 보정 점수 | 1 | 1 |
| 이벤트 1~5에서 행동 | +1 | 0 |
| 이벤트 6~10에서 행동 | 0 | +1 |
| 행동하지 않음 | 0 | 0 |

`BranchScoreState`는 점수가 음수가 되지 않게 0에서 막고, 증가 결과가 `int.MaxValue`를 넘지 않게 방어한다. 기본 점수는 일기, 완료 이벤트, 날짜별 행동 횟수에 포함되지 않는다.

### 6.2 가중 확률 계산

최종 점수가 다음과 같다고 할 때:

```text
total = SignalScore + JoinScore
signalRatio = SignalScore / total
joinRatio = JoinScore / total
```

가중 판정이 켜져 있으면 실제 경계 비교는 다음과 같다.

```text
randomRoll < signalRatio  → Signal
randomRoll >= signalRatio → Join
```

따라서 정확히 경계값과 같은 `randomRoll`은 Join으로 판정된다.

예:

- 최종 1/1: `signalRatio = 0.5`
  - `randomRoll = 0.499999` → Signal
  - `randomRoll = 0.5` → Join
- 최종 2/1: Signal 약 66.67%, Join 약 33.33%
- 최종 1/3: Signal 25%, Join 75%

한쪽 점수가 0이면 roll과 관계없이 점수가 있는 계열을 선택한다. 양쪽 총점이 0이면 판정 실패지만, Sandbox는 시작 점수가 1/1이므로 모든 날짜에 거절해도 총점이 2이며 50:50 판정에 성공한다.

### 6.3 randomRoll 정규화

순수 `BranchDecisionResolver`의 입력 정규화는 다음과 같다.

- `NaN` → `0.5`
- 0 이하 → `0`
- 1 이상 → `1`
- 그 외 → 입력값 유지

통합 Sandbox UI의 Presenter는 UI 입력을 0 이상 1 미만으로 제한하기 위해 별도로 다음 처리 후 코어에 전달한다.

- 숫자가 아닌 값, `NaN`, 무한대 → 입력 실패, 기존 고정값 유지
- 0 이하 → `0`
- 1 이상 → `0.999999`

순수 코어는 `UnityEngine.Random`을 사용하지 않는다. 단독 Sandbox Presenter는 13일 차에 `UnityEngine.Random.value`를 외부 roll로 전달한다. 통합 Sandbox Presenter는 고정값 사용 여부에 따라 고정 roll 또는 `UnityEngine.Random.value`를 전달한다.

### 6.4 비가중 옵션

`WeightedRandomDecision = false`이고 점수가 다르면 높은 점수 계열을 선택한다. 점수가 같으면 비가중 옵션이어도 같은 50:50 경계 규칙을 사용한다. 현재 통합 Sandbox는 `BranchSandboxSettings.Create(true)`를 사용하므로 가중 판정이 켜져 있다.

---

## 7. 2분기 진행 구조

### 7.1 임시 이벤트

`Quarter2SandboxEventProvider`가 다음 6개 이벤트를 만든다.

Signal:

1. `Q2_SIGNAL_01` — 구조신호 2분기 이벤트 1
2. `Q2_SIGNAL_02` — 구조신호 2분기 이벤트 2
3. `Q2_SIGNAL_03` — 구조신호 2분기 이벤트 3

Join:

1. `Q2_JOIN_01` — 합류 2분기 이벤트 1
2. `Q2_JOIN_02` — 합류 2분기 이벤트 2
3. `Q2_JOIN_03` — 합류 2분기 이벤트 3

`Quarter2EventCatalog.TryValidate(...)`는 Signal과 Join에 각각 정확히 3개가 있고 순서 1, 2, 3이 모두 존재해야 성공한다.

### 7.2 선택과 다음 단계

각 이벤트에서 사용할 수 있는 결정은 다음 두 개다.

- `Quarter2Decision.Progress` — 화면 문구 `진행한다`
- `Quarter2Decision.Reject` — 화면 문구 `거절한다`

정상 흐름:

```text
WaitingForChoice
→ SelectQuarter2Decision(Progress 또는 Reject)
→ 현재 이벤트 완료 및 카운트 반영
→ WaitingForAdvance
→ 외부 AdvanceQuarter2Step()
→ 다음 순서 이벤트의 WaitingForChoice
```

같은 이벤트에서 결정을 두 번 내릴 수 없다. `WaitingForAdvance`에서는 선택 버튼이 비활성화되고 다음 단계 요청만 가능하다.

2분기 코어에는 날짜 값이나 날짜 증가 API가 없다. `AdvanceQuarter2Step()`은 “다음 이벤트 또는 반대 계열 이벤트를 시작한다”는 의미일 뿐, 같은 날인지 다음 날인지 결정하지 않는다. 실제 발생 날짜와 간격은 향후 Bridge 또는 상위 날짜 운영 시스템의 책임이다.

---

## 8. 2분기 계열 실패 및 전환 규칙

### 8.1 Progress와 Reject

- `Progress`
  - 현재 계열의 `ProgressCount + 1`
  - `CompletedEventCount + 1`
- `Reject`
  - 현재 계열의 `RejectCount + 1`
  - `CompletedEventCount + 1`

### 8.2 통과

현재 이벤트 순서가 3이고 두 번째 Reject로 실패하지 않았다면 현재 계열을 통과한다.

- 이벤트 3까지 완료
- `RejectCount`가 0 또는 1
- Phase → `Quarter2Passed`
- `PassedRoute`에 현재 계열 저장

Progress가 2회가 되어도 이벤트 2에서는 조기 통과하지 않는다.

가능한 통과 예:

- Progress, Progress, Progress
- Progress, Reject, Progress
- Reject, Progress, Progress

### 8.3 실패

`RejectCount`가 2가 되는 순간 현재 계열이 즉시 실패한다.

```text
두 번째 Reject
→ 현재 계열 Failed = true
→ 남은 현재 계열 이벤트 생략
→ 반대 계열이 실패하지 않았다면 RouteSwitchPending
```

두 번째 Reject가 이벤트 2에서 발생하면 현재 계열 이벤트 3은 실행하지 않는다.

### 8.4 반대 계열 전환

`RouteSwitchPending`에서는 외부의 `AdvanceQuarter2Step()` 호출을 기다린다. 호출 후:

- `CurrentRoute`가 반대 계열로 변경된다.
- `CurrentEventOrder = 1`
- 반대 계열 `Attempted = true`
- 반대 계열 Progress/Reject 카운트는 0에서 시작한다.
- 먼저 실패한 계열의 Progress/Reject/Failed 기록은 유지된다.

### 8.5 양쪽 실패

반대 계열에서도 Reject가 2회가 되어 Signal과 Join이 모두 실패하면:

- Phase → `AllBranchesFailed`
- `AreAllBranchesFailed = true`
- `PassedRoute = None`
- 추가 선택과 다음 단계 진행 차단

이 상태에서 마지막 벙커 Scene이나 실제 엔딩은 실행하지 않는다.

### 8.6 대표 경로

```text
1. Signal 최초 진입
   → Signal 이벤트 3까지 Reject 0~1회
   → Quarter2Passed / Signal

2. Signal 최초 진입
   → Signal Reject 2회
   → RouteSwitchPending
   → Join 이벤트 1부터 시작
   → Join 통과
   → Quarter2Passed / Join

3. Join 최초 진입
   → Join Reject 2회
   → RouteSwitchPending
   → Signal 이벤트 1부터 시작
   → Signal 통과
   → Quarter2Passed / Signal

4. 최초 계열 Reject 2회
   → 반대 계열 전환
   → 반대 계열도 Reject 2회
   → AllBranchesFailed
```

---

## 3분기 공통 코어

3분기는 실제 스토리나 지도 배치가 아니라 Signal·Join 양쪽에서 재사용할 수 있는 순수 C# 진행 틀만 구현되어 있다. 기존 `BranchRoute`를 재사용하며 Unity, Scene, UI, 탐사, 날짜 시스템에 의존하지 않는다.

### 진행 상태

```text
NotStarted
→ StartQuarter3(passedRoute)
→ CollectingClues
→ 외부 OpenFinalChoice()
→ FinalChoiceOpen
→ SelectFinalChoice(finalChoiceId)
→ Resolved
```

- `CollectingClues`: 현재 계열과 `SourceId`가 일치하는 단서를 조회·획득할 수 있다.
- `FinalChoiceOpen`: 최종 선택지가 열려 있지만 아직 확정 전이므로 단서 획득도 계속 허용한다.
- `Resolved`: 선택 결과를 보관하며 추가 단서 획득과 추가 선택을 차단한다.
- 단서 획득 개수는 `OpenFinalChoice()` 또는 `SelectFinalChoice(...)`의 자격 조건이 아니다. 획득 단서가 0개여도 외부가 개방하면 선택할 수 있다.

### 단서와 SourceId

`Quarter3ClueDefinition`의 최소 필드는 다음과 같다.

- `ClueId`: 단서 고유 ID
- `Route`: `Signal` 또는 `Join`
- `SourceId`: 출처를 식별하는 범용 ID

`SourceId`는 지역에 한정되지 않는다. 향후 탐사지, 고정 스토리, 이벤트 또는 다른 콘텐츠 ID를 전달할 수 있으며 Quarter3 코어는 의미를 해석하지 않는다. 실제 단서 문장, 지도 구역명, 배치 수, 출현 날짜는 런타임 코어에 들어 있지 않다.

`Quarter3ClueCatalog`는 외부에서 전달된 정의만 등록한다. ID 중복·null·공백·유효하지 않은 계열을 차단하고 ID, 계열, 계열+출처로 조회한다. `GetAvailableClues(sourceId)`는 현재 계열·출처의 미획득 정의만 읽기 전용 목록으로 반환한다.

### 최종 선택과 Catalog 완전성

`Quarter3FinalChoiceDefinition`은 `FinalChoiceId`와 `Route`만 가진다. 실제 엔딩명·설명·UI 문구는 포함하지 않는다.

`Quarter3FinalChoiceCatalog`는 기존 Catalog 패턴처럼 정의를 하나씩 등록하는 부분 구성을 허용한다. 다만 `TryValidate(...)`는 Signal과 Join 각각 정확히 2개인지 검사하며, `StartQuarter3(...)`가 이 완전성 검증을 통과해야 시작된다. 따라서 구성 중 부분 Catalog는 조회·테스트할 수 있지만 불완전하거나 과다한 Catalog로 실제 Flow를 시작할 수는 없다.

### 공개 API

| API 또는 속성 | 책임 |
|---|---|
| `StartQuarter3(BranchRoute)` | `NotStarted`에서 Signal/Join 계열로 시작 |
| `GetAvailableClues(string sourceId)` | 현재 계열·출처의 미획득 단서 조회 |
| `TryAcquireClue(string clueId)` | 단서 획득 및 상태 enum·실패 사유 반환 |
| `HasClue(string clueId)` | 특정 단서 보유 여부 |
| `GetAcquiredClues()` | 획득 순서의 단서 정의를 읽기 전용으로 조회 |
| `GetSourceProgress(string sourceId)` | 현재 계열 기준 전체·획득·잔여 개수 |
| `GetFinalChoiceOptions()` | 현재 계열의 최종 선택지 2개 조회 |
| `OpenFinalChoice()` | 외부 진행 시스템이 최종 선택 개방 |
| `SelectFinalChoice(string finalChoiceId)` | 현재 계열 선택지 확정 후 `Resolved` |
| `Reset()` | Phase·계열·단서·선택 결과 초기화 및 재사용 |
| `Phase`, `CurrentRoute`, `IsActive`, `IsFinalChoiceOpen`, `IsResolved` | 읽기 전용 진행 상태 |
| `AcquiredClueIds`, `SelectedFinalChoiceId` | 외부 수정이 불가능한 결과 상태 |

단서 획득 실패는 `Quarter3ClueAcquisitionStatus`, 최종 선택 실패는 `Quarter3FinalSelectionStatus`로 구분한다. 시작 전, 잘못된 ID, 미등록 ID, 다른 계열, 중복, 개방 전, 해결 후 상태를 각각 명확히 반환하며 실패 시 기존 상태를 손상하지 않는다.

### Campaign과 Sandbox 연결 상태

- `EndingRouteCampaignController`가 `Quarter3FlowController`를 외부에서 명시적으로 주입받는다.
- `Quarter2Passed`에서 `AdvanceToQuarter3()`가 성공하면 통과 계열로 Quarter3를 시작한다.
- Q3 단서·진행도·최종 선택 API는 Campaign Wrapper를 통해 사용한다.
- 정상 최종 선택 성공 즉시 Campaign도 `Quarter3Resolved`가 된다.
- `EndingRouteCampaignSandbox`가 Q1→Q2→Q3 수동 통합 검증 Scene으로 확장되었다.
- `Quarter3SandboxDataProvider`는 `TEST_` ID만 제공하며 실제 콘텐츠 Provider가 아니다.
- 실제 Shelter·탐사·DiaryUI·날짜·엔딩 연결과 실제 단서 콘텐츠는 여전히 미구현이다.

---

## 9. 1분기·2분기·3분기 통합 구조

### 9.1 Campaign Phase

```text
NotStarted
→ StartQuarter1()
→ Quarter1Running
→ 1분기 13일 차 판정
→ Quarter1Resolved
→ 외부 AdvanceToQuarter2()
→ Quarter2Running
   ├─ Quarter2Passed
   │  → 외부 AdvanceToQuarter3()
   │  → Quarter3Running
   │  → 외부 OpenQuarter3FinalChoice()
   │  → SelectQuarter3FinalChoice(...)
   │  → Quarter3Resolved
   └─ AllBranchesFailed
```

1분기 판정 이벤트는 Campaign Controller가 구독한다. 판정 결과가 들어오면 `EndingRouteCampaignState.RecordQuarter1Result(...)`가 실행되고 `Quarter1Resolved`가 된다.

### 9.2 1분기에서 2분기로 전달되는 값

- 전달: `Quarter1Route`의 `Signal` 또는 `Join`
- 전달하지 않음: Signal 점수, Join 점수, 일기, 날짜, 선택 이벤트 목록

따라서 1분기 점수가 매우 높아도 2분기 Progress/Reject 카운트에는 영향을 주지 않는다.

1분기 판정 직후 2분기를 자동으로 시작하지 않는다. 외부에서 `AdvanceToQuarter2()`를 호출해야 `Quarter2FlowController.StartQuarter2(Quarter1Route)`가 실행된다.

2분기 통과 직후에도 3분기를 자동으로 시작하지 않는다. 외부에서 `AdvanceToQuarter3()`를 호출해야 `Quarter3FlowController.StartQuarter3(Quarter2PassedRoute)`가 실행된다. `AllBranchesFailed`에서는 이 호출이 실패하고 Quarter3는 `NotStarted`를 유지한다.

### 9.3 Campaign Controller의 정상 조작 API

| API | 역할 |
|---|---|
| `StartQuarter1()` | 1분기 시작 및 `Quarter1Running` 전환 |
| `AdvanceToQuarter2()` | 유효한 1분기 결과를 확인하고 2분기 명시적 시작 |
| `SelectQuarter2Decision(Quarter2Decision)` | 2분기 선택 적용 후 Campaign 상태 자동 동기화 |
| `AdvanceQuarter2Step()` | 다음 이벤트 또는 반대 계열 이벤트 1 시작 후 자동 동기화 |
| `GetCurrentQuarter2Event()` | `Quarter2Running`이며 선택 대기 중인 현재 이벤트 조회 |
| `AdvanceToQuarter3()` | 2분기 통과 계열로 3분기 명시적 시작 |
| `GetQuarter3AvailableClues(string)` | 현재 계열·Source의 미획득 단서 조회 |
| `AcquireQuarter3Clue(string)` | Q3 단서 획득 결과 전달 |
| `HasQuarter3Clue(string)` / `GetQuarter3AcquiredClues()` | Q3 획득 단서 읽기 |
| `GetQuarter3SourceProgress(string)` | 현재 계열의 Source별 진행도 |
| `GetQuarter3FinalChoiceOptions()` | 현재 계열 최종 선택지 2개 조회 |
| `OpenQuarter3FinalChoice()` | 외부 호출로 최종 선택 개방 |
| `SelectQuarter3FinalChoice(string)` | 선택 성공 즉시 Campaign `Quarter3Resolved` |
| `ResetCampaign()` | 세 하위 Controller와 Campaign 상태 초기화 |

`SynchronizeState()`는 기존 테스트·진단·호환 목적으로 public으로 남아 있다. 정상 UI 흐름에서는 래퍼 API가 선택·진행 직후 상태를 자동 동기화하므로 별도로 호출할 필요가 없다.

현재 `EndingRouteCampaignController`는 호환성과 테스트를 위해 세 하위 Controller public 참조를 제공한다. 이 공개 참조가 있더라도 Presenter는 2·3분기 하위 Controller를 직접 조작하지 않고 Campaign Wrapper만 사용한다.

### 9.4 읽기 전용 상태

`EndingRouteCampaignState`에서 읽을 수 있는 값:

- `Phase`
- `Quarter1Route`
- `Quarter2CurrentRoute`
- `Quarter2PassedRoute`
- `Quarter3FinalChoiceId`
- `AreAllBranchesFailed`
- `Quarter1DecisionResult`
- `LastTransitionResult`

`EndingRouteCampaignController`가 제공하는 2분기 조회 프록시:

- `Quarter2Phase`
- `Quarter2CurrentRoute`
- `Quarter2CurrentEventOrder`
- `SignalProgressCount`
- `SignalRejectCount`
- `SignalFailed`
- `JoinProgressCount`
- `JoinRejectCount`
- `JoinFailed`
- `Quarter2PassedRoute`
- `AreAllBranchesFailed`
- `GetCurrentQuarter2Event()`

3분기 조회 프록시:

- `Quarter3Phase`
- `Quarter3CurrentRoute`
- `Quarter3IsActive`
- `Quarter3IsFinalChoiceOpen`
- `Quarter3IsResolved`
- `Quarter3AcquiredClueIds`
- `Quarter3SelectedFinalChoiceId`

최종 선택 원본은 `Quarter3RuntimeState.SelectedFinalChoiceId`다. Campaign State의 `Quarter3FinalChoiceId`는 Campaign 결과를 한곳에서 읽기 위한 동기화 사본이며 Wrapper 성공 또는 진단용 `SynchronizeState()`에서 갱신된다.

### 9.5 Reset

`ResetCampaign()`은:

1. `BranchOneFlowController.Reset()`
2. `Quarter2FlowController.Reset()`
3. `Quarter3FlowController.Reset()`
4. `EndingRouteCampaignState.Reset()`
5. `StateChanged` 알림

순서로 실행된다. 이 직후 Campaign과 1·2·3분기 런타임은 모두 `NotStarted`이며 Q3 단서와 최종 선택 ID도 비어 있다.

통합 Sandbox의 “전체 초기화” 버튼은 `ResetCampaign()` 뒤 다시 `StartQuarter1()`을 호출한다. 그래서 화면에서는 즉시 2일 차, Signal 1, Join 1의 새 캠페인이 시작된다.

---

## 10. Sandbox Scene 2개 설명

### 10.1 A. BranchOneSandbox

- Scene: `Assets/Features/BranchOnePrototype/Scenes/BranchOneSandbox.unity`
- Presenter: `BranchSandboxPresenter`
- View: `BranchSandboxView`
- Builder: `BranchOneSandboxSceneBuilder`
- Builder 메뉴: `Yesterday Map > Branch One > Build Sandbox Scene`
- Build Settings: 미등록, `buildIndex = -1`
- Missing Script: 0

Hierarchy:

```text
BranchOneSandbox
├─ Main Camera
├─ Directional Light
├─ EventSystem
├─ SandboxCanvas
│  ├─ Background
│  ├─ HeaderPanel
│  ├─ EventPanel
│  ├─ DiaryPanel
│  └─ DecisionPanel
└─ BranchSandboxPresenter
```

용도:

- 1분기 단독 회귀 테스트
- 이벤트 1~10의 미리보기와 선택
- 2~13일 차 흐름
- 점수 누적
- 날짜별 일기 탐색
- 13일 차 Signal/Join 판정

이 Scene은 고정 roll 입력 UI가 없다. 판정 시 `BranchSandboxPresenter`가 `UnityEngine.Random.value`를 외부 roll로 전달한다.

### 10.2 B. EndingRouteCampaignSandbox

- Scene: `Assets/Features/BranchOnePrototype/Scenes/EndingRouteCampaignSandbox.unity`
- Presenter: `EndingRouteCampaignSandboxPresenter`
- View: `EndingRouteCampaignSandboxView`
- Builder: `EndingRouteCampaignSandboxSceneBuilder`
- Builder 메뉴: `Yesterday Map > Build Ending Route Campaign Sandbox`
- Build Settings: 미등록, `buildIndex = -1`
- Missing Script: 0

Hierarchy:

```text
EndingRouteCampaignSandbox
├─ Main Camera
├─ Directional Light
├─ EventSystem
├─ CampaignSandboxCanvas
│  ├─ Background
│  ├─ HeaderPanel
│  ├─ Quarter1Panel
│  ├─ Quarter1ResultPanel
│  ├─ Quarter2Panel
│  ├─ Quarter2RouteStatusPanel
│  ├─ Quarter3Panel
│  ├─ CampaignResultPanel
│  └─ ControlPanel
└─ EndingRouteCampaignSandboxPresenter
```

용도:

- 1분기 시작부터 3분기 종료까지 통합 검증
- 1분기 판정 결과의 Campaign 저장
- 외부 버튼을 통한 명시적 2분기 시작
- Progress/Reject
- `RouteSwitchPending`
- 반대 계열 이벤트 1부터 재시작
- `Quarter2Passed`
- 외부 버튼을 통한 명시적 3분기 시작
- Source A/B 단서 획득과 출처별 진행도
- 단서가 0개인 상태에서도 외부 최종 선택 개방
- 현재 계열 최종 선택지 2개와 `Quarter3Resolved`
- `AllBranchesFailed`
- 전체 Reset

통합 Sandbox에는 1분기 단독 Sandbox의 날짜별 일기 탐색 Panel이 없다. 1분기 선택은 동일한 코어를 사용해 일기에 기록되지만, 이 Scene의 목적은 Campaign 전환과 2·3분기 상태 확인이다. Q3 데이터는 `Quarter3SandboxDataProvider`의 `TEST_` ID이며 실제 게임 콘텐츠가 아니다.

### 10.3 Scene 비교

| 비교 항목 | `BranchOneSandbox` | `EndingRouteCampaignSandbox` |
|---|---|---|
| 테스트 범위 | 1분기 단독 | 1분기 시작~3분기 종료 |
| 시작 날짜 | 2일 차 | 2일 차 |
| 종료 지점 | 13일 차 계열 판정 | `Quarter3Resolved` 또는 `AllBranchesFailed` |
| 2분기 지원 | 없음 | 있음 |
| 3분기 지원 | 없음 | Campaign Wrapper와 테스트 데이터로 지원 |
| 일기 UI 확인 | 날짜별 메인·이벤트·탐사 영역 확인 | 별도 일기 Panel 없음 |
| Campaign Controller 사용 | 안 함 | 사용 |
| 고정 randomRoll UI | 없음 | 있음 |
| 사용 목적 | 1분기·일기 회귀 | 1→2→3분기 통합 및 계열 전환 검증 |
| Build Settings | 미등록 | 미등록 |

---

## 11. Unity에서 직접 테스트하는 방법

### 11.1 BranchOneSandbox

1. Project 창에서 `Assets/Features/BranchOnePrototype/Scenes/BranchOneSandbox.unity`를 연다.
2. Play를 누른다.
3. 상단 날짜가 2일 차이고 점수가 Signal 1 / Join 1인지 확인한다.
4. 이벤트 1~10 중 하나를 누른다.
5. 제목·본문 미리보기만 표시되고 점수·일기·완료 상태가 변하지 않는지 확인한다.
6. `행동을 한다` 또는 `행동하지 않는다`를 누른다.
7. 선택 결과, 점수, 이벤트 일기, 하루 종료 버튼 상태를 확인한다.
8. `하루 종료`로 다음 날짜로 이동한다.
9. 이전/다음 일기 버튼으로 지난 날짜 기록이 유지되는지 확인한다.
10. 12일 차까지 반복하고 하루를 종료한다.
11. 13일 차 판정 결과와 최종 비율을 확인한다.
12. 테스트 초기화 버튼으로 2일 차, 1/1, 빈 기록으로 돌아오는지 확인한다.

### 11.2 EndingRouteCampaignSandbox

1. Project 창에서 `Assets/Features/BranchOnePrototype/Scenes/EndingRouteCampaignSandbox.unity`를 연다.
2. Play를 누른다.
3. 1분기 이벤트 미리보기와 행동 선택을 12일 차까지 진행한다.
4. 13일 차에 `Quarter1Resolved`와 Signal/Join 결정 계열을 확인한다.
5. 1분기 결과 Panel의 2분기 시작 버튼을 누른다.
6. `Quarter2Running`, 현재 계열, 이벤트 순서 1을 확인한다.
7. `진행한다` 또는 `거절한다`를 선택한다.
8. `WaitingForAdvance`에서는 다음 이벤트 버튼을 누른다.
9. Reject 2회 후 `RouteSwitchPending`이면 `반대 계열 시작` 버튼을 누른다.
10. 반대 계열 이벤트 1과 첫 계열 실패 기록 유지 여부를 확인한다.
11. `Quarter2Passed`이면 3분기가 자동 시작되지 않는지 확인하고 `3분기 시작` 버튼을 누른다.
12. `Quarter3Running`과 2분기 통과 계열 전달을 확인한다.
13. Source A/B 버튼으로 단서를 획득하고 전체·획득·잔여 진행도를 확인한다.
14. `최종 선택 개방`을 누른 뒤에도 남은 단서를 획득할 수 있는지 확인한다.
15. 현재 계열 선택지 2개 중 하나를 눌러 `Quarter3Resolved`와 선택 ID를 확인한다.
16. 양쪽 2분기 실패 경로에서는 `AllBranchesFailed`가 유지되고 3분기 시작이 불가능한지 확인한다.
17. 전체 초기화 버튼을 눌러 2일 차, 1/1, 모든 Q2 카운트와 Q3 단서·선택 ID가 초기화되는지 확인한다.

### 11.3 고정 randomRoll UI

통합 Sandbox의 `ControlPanel`에 고정 roll Toggle과 입력 필드가 있다.

- 기본 상태: 고정값 사용 켜짐
- 기본값: `0`
- Toggle 켜짐: 입력한 `fixedRandomRoll` 사용
- Toggle 꺼짐: 판정 시 `UnityEngine.Random.value` 사용
- 허용 의도 범위: 0 이상 1 미만
- 0 이하: 0으로 보정
- 1 이상: 0.999999로 보정
- 숫자가 아니거나 `NaN`/무한대: 오류 표시 후 기존 값 유지
- 전달 시점: 12일 차 종료 후 13일 차 `ResolvingBranch`에 들어갈 때 정확히 한 번

결과 재현 예:

- 모든 날 거절하여 1/1인 상태에서 고정 roll `0` → Signal
- 같은 1/1에서 고정 roll `0.5` → Join

### 11.4 수동 확인 체크리스트

- [ ] 이벤트 미리보기만으로 점수와 일기가 변하지 않는다.
- [ ] 행동 확정 전 하루 종료가 차단된다.
- [ ] 행동 확정 후 같은 날 두 번째 선택이 차단된다.
- [ ] 다음 날 pending 이벤트가 초기화된다.
- [ ] 시작 점수와 Reset 점수가 1/1이다.
- [ ] 13일 차에는 1분기 이벤트를 선택할 수 없다.
- [ ] 1분기 판정 직후 2분기가 자동 시작되지 않는다.
- [ ] 2분기 이벤트 순서가 1→2→3이다.
- [ ] Progress 2회만으로 조기 통과하지 않는다.
- [ ] Reject 2회 즉시 남은 현재 계열 이벤트가 생략된다.
- [ ] 반대 계열이 이벤트 1, 카운트 0/0에서 시작한다.
- [ ] 첫 계열 실패 기록이 유지된다.
- [ ] `Quarter2Passed` 직후 3분기가 자동 시작되지 않는다.
- [ ] 3분기 계열이 2분기 통과 계열과 같다.
- [ ] Source A/B 단서 획득과 진행도가 일치한다.
- [ ] `FinalChoiceOpen`에서도 남은 단서를 획득할 수 있다.
- [ ] 단서 0개 상태에서도 최종 선택을 개방·확정할 수 있다.
- [ ] 최종 선택 성공 직후 Campaign이 `Quarter3Resolved`다.
- [ ] `AllBranchesFailed`에서는 Q3가 `NotStarted`를 유지한다.
- [ ] 종료 후 추가 선택과 진행이 차단된다.
- [ ] Console에 컴파일 오류, `NullReferenceException`, `MissingReferenceException`이 없다.

---

## 12. 자동 테스트 구성과 실행 방법

### 12.1 테스트 어셈블리

- `YesterdayMap.BranchOne.Tests`
  - 순수 코어 EditMode 테스트
  - `noEngineReferences: true`
- `YesterdayMap.BranchOne.Sandbox.Tests`
  - Sandbox Provider와 1분기 2단계 흐름 테스트
  - Unity/Sandbox 어셈블리 참조

### 12.2 테스트 파일

| 테스트 파일 | 검증 내용 |
|---|---|
| `Tests/Editor/BranchEventCatalogTests.cs` | ID·번호 조회, 중복 방어, 날짜 범위 필터 |
| `Tests/Editor/BranchOneCoreTests.cs` | 점수, 변경 알림, 음수 방어, 일기 보존, 0점 실패, 비율·동률 판정 |
| `Tests/Editor/BranchOneFlowControllerTests.cs` | 2~13일 흐름, 일기 생성·보존, 하루 선택 제한, 판정, Reset |
| `Tests/Editor/Quarter2EventCatalogTests.cs` | 중복 ID·계열별 순서 방어, 계열별 정확히 3개 검증 |
| `Tests/Editor/Quarter2FlowControllerTests.cs` | 시작 계열, 선택·진행, 조기 통과 방지, Reject 실패, 계열 전환, 최종 상태 |
| `Tests/Editor/Quarter3CatalogTests.cs` | 단서·최종 선택 정의 검증, null·중복 방어, 계열·출처 필터, 계열별 선택지 2개 완전성 |
| `Tests/Editor/Quarter3FlowControllerTests.cs` | Signal/Join 시작, 단서 조회·획득·실패 상태, 출처별 진행도, 외부 개방, 최종 선택, Reset |
| `Tests/Editor/EndingRouteCampaignControllerTests.cs` | 1→2분기 전환, 명시적 시작, 결과 동기화, 최종 상태, Reset |
| `Tests/Editor/EndingRouteCampaignQuarter2ApiTests.cs` | Campaign 래퍼 선택·진행, 자동 동기화, 읽기 값, 종료 후 차단 |
| `Tests/Editor/EndingRouteCampaignQuarter3ApiTests.cs` | Q3 명시적 진입, 계열 전달, 단서·진행도·최종 선택 래퍼, 자동 해결, AllBranchesFailed 차단, 전체 Reset |
| `Tests/SandboxEditor/BranchSandboxTwoStepTests.cs` | ACT/DECLINE Provider, 시작 점수 1/1, 미리보기 무변경, 거절 일기, 전체 1분기 흐름 |
| `Tests/SandboxEditor/Quarter2SandboxEventProviderTests.cs` | 6개 이벤트의 유효성, Signal·Join ID와 고정 순서 |
| `Tests/SandboxEditor/Quarter3SandboxDataProviderTests.cs` | TEST 전용 단서 5개, Source 필터, 계열별 최종 선택지 2개 |

지원용 Factory:

- `BranchTestEventFactory.cs`
- `Quarter2TestEventFactory.cs`
- `Quarter3TestFactory.cs`
- `EndingRouteCampaignTestFactory.cs`

`Quarter3TestFactory`의 데이터는 테스트 전용이다. Signal 단서 3개(`SourceA` 2개, `SourceB` 1개), Join 단서 2개(`SourceA`·`SourceB` 각 1개), 계열별 최종 선택지 2개를 만들며 런타임 코어에는 이 ID나 기본 콘텐츠가 하드코딩되어 있지 않다.

현재 별도의 View 클릭 자동화 또는 PlayMode UI 테스트 파일은 **현재 코드에서 확인되지 않음**이다. Scene UI와 대표 통합 경로는 기존 수동 Play 검증 기록을 유지하며, 이번 Quarter3 작업에서는 Play Mode를 실행하지 않았다.

### 12.3 Unity Test Runner 실행

1. Unity 메뉴에서 `Window > General > Test Runner`를 연다.
2. `EditMode` 탭을 선택한다.
3. 전체 테스트를 선택한다.
4. `Run All`을 실행한다.
5. 실패, Skip, Console 오류가 없는지 확인한다.

2026-07-31 Quarter3 Campaign·Sandbox 통합 후 Unity Test Runner로 전체 EditMode 테스트를 재실행한 결과는 **214/214 통과, 실패 0, Skip 0**이다. 기존 Quarter3 코어 테스트 69개에 Campaign·Sandbox 통합 테스트 **33개**가 추가되었다. 신규 테스트는 Q3 명시적 진입, 계열 전달, Wrapper 호출 조건, 단서·진행도, 외부 최종 선택 개방, 자동 `Quarter3Resolved`, `AllBranchesFailed` 차단, 전체 Reset과 Sandbox 테스트 데이터 구성을 검증한다.

---

## 13. 현재 구현되지 않은 내용

### 실제 Shelter에 구현된 부분

- ExitDoor 보조 상호작용을 통한 1분기 문 앞 이벤트 표시
- 이벤트 선택 처리
- Signal 또는 Join 점수 반영
- `BranchDiaryRepository` 내부 이벤트 기록
- `DayCycleManager.CurrentDay`를 Branch 상태에 부분 반영

### 아직 구현되지 않은 부분

- `DayCycleManager`와 Branch 날짜의 완전 통합
- Shelter의 13일 차 1분기 계열 판정
- Shelter의 외부 `randomRoll` 전달
- 2분기 실제 게임 연결
- 3분기 실제 게임 연결
- Campaign 실제 게임 연결
- 기존 `DiaryUI`와 `BranchDiaryRepository`의 정식 통합
- 기존 `EndingManager`·`GameManager`와 Campaign 결과 연결
- 기존 `EventManager`와 일반·야간 이벤트 배치 통합
- Save/Load
- 정식 이벤트 데이터 외부화
- ScriptableObject 기반 이벤트 데이터
- CSV 또는 Excel Importer
- 실제 캐릭터 스탯 변화
- 실제 자원·아이템 변화
- 탐사 시스템 연결
- 2분기 이벤트가 발생하는 날짜와 간격
- 일반 이벤트와 2분기 이벤트의 같은 날 배치 규칙
- 실제 단서·최종 선택 콘텐츠
- Quarter3의 지도 배치·단서 개수·날짜별 스토리 순서
- 4분기 및 실제 최종 엔딩 실행
- `AllBranchesFailed` 이후 마지막 벙커 실제 진입
- Sandbox Build Settings 등록
- 정식 게임 UI, 아트, 연출
- 1분기 점수와 실제 게임 진행도 사이의 연결

현재 `DiaryDayRecord`에 메인 일기와 탐사 기록을 담을 자리는 있고 Shelter 문 앞 이벤트 선택은 `EventRecords`에 기록되지만, 실제 게임 `DiaryUI`나 탐사 기록과 동기화하는 Adapter는 없다.

---

## 14. 실제 게임 Scene 연결 시 주의사항

### 14.1 실제 Shelter의 1분기 부분 연결

실제 Shelter에는 기존 Sandbox 오브젝트를 복사한 것이 아닌 Shelter 전용 1분기 부분 Bridge가 존재한다.

관련 파일:

- `Assets/_Project/Scripts/Events/ShelterEventDialogueController.cs`
- `Assets/_Project/Scripts/Events/ShelterEventDialogueUI.cs`
- `Assets/_Project/Scripts/Shelter/DoorObject.cs`
- `Assets/_Project/Editor/ShelterEventDialogueSceneInstaller.cs`
- `Assets/_Project/Scenes/Shelter.unity`
- `Assets/_Project/Prefabs/Facilities/ExitDoor.prefab`

현재 호출 흐름:

```text
PlayerInteraction
→ DoorObject.AlternateInteract()
→ ShelterEventDialogueController.TryOpenTodaysEvent()
→ ShelterEventDialogueUI.ShowEvent()
→ UI ChoiceRequested
→ BranchOneFlowController.SelectEvent()
```

현재 연결된 기능:

- ExitDoor 보조 상호작용으로 1분기 이벤트 열기
- 이벤트 제목·본문·선택지 표시
- 선택 처리
- Signal 또는 Join 점수 반영
- `BranchDiaryRepository` 내부 기록
- `DayCycleManager` 날짜를 Branch 상태에 부분 반영

현재 연결되지 않은 기능:

- 13일 차 계열 판정
- 외부 `randomRoll` 전달
- `EndingRouteCampaignController`
- 2분기 이벤트
- 기존 `DiaryUI` 표시
- 기존 `EndingManager` 연결
- 기존 `GameManager` 연결
- Save/Load

`ShelterEventDialogueController`는 `BranchOneFlowController`를 외부에서 주입받지 않고 직접 생성해 소유하며, Shelter용 이벤트 10개도 Controller 내부의 `CreateShelterCatalog()`에서 생성한다. `ShelterEventDialogueUI`는 이벤트 표시와 입력 전달만 담당하고 점수나 Flow 상태를 직접 수정하지 않는다.

현재 연결 분류:

- 이벤트 표시: 연결
- 선택 처리: 연결
- 점수 반영: 연결
- Branch 내부 일기 기록: 연결
- 기존 `DiaryUI` 표시: 미연결
- 날짜 완전 통합: 미연결
- 13일 판정: 미연결
- 2분기: 미연결
- Campaign: 미연결

Shelter Scene에서 `ShelterEventDialogueController`와 `ShelterEventDialogueUI`는 Missing Script 없이 직렬화되어 있다. Controller는 활성 상태이며, `DoorEventDialoguePanel`은 초기 비활성 상태에서 이벤트를 열 때 활성화된다. ExitDoor Prefab 인스턴스의 `eventDialogue` Scene override가 Controller를 참조한다.

### 14.2 날짜 부분 동기화와 기존 Shelter 시스템의 실제 역할

#### 날짜 상태

- 실제 Shelter 날짜: `DayCycleManager.CurrentDay`
- Branch 내부 날짜: `BranchRuntimeState.CurrentDay`
- 두 날짜는 같은 객체가 아니다.
- `ShelterEventDialogueController`가 `DayCycleManager.DayChanged`를 구독한다.
- 날짜 변경 시 `BranchRuntimeState.BeginEventDay(day)`를 직접 호출한다.
- Shelter에서는 `BranchOneFlowController.AdvanceDay()`를 사용하지 않는다.
- 기존 게임은 `MaxDays = 7`인 7일 종료 구조다.
- 현재 정상 흐름에서는 13일 차 계열 판정에 도달할 수 없다.
- 7일 차 종료 후 `DayChanged`가 호출되지 않아 Branch 상태가 7일 차에 남을 수 있다.

이 날짜 구조를 수정하고 정식 일정을 연결하는 것은 Shelter 통합 담당자의 작업 범위다. BranchOnePrototype 담당자는 담당자 협의 없이 기존 날짜 시스템을 임의로 변경하지 않는다.

#### `DayCycleManager`

경로: `Assets/_Project/Scripts/Core/DayCycleManager.cs`

- 현재 날짜는 1일부터 시작한다.
- `MaxDays = 7` 상수로 7일 생존 제한을 사용한다.
- 하루 행동력, 배급, 야간 생리 변화, 야간 사건, 다음 날을 관리한다.
- `EndDay()` 순서는 배급 → 생리 변화 → `EventManager.ResolveNightEvent()` → 날짜 증가다.
- 7일을 넘으면 `GameManager.CompleteSurvival(false)`를 호출하고 `DayChanged`를 호출하지 않는다.

`ShelterEventDialogueController`가 이 Manager를 직접 참조하지만 날짜 상태는 별도로 관리한다. 현재 Branch 기능의 2~13일 구조와 직접 충돌하므로, 기존 `MaxDays`를 바로 13으로 바꾸지 말고 통합 담당자와 정식 날짜 정책을 공동 협의해야 한다.

#### `EventManager`

경로: `Assets/_Project/Scripts/Events/EventManager.cs`

- 현재 `ResolveNightEvent()`는 “큰 사건 없이 밤이 지나갔습니다.” 메시지를 `UIManager`에 표시한다.
- Branch의 선택지 이벤트 카탈로그 역할을 하지 않으며 Branch 시스템과 직접 연결되어 있지 않다.

Branch 문 앞 이벤트와 기존 야간 `EventManager`는 서로 독립된 실행 경로이므로 같은 날 둘 다 발생할 수 있다. 실행 순서와 공존 정책은 Shelter 통합 담당자와 이벤트 진행 시스템 담당자가 공동 협의한다.

#### `DiaryUI`

경로: `Assets/_Project/Scripts/UI/DiaryUI.cs`

- 플레이어 상태, 보유 자원, 메인 일기, 탐사 기록 4개 화면을 표시한다.
- 메인 일기는 날짜별 영구 기록이 아니라 현재 `UIManager.CurrentMessage`를 보여준다.
- 탐사 기록은 `ExplorationManager.ExplorationHistory`를 읽는다.
- Branch의 날짜별 `DiaryDayRecord`를 읽지 않는다.

정식 연결 시 기존 UI에 Repository를 직접 끼워 넣기 전에 View model 또는 Diary Adapter를 설계해야 한다.

#### `EndingManager`와 `GameManager`

경로:

- `Assets/_Project/Scripts/Core/EndingManager.cs`
- `Assets/_Project/Scripts/Core/GameManager.cs`

현재 종료 조건:

- 캐릭터 사망 → `GameOver`
- 7일 생존 완료 → 현재 `GameManager.CompleteSurvival(false)` 경로로 생존 엔딩
- `EndingManager`가 UI를 표시하고 `Time.timeScale = 0`으로 만든다.

Branch의 `Quarter2Passed`와 `AllBranchesFailed`를 기존 종료 조건에 어떻게 매핑할지는 정해지지 않았다. 현재 Campaign 코어는 기존 `EndingManager`를 호출하지 않는다.

#### `UIManager`

경로: `Assets/_Project/Scripts/UI/UIManager.cs`

- `ShelterEventDialogueController`가 직접 참조하고 이벤트 처리 결과 메시지를 `ShowMessage()`로 전달한다.
- Branch 점수나 `BranchDiaryRepository`를 표시하지 않는다.
- 현재 `Start()`에서 하루 종료 버튼을 비활성화하고, 하루 종료는 침대 상호작용 경로를 사용한다.

기존 날짜 진행은 Branch 이벤트 완료 여부를 확인하지 않으므로, 문 앞 이벤트를 선택하지 않고도 하루가 진행될 수 있다.

#### 기존 시스템 연결 관계 요약

- `DayCycleManager`: `ShelterEventDialogueController`가 직접 참조하지만 날짜 상태는 별도로 관리
- `UIManager`: Controller가 직접 참조
- `EventManager`: Branch 시스템과 직접 연결 없음
- `DiaryUI`: `BranchDiaryRepository`와 연결 없음
- `GameManager`: Controller 및 Campaign과 직접 연결 없음
- `EndingManager`: Controller 및 Campaign과 직접 연결 없음

### 14.3 클래스명 충돌

프로젝트에는 서로 다른 `GameManager`가 두 개 있다.

- `YesterdayMap.Core.GameManager`
- 전역 namespace의 `Assets/04.Scripts/Core/GameManager.cs`

향후 Shelter 통합 코드에서 `GameManager`를 축약해서 사용하면 잘못된 타입을 참조할 수 있다. `YesterdayMap.Core.GameManager`처럼 namespace를 명시해야 한다.

현재 Branch 타입은 모두 `YesterdayMap.BranchOne` 계열 namespace를 사용해 `EventManager`, `EndingManager`, `UIManager` 같은 기존 전역·프로젝트 타입과 직접적인 이름 충돌을 피한다.

### 14.4 asmdef 경계와 Bridge 방향

`Assets/_Project`에는 asmdef가 없으므로 기존 Shelter 코드는 `Assembly-CSharp`에 속한다. 기능의 `YesterdayMap.BranchOne` asmdef가 `Assembly-CSharp`를 참조하는 방향은 사용할 수 없다.

현재 구조:

```text
Shelter Scene
→ Assembly-CSharp 소속 ShelterEventDialogueController(MonoBehaviour)
   ├─ DayCycleManager / UIManager / DoorObject 참조
   └─ BranchOneFlowController와 1분기 순수 코어 생성·호출
```

명명된 `BranchOneShelterBridge`는 없지만 `ShelterEventDialogueController`가 Shelter 전용 1분기 부분 Bridge 역할을 수행한다. 새 Bridge나 새 `BranchOneFlowController` 소유자를 중복 생성하지 않는다. 2분기·Campaign의 실제 연결 구조는 통합 담당자와 협의해 결정하며, Branch 코어 코드를 `DayCycleManager`나 `EventManager` 안에 복사하지 않는다.

### 14.5 충돌 위험이 높은 파일과 Scene

다음 파일은 여러 시스템의 공용 연결점이다.

- `Assets/_Project/Scenes/Shelter.unity`
- `Assets/_Project/Scenes/Scavenge.unity`
- `Assets/_Project/Scripts/Events/ShelterEventDialogueController.cs`
- `Assets/_Project/Scripts/Events/ShelterEventDialogueUI.cs`
- `Assets/_Project/Scripts/Shelter/DoorObject.cs`
- `Assets/_Project/Editor/ShelterEventDialogueSceneInstaller.cs`
- `Assets/_Project/Prefabs/Facilities/ExitDoor.prefab`
- `Assets/_Project/Scripts/Core/DayCycleManager.cs`
- `Assets/_Project/Scripts/Events/EventManager.cs`
- `Assets/_Project/Scripts/UI/DiaryUI.cs`
- `Assets/_Project/Scripts/UI/UIManager.cs`
- `Assets/_Project/Scripts/Core/GameManager.cs`
- `Assets/_Project/Scripts/Core/EndingManager.cs`
- `Assets/_Project/Editor/V02ProjectBuilder.cs`
- `ProjectSettings/EditorBuildSettings.asset`
- `ProjectSettings/ProjectSettings.asset`

특히 Scene, Canvas, 공용 Manager, Builder는 팀원이 동시에 수정할 가능성이 높아 Git 충돌 위험이 크다.

### 14.6 Scene 변경 도구 경고

`ShelterEventDialogueSceneInstaller`는 읽기 전용 검증 도구가 아니다. 실행 시 실제 `Shelter.unity`를 열고 Controller, UI Panel, ExitDoor 참조를 변경한 뒤 Scene과 Asset을 저장한다. 담당자 협의 없이 실행하지 않는다.

메뉴 `Yesterday Map > Build v0.2 2.5D Project`는 단순 검증 메뉴가 아니다.

실제 `BuildAll()`은:

1. 재료·Prefab·데이터를 생성하거나 저장한다.
2. `MainMenu`, `Prologue`, `Scavenge`, `Shelter`, `Exploration` Scene을 새 Empty Scene부터 다시 만든다.
3. `Assets/_Project/Scenes/` 아래의 동일한 Scene 파일 경로에 저장한다.
4. Build Settings Scene 배열을 5개 Scene으로 다시 할당한다.

따라서 기존 Scene 수작업 변경을 덮어쓸 수 있다. 현재 `V02ProjectBuilder`는 Shelter 부분 연결을 다시 설치하지 않으므로 실행하면 Controller, UI Panel, ExitDoor Inspector 연결이 덮어써지거나 사라질 위험이 있다. 담당자 협의 없이 실행하지 않는다.

Sandbox Builder도 각 Sandbox Scene을 새 Scene으로 재생성해 같은 경로에 저장한다. Scene을 수동 수정했다면 해당 Builder 실행 전에 변경 보존 여부를 확인해야 한다.

---

## 15. 다음 작업 권장 순서

1. BranchOnePrototype 기준 1·2·3분기 코어 상태를 유지한다.
2. 인수인계 문서와 마지막 자동 테스트 결과를 현재 코드 기준으로 유지한다.
3. Shelter 영역은 읽기 전용 참고 대상으로 보호한다.
4. 현재 1→2→3분기 Campaign과 통합 Sandbox를 회귀 기준으로 유지한다.
5. 실제 단서 콘텐츠·SourceId Provider·날짜별 스토리 순서를 기획 확정 후 외부 계층에서 구현한다.
6. 실제 콘텐츠 Adapter와 Campaign 공개 API 사이의 책임 경계를 검증한다.
7. Quarter3 결과를 실제 엔딩 시스템에 전달하는 정책을 통합 담당자와 협의한다.
8. 실제 Shelter·탐사·DiaryUI 연결은 별도 통합 작업으로 진행한다.
9. Shelter 통합 담당자에게 필요한 공개 API와 연결 조건을 정리한다.
10. 실제 Shelter 연결은 담당자 협의 후 별도 작업으로 진행한다.

다음 항목은 이벤트 진행 시스템 담당자의 직접 작업 순서에서 제외한다.

- `DayCycleManager` 수정
- `Shelter.unity` 또는 Shelter Prefab 수정
- 새 Shelter Bridge 생성
- Shelter UI에 2분기 화면 추가
- 기존 `DiaryUI` 수정
- `EndingManager` 또는 `GameManager` 수정

위 항목은 Shelter 통합 담당자의 작업이거나 공동 협의 항목이다.

---

## 16. 수정 금지 또는 충돌 주의 파일

> [!WARNING]
> - 기존 `BranchOneSandbox`는 1분기와 일기 회귀 테스트용으로 보존한다.
> - `EndingRouteCampaignSandbox`는 1→2→3분기 통합과 계열 전환 테스트용으로 보존한다.
> - 두 Sandbox를 실제 게임 Scene으로 취급하지 않는다.
> - 두 Sandbox를 Build Settings에 임의로 등록하지 않는다.
> - 실제 이벤트 문장을 C# Provider에 대량 하드코딩하지 않는다.
> - Presenter가 `Quarter2FlowController`를 직접 조작하지 않는다.
> - 정상 2분기 UI 흐름은 `EndingRouteCampaignController` 래퍼 API를 사용한다.
> - 2분기 코어에 날짜 증가 코드를 넣지 않는다.
> - `Quarter2Passed`에서 3분기를 자동 실행하지 않고 외부 `AdvanceToQuarter3()` 호출을 유지한다.
> - 기존 `Runtime/Quarter3` 코어를 복제하거나 Campaign·Sandbox 안에 같은 규칙을 다시 구현하지 않는다.
> - Quarter3 코어에 실제 단서 문장, 지도 배치, 날짜별 스토리 순서, 실제 엔딩명을 하드코딩하지 않는다.
> - `SourceId`를 지역 ID로 한정하지 않고 범용 출처 ID로 유지한다.
> - 최종 선택은 외부 `OpenFinalChoice()` 요청으로 개방하며 단서 개수를 자격 조건으로 추가하지 않는다.
> - `AllBranchesFailed`에서 마지막 벙커를 자동 실행하지 않는다.
> - Scene Builder를 검증 메뉴로 오해해 무분별하게 실행하지 않는다.
> - `ShelterEventDialogueSceneInstaller`는 실제 Shelter Scene을 열고 저장하는 변경 도구이므로 담당자 협의 없이 실행하지 않는다.
> - `V02ProjectBuilder`는 현재 Shelter 부분 연결을 덮어쓰거나 제거할 수 있으므로 담당자 협의 없이 실행하지 않는다.
> - 현재 Shelter에는 이미 1분기 부분 Bridge가 있으므로 새 Bridge나 새 Flow 소유자를 중복 생성하지 않는다.
> - 기능 작업 중 `Assets/_Project`, `Assets/04.Scripts`, `ProjectSettings`, `Packages`를 임의로 수정하지 않는다.
> - 이벤트 진행 시스템 담당자의 기본 수정 범위는 `Assets/Features/BranchOnePrototype/` 안으로 제한한다.

실제 확인 없이 수정하지 말아야 하는 대상:

- `Assets/_Project/`
- `Assets/04.Scripts/`
- `Assets/_Project/Scenes/Shelter.unity`
- `Assets/_Project/Scenes/Scavenge.unity`
- `Assets/_Project/Scripts/Events/ShelterEventDialogueController.cs`
- `Assets/_Project/Scripts/Events/ShelterEventDialogueUI.cs`
- `Assets/_Project/Scripts/Shelter/DoorObject.cs`
- `Assets/_Project/Editor/ShelterEventDialogueSceneInstaller.cs`
- `Assets/_Project/Prefabs/Facilities/ExitDoor.prefab`
- `Assets/_Project/Scripts/UI/DiaryUI.cs`
- `Assets/_Project/Scripts/Core/DayCycleManager.cs`
- `Assets/_Project/Scripts/Events/EventManager.cs`
- `Assets/_Project/Scripts/Core/EndingManager.cs`
- `Assets/_Project/Scripts/Core/GameManager.cs`
- `Assets/_Project/Scripts/UI/UIManager.cs`
- `Assets/_Project/Editor/V02ProjectBuilder.cs`
- `ProjectSettings/EditorBuildSettings.asset`
- `ProjectSettings/ProjectSettings.asset`
- `Packages/`

---

## 17. 빠른 확인용 체크리스트

새 작업자가 프로젝트를 받았을 때:

- [ ] Unity 프로젝트 컴파일 확인
- [ ] `BranchOneSandbox` Scene 열기
- [ ] 1분기 정상 진행 확인
- [ ] `EndingRouteCampaignSandbox` Scene 열기
- [ ] 1분기→2분기 정상 통과 확인
- [ ] 첫 계열 실패 후 반대 계열 전환 확인
- [ ] `AllBranchesFailed` 확인
- [ ] Quarter3 순수 코어가 `NotStarted → CollectingClues → FinalChoiceOpen → Resolved`로 동작하는지 확인
- [ ] Quarter3 `FinalChoiceOpen` 단서 획득 허용과 `Resolved` 이후 차단 확인
- [ ] Quarter3 Campaign Wrapper와 통합 Sandbox 연결을 확인
- [ ] Reset 확인
- [ ] EditMode 전체 테스트 실행
- [ ] Sandbox가 Build Settings 미등록인지 확인
- [ ] 실제 Shelter에 1분기 부분 연결이 존재함을 확인
- [ ] 13일 판정은 Shelter에 미연결임을 확인
- [ ] 2·3분기와 Campaign은 Shelter에 미연결임을 확인
- [ ] Shelter 시작 점수는 현재 0/0임을 확인
- [ ] Sandbox 시작 점수는 1/1임을 확인
- [ ] 현재 작업 범위가 `Assets/Features/BranchOnePrototype/` 내부인지 확인
- [ ] Shelter 통합 파일을 수정하지 않았는지 확인
- [ ] Scene Builder와 Installer를 실행하지 않았는지 확인
- [ ] 변경 전 Git 브랜치와 작업 파일 확인
- [ ] `V02ProjectBuilder` 실행 여부를 팀과 확인
- [ ] 현재 Shelter 부분 Bridge와 Flow 소유자를 중복 생성하지 않았는지 확인
- [ ] 2분기 날짜 정책이 외부 책임으로 유지되는지 확인

---

## 18. 향후 작업 시 사용할 Codex 공통 프롬프트

아래 프롬프트를 이 기능의 수정·확장 요청 마지막에 붙여 사용한다. 기능 변경과 문서 동기화를 하나의 완료 조건으로 묶기 위한 공통 규칙이다.

프로젝트 구조, 기능 폴더, 문서 경로, Scene 수, 분기 범위, 보호 대상, 테스트 방식, Build Settings 정책 또는 Bridge 구조가 달라졌다면 이 공통 프롬프트도 같은 작업에서 현재 구조에 맞게 갱신해야 한다.

### 복사용 공통 프롬프트

```text
──────────────────────────────
[작업 담당 범위 보호 — 필수]
──────────────────────────────

이번 작업은 이벤트 진행 시스템 담당 범위만 수행한다.

기본 수정 범위:

Assets/Features/BranchOnePrototype/

다음 실제 게임 파일과 Scene은 읽기 전용 참고 대상으로 취급한다.

- Assets/_Project/Scenes/Shelter.unity
- Assets/_Project/Scripts/Events/ShelterEventDialogueController.cs
- Assets/_Project/Scripts/Events/ShelterEventDialogueUI.cs
- Assets/_Project/Scripts/Shelter/DoorObject.cs
- Assets/_Project/Editor/ShelterEventDialogueSceneInstaller.cs
- Assets/_Project/Prefabs/Facilities/ExitDoor.prefab
- Assets/_Project/Scripts/Core/DayCycleManager.cs
- Assets/_Project/Scripts/Events/EventManager.cs
- Assets/_Project/Scripts/UI/DiaryUI.cs
- Assets/_Project/Scripts/Core/GameManager.cs
- Assets/_Project/Scripts/Core/EndingManager.cs

사용자가 Shelter 통합 작업을 명시적으로 요청하고
담당자 협의가 확인되지 않은 경우에는
위 파일을 수정하지 마.

현재 Shelter에는 이미 1분기 부분 Bridge가 존재하므로
새 Bridge나 새로운 BranchOneFlowController 소유자를 중복 생성하지 마.

현재 실제 상태:

- Shelter 1분기 이벤트 표시·선택·점수:
  부분 연결
- Shelter 시작 점수:
  Signal 0 / Join 0
- Sandbox 시작 점수:
  Signal 1 / Join 1
- 13일 차 판정:
  Shelter 미연결
- 2분기·3분기와 Campaign:
  Shelter 미연결

현재 작업에서는 독립 코어, Campaign, Sandbox,
테스트, 인수인계 문서만 수정한다.

실제 날짜 통합, Shelter UI, DiaryUI, EventManager,
EndingManager와 GameManager 연결은 Shelter 통합 담당자의 작업 범위다.

현재 Quarter3 상태:

- `Runtime/Quarter3`에 Unity 비의존 순수 코어가 존재
- 실제 단서 콘텐츠와 최종 선택 문구는 아직 없음
- `SourceId`는 탐사지·스토리·이벤트 등에 재사용하는 범용 출처 ID
- 날짜별 스토리 순서는 Quarter3 코어가 관리하지 않음
- 최종 선택은 외부 `OpenFinalChoice()` 호출로 개방
- 단서 개수와 관계없이 최종 선택 가능
- Campaign은 Q1→Q2→Q3를 연결하며 `AdvanceToQuarter3()`로 명시적 진입
- 정상 Q3 조작은 `EndingRouteCampaignController` Wrapper API 사용
- `EndingRouteCampaignSandbox`는 Q1→Q2→Q3 통합 테스트 Scene
- `Quarter3SandboxDataProvider`는 `TEST_` ID만 제공
- 기존 Quarter3 코어를 Campaign 또는 Sandbox에서 복제·중복 생성하지 않음
- 실제 Shelter·탐사·DiaryUI 연결은 현재 범위 밖

──────────────────────────────
[인수인계 문서 동기화 — 필수]
──────────────────────────────

이번 기능 작업이 완료되면 반드시 다음 인수인계 문서도
현재 실제 코드 기준으로 함께 갱신해라.

Assets/Features/BranchOnePrototype/HANDOFF_1Q_2Q_BRANCH_SYSTEM.md

인수인계 문서 갱신은 선택 사항이 아니며,
문서 갱신까지 끝나야 이번 작업이 완료된 것으로 본다.

기억이나 이전 보고만 보고 수정하지 말고,
이번 작업으로 변경된 실제 코드, Scene, 테스트,
asmdef, 파일 구조를 직접 확인한 뒤 반영해라.

단순히 문서 맨 아래에 작업 내역만 추가하지 말고,
기존 본문에서 영향을 받은 부분을 직접 찾아 최신 상태로 수정해라.

최소한 다음 항목을 점검해라.

- 문서 작성일 또는 최종 갱신일
- 현재 구현 범위
- 구현 완료 항목
- 전체 폴더 및 파일 구조
- 신규·수정·삭제 파일
- 클래스별 책임
- API와 메서드 이름
- Phase 및 상태 전환 구조
- 이벤트 진행 순서
- 판정 규칙
- Scene 개수와 역할
- Scene Hierarchy
- Sandbox 테스트 방법
- 실제 게임 Scene 연결 상태
- Build Settings 등록 상태
- 자동 테스트 파일과 테스트 개수
- 최종 테스트 성공·실패 결과
- 현재 미구현 항목
- 충돌 주의 파일
- 다음 작업 권장 순서
- 빠른 확인용 체크리스트

이번 작업과 관련 없는 문서 내용을 임의로 다시 쓰거나
확정되지 않은 내용을 새로 추가하지 마.

기존 내용과 실제 구현이 달라진 경우:

1. 낡은 내용을 삭제하거나 수정
2. 현재 구현을 기준으로 교체
3. 완료 보고에 변경 이유 명시

기능이 새 분기로 확장되거나 문서 제목과 범위가 맞지 않게 된 경우에는
문서 파일을 바로 분리하거나 이름을 바꾸지 말고,
기존 문서를 유지하면서 제목과 대상 범위를 확장해라.

예:

- 기존: 1분기·2분기 엔딩 계열 시스템 인수인계
- 3분기 추가 후:
  1분기·2분기·3분기 엔딩 계열 시스템 인수인계

파일명을 변경할 필요가 있다면 먼저 기존 참조 위치를 확인하고,
완료 보고에서 변경 필요성을 설명해라.
사용자 요청 없이 임의로 파일명을 변경하지 마.

──────────────────────────────
[공통 프롬프트 자체 동기화]
──────────────────────────────

인수인계 문서 마지막의 다음 항목도 확인해라.

## 향후 작업 시 사용할 Codex 공통 프롬프트

이번 작업으로 다음 내용이 변경된 경우에는
문서 안의 공통 프롬프트도 최신 구조에 맞게 수정해라.

- 기능 폴더 경로
- 인수인계 문서 경로
- Scene 개수
- 분기 범위
- 보호 대상 폴더
- 테스트 실행 방식
- Build Settings 정책
- Bridge 또는 Adapter 구조
- 수정 금지 파일
- 완료 보고 항목

즉, 실제 작업 구조가 바뀌었는데
옛날 경로나 옛날 Scene 이름이 공통 프롬프트에 남아 있으면 안 된다.

공통 프롬프트는 다음 작업자가 그대로 복사해도
현재 프로젝트에 맞게 작동하는 상태로 유지해라.

현재 기준:

- 기능 폴더: Assets/Features/BranchOnePrototype/
- 인수인계 문서:
  Assets/Features/BranchOnePrototype/HANDOFF_1Q_2Q_BRANCH_SYSTEM.md
- 대상 분기: 1분기·2분기·3분기
- Sandbox Scene:
  Assets/Features/BranchOnePrototype/Scenes/BranchOneSandbox.unity
  Assets/Features/BranchOnePrototype/Scenes/EndingRouteCampaignSandbox.unity
- 두 Sandbox Scene은 Build Settings 미등록 상태를 유지
- 순수 코어 테스트는 Unity Test Runner의 EditMode에서 실행
- 실제 Shelter에는 Assembly-CSharp 소속 `ShelterEventDialogueController` 기반 1분기 부분 Bridge가 존재
- Shelter 1분기 이벤트 표시·선택·점수는 부분 연결, 시작 점수는 Signal 0 / Join 0
- Sandbox 시작 점수는 Signal 1 / Join 1
- Shelter의 13일 판정, 2·3분기와 Campaign은 미연결
- 2분기 정상 외부 조작은 EndingRouteCampaignController 래퍼 API 사용
- Quarter3 순수 코어는 `Runtime/Quarter3`에 존재하고 실제 콘텐츠는 없음
- Quarter3 `SourceId`는 지역에 한정되지 않는 범용 출처 ID
- Quarter3 코어는 날짜별 스토리 순서를 관리하지 않음
- Quarter3 최종 선택은 외부 `OpenFinalChoice()` 호출로 개방하며 단서 수와 무관하게 선택 가능
- Campaign은 `AdvanceToQuarter3()`와 Quarter3 Wrapper API로 1→2→3분기 연결
- `EndingRouteCampaignSandbox`는 기존 Scene 하나를 Q1→Q2→Q3 통합 검증용으로 확장
- `Quarter3SandboxDataProvider`의 `TEST_` ID는 Sandbox 전용이며 실제 콘텐츠가 아님
- 실제 Shelter·탐사·DiaryUI 연결과 실제 Q3 콘텐츠는 현재 범위 밖
- 보호 대상:
  Assets/_Project/Scenes/Shelter.unity
  Assets/_Project/Scripts/Events/ShelterEventDialogueController.cs
  Assets/_Project/Scripts/Events/ShelterEventDialogueUI.cs
  Assets/_Project/Scripts/Shelter/DoorObject.cs
  Assets/_Project/Editor/ShelterEventDialogueSceneInstaller.cs
  Assets/_Project/Prefabs/Facilities/ExitDoor.prefab
  Assets/_Project/Scripts/Core/DayCycleManager.cs
  Assets/_Project/Scripts/Events/EventManager.cs
  Assets/_Project/Scripts/UI/DiaryUI.cs
  Assets/_Project/Scripts/Core/GameManager.cs
  Assets/_Project/Scripts/Core/EndingManager.cs
  Assets/04.Scripts
  ProjectSettings
  Packages
  기존 게임 Scene과 Prefab
  Build Settings

──────────────────────────────
[문서 갱신 검증]
──────────────────────────────

작업 완료 전에 다음을 확인해라.

1. 변경된 실제 파일이 문서 파일 구조에 반영됐는지
2. 삭제되거나 이름이 바뀐 파일이 문서에 남아 있지 않은지
3. 새 API와 Phase가 문서에 반영됐는지
4. 이벤트 진행 규칙이 실제 코드와 일치하는지
5. Scene 설명이 실제 Scene과 일치하는지
6. 테스트 개수가 실제 Test Runner 결과와 일치하는지
7. 미구현 항목 중 이번에 구현된 내용이 제거됐는지
8. 다음 작업 순서가 현재 상태에 맞게 변경됐는지
9. 공통 프롬프트 안의 경로와 이름이 최신인지
10. Markdown 경로와 코드 표기가 정확한지

이번 작업에서 Scene을 수정하지 않았다면
문서 확인을 이유로 Scene을 저장하거나 재직렬화하지 마.

──────────────────────────────
[Git diff 확인]
──────────────────────────────

최종 Git diff에서 다음을 구분해 확인해라.

- 이번 기능 작업으로 의도한 변경 파일
- 갱신된 인수인계 Markdown 파일
- Unity가 생성한 인수인계 파일의 .meta
- 의도하지 않은 기존 파일 변경

문서 갱신 과정에서 Scene, Prefab, ProjectSettings,
Packages, Build Settings가 의도치 않게 변경됐다면 되돌려라.

──────────────────────────────
[완료 보고에 추가할 내용]
──────────────────────────────

기존 완료 보고 항목과 함께 다음도 반드시 보고해라.

- 인수인계 문서 수정 여부
- 인수인계 문서 경로
- 문서에서 수정한 목차
- 이번 작업으로 새로 추가한 설명
- 기존 설명 중 삭제·교체한 내용
- 최종 갱신일
- 문서에 기록한 최신 테스트 개수
- 공통 프롬프트 수정 여부
- 공통 프롬프트에서 변경한 경로·규칙
- 실제 코드와 문서의 불일치가 남아 있는지
- Git diff에 나타난 문서 외 변경 파일

인수인계 문서를 수정하지 않았다면
“수정할 필요가 없었다”라고만 보고하지 말고,
어떤 항목을 확인했고 왜 변경이 필요하지 않았는지 설명해라.
```
## Q3 Join 물자 지원 이벤트

- Signal 계열은 기존처럼 탐사 Source에서 단서를 획득한다.
- Join 계열은 1~4일차에 일반 생존자 무리 또는 붉은 완장 경비대를 선택하고 통조림/물 1개를 지원해 단서를 획득한다.
- 날짜별 두 대상, 총 8개 정의가 있으며 한 날짜에는 한 대상만 처리할 수 있어 한 플레이의 최대 획득 단서는 4개다.
- 통조림과 물이 모두 0이면 해당 날짜는 `MissedNoResources`로 정상 종료된다. 단서와 지원 횟수는 늘지 않으며 재시도할 수 없다.
- 선택 자원만 없고 다른 자원이 있으면 `SelectedResourceUnavailable`이며 날짜 기회는 유지된다.
- 대상별 지원 횟수는 표시와 후속 콘텐츠용 정보이며 최종 선택을 자동 결정하거나 선택지를 숨기지 않는다.
- 단서가 0개여도 외부에서 Q3 최종 선택을 열 수 있다.
- 현재 구현은 실제 Shelter 인벤토리와 연결하지 않고 불변 수량 Snapshot을 입력받으며, 성공 결과의 `ConsumeResource`와 `ConsumeAmount = 1`을 외부 통합 계층에 제공한다.
- Sandbox는 통조림 2개와 물 2개의 테스트 수량을 사용하도록 확장 가능한 데이터와 Controller를 제공한다.
- `EndingRouteCampaignSandbox`의 Join Q3 패널에서 대상, 통조림/물, 테스트 수량,
  지원 실행과 다음 지원 일차를 직접 조작할 수 있다. Signal Q3에서는 기존 Source
  단서 버튼을 그대로 사용한다.
- Sandbox 테스트 물자는 통조림 2개/물 2개로 시작하고 `Supported`일 때 반환된
  소비 자원만 1개 차감한다. `MissedNoResources`는 날짜만 처리하고,
  `SelectedResourceUnavailable`은 날짜를 Open으로 유지해 다른 물자로 재시도한다.
- 지원 일차는 처리 완료 후에만 1~4일 사이에서 진행하며 4일 종료가 최종 선택을
  자동으로 열지는 않는다. 지원 횟수와 무관하게 Join 최종 선택지 두 개가 유지된다.
- Campaign 생성자는 Join 지원 Catalog의 8개 정의와 Q3 Join 단서/Source 연결을
  즉시 검증한다. 잘못되거나 빈 Catalog는 `ArgumentException`으로 조기 실패한다.
- 성공 preflight에는 지원 상태 버전이 저장된다. Campaign은 단서 등록 전에 Commit
  가능성을 재검증하고, Commit은 상태 변경·중복·정의 불일치를 조용히 무시하지 않고
  예외로 드러낸다. Unity 단일 스레드의 동일 호출 안에서 단서 등록과 지원 기록을
  연속 확정해 정상 반환 시 두 상태가 항상 함께 존재한다.
- Join 지원 Controller, Campaign 통합, Catalog 실패 검증을 포함한 전체 EditMode
  테스트 결과는 238/238, 실패 0, Skip 0이다.
