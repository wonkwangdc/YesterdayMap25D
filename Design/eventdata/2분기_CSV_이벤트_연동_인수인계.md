# 2분기 CSV 이벤트 연동 인수인계

작성 기준: 2026-08-03  
대상 프로젝트: 《어제의 지도》 Unity 6000.3.20f1

## 1. 사용한 원본 CSV

- `Design/eventdata/2분기 구조신호계열 이벤트 .csv`
- `Design/eventdata/2분기 합류계열 이벤트 .csv`

파일명에는 `이벤트`와 `.csv` 사이에 공백이 있으므로 파일을 검색하거나 자동화할 때 주의한다.

## 2. 현재 연동 방식

현재 게임은 실행 중에 CSV 파일을 직접 읽지 않는다.

CSV의 확정 내용을 아래 코드 카탈로그에 수동으로 옮겨 등록하는 방식이다.

- `Assets/_Project/Scripts/Events/ShelterCampaignCatalog.cs`
  - `CreateQuarter2Catalog()`
  - `RegisterQuarter2(...)`

따라서 CSV만 수정해도 게임 내용은 자동으로 바뀌지 않는다. CSV가 변경되면
`ShelterCampaignCatalog.CreateQuarter2Catalog()`의 해당 이벤트 데이터도 함께 수정해야 한다.

## 3. CSV 열과 런타임 데이터 매핑

2분기 이벤트 하나는 `Quarter2EventDefinition`으로 저장된다.

| CSV 열 | 런타임 필드 | 게임에서 사용하는 위치 |
|---|---|---|
| `순서` | `Order` | 현재 계열에서 1 → 2 → 3 순서로 진행 |
| `EventId 가안` | `EventId` | 이벤트 고유 식별자 |
| `제목` | `Title` | 이벤트 대화창 제목 및 일기 제목 |
| `발생 위치` | `Location` | 이벤트 대화창 상단 화자/위치 영역 |
| `상황 요약` | `Body` | 선택 전 상황 설명 |
| `진행 선택` | `ProgressChoiceText` | 첫 번째 선택지 |
| `진행 결과/효과` 또는 `진행 결과` | `ProgressResultText` | 진행 선택 후 당일 메인 일기에 표시 |
| `거부 선택` | `RejectChoiceText` | 두 번째 선택지 |
| `거부 결과` | `RejectResultText` | 거부 선택 후 당일 메인 일기에 표시 |

런타임 모델 위치:

- `Assets/Features/BranchOnePrototype/Runtime/Quarter2/Quarter2EventDefinition.cs`

## 4. 등록된 이벤트

### 구조신호 계열

| 순서 | EventId | 제목 |
|---:|---|---|
| 1 | `Q2_SIGNAL_EVT_01` | 잡음 뒤의 목소리 |
| 2 | `Q2_SIGNAL_EVT_02` | 신호가 닿는 장소 |
| 3 | `Q2_SIGNAL_EVT_03` | 생존자 응답 요청 |

### 합류 계열

| 순서 | EventId | 제목 |
|---:|---|---|
| 1 | `Q2_JOIN_EVT_01` | 문 앞의 교환 상자 |
| 2 | `Q2_JOIN_EVT_02` | 저녁의 노크 |
| 3 | `Q2_JOIN_EVT_03` | 붉은 천을 묶은 방문자 |

## 5. 인게임 표시 흐름

```text
1분기 판정으로 2분기 계열 결정
        ↓
ShelterCampaignCatalog에서 해당 계열 1번 이벤트 조회
        ↓
ShelterEventDialogueController.OpenQuarter2Event()
        ↓
발생 위치 + 제목 + 상황 요약 표시
        ↓
진행 선택 / 거부 선택 표시
        ↓
플레이어 선택
        ↓
ShelterEventDialogueController.SelectQuarter2()
        ↓
기존 2분기 진행·거부 판정 반영
        ↓
선택한 결과를 당일 메인 일기에 기록
        ↓
대화창에는 “일기장을 열어서 결과를 확인하세요.”만 표시
        ↓
다음 날 2분기 다음 단계 진행
```

관련 코드:

- `Assets/_Project/Scripts/Events/ShelterEventDialogueController.cs`
  - `OpenQuarter2Event()`: 선택 전 이벤트 화면 구성
  - `SelectQuarter2()`: 선택 처리, 결과 기록, 결과 확인 안내
  - `ApplyQuarter2ProgressEffects()`: CSV에 적힌 실제 스탯 효과 적용
  - `GetEventDiaryRecords()`: 현재 일차의 이벤트 기록 제공
- `Assets/_Project/Scripts/Events/ShelterEventDialogueUI.cs`
  - `ShowChoice()`: 상황과 두 선택지 표시
  - `ShowDiaryResultPrompt()`: 선택 후 일기장 확인 안내 표시
- `Assets/_Project/Scripts/UI/DiaryUI.cs`
  - `BuildMainDiaryText()`: 현재 일차 기록을 메인 일기장에 출력

## 6. 일기장 연동 규칙

선택이 완료되면 아래 형식으로 `campaignRecords`에 기록한다.

```text
현재 일차 - 이벤트 제목

선택한 결과 문구
```

