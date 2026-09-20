# 《어제의 지도》 생성 아트 카탈로그

Unity의 `Assets` 폴더 밖에서 관리하는 기획·콘셉트 이미지입니다. 게임에 실제로 사용할 이미지가 확정되면 필요한 파일만 `Assets`로 복사합니다.

## 환경 디자인

### 시작 파밍 주택 — 60~100초 동선 설계

- 파일: `Environment/House/HouseLootingLayout_60-100sec_v01.png`
- 생성일: 2026-07-23
- 용도: 게임 시작 직후 제한 시간 파밍 단계의 주택 구조 참고
- 시작 위치: 아래 중앙 현관
- 도착 위치: 왼쪽 중앙 지하 벙커 계단
- 핵심 설계:
  - 중앙 거실·식당을 이동 허브로 사용
  - 주방에는 식량과 물 배치
  - 욕실에는 약품 배치
  - 공구실에는 공구, 배터리와 연료 배치
  - 가장 먼 침실·홈오피스에는 가방, 라디오와 지도 배치
  - 필수품 위주의 짧은 동선과 고가치 물자를 노리는 긴 동선을 함께 제공
- 제작 방식: OpenAI 내장 이미지 생성
- 참고 이미지: 사용자가 제공한 주택 평면 스케치

## 캐릭터 디자인

### 주인공 콘셉트 1 — 좀비 아포칼립스 덕후·DIY 벙커 제작자

- 폴더: `Characters/Protagonist/`
- 생성일: 2026-07-23
- 콘셉트 원화: `Protagonist_Concept_v01.png`
- 디자인·생성 기록: `Protagonist_DesignNotes_v01.md`
- Unity용 투명 보행 시트: `Protagonist_Walk_4Dir_16Frames_Unity_v01.png`
- 투명 처리 전 원본: `Protagonist_Walk_4Dir_16Frames_Chroma_v01.png`
- 개별 프레임: `Frames/Protagonist_[Down|Left|Right|Up]_01~04.png`
- 캐릭터 방향:
  - 30대 초반의 평범한 한국인 생존자
  - 좀비 아포칼립스를 좋아해 집 아래에 DIY 벙커를 만든 준비형 덕후
  - 군인이나 영웅보다는 영리하고 약간 피곤해 보이는 생활형 인물
  - 짙은 청회색 우의 재킷, 청록색 후드, 올리브 카고 바지와 등산화
  - 검은 배낭, 주황색 비상 호루라기, 손전등과 손으로 그린 지도를 소지
- 애니메이션 구성:
  - 4방향 × 방향별 4프레임 = 총 16프레임
  - 행 순서: 아래 보기, 왼쪽 보기, 오른쪽 보기, 위 보기
  - 열 순서: 왼발 접지, 통과 자세, 오른발 접지, 반대 통과 자세
  - Unity용 시트 크기: 1252 × 1252 px
  - 셀 크기: 313 × 313 px
- Unity 권장 설정:
  - Sprite Mode: `Multiple`
  - Sprite Editor → Slice → Type: `Grid by Cell Count`
  - Column & Row: `4 × 4`
  - Pivot: `Bottom Center`
  - Filter Mode: 부드러운 표현은 `Bilinear`, 또렷한 표현은 `Point`
- 제작 방식: OpenAI 내장 이미지 생성 후 크로마키 투명화 및 균등 분할