- 진행 선택: `ProgressResultText`
- 거부 선택: `RejectResultText`

메인 일기장은 현재 일차와 일치하는 기록만 가져온다. 날짜가 바뀌면 이전 일차의
이벤트 결과는 메인 일기장에서 보이지 않는다.

선택 직후 이벤트 대화창에는 실제 결과를 직접 보여주지 않고 다음 문구만 표시한다.

```text
일기장을 열어서 결과를 확인하세요.
```

## 7. 실제 스탯 효과

현재 CSV에서 실제 스탯 변화가 명시된 항목만 적용한다.

| 이벤트 | 선택 | 실제 적용 |
|---|---|---|
| 잡음 뒤의 목소리 | 진행 | 정신력 5 감소 |
| 신호가 닿는 장소 | 진행 | 체력·배고픔·갈증 각각 5 감소 |
| 나머지 2분기 이벤트 | 진행 또는 거부 | 현재 별도 스탯 변화 없음 |

수치는 `Quarter2EventDefinition`의 다음 필드에 저장된다.

- `ProgressHealthDelta`
- `ProgressHungerDelta`
- `ProgressThirstDelta`
- `ProgressMoraleDelta`

CSV에서 새로운 스탯·아이템 효과가 추가되면 데이터 필드와
`ApplyQuarter2ProgressEffects()`를 함께 확장해야 한다.

## 8. 진행/거부 수치에 대한 주의점

CSV에는 모든 이벤트의 `진행 +1`, `거부 +1`이 모두 1로 적혀 있다.

현재 런타임은 이 두 열을 공통 계열 점수로 직접 읽지 않는다. 기존 2분기 시스템 규칙을
유지하여 다음처럼 처리한다.

- 진행 선택 → 해당 계열의 `ProgressCount` 1 증가
- 거부 선택 → 해당 계열의 `RejectCount` 1 증가
- 거부가 2회 누적되면 현재 계열 실패 및 반대 계열 전환 판정
- 3개의 이벤트를 통과하면 해당 계열로 2분기 통과

CSV의 `진행 +1`, `거부 +1`을 별도의 동일 점수로 사용하려면
`Quarter2FlowController`의 판정 규칙을 다시 설계해야 한다.

## 9. 테스트용 오른쪽 위 HUD

기존 1분기 테스트 HUD가 2분기에 들어가면 자동으로 2분기 표시로 전환된다.

표시 항목:

- 현재 일차
- 현재 계열
- 현재 이벤트 순서
- 구조신호 진행/거부 누적
- 합류 진행/거부 누적
- 최근 이벤트 제목과 진행/거부 선택

관련 파일:

- `Assets/_Project/Scripts/UI/Quarter1ScoreDebugHUD.cs`
- `Assets/_Project/Editor/Quarter1ScoreDebugHUDInstaller.cs`
- `Assets/_Project/Scenes/Shelter.unity`

이 HUD와 1분기 스킵 버튼은 테스트 전용이다. 테스트 종료 후에는
`Yesterday Map > Debug > Remove Quarter 1 Score HUD` 메뉴로 씬의 HUD 오브젝트를
삭제할 수 있다. 이후 테스트용 공개 프로퍼티가 더 이상 필요하지 않으면
`ShelterEventDialogueController`의 `Quarter2...` 디버그 조회 프로퍼티도 함께 정리한다.

## 10. CSV 수정 시 작업 순서

1. 두 CSV 중 대상 이벤트 내용을 수정한다.
2. `ShelterCampaignCatalog.CreateQuarter2Catalog()`에서 같은 EventId의 데이터를 수정한다.
3. 스탯 효과가 바뀌면 `Progress...Delta` 값 또는 효과 적용 코드를 수정한다.
4. Unity가 스크립트 컴파일을 마칠 때까지 기다린다.
5. 구조신호 진입과 합류 진입을 각각 테스트한다.
6. 대화창에서 위치·제목·상황·두 선택지가 CSV와 일치하는지 확인한다.
7. 진행과 거부를 각각 선택해 당일 메인 일기의 결과 문구를 확인한다.
8. 날짜가 바뀐 뒤 전날 이벤트 기록이 메인 일기장에서 사라지는지 확인한다.
9. 오른쪽 위 테스트 HUD의 진행/거부 누적과 최근 선택을 확인한다.

## 11. 검증 체크리스트

- 구조신호 계열 3개가 순서대로 등장한다.
- 합류 계열 3개가 순서대로 등장한다.
- 선택 전 화면이 CSV의 상황 요약 및 선택지와 일치한다.
- 진행 선택 결과가 메인 일기에 표시된다.
- 거부 선택 결과가 메인 일기에 표시된다.
- 결과 대화창에는 일기장 확인 안내만 표시된다.
- 명시된 스탯 효과가 실제 캐릭터 상태에 반영된다.
- 기존 진행·거부 누적과 계열 전환 규칙이 유지된다.
- 이전 일차의 이벤트 결과가 현재 일기에 남지 않는다.
- 테스트 HUD의 값이 실제 선택과 일치한다.
- Console에 C# 오류, `NullReferenceException`, `MissingReferenceException`이 없다.

