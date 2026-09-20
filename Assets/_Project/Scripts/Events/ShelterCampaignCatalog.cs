using System;
using System.Collections.Generic;
using YesterdayMap.BranchOne;
using YesterdayMap.BranchOne.Events;
using YesterdayMap.BranchOne.Quarter2;
using YesterdayMap.BranchOne.Quarter3;
using YesterdayMap.BranchOne.Quarter3.JoinSupport;

namespace YesterdayMap.Events
{
    public readonly struct ShelterIntroDialogueLine
    {
        public ShelterIntroDialogueLine(string speaker, string body)
        {
            Speaker = speaker ?? string.Empty;
            Body = body ?? string.Empty;
        }

        public string Speaker { get; }
        public string Body { get; }
    }

    // Production story data used by the Shelter scene campaign bridge.
    public static class ShelterCampaignCatalog
    {
        public const string SignalMilitaryChoice = "Q3_SIGNAL_MILITARY";
        public const string SignalSecondFrequencyChoice = "Q3_SIGNAL_SECOND_FREQUENCY";
        public const string JoinSurvivorsChoice = "Q3_JOIN_SURVIVORS";
        public const string JoinRedArmbandChoice = "Q3_JOIN_RED_ARMBAND";
        public const string SignalConvenienceStoreSource = "Q3_SIGNAL_LOCATION_CONVENIENCE_STORE";
        public const string SignalHospitalSource = "Q3_SIGNAL_LOCATION_HOSPITAL";
        public const string SignalPoliceStationSource = "Q3_SIGNAL_LOCATION_POLICE_STATION";
        public const string SignalResidentialAreaSource = "Q3_SIGNAL_LOCATION_RESIDENTIAL_AREA";
        public const string SignalCommunicationsSource = "Q3_SIGNAL_LOCATION_COMMUNICATIONS";

        public static BranchEventCatalog CreateQuarter1Catalog()
        {
            return ShelterQuarter1EventCatalog.CreateScheduledCatalog();
        }

        public static BranchEventCatalog CreateQuarter1Catalog(int randomSeed)
        {
            return ShelterQuarter1EventCatalog.CreateScheduledCatalog(randomSeed);
        }

        public static Quarter2EventCatalog CreateQuarter2Catalog()
        {
            Quarter2EventCatalog catalog = new();
            RegisterQuarter2(
                catalog,
                "Q2_SIGNAL_EVT_01",
                BranchRoute.Signal,
                1,
                "잡음 뒤의 목소리",
                "늦은 시간, 라디오에서 일정한 간격으로 짧은 음성과 신호음이 잡힌다.\n\n" +
                "내용은 대부분 잡음에 묻혀 정확히 알아들을 수 없지만, 누군가 특정 주파수와 방송 시간을 반복해서 말하는 것처럼 들린다.\n\n" +
                "오래전에 녹음된 자동 반복 방송일 수도 있고, 현재 누군가가 보내고 있는 신호일 수도 있다. 방송은 오랫동안 이어지고, 반복되는 잡음은 주인공의 신경을 거슬리게 한다.",
                "벙커 · 늦은 시간 · 라디오",
                "방송 시간과 내용을 기록하며 계속 듣는다",
                "라디오에서 같은 숫자와 시간이 계속 반복됐다.\n" +
                "대부분은 잡음에 묻혀 있었지만, 우연이라고 보기에는 일정한 규칙이 있었다.\n" +
                "무엇을 위한 방송인지는 알 수 없지만 다음 방송 시간은 기록해두었다.\n" +
                "주인공은 잡음 속에서 반복되는 숫자와 방송 시간을 기록한다. 신호의 정체는 알 수 없지만, 일정한 규칙이 있다는 사실을 확인한다.\n" +
                "정신력이 소폭 감소한다",
                "의미 없는 잡음이라 판단하고 소리를 무시한다",
                "한동안 잡음 섞인 목소리가 이어졌다.\n" +
                "오래된 자동 방송인지, 누군가 지금 보내는 신호인지 확인할 수 없었다.\n" +
                "의미 없는 소리에 정신을 빼앗기지 않기 위해 오늘은 라디오를 멀리했다.\n" +
                "불확실한 신호에 시간을 낭비하지 않기로 한다. 라디오를 완전히 끄거나 버리지는 않지만, 그날은 방송을 확인하지 않고 휴식을 취한다.",
                progressMoraleDelta: -5f);
            RegisterQuarter2(
                catalog,
                "Q2_SIGNAL_EVT_02",
                BranchRoute.Signal,
                2,
                "신호가 닿는 장소",
                "탐사 도중, 탐사 장소 인근에서 정체불명의 주파수가 잡힌다. 특정 구역에 가까워질수록 짧은 음성과 신호음이 점점 선명해진다.\n\n" +
                "하지만 신호가 가장 강하게 잡히는 구역에는 좀비가 많고, 오래 머무르면 다른 생존자에게 자신의 존재를 들킬 위험도 있다.\n\n" +
                "신호를 더 자세히 확인하려면 현재 탐사 경로를 벗어나 위험한 구역까지 들어가야 한다",
                "탐사 결과 후속 이벤트",
                "위험을 감수하고 신호가 강해지는 구역을 조사한다",
                "탐사 경로를 벗어나 신호가 강해지는 곳까지 들어갔다.\n" +
                "방송에서는 최근 날짜와 이 지역의 상황이 언급됐다.\n" +
                "누군가 지금도 방송을 보내고 있을 가능성은 높아졌지만, 발신자가 누구인지는 알 수 없다.\n" +
                "주인공은 신호가 강해지는 방향을 따라가 방송을 확인한다.\n" +
                "같은 문장이 반복되는 사이, 새로운 날짜와 주변 지역의 최근 상황이 언급된다. 오래전에 녹음된 자동 반복 방송이 아니라, 누군가 현재 방송을 송출하고 있을 가능성이 높아진다.\n\n" +
                "다만 방송을 보내는 사람이 누구인지는 여전히 알 수 없다.\n" +
                "체력·배고픔·갈증 소폭 감소",
                "더 깊이 들어가지 않고 탐사를 마친다",
                "탐사 중 정체불명의 신호가 잡혔다.\n" +
                "더 안쪽으로 들어가면 내용을 확인할 수 있었겠지만, 주변에는 좀비가 너무 많았다.\n" +
                "확인되지 않은 방송보다 무사히 벙커로 돌아가는 일을 우선했다.\n" +
                "불확실한 신호 때문에 추가 위험을 감수하지 않기로 한다. 신호의 정체는 확인하지 못하지만 체력과 생존 자원을 보존한다.",
                progressHealthDelta: -5f,
                progressHungerDelta: -5f,
                progressThirstDelta: -5f,
                requiresExploration: true);
            RegisterQuarter2(
                catalog,
                "Q2_SIGNAL_EVT_03",
                BranchRoute.Signal,
                3,
                "생존자 응답 요청",
                "어느 날 갑자기 전파 상태가 달라지면서, 라디오에서 이전보다 훨씬 또렷한 방송이 잡힌다.\n\n" +
                "방송에서는 살아 있는 사람이 있다면 정해진 시간 안에 짧은 신호를 보내라고 요구한다. 이름이나 정확한 위치는 요구하지 않지만, 응답할 경우 정체를 알 수 없는 누군가에게 자신의 생존 사실이 알려지게 된다.\n\n" +
                "방송의 발신자가 군인지, 민간 구조대인지, 일반 생존자 집단인지, 다른 목적을 가진 세력인지는 아직 확인할 수 없다.",
                "벙커 · 라디오",
                "위치를 숨긴 채 짧은 응답 신호를 보낸다",
                "주인공은 자신의 위치나 인적 사항을 밝히지 않고, 생존자가 존재한다는 사실만 알리는 짧은 신호를 보낸다.\n\n" +
                "잠시 동안 아무런 반응이 없다. 그러다 잡음 사이로 짧은 목소리가 돌아온다.\n\n" +
                "누군가 “……확인.”\n" +
                "방송은 살아 있는 사람이 있다면 짧은 신호를 보내라고 했다.\n" +
                "위치와 이름은 밝히지 않고 생존자가 있다는 사실만 알렸다.\n" +
                "잠시 뒤 잡음 사이로 “확인”이라는 대답이 돌아왔다.\n" +
                "누군가 내 신호를 들었다. 하지만 그들이 누구인지는 아직 모른다\n" +
                "상대가 주인공의 신호를 실시간으로 받았다는 사실은 확인됐지만, 상대의 정체와 목적은 여전히 알 수 없다.",
                "위치 노출 가능성을 고려해 응답하지 않는다",
                "방송에서는 살아 있는 사람이 있다면 응답하라고 했다.\n" +
                "정확한 위치를 요구하지는 않았지만, 신호를 보내면 누군가에게 내 존재가 알려진다.\n" +
                "상대가 누구인지 알 수 없는 이상 응답하지 않는 편이 안전하다고 판단했다.\n" +
                "방송을 들었지만 응답하지 않는다. 상대에게 생존 사실을 알리지 않음으로써 벙커가 노출될 가능성을 피한다.");
            RegisterQuarter2(
                catalog,
                "Q2_JOIN_EVT_01",
                BranchRoute.Join,
                1,
                "문 앞의 교환 상자",
                "플레이어가 탐사를 마치고 벙커로 복귀한다.\n\n" +
                "벙커 출입문 앞에는 떠날 때 없었던 작은 상자 하나가 놓여 있다.\n\n" +
                "플레이어가 주변을 확인하지만 사람은 보이지 않는다.\n\n" +
                "상자 안에는 소량의 물자와 짧은 메모가 들어 있다.\n\n" +
                "> “이 근처에서 활동하는 사람이 있다는 것을 알고 있다.”\n" +
                "> “싸울 생각은 없다.”\n" +
                "> “서로의 위치를 묻지 않고 물자나 정보만 교환하고 싶다.”\n" +
                "> “교환할 생각이 있다면 상자 안에 표시를 남겨라.”\n\n" +
                "상자에는 정확한 거점 위치나 작성자의 이름이 적혀 있지 않다.\n\n" +
                "근처에 여러 사람이 활동하고 있으며, 벙커의 존재를 어느 정도 알고 있다는 사실만 확인할 수 있다.",
                "탐사 후 벙커 출입문 앞",
                "교환 의사를 표시한다",
                "자신의 이름이나 보유 물자를 적지 않고, 메모 아래에 짧은 표시를 남긴다.\n\n" +
                "“위치는 공개하지 않는다.”\n" +
                "“필요한 정보가 있다면 이곳에 남겨라.”\n\n" +
                "상자를 다시 출입문 바깥에 놓는다.",
                "상자를 치운다",
                "상대가 벙커 주변을 다시 찾아오지 못하도록 상자와 메모를 치운다.\n" +
                "어떤 답변도 남기지 않는다.\n\n" +
                "누군가 벙커 앞에 교환 상자를 두고 갔다.\n" +
                "물자 교환은 핑계일 수도 있다.\n" +
                "상자와 메모를 모두 치웠다.",
                requiresExploration: true);
            RegisterQuarter2(
                catalog,
                "Q2_JOIN_EVT_02",
                BranchRoute.Join,
                2,
                "저녁의 노크",
                "플레이어가 탐사를 끝내고 벙커에서 획득 물자를 정리하던 중, 출입문 바깥에서 일정한 간격으로 문을 두드리는 소리가 들린다.\n\n" +
                "문밖의 사람은 자신이 근처 임시 거점에서 나온 탐사자라고 밝힌다.\n\n" +
                "그는 벙커 문을 열어달라고 요구하지 않는다.\n\n" +
                "대신 오늘 탐사 중 확인한 좀비 이동 경로를 알려주겠다며, 문을 사이에 두고 잠시 이야기하자고 제안한다.\n\n" +
                "> “문을 열 필요는 없다.”\n" +
                "> “서로 얼굴을 보지 않아도 된다.”\n" +
                "> “이 근처 북쪽 길은 당분간 이용하지 않는 편이 좋다.”\n\n" +
                "상대는 최근 주변에서 좀비 수가 늘고 있으며, 자신들의 거점도 탐사 경로를 바꾸고 있다고 말한다.",
                "탐사 후 벙커 안 · 출입문",
                "문을 사이에 두고 대화한다",
                "플레이어는 문을 열지 않은 채 상대의 이야기를 듣는다.\n\n" +
                "자신도 오늘 탐사에서 확인한 위험 지역 한 곳을 알려준다.\n\n" +
                "양쪽은 벙커와 임시 거점의 정확한 위치, 보유 물자와 인원수는 말하지 않는다.\n\n" +
                "저녁에 누군가 벙커 문을 두드렸다.\n" +
                "문은 열지 않고 서로가 확인한 위험 지역만 이야기했다.\n" +
                "그는 근처에 여러 사람이 머무는 거점이 있다고 말했다.\n\n" +
                "대화를 마친 상대는 더 이상 문을 두드리지 않고 돌아간다.",
                "아무 대답도 하지 않는다",
                "플레이어는 벙커 안에 사람이 없는 것처럼 조용히 기다린다.\n\n" +
                "문밖의 사람은 몇 차례 더 말을 걸지만, 강제로 문을 열거나 위협하지 않는다.\n\n" +
                "잠시 후 발소리가 멀어진다.\n\n" +
                "누군가 문밖에서 말을 걸었다.\n" +
                "문을 열 필요가 없다고 했지만 대답하지 않았다.\n" +
                "한참 뒤 발소리가 사라졌다.",
                requiresExploration: true);
            RegisterQuarter2(
                catalog,
                "Q2_JOIN_EVT_03",
                BranchRoute.Join,
                3,
                "붉은 천을 묶은 방문자",
                "플레이어가 탐사를 마치고 벙커로 돌아오자 출입문 근처에 두 사람이 기다리고 있다.\n\n" +
                "한 명은 다리를 다친 채 벽에 기대어 있고, 다른 한 명은 팔에 붉은 천을 묶고 주변을 경계하고 있다.\n\n" +
                "붉은 천을 묶은 사람은 자신이 근처 임시 거점의 경비를 담당하고 있다고 설명한다.\n\n" +
                "이 시점에서 붉은 완장은 약탈자의 상징이 아니라, 거점의 경비 인원을 구분하기 위한 표시다.\n\n" +
                "두 사람은 외부 탐사를 마치고 거점으로 돌아가던 중 좀비에게 쫓겼으며, 부상자가 다시 걸을 수 있을 때까지만 벙커 출입문 주변에서 쉬게 해달라고 요청한다.\n\n" +
                "> “안으로 들어갈 생각은 없다.”\n" +
                "> “문도 열지 않아도 된다.”\n" +
                "> “저 사람이 움직일 수 있을 때까지만 여기 있게 해달라.”",
                "탐사 후 벙커 출입문 근처",
                "출입문 밖에서 잠시 쉬게 한다",
                "플레이어는 벙커 문을 열지 않은 상태로 두 사람이 외부의 가려진 공간에 잠시 머무르는 것을 허락한다.\n\n" +
                "붉은 완장을 찬 경비 인원은 주변을 살피고, 부상자는 잠시 휴식을 취한다.\n\n" +
                "시간이 지나 부상자가 다시 움직일 수 있게 되자 두 사람은 자리에서 일어난다.\n\n" +
                "떠나기 전 경비 인원은 자신들의 거점에도 여러 생존자가 있으며, 주변 사람들과 물자와 정보를 제한적으로 교환하고 있다고 말한다.\n\n" +
                "정확한 거점 위치는 밝히지 않는다.\n\n" +
                "벙커 앞에 부상자와 붉은 천을 묶은 사람이 와 있었다.\n" +
                "문은 열지 않고, 밖에서 잠시 쉬도록 허락했다.\n" +
                "붉은 천은 근처 거점의 경비를 구분하기 위한 표시라고 했다.",
                "다른 장소로 이동하라고 한다",
                "플레이어는 자신의 벙커 근처에 외부인을 머물게 할 수 없다고 말한다.\n\n" +
                "붉은 완장 경비 인원은 플레이어를 위협하거나 문을 열려고 하지 않는다.\n\n" +
                "그는 부상자의 짐 일부를 버리고, 부상자를 부축해 다른 장소로 이동한다.\n\n" +
                "부상자를 데리고 온 사람들이 벙커 앞에서 쉬게 해달라고 했다.\n" +
                "하지만 외부인이 이곳에 머물게 둘 수는 없었다.\n" +
                "붉은 천을 묶은 사람은 부상자를 데리고 자리를 떠났다.",
                requiresExploration: true);
            return catalog;
        }

        public static bool TryGetQuarter2IntroDialogue(
            string eventId,
            out IReadOnlyList<ShelterIntroDialogueLine> lines)
        {
            switch (eventId)
            {
                case "Q2_SIGNAL_EVT_01":
                    lines = new[]
                    {
                        new ShelterIntroDialogueLine("라디오", "치지직…… 치직……"),
                        new ShelterIntroDialogueLine("라디오", "……시…… 치직…… 반복……"),
                        new ShelterIntroDialogueLine("한도윤", "……뭐야? 방금 사람 목소리였나?"),
                        new ShelterIntroDialogueLine("라디오", "치지직…… 주파수…… 시간…… 치직……"),
                        new ShelterIntroDialogueLine("한도윤", "같은 말이 계속 반복되는 것 같은데……."),
                        new ShelterIntroDialogueLine("한도윤", "조금 더 들어보면 뭔가 알 수 있을지도 모르겠어.")
                    };
                    return true;

                case "Q2_SIGNAL_EVT_02":
                    lines = new[]
                    {
                        new ShelterIntroDialogueLine("라디오", "치직…… 치지직……"),
                        new ShelterIntroDialogueLine("한도윤", "아까보다 신호가 선명해졌네."),
                        new ShelterIntroDialogueLine("라디오", "……치직…… 이 지역…… 현재……"),
                        new ShelterIntroDialogueLine("한도윤", "이쪽으로 갈수록 더 잘 들리는 건가?"),
                        new ShelterIntroDialogueLine("한도윤", "문제는 저 앞에 좀비가 너무 많다는 건데…….")
                    };
                    return true;

                case "Q2_SIGNAL_EVT_03":
                    lines = new[]
                    {
                        new ShelterIntroDialogueLine("라디오", "치지직…… 살아 있는 사람이 있다면…… 응답하라."),
                        new ShelterIntroDialogueLine("라디오", "정해진 시간 안에 짧은 신호로 응답하라…… 치직……."),
                        new ShelterIntroDialogueLine("한도윤", "이번엔 확실히 들렸어."),
                        new ShelterIntroDialogueLine("한도윤", "생존자를 찾고 있는 건가……?"),
                        new ShelterIntroDialogueLine("한도윤", "하지만 내가 대답하면 누군가한테 내 존재가 알려지겠지.")
                    };
                    return true;

                case "Q2_JOIN_EVT_01":
                    lines = new[]
                    {
                        new ShelterIntroDialogueLine("한도윤", "……이게 뭐지? 나갈 때는 없었는데."),
                        new ShelterIntroDialogueLine("한도윤", "상자 안에 메모가 있네……."),
                        new ShelterIntroDialogueLine("메모", "“이 근처에서 활동하는 사람이 있다는 것을 알고 있다.”"),
                        new ShelterIntroDialogueLine("메모", "“싸울 생각은 없다. 위치를 묻지 않고 물자나 정보만 교환하고 싶다.”"),
                        new ShelterIntroDialogueLine("한도윤", "……내가 여기 있다는 건 이미 눈치챘다는 건가."),
                        new ShelterIntroDialogueLine("한도윤", "답을 남길지, 그냥 치울지 정해야겠네.")
                    };
                    return true;

                case "Q2_JOIN_EVT_02":
                    lines = new[]
                    {
                        new ShelterIntroDialogueLine("문밖의 사람", "똑. 똑. 똑."),
                        new ShelterIntroDialogueLine("한도윤", "……누구지?"),
                        new ShelterIntroDialogueLine("문밖의 사람", "놀라게 했다면 미안하다. 문을 열 필요는 없다."),
                        new ShelterIntroDialogueLine("문밖의 사람", "근처 임시 거점에서 왔다. 북쪽 길은 당분간 피하는 게 좋아."),
                        new ShelterIntroDialogueLine("한도윤", "정보만 주겠다고……?"),
                        new ShelterIntroDialogueLine("문밖의 사람", "서로 아는 만큼만 말하자. 얼굴도 볼 필요 없어."),
                        new ShelterIntroDialogueLine("한도윤", "……대답해도 되는 걸까.")
                    };
                    return true;

                case "Q2_JOIN_EVT_03":
                    lines = new[]
                    {
                        new ShelterIntroDialogueLine("한도윤", "……사람이 둘이나 있잖아."),
                        new ShelterIntroDialogueLine("문밖의 사람", "문은 열지 마. 안으로 들어갈 생각은 없어."),
                        new ShelterIntroDialogueLine("문밖의 사람", "이 사람이 다리를 다쳤다. 다시 걸을 수 있을 때까지만 여기서 쉬게 해줘."),
                        new ShelterIntroDialogueLine("한도윤", "팔에 묶은 그 붉은 천은 뭐지?"),
                        new ShelterIntroDialogueLine("문밖의 사람", "근처 거점 경비를 구분하는 표시야. 다른 뜻은 없어."),
                        new ShelterIntroDialogueLine("한도윤", "……문밖에서 쉬게 할지, 돌려보낼지 정해야겠네.")
                    };
                    return true;

                default:
                    lines = Array.Empty<ShelterIntroDialogueLine>();
                    return false;
            }
        }

        public static Quarter3ClueCatalog CreateQuarter3ClueCatalog()
        {
            Quarter3ClueCatalog catalog = new();
            for (int day = 1; day <= 4; day++)
            {
                RegisterClue(catalog, SignalClueId(day), BranchRoute.Signal, SignalSourceId(day));
            }

            RegisterJoinClue(
                catalog, 1, Quarter3JoinSupportTarget.SurvivorGroup,
                "공동 배급 기록", CreateJoinSupplyShareRecord());
            RegisterJoinClue(
                catalog, 1, Quarter3JoinSupportTarget.RedArmband,
                "경비대 활동 기록", CreateJoinGuardActivityRecord());
            RegisterJoinClue(
                catalog, 2, Quarter3JoinSupportTarget.SurvivorGroup,
                "부상자 치료 기록", CreateJoinInjuryCareRecord());
            RegisterJoinClue(
                catalog, 2, Quarter3JoinSupportTarget.RedArmband,
                "구조 대상 선별 기준", CreateJoinRescueCriteriaRecord());
            RegisterJoinClue(
                catalog, 3, Quarter3JoinSupportTarget.SurvivorGroup,
                "지하 통로 조사 결과", CreateJoinTunnelSurveyRecord());
            RegisterJoinClue(
                catalog, 3, Quarter3JoinSupportTarget.RedArmband,
                "차량 및 탑승자 계획", CreateJoinVehiclePlanRecord());
            RegisterJoinClue(
                catalog, 4, Quarter3JoinSupportTarget.SurvivorGroup,
                "협상 계획과 리스크 관리 계획서", CreateJoinNegotiationPlanRecord());
            RegisterJoinClue(
                catalog, 4, Quarter3JoinSupportTarget.RedArmband,
                "물류창고 점거 계획서", CreateJoinWarehouseSeizurePlanRecord());

            RegisterClue(
                catalog,
                "Q3_SIGNAL_CLUE_MATCHED_WARNING",
                BranchRoute.Signal,
                SignalConvenienceStoreSource,
                "맞아떨어진 경고",
                CreateMatchedWarningRecord());
            RegisterClue(
                catalog,
                "Q3_SIGNAL_CLUE_PRECHECK",
                BranchRoute.Signal,
                SignalHospitalSource,
                "구조 전 확인 사항",
                CreateRescuePrecheckRecord());
            RegisterClue(
                catalog,
                "Q3_SIGNAL_CLUE_ABORTED_EVAC",
                BranchRoute.Signal,
                SignalPoliceStationSource,
                "중단된 대피 작전",
                CreateAbortedEvacuationRecord());
            RegisterClue(
                catalog,
                "Q3_SIGNAL_CLUE_AMBUSH_REPORT",
                BranchRoute.Signal,
                SignalPoliceStationSource,
                "구조방송 관련 습격 보고서",
                CreateAmbushReportRecord());
            RegisterClue(
                catalog,
                "Q3_SIGNAL_CLUE_LAST_WAIT",
                BranchRoute.Signal,
                SignalResidentialAreaSource,
                "기다림의 마지막 기록",
                CreateLastWaitRecord());
            RegisterClue(
                catalog,
                "Q3_SIGNAL_CLUE_LIVE_TRANSMISSION",
                BranchRoute.Signal,
                SignalCommunicationsSource,
                "오늘 날짜의 송출 기록",
                CreateLiveTransmissionRecord());

            return catalog;
        }

        public static Quarter3JoinSupportCatalog CreateJoinSupportCatalog()
        {
            Quarter3JoinSupportCatalog catalog = new();
            RegisterSupport(catalog, 1, Quarter3JoinSupportTarget.SurvivorGroup, "일반 생존자들의 공동 배급을 지원한다");
            RegisterSupport(catalog, 1, Quarter3JoinSupportTarget.RedArmband, "붉은 완장 경비대의 경비 물자를 지원한다");
            RegisterSupport(catalog, 2, Quarter3JoinSupportTarget.SurvivorGroup, "일반 생존자들과 부상자 치료를 돕는다");
            RegisterSupport(catalog, 2, Quarter3JoinSupportTarget.RedArmband, "붉은 완장 경비대의 구조 기록 정리를 돕는다");
            RegisterSupport(catalog, 3, Quarter3JoinSupportTarget.SurvivorGroup, "일반 생존자들과 지하 통로 자료를 조사한다");
            RegisterSupport(catalog, 3, Quarter3JoinSupportTarget.RedArmband, "붉은 완장 경비대의 차량 확보 계획을 검토한다");
            RegisterSupport(catalog, 4, Quarter3JoinSupportTarget.SurvivorGroup, "일반 생존자들의 협상 준비를 돕는다");
            RegisterSupport(catalog, 4, Quarter3JoinSupportTarget.RedArmband, "붉은 완장 경비대의 물류창고 조사에 동행한다");

            return catalog;
        }

        public static Quarter3FinalChoiceCatalog CreateFinalChoiceCatalog()
        {
            Quarter3FinalChoiceCatalog catalog = new();
            RegisterFinalChoice(catalog, SignalMilitaryChoice, BranchRoute.Signal);
            RegisterFinalChoice(catalog, SignalSecondFrequencyChoice, BranchRoute.Signal);
            RegisterFinalChoice(catalog, JoinSurvivorsChoice, BranchRoute.Join);
            RegisterFinalChoice(catalog, JoinRedArmbandChoice, BranchRoute.Join);
            return catalog;
        }

        public static bool TryGetQuarter3SignalStory(
            int localDay,
            out string title,
            out string body)
        {
            switch (localDay)
            {
                case 1:
                    title = "두 번째 신호의 등장";
                    body =
                        "라디오에서 이전보다 선명해진 군용 주파수 방송이 들려온다.\n\n" +
                        "방송 발신자는 자신들이 현재 지역의 생존자들을 수색 중인 군 구조부대라고 밝힌다.\n\n" +
                        "군은 생존자들에게 당분간 현재 위치를 유지하고, 추후 전달될 구조 지점과 이동 시간을 기다리라고 안내한다.\n\n" +
                        "방송이 끝난 뒤 기존에 듣지 못했던 다른 주파수에서 새로운 목소리가 들려온다.\n\n" +
                        "두 번째 방송은 자신들도 지역 생존자들을 구조하고 있다고 말하지만, 정확한 소속과 위치는 밝히지 않는다.";
                    return true;
                case 2:
                    title = "민간 구조팀의 제안";
                    body =
                        "두 번째 주파수가 다시 방송을 시작한다.\n\n" +
                        "발신자는 자신들을 군과 별도로 활동하는 민간 구조팀이라고 소개한다.\n\n" +
                        "이들은 군이 안내하는 구조 지점보다 가까운 장소에서 생존자들을 모으고 있으며, 이동 가능한 생존자라면 자신들이 안내하는 집결 장소로 올 수 있다고 말한다.\n\n" +
                        "군 주파수에서는 아직 정확한 구조 장소를 공개하지 않고, 구조 작전을 준비 중이라는 안내만 반복한다.";
                    return true;
                case 3:
                    title = "서로 다른 이동 계획";
                    body =
                        "군 주파수가 구체적인 구조 계획을 발표한다.\n\n" +
                        "군은 구조 차량이 도시 외곽 도로를 통해 이동할 예정이며, 지정된 시간에 외곽 구조 지점으로 이동하라고 안내한다.\n\n" +
                        "구조 지점은 벙커에서 멀리 떨어져 있고 이동 경로도 길지만, 군은 차량과 무장 병력을 이용해 생존자들을 안전 구역으로 이송하겠다고 말한다.\n\n" +
                        "이후 두 번째 주파수에서도 구체적인 계획을 제시한다.\n\n" +
                        "두 번째 주파수는 군이 지정한 외곽 지점까지 갈 필요가 없으며, 벙커에서 더 가까운 집결 장소와 비교적 안전한 이동 경로를 알고 있다고 주장한다.\n\n" +
                        "두 방송 모두 이틀 뒤를 이동 시점으로 제시했다.\n\n" +
                        "어느 쪽의 신호를 따를지 결정할 시간은 이틀밖에 남지 않았다.";
                    return true;
                case 4:
                    title = "서로를 향한 경고";
                    body =
                        "군 주파수는 최근 민간 구조대를 자칭하는 미확인 방송이 생존자들을 유인하고 있다는 제보가 들어왔다고 말한다.\n\n" +
                        "군은 신원이 확인되지 않은 방송에 현재 위치와 보유 물자 정보를 전달하지 말고, 공식 구조신호만 따르라고 경고한다.\n\n" +
                        "잠시 후 두 번째 주파수에서도 군을 비판하는 방송이 들려온다.\n\n" +
                        "두 번째 주파수는 군이 과거에도 구조 지점을 약속한 뒤 생존자들을 구하지 못한 적이 있으며, 외곽까지 이동하는 것은 위험한 선택이라고 주장한다.\n\n" +
                        "양쪽 모두 자신들의 방식이 더 안전하며 상대방의 방송을 믿어서는 안 된다고 말한다.\n\n" +
                        "두 주파수 모두 내일 최종 집결 위치와 출발 시간을 다시 방송하겠다고 알렸다.\n\n" +
                        "내일까지는 어느 신호를 따를지 결정해야 한다.";
                    return true;
                case 5:
                    title = "최종 선택";
                    body =
                        "군 주파수에서 마지막 방송이 전달된다.\n\n" +
                        "군은 구조 차량이 예정대로 출발했으며, 정해진 시간까지 외곽 구조 지점으로 이동한 생존자만 탑승시킬 수 있다고 말한다.\n\n" +
                        "이어서 두 번째 주파수에서도 마지막 방송이 들려온다.\n\n" +
                        "두 번째 주파수는 가까운 집결 장소에서 생존자들을 기다리고 있으며, 군의 먼 구조 지점으로 이동하기 전에 자신들에게 오라고 말한다.\n\n" +
                        "두 주파수의 집결 시간이 겹치기 때문에 플레이어는 어느 한쪽만 선택할 수 있다.";
                    return true;
                default:
                    title = string.Empty;
                    body = string.Empty;
                    return false;
            }
        }

        public static bool TryGetQuarter3SignalIntroDialogue(
            int localDay,
            out IReadOnlyList<ShelterIntroDialogueLine> lines)
        {
            switch (localDay)
            {
                case 1:
                    lines = new[]
                    {
                        new ShelterIntroDialogueLine("라디오", "치지직…… 현재 이 주파수를 듣고 있는 생존자에게 알린다."),
                        new ShelterIntroDialogueLine("군 방송", "우리는 이 지역의 생존자들을 수색 중인 군 구조부대다."),
                        new ShelterIntroDialogueLine("군 방송", "현재 위치를 유지하고, 추후 전달할 구조 지점과 이동 시간을 기다려라."),
                        new ShelterIntroDialogueLine("한도윤", "……군이라고?"),
                        new ShelterIntroDialogueLine("라디오", "치지직…… 치직……"),
                        new ShelterIntroDialogueLine("민간 방송", "이 방송을 듣는 생존자들은 주의해라. 우리도 이 지역의 생존자들을 구조하고 있다."),
                        new ShelterIntroDialogueLine("한도윤", "잠깐…… 다른 주파수에서도 방송이 나오잖아."),
                        new ShelterIntroDialogueLine("한도윤", "대체 어느 쪽이 진짜지……?"),
                        new ShelterIntroDialogueLine("한도윤", "두 방송 중 어느 쪽이 진짜인지 확인할 단서를 찾아봐야겠어.")
                    };
                    return true;
                case 2:
                    lines = new[]
                    {
                        new ShelterIntroDialogueLine("민간 방송", "어제 방송을 들은 생존자들에게 다시 알린다."),
                        new ShelterIntroDialogueLine("민간 방송", "우리는 군과 별도로 움직이는 민간 구조팀이다."),
                        new ShelterIntroDialogueLine("민간 방송", "군 구조 지점보다 가까운 곳에서 생존자들을 모으고 있다."),
                        new ShelterIntroDialogueLine("한도윤", "민간 구조팀……."),
                        new ShelterIntroDialogueLine("민간 방송", "이동할 수 있다면 우리가 안내하는 집결 장소로 와라."),
                        new ShelterIntroDialogueLine("라디오", "치직…… 현재 구조 작전을 준비 중이다. 위치를 유지하라……."),
                        new ShelterIntroDialogueLine("한도윤", "군은 아직 장소를 안 알려주고 있고…… 이쪽은 지금 오라고 하네.")
                    };
                    return true;
                case 3:
                    lines = new[]
                    {
                        new ShelterIntroDialogueLine("군 방송", "구조 계획을 알린다. 구조 차량은 도시 외곽 도로를 통해 이동한다."),
                        new ShelterIntroDialogueLine("군 방송", "지정된 시간에 외곽 구조 지점으로 이동하라. 무장 병력이 생존자를 안전 구역으로 이송한다."),
                        new ShelterIntroDialogueLine("한도윤", "외곽까지 가야 한다고……? 거리가 꽤 먼데."),
                        new ShelterIntroDialogueLine("라디오", "치지직……"),
                        new ShelterIntroDialogueLine("민간 방송", "굳이 외곽까지 갈 필요는 없다. 더 가까운 집결 장소가 있다."),
                        new ShelterIntroDialogueLine("민간 방송", "우리는 비교적 안전한 이동 경로를 확보했다."),
                        new ShelterIntroDialogueLine("한도윤", "둘 다 같은 날 움직이라고 하네."),
                        new ShelterIntroDialogueLine("한도윤", "……결정할 시간이 이틀밖에 없는 건가.")
                    };
                    return true;
                case 4:
                    lines = new[]
                    {
                        new ShelterIntroDialogueLine("군 방송", "최근 민간 구조대를 자칭하는 미확인 방송이 생존자를 유인하고 있다는 제보가 있다."),
                        new ShelterIntroDialogueLine("군 방송", "신원이 확인되지 않은 방송에 위치나 보유 물자를 알리지 마라. 공식 구조신호만 따라라."),
                        new ShelterIntroDialogueLine("한도윤", "……저쪽 방송을 말하는 건가."),
                        new ShelterIntroDialogueLine("라디오", "치직…… 치지직……"),
                        new ShelterIntroDialogueLine("민간 방송", "군의 말을 그대로 믿지 마라. 그들은 이전에도 구조 지점을 약속하고 사람들을 구하지 못했다."),
                        new ShelterIntroDialogueLine("민간 방송", "외곽까지 가는 길이 정말 안전한지 직접 생각해라."),
                        new ShelterIntroDialogueLine("한도윤", "서로 상대가 위험하다고 말하고 있어……."),
                        new ShelterIntroDialogueLine("한도윤", "내일까지는 하나를 골라야 해.")
                    };
                    return true;
                case 5:
                    lines = new[]
                    {
                        new ShelterIntroDialogueLine("군 방송", "최종 안내다. 구조 차량은 예정대로 출발했다."),
                        new ShelterIntroDialogueLine("군 방송", "정해진 시간까지 외곽 구조 지점에 도착한 생존자만 탑승할 수 있다."),
                        new ShelterIntroDialogueLine("한도윤", "……이제 정말 움직여야 하는 건가."),
                        new ShelterIntroDialogueLine("라디오", "치지직……"),
                        new ShelterIntroDialogueLine("민간 방송", "우리는 가까운 집결 장소에서 기다리고 있다."),
                        new ShelterIntroDialogueLine("민간 방송", "군의 먼 구조 지점으로 가기 전에 우리 쪽으로 와라."),
                        new ShelterIntroDialogueLine("한도윤", "시간이 겹쳐. 둘 다 갈 수는 없어."),
                        new ShelterIntroDialogueLine("한도윤", "지금까지 확인한 단서들을 생각해서…… 결정해야 한다.")
                    };
                    return true;
                default:
                    lines = Array.Empty<ShelterIntroDialogueLine>();
                    return false;
            }
        }

        public static bool TryGetQuarter3JoinCommonStory(
            int localDay,
            out string title,
            out string body)
        {
            switch (localDay)
            {
                case 1:
                    title = "갈라지기 시작한 배급";
                    body =
                        "외부 생존자들과의 접촉을 이어가기로 한 플레이어는, 그들이 머무는 임시 거점과 물자와 정보를 교환하는 관계를 유지한다.\n\n" +
                        "거점에는 식량 배급과 부상자 치료, 생활 공간 관리를 담당하는 일반 생존자들과 외부 탐사와 좀비 방어를 담당하는 경비대가 함께 생활하고 있다.\n\n" +
                        "경비대원들은 서로를 구분하기 위해 팔에 붉은 천을 묶고 있다. 이때의 붉은 완장은 약탈자의 상징이 아니라 거점 경비를 담당하는 사람들의 표시다.\n\n" +
                        "플레이어는 아직 거점에 완전히 합류하지 않고 자신의 벙커에서 생활한다. 대신 생존자들과 정해진 장소에서 만나거나, 거점의 사람이 플레이어를 찾아오는 방식으로 관계를 이어간다.\n\n" +
                        "최근 외부 탐사가 연달아 실패하고 부상자가 늘어나면서 거점의 식량과 물이 부족해지고 있다.\n\n" +
                        "부족한 물자를 누구에게 우선 배급해야 하는지를 두고 일반 생존자들과 붉은 완장 경비대 사이에 갈등이 발생한다.";
                    return true;
                case 2:
                    title = "한 사람을 구하는 비용";
                    body =
                        "다음 날, 외부 탐사에 나갔던 사람들이 좀비에게 쫓겨 거점으로 돌아온다.\n\n" +
                        "탐사 인원 한 명이 심한 부상을 입어 혼자 움직일 수 없게 되었고, 다른 사람들은 그를 현장에 남겨둔 채 돌아올 수밖에 없었다고 말한다.\n\n" +
                        "붉은 완장 경비대는 남겨진 부상자를 데려오기 위해 다시 위험 지역으로 들어간다.\n\n" +
                        "경비대는 부상자를 데리고 돌아오는 데 성공하지만, 구조 과정에서 경비대원 한 명도 부상을 입는다.\n\n" +
                        "거점 내부에서는 두 부상자를 치료하고 계속 부양해야 하는지를 두고 새로운 갈등이 발생한다.";
                    return true;
                case 3:
                    title = "두 개의 탈출 계획";
                    body =
                        "거점의 정찰을 담당하던 사람이 대규모 좀비 무리가 현재 지역으로 이동하고 있다는 사실을 확인한다.\n\n" +
                        "좀비 무리는 며칠 안에 거점 주변에 도착할 것으로 예상된다.\n\n" +
                        "현재의 방어 시설과 경비 인원으로는 대규모 무리를 막기 어렵다.\n\n" +
                        "생존자들은 늦어도 이틀 뒤에는 거점을 떠나야 한다는 결론을 내린다.\n\n" +
                        "이를 계기로 공동체 내부에서 서로 다른 두 개의 탈출 계획이 제시된다.\n\n" +
                        "일반 생존자들은 지하 정비 통로를 이용하는 계획을 주장한다.\n\n" +
                        "붉은 완장 경비대는 물류창고의 차량을 확보하는 계획을 주장한다.";
                    return true;
                case 4:
                    title = "다른 사람의 자리";
                    body =
                        "붉은 완장 경비대가 물류창고 주변에서 다른 생존자 집단의 흔적을 발견한다.\n\n" +
                        "물류창고를 이용하려는 사람들이 이미 존재한다는 사실이 알려지면서, 차량 확보 계획을 두고 공동체 내부의 갈등이 더욱 심해진다.\n\n" +
                        "일반 생존자들은 먼저 상대와 대화해야 한다고 주장한다.\n\n" +
                        "붉은 완장 경비대는 좀비 무리가 도착하기 전에 행동해야 한다고 주장한다.\n\n" +
                        "논쟁이 계속되는 동안 붉은 완장 경비대는 차량 확보 준비에 필요하다며 공동 창고에 있던 무기와 식량 일부를 자신들의 구역으로 옮긴다.\n\n" +
                        "일반 생존자들은 그 물자가 모두가 함께 모은 것이라며 항의한다.\n\n" +
                        "이 사건을 계기로 하나였던 공동체는 사실상 일반 생존자 무리와 붉은 완장 무리로 갈라진다.\n\n" +
                        "좀비 무리가 예상보다 빠르게 접근하고 있어, 두 무리 모두 내일 아침 거점을 떠나기로 했다.\n\n" +
                        "나 역시 그전까지 누구와 함께 갈지 결정해야 한다.";
                    return true;
                case 5:
                    title = "최종 선택";
                    body =
                        "결국 아침이 밝았다.\n\n" +
                        "밤새 거점 밖에서 들려오던 소리는 날이 밝을수록 가까워졌다. 멀리서 이어지는 울음소리와 무언가 무너지는 소리만으로도 대규모 좀비 무리가 이곳을 향해 오고 있다는 것을 알 수 있었다.\n\n" +
                        "이제 이 거점도 오래 버티지 못한다.\n\n" +
                        "나 역시 계속 벙커에 남아 있을 수는 없다. 좀비 무리가 이 지역을 뒤덮기 전에 이곳을 떠날 방법을 선택해야 한다.\n\n" +
                        "일반 생존자들과 붉은 완장 경비대는 각자의 방식으로 떠날 준비를 마치고 나를 기다리고 있다.\n\n" +
                        "지난 며칠 동안 확인한 기록들이 머릿속을 스친다. 어느 쪽도 안전한 길은 아니며, 누구를 따라가느냐에 따라 앞으로의 생존 방식도 완전히 달라질 것이다.\n\n" +
                        "더는 결정을 미룰 시간이 없다.\n\n" +
                        "오늘, 나는 누구와 함께 이곳을 떠날지 선택해야 한다.";
                    return true;
                default:
                    title = string.Empty;
                    body = string.Empty;
                    return false;
            }
        }

        public static bool TryGetQuarter3JoinIntroDialogue(
            int localDay,
            out IReadOnlyList<ShelterIntroDialogueLine> lines)
        {
            switch (localDay)
            {
                case 1:
                    lines = new[]
                    {
                        new ShelterIntroDialogueLine("일반 생존자", "이번 배급도 줄여야 해. 남은 식량으로는 모두에게 똑같이 나눠줄 수 없어."),
                        new ShelterIntroDialogueLine("붉은 완장 경비대", "밖에 나가서 물자를 구하는 사람들까지 굶기면 다음 탐사는 누가 나가?"),
                        new ShelterIntroDialogueLine("일반 생존자", "부상자들은 며칠째 제대로 먹지도 못했어."),
                        new ShelterIntroDialogueLine("붉은 완장 경비대", "경비가 무너지면 그 부상자들도 지킬 수 없어."),
                        new ShelterIntroDialogueLine("한도윤", "……분위기가 전보다 많이 달라졌네."),
                        new ShelterIntroDialogueLine("한도윤", "물자가 부족해지면서 서로 생각하는 우선순위도 갈리기 시작한 건가.")
                    };
                    return true;
                case 2:
                    lines = new[]
                    {
                        new ShelterIntroDialogueLine("탐사 생존자", "한 명이 아직 밖에 남아 있어. 다리를 다쳐서 혼자 움직일 수가 없었어."),
                        new ShelterIntroDialogueLine("일반 생존자", "그 상태로 두고 왔다고?"),
                        new ShelterIntroDialogueLine("탐사 생존자", "우리까지 잡히면 전부 끝이었어…… 어쩔 수 없었어."),
                        new ShelterIntroDialogueLine("붉은 완장 경비대", "남겨둔 위치를 말해. 우리가 다시 들어간다."),
                        new ShelterIntroDialogueLine("일반 생존자", "또 사람을 보내겠다고? 지금도 다친 사람이 얼마나 많은데."),
                        new ShelterIntroDialogueLine("한도윤", "한 사람을 데려오기 위해 또 다른 사람이 위험해질 수도 있어…….")
                    };
                    return true;
                case 3:
                    lines = new[]
                    {
                        new ShelterIntroDialogueLine("정찰 생존자", "큰일이야. 북쪽에서 대규모 좀비 무리가 이쪽으로 움직이고 있어."),
                        new ShelterIntroDialogueLine("일반 생존자", "얼마나 남았어?"),
                        new ShelterIntroDialogueLine("정찰 생존자", "길어야 이틀. 지금 방어 시설로는 못 막아."),
                        new ShelterIntroDialogueLine("일반 생존자", "지하 정비 통로를 쓰자. 차량 없이도 사람들을 이동시킬 수 있어."),
                        new ShelterIntroDialogueLine("붉은 완장 경비대", "그 좁은 길에 전부 몰려 들어가겠다고? 물류창고 차량을 확보하는 게 낫다."),
                        new ShelterIntroDialogueLine("한도윤", "……결국 이곳을 떠나야 하는 건가."),
                        new ShelterIntroDialogueLine("한도윤", "그리고 방법도 하나가 아니네.")
                    };
                    return true;
                case 4:
                    lines = new[]
                    {
                        new ShelterIntroDialogueLine("일반 생존자", "물류창고에 다른 사람들이 있다면 먼저 대화를 해봐야 해."),
                        new ShelterIntroDialogueLine("붉은 완장 경비대", "대화하다가 좀비 무리가 먼저 도착하면 끝이야. 움직일 수 있을 때 차량부터 확보해야 해."),
                        new ShelterIntroDialogueLine("일반 생존자", "잠깐, 공동 창고에 있던 무기랑 식량은 어디 갔어?"),
                        new ShelterIntroDialogueLine("붉은 완장 경비대", "내일 움직이려면 준비가 필요해. 경비 쪽에서 관리하겠다."),
                        new ShelterIntroDialogueLine("일반 생존자", "그건 모두가 같이 모은 물자야. 마음대로 가져갈 수는 없어."),
                        new ShelterIntroDialogueLine("한도윤", "……이제는 같은 거점 사람들끼리도 서로 믿지 못하는 건가."),
                        new ShelterIntroDialogueLine("한도윤", "내일이면 둘 다 떠난다. 나도 그전까지 정해야 해.")
                    };
                    return true;
                case 5:
                    lines = new[]
                    {
                        new ShelterIntroDialogueLine("상황", "(멀리서 좀비들의 울음소리와 무언가 무너지는 소리가 이어진다.)"),
                        new ShelterIntroDialogueLine("일반 생존자", "우리는 준비 끝났어. 지하 통로로 이동할 거야. 같이 갈 생각이면 지금 와."),
                        new ShelterIntroDialogueLine("붉은 완장 경비대", "차량도 곧 출발한다. 여기 남아 있으면 무리에 갇히게 돼."),
                        new ShelterIntroDialogueLine("일반 생존자", "어느 쪽을 고르든 지금 결정해야 해."),
                        new ShelterIntroDialogueLine("붉은 완장 경비대", "시간 없어. 우리도 더 기다릴 수는 없다."),
                        new ShelterIntroDialogueLine("한도윤", "……지난 며칠 동안 본 것들을 생각해보자."),
                        new ShelterIntroDialogueLine("한도윤", "누구와 함께 이곳을 떠날지, 이제 결정해야 한다.")
                    };
                    return true;
                default:
                    lines = Array.Empty<ShelterIntroDialogueLine>();
                    return false;
            }
        }

        public static bool TryGetLastBunkerDiary(
            int localDay,
            out string entryId,
            out string title,
            out string body,
            out string explorationResultAppend)
        {
            explorationResultAppend = string.Empty;
            switch (localDay)
            {
                case 1:
                    entryId = "LAST_BUNKER_DAY_01_DIARY";
                    title = "계속되는 정적";
                    body =
                        "오늘따라 벙커 주변이 유난히 조용하게 느껴졌다.\n" +
                        "며칠 동안 사람의 목소리도, 문밖의 기척도 듣지 못한 것 같다.\n" +
                        "모두 다른 곳으로 이동한 걸까.";
                    return true;
                case 2:
                    entryId = "LAST_BUNKER_DAY_02_DIARY";
                    title = "사라진 흔적";
                    body =
                        "탐사 중 오래전에 남겨진 흔적은 몇 개 발견했다.\n" +
                        "하지만 최근에 누군가 지나갔다고 볼 만한 흔적은 어디에도 없었다.\n" +
                        "내가 발견하지 못했을 뿐이라고 생각하고 싶다.";
                    explorationResultAppend =
                        "오래전에 남겨진 흔적은 있었지만, 최근에 누군가 지나간 흔적은 보이지 않았다.";
                    return true;
                case 3:
                    entryId = "LAST_BUNKER_DAY_03_DIARY";
                    title = "돌아오지 않는 반응";
                    body =
                        "확인할 수 있는 신호는 전부 살펴봤지만 아무런 반응도 없었다.\n" +
                        "누군가 듣고 있으면서 대답하지 않는 것인지, 애초에 들을 사람이 없는 것인지는 알 수 없다.\n" +
                        "내일은 조금 더 직접 확인해봐야겠다.";
                    return true;
                case 4:
                    entryId = "LAST_BUNKER_DAY_04_DIARY";
                    title = "남겨진 사람";
                    body =
                        "기다리고 있으면 언젠가는 누군가 나타날 거라고 생각했다.\n" +
                        "하지만 지금까지 기다리고 있는 사람은 나뿐이었다.\n" +
                        "어쩌면 정말 이 근처에는 나밖에 남지 않은 걸지도 모른다.";
                    return true;
                case 8:
                    entryId = "LAST_BUNKER_DAY_08_DIARY";
                    title = "익숙해진 하루";
                    body =
                        "오늘도 평소처럼 밖에 나갔다가 벙커로 돌아왔다.\n" +
                        "물건을 정리하고 내일 필요한 것들을 확인했다.\n" +
                        "어제와 크게 다르지 않은 하루였다.\n" +
                        "이런 생활도 어느새 익숙해진 것 같다.";
                    explorationResultAppend =
                        "익숙한 길을 따라 필요한 물건을 찾았다. " +
                        "오래된 흔적들은 그대로였고 새롭게 달라진 것은 없었다.";
                    return true;
                default:
                    entryId = string.Empty;
                    title = string.Empty;
                    body = string.Empty;
                    return false;
            }
        }

        public static bool TryGetLastBunkerIsolationEvent(
            int localDay,
            out BranchEventDefinition definition,
            out string todayDiary)
        {
            todayDiary = string.Empty;
            switch (localDay)
            {
                case 5:
                    definition = new BranchEventDefinition(
                        "LAST_BUNKER_DAY_05_EVENT",
                        1005,
                        "남아 있는 사람을 찾다",
                        "며칠째 아무런 연락도 없다.\n" +
                        "계속 기다리기만 해서는 다른 생존자가 남아 있는지 확인할 수 없을 것 같다.\n" +
                        "우선 내가 이곳에 있다는 사실을 알려야 한다.",
                        1,
                        int.MaxValue,
                        false,
                        new[]
                        {
                            new BranchEventChoice(
                                "LAST_BUNKER_DAY_05_SIGNAL",
                                "외부에 전달할 수 있는 신호를 시도한다",
                                "오늘 외부에 신호를 보내 내가 이곳에 있다는 사실을 알리려 했다.\n" +
                                "한동안 기다렸지만 아무런 응답도 없었다.\n" +
                                "탐사에서도 최근에 사람이 지나간 흔적은 발견하지 못했다.\n" +
                                "누군가 숨어 있는 것인지, 정말 아무도 없는 것인지는 아직 알 수 없다.",
                                0,
                                0,
                                string.Empty),
                            new BranchEventChoice(
                                "LAST_BUNKER_DAY_05_TRACE",
                                "벙커 근처에 생존자의 흔적을 남긴다",
                                "오늘 벙커 근처에 다른 생존자가 알아볼 수 있는 흔적을 남겼다.\n" +
                                "한동안 기다렸지만 아무런 반응도 없었다.\n" +
                                "탐사에서도 최근에 사람이 지나간 흔적은 발견하지 못했다.\n" +
                                "누군가 숨어 있는 것인지, 정말 아무도 없는 것인지는 아직 알 수 없다.",
                                0,
                                0,
                                string.Empty)
                        });
                    todayDiary =
                        "오늘도 탐사를 다녀왔다.물건은 남아 있었지만 최근에 누군가 다녀간 흔적은 없었다.\n" +
                        "먼지가 쌓인 바닥에는 내 발자국만 새롭게 남았다.";
                    return true;
                case 6:
                    definition = new BranchEventDefinition(
                        "LAST_BUNKER_DAY_06_EVENT",
                        1006,
                        "오래 버틸 공간",
                        "벙커의 시설을 점검하던 중 수납장과 출입문이 많이 낡았다는 사실을 발견했다.\n" +
                        "누군가를 기다리는 동안에도 이곳은 계속 망가지고 있다.\n" +
                        "이제는 가지고 있는 자원으로 한쪽부터 손봐야 할 것 같다.",
                        1,
                        int.MaxValue,
                        false,
                        new[]
                        {
                            new BranchEventChoice(
                                "LAST_BUNKER_DAY_06_STORAGE",
                                "수납장을 수리한다",
                                "망가진 수납장을 다시 고정하고 남아 있는 물건들을 정리했다.\n" +
                                "적어도 물건들은 이전보다 안전하게 보관할 수 있을 것 같다.\n" +
                                "며칠 동안 내가 이곳에 있다는 사실을 알리려 했지만 아무런 답도 없었다.\n" +
                                "세상에 정말 나 혼자만 남은 것인지는 알 수 없다.\n" +
                                "하지만 적어도 내가 닿을 수 있는 곳에는 아무도 없는 것 같다.\n" +
                                "이제는 누군가를 기다리는 것보다 이곳에서 얼마나 오래 버틸 수 있을지를 생각해야 한다.",
                                0,
                                0,
                                string.Empty),
                            new BranchEventChoice(
                                "LAST_BUNKER_DAY_06_DOOR",
                                "벙커 문을 수리한다",
                                "느슨해진 벙커 문의 경첩과 잠금장치를 다시 고쳤다.\n" +
                                "이 문이 버텨주는 동안은 안에서 살아남을 수 있을 것 같다.\n" +
                                "며칠 동안 내가 이곳에 있다는 사실을 알리려 했지만 아무런 답도 없었다.\n" +
                                "세상에 정말 나 혼자만 남은 것인지는 알 수 없다.\n" +
                                "하지만 적어도 내가 닿을 수 있는 곳에는 아무도 없는 것 같다.\n" +
                                "이제는 누군가를 기다리는 것보다 이곳에서 얼마나 오래 버틸 수 있을지를 생각해야 한다.",
                                0,
                                0,
                                string.Empty)
                        });
                    todayDiary =
                        "오늘은 벙커 곳곳을 점검했다. 기다리는 동안에도 이곳은 조금씩 낡아가고 있었다. " +
                        "이제는 누군가를 기다리는 것보다, 여기서 오래 버틸 준비를 해야 할 것 같다.";
                    return true;
                case 7:
                    definition = new BranchEventDefinition(
                        "LAST_BUNKER_DAY_07_EVENT",
                        1007,
                        "비어 있는 자리",
                        "벙커 안을 둘러보니 사용하지 않는 물건들이 여기저기 그대로 남아 있다.\n" +
                        "누군가와 함께 사용할 것처럼 비워둔 자리도 있고, 오래 손대지 않은 물건도 많다.\n" +
                        "언제까지 이곳에서 지내게 될지는 모르겠지만 계속 이렇게 둘 수는 없을 것 같다.",
                        1,
                        int.MaxValue,
                        false,
                        new[]
                        {
                            new BranchEventChoice(
                                "LAST_BUNKER_DAY_07_ORGANIZE",
                                "사용하지 않는 물건을 정리한다",
                                "오늘 벙커 안에 쌓여 있던 물건들을 정리했다.\n" +
                                "비어 있던 공간이 조금 넓어지고 필요한 물건도 찾기 쉬워졌다.\n" +
                                "처음 이곳에 들어왔을 때와는 많이 달라졌다.\n" +
                                "이제는 정말 여기서 살아간다는 생각으로 정리해야 할 것 같다.",
                                0,
                                0,
                                string.Empty),
                            new BranchEventChoice(
                                "LAST_BUNKER_DAY_07_KEEP",
                                "그대로 둔다",
                                "오늘은 벙커 안의 물건들을 그대로 두었다.\n" +
                                "치워버리고 나면 정말 돌아올 사람이 아무도 없다는 걸 인정하는 것 같았다.\n" +
                                "당장 생활에 방해되는 것도 아니니 조금 더 이대로 두기로 했다.",
                                0,
                                0,
                                string.Empty)
                        });
                    todayDiary =
                        "오늘은 벙커 안을 천천히 둘러봤다.\n" +
                        "예전에는 잠시 머물 곳이라고 생각했는데, 이제는 이곳에서 보내는 시간이 더 익숙해지고 있다.";
                    return true;
                case 9:
                    definition = new BranchEventDefinition(
                        "LAST_BUNKER_DAY_09_EVENT",
                        1009,
                        "남기는 기록",
                        "일기장을 펼쳐보니 처음 벙커에 들어왔을 때부터 지금까지의 기록이 꽤 많이 쌓여 있다.\n" +
                        "매일 있었던 일을 하나씩 적어왔지만 최근에는 비슷한 내용이 반복되고 있다.\n" +
                        "앞으로도 계속 기록을 남길지 잠시 고민하게 된다.",
                        1,
                        int.MaxValue,
                        false,
                        new[]
                        {
                            new BranchEventChoice(
                                "LAST_BUNKER_DAY_09_DAILY_RECORD",
                                "앞으로도 매일 기록을 남긴다",
                                "특별한 일이 없더라도 계속 기록을 남기기로 했다.\n" +
                                "같은 하루가 반복되더라도 그날 내가 무엇을 했는지는 남겨두고 싶다.\n" +
                                "읽어줄 사람이 있는지는 모르겠지만, 적어도 내가 여기서 살아왔다는 흔적은 될 것이다.",
                                0,
                                0,
                                string.Empty),
                            new BranchEventChoice(
                                "LAST_BUNKER_DAY_09_ESSENTIAL_RECORD",
                                "필요한 내용만 기록한다",
                                "이제 매일 비슷한 내용을 적을 필요는 없을 것 같다.\n" +
                                "필요한 일이나 특별한 변화가 있을 때만 기록하기로 했다.\n" +
                                "일기를 쓰는 시간이 줄어들어도 하루는 계속 흘러갈 것이다.",
                                0,
                                0,
                                string.Empty)
                        });
                    todayDiary =
                        "오늘도 해야 할 일을 모두 끝냈다.\n" +
                        "벙커 안의 물건도, 밖으로 나가는 길도 이제는 익숙하다.\n" +
                        "내일도 아마 오늘과 크게 다르지 않을 것이다.";
                    return true;
                default:
                    definition = null;
                    return false;
            }
        }

        public static bool TryGetQuarter3JoinEventStory(
            int localDay,
            out string title,
            out string survivorClaim,
            out string redClaim)
        {
            switch (localDay)
            {
                case 1:
                    title = "갈라지기 시작한 배급";
                    survivorClaim =
                        "\"지금 물자가 부족하다고 해서 아픈 사람부터 굶길 수는 없다.\"\n" +
                        "\"적어도 모두가 버틸 수 있을 만큼은 나눠야 한다.\"\n\n" +
                        "일반 생존자들은 부상자와 비전투 인원을 포함해 모든 사람에게 최소한의 식량과 물을 배급해야 한다고 주장한다.\n\n" +
                        "물자를 가져오지 못했더라도 공동체에 속한 사람이라면 기본적인 배급을 받아야 한다는 입장이다.";
                    redClaim =
                        "\"밖에 나가는 사람들이 쓰러지면 다음 물자는 누가 가져오지?\"\n" +
                        "\"거점을 지키는 사람들이 먼저 버텨야 모두가 산다.\"\n\n" +
                        "붉은 완장 경비대는 탐사와 경계를 담당하는 사람들에게 물자를 우선 배급해야 한다고 주장한다.\n\n" +
                        "경비 인원이 약해지면 좀비의 공격을 막을 수 없고, 외부 탐사도 중단될 것이라는 입장이다.";
                    return true;
                case 2:
                    title = "한 사람을 구하는 비용";
                    survivorClaim =
                        "\"지금까지 함께 살아온 사람이다.\"\n" +
                        "\"다쳤다는 이유만으로 밖에 남겨둘 수는 없다.\"\n\n" +
                        "일반 생존자들은 부상자가 당장 일을 하지 못하더라도 공동체의 구성원이므로 계속 치료해야 한다고 주장한다.\n\n" +
                        "회복하면 다시 거점의 일에 참여할 수도 있다는 입장이다.";
                    redClaim =
                        "\"한 사람을 구하러 갔다가 우리 쪽에서도 한 명이 다쳤다.\"\n" +
                        "\"앞으로도 매번 이런 식으로 모두를 구할 수는 없다.\"\n\n" +
                        "붉은 완장 경비대는 한 사람을 살리기 위해 더 많은 사람을 위험에 빠뜨릴 수는 없다고 주장한다.\n\n" +
                        "앞으로는 구조 요청이 발생해도 상황을 먼저 판단해야 한다는 입장이다.";
                    return true;
                case 3:
                    title = "두 개의 탈출 계획";
                    survivorClaim =
                        "\"차량이 없어도 움직일 수 있다.\"\n" +
                        "\"통로를 이용하면 부상자와 나머지 사람들도 모두 데려갈 수 있다.\"\n\n" +
                        "일반 생존자들은 오래된 지하 정비 통로를 이용해 도시 외곽의 공공 대피소로 이동하자고 주장한다.\n\n" +
                        "차량 없이 이동할 수 있고, 부상자와 비전투 인원도 함께 갈 수 있다는 입장이다.";
                    redClaim =
                        "\"차량만 확보하면 저 무리가 도착하기 전에 도시를 벗어날 수 있다.\"\n" +
                        "\"통로 안에서 길이 막히는 것보다 훨씬 확실한 방법이다.\"\n\n" +
                        "붉은 완장 경비대는 물류창고의 차량과 연료를 확보해 도로를 통해 도시를 빠져나가자고 주장한다.";
                    return true;
                case 4:
                    title = "다른 사람의 자리";
                    survivorClaim =
                        "\"그 사람들도 우리와 다르지 않다.\"\n" +
                        "\"차량을 나눌 수 있는지 먼저 이야기해야 한다.\"\n\n" +
                        "일반 생존자들은 물류창고 사람들과 협상을 시도해야 한다고 주장한다.\n\n" +
                        "차량이나 물자를 나누고 함께 탈출할 방법이 있는지 먼저 확인해야 한다는 입장이다.";
                    redClaim =
                        "\"저쪽도 사람을 전부 태울 수 없다.\"\n" +
                        "\"우리가 먼저 확보하지 않으면 결국 저들이 차량을 가지고 떠날 것이다.\"\n\n" +
                        "붉은 완장 경비대는 협상을 시도할 시간이 부족하며, 좀비 무리가 도착하기 전에 차량을 확보해야 한다고 주장한다.";
                    return true;
                case 5:
                    title = "최종 선택";
                    survivorClaim =
                        "일반 생존자들은 남은 식량과 장비를 나누어 들고 지하 정비 통로로 이동할 준비를 한다.\n\n" +
                        "부상자들은 다른 사람들의 부축을 받고 있으며, 일부 생존자들은 가져갈 수 없는 물자를 거점에 남긴다.\n\n" +
                        "이들은 외곽의 공공 대피소가 현재도 안전한지 알지 못한다.\n\n" +
                        "지하 통로의 상태도 완전히 확인되지 않았다.\n\n" +
                        "그러나 누구도 거점에 남겨두지 않겠다고 말한다.";
                    redClaim =
                        "붉은 완장 경비대는 공동 창고에서 가져온 무기와 식량을 챙기고 물류창고로 출발할 준비를 한다.\n\n" +
                        "그들은 차량과 연료를 확보하면 좀비 무리가 도착하기 전에 도시를 빠져나갈 수 있다고 주장한다.\n\n" +
                        "경비대장은 플레이어에게 붉은 천을 건넨다.\n\n" +
                        "\"우리와 갈 거라면 이걸 차.\"\n" +
                        "\"차량에는 네 자리도 준비해뒀다.\"";
                    return true;
                default:
                    title = string.Empty;
                    survivorClaim = string.Empty;
                    redClaim = string.Empty;
                    return false;
            }
        }

        public static bool TryGetQuarter3JoinFinalChoiceRecord(
            string choiceId,
            out string record)
        {
            if (choiceId == JoinSurvivorsChoice)
            {
                record = "「낯선 사람들과」 엔딩 방향 확정";
                return true;
            }

            if (choiceId == JoinRedArmbandChoice)
            {
                record = "「붉은 완장」 엔딩 방향 확정";
                return true;
            }

            record = string.Empty;
            return false;
        }

        private static string CreateJoinSupplyShareRecord()
        {
            return @"배급을 돕는 과정에서 일반 생존자들이 부상자와 비전투 인원에게도 실제로 물자를 나누고 있다는 사실을 확인한다.

일부 사람은 자신의 몫을 줄여 부상자에게 양보하고 있다.

하지만 현재와 같은 방식으로 계속 배급하면 남은 물자가 빠르게 고갈될 가능성이 높다. 거점 내부에서도 물자를 구해온 사람과 그렇지 못한 사람이 같은 양을 받는 것이 공정한지를 두고 불만이 생기고 있다.";
        }

        private static string CreateJoinGuardActivityRecord()
        {
            return @"경비 물자를 정리하며 경비대원들과 대화를 나눈다.

그들이 실제로 거점 주변의 좀비를 막고 탐사 인원을 보호해왔다는 사실을 확인한다. 최근에는 경비 인원이 줄어들고 부상자가 늘면서 순찰조를 제대로 운영하기도 어려운 상황이다.

하지만 일부 경비대원은 앞으로 물자가 더 부족해지면 전투 능력이 없는 사람들의 배급을 줄여야 한다고 말하기 시작한다.";
        }

        private static string CreateJoinInjuryCareRecord()
        {
            return @"치료 작업을 돕는 과정에서 일반 생존자들이 지금까지 부상자를 의도적으로 버리지 않았다는 사실을 확인한다.

과거에 치료받은 사람 중 일부는 회복한 뒤 탐사와 거점 수리에 다시 참여하고 있다.

하지만 이번 부상자는 회복까지 오랜 시간이 걸릴 가능성이 높다. 치료를 계속하면 남아 있는 물자와 의약품이 크게 줄어들 수 있다.

일반 생존자들 내부에서도 모든 부상자를 끝까지 책임질 수 있을지를 두고 의견이 갈리고 있다.";
        }

        private static string CreateJoinRescueCriteriaRecord()
        {
            return @"구조 기록을 정리하면서 붉은 완장 경비대가 실제로 여러 차례 위험에 빠진 생존자들을 구해왔다는 사실을 확인한다.

하지만 구조 작전이 반복될수록 경비 인원과 물자가 소모되고 있으며, 거점을 지킬 수 있는 사람도 줄어들고 있다.

경비대장은 앞으로 모든 구조 요청에 응답하지 않고, 구조 가능성과 대상의 상태를 기준으로 구조 대상을 선별할 계획이라고 말한다.

플레이어는 벙커 생존 경험과 탐사 능력이 있기 때문에 우선 구조 대상에 포함될 수 있다는 말도 듣는다.";
        }

        private static string CreateJoinTunnelSurveyRecord()
        {
            return @"오래된 시설 도면과 관리 기록을 통해 지하 통로가 실제로 도시 외곽 방향까지 연결되어 있다는 사실을 확인한다.

통로 중간에는 일부 붕괴된 구간이 있으며, 붕괴 지역을 피하는 우회로도 기록되어 있다.

하지만 우회로가 현재도 이용 가능한지는 확인되지 않았다.

부상자와 많은 물자를 함께 옮기면 이동 시간이 크게 늘어날 가능성도 있다.";
        }

        private static string CreateJoinVehiclePlanRecord()
        {
            return @"경비대가 수집한 자료를 통해 물류창고에 차량과 비상 연료가 남아 있을 가능성이 높다는 사실을 확인한다.

차량을 확보하면 좀비 무리가 도착하기 전에 도시를 벗어날 가능성이 높다.

하지만 탑승 가능한 인원은 현재 거점 인원의 절반 정도에 불과하다.

경비대는 전투 능력과 기술, 건강 상태와 보유 물자를 기준으로 탑승자 후보를 정리하고 있다.

플레이어는 벙커 생존 경험과 탐사 능력 때문에 탑승 가능 대상에 포함되어 있다.";
        }

        private static string CreateJoinNegotiationPlanRecord()
        {
            return @"협상 준비를 돕는 과정에서 물류창고 안에 부상자와 비전투 인원도 머무르고 있다는 사실을 확인한다.

창고 생존자들도 차량을 이용해 도시를 떠날 준비를 하고 있다.

차량 일부를 나누거나 함께 이동하는 협상을 받아들일 가능성은 남아 있지만, 협상이 길어질 경우 탈출 시기를 놓칠 수 있다.

협상이 실패하면 일반 생존자들은 지하 통로로 이동해야 하지만, 그때는 준비 시간도 더욱 부족해진다.";
        }

        private static string CreateJoinWarehouseSeizurePlanRecord()
        {
            return @"물류창고 주변 조사에 동행하면서 붉은 완장 경비대가 단순한 확인이 아니라 기습 점거를 준비하고 있다는 사실을 알게 된다.

경비대는 창고 출입구를 차단한 뒤 차량 열쇠와 연료를 강제로 확보할 계획이다.

저항하는 사람은 제압하고, 나머지 생존자들은 창고와 차량에서 몰아낼 생각이다.

플레이어가 붉은 완장과 함께 간다면 출입구 차단이나 물자 운반에 참여해야 한다는 설명도 듣는다.";
        }

        private static string CreateMatchedWarningRecord()
        {
            return @"정체를 알 수 없는 방송에서 북쪽 도로로 가지 말라고 했다.

도로 한가운데가 무너졌고 그 주변에 좀비들이 몰려 있다고 했다.
처음에는 사람들을 다른 길로 유인하려는 함정이라고 생각했다.

그래도 확인은 해야 할 것 같아서 옥상으로 올라갔다.
방송에서 말한 그대로였다.
도로는 완전히 끊겨 있었고, 아래에는 좀비들이 몰려 있었다.

저 방송을 보내는 사람들이 누군지는 모른다.
하지만 적어도 이 동네에서 무슨 일이 벌어지고 있는지는 알고 있다.

북쪽으로 가지 마라.
이걸 발견한 사람도 다른 길을 찾아라.";
        }

        private static string CreateRescuePrecheckRecord()
        {
            return @"구조팀이라는 사람들이 응답해 왔다.

처음에는 살아 있는 사람이 몇 명인지 물었다.
걸을 수 없는 부상자가 있는지, 응급환자가 몇 명인지도 확인했다.
여기까지는 구조를 준비하는 과정이라고 생각했다.

그런데 그다음부터는 계속 물자에 관해 물었다.

남아 있는 식량의 양.
물의 양.
항생제와 진통제, 붕대와 소독약의 수량.
이동 가능한 차량이 있는지.
차량에 연료가 얼마나 남았는지.
무기를 가진 사람이 있는지.

필요한 정보를 알려주면 가장 가까운 집결 장소를 안내하겠다고 했다.
정확한 위치는 아직 말해주지 않았다.

구조를 준비하기 위한 질문일 수도 있다.
부상자와 차량을 옮기려면 필요한 정보일지도 모른다.

하지만 사람보다 물자에 관해 더 오래 물었다는 점이 마음에 걸린다.";
        }

        private static string CreateAbortedEvacuationRecord()
        {
            return @"[민간인 대피 작전 경과 보고]

민간인 대피 차량 3대가 외곽 집결지를 향해 출발했다.

이동 도중 주요 도로가 봉쇄되었으며, 선두 차량과의 통신이 끊겼다.
후속 차량은 진입을 중단하고 출발 지점으로 복귀했다.

선두 차량의 현재 위치와 탑승 인원의 상태는 확인되지 않는다.

추가 병력과 구조 차량 투입을 요청했으나 승인되지 않았다.

남아 있는 구조 대상자에게는 현 위치를 유지하고 추가 방송을 기다리도록 전달했다.

해당 지역의 대피 작전은 무기한 중단한다.
잔류 구조 대상자의 생사 여부는 확인되지 않았다.

[대외 발표 및 문의 대응 지침]

작전 중단 사실과 미확인 인원에 관한 내용은 대외 비공개로 처리한다.

관련 내용이 공개될 경우 대피를 기다리는 생존자들이 지정 구역을 이탈하거나 통제되지 않은 이동을 시도할 가능성이 있다.

외부 문의에는 다음과 같이 안내할 것.

“현장 상황에 따른 이동 경로 재조정으로 구조 일정이 연기되었다.”

작전 실패, 통신 두절 차량 및 미확인 탑승 인원에 관한 내용은 지휘부 승인 없이 공개하지 말 것.

[문서 가장자리의 연필 메모]

연기가 아니라 중단이다.
기다리고 있는 사람들은 이 사실을 모른다.";
        }

        private static string CreateAmbushReportRecord()
        {
            return @"[미확인 구조방송 관련 습격 사건 보고]

민간 구조대를 자칭한 무전방송을 듣고 지정된 장소로 이동하던 생존자들이 무장 집단의 습격을 받았다.

해당 방송에서는 인근 생존자들에게 가까운 집결 장소와 안전한 이동 경로를 안내했다.
이동 시 식량, 물, 의약품 등 생존에 필요한 물자를 준비하도록 요구한 것으로 확인된다.

사건 이후 구조된 생존자 1명은 집결 장소로 이동하던 도중 복면을 착용한 무장 인원들이 도로를 차단했다고 진술했다.

무장 인원들은 생존자들이 보유한 식량, 물, 의약품, 무기 및 차량을 빼앗았다.

저항한 일부 생존자는 공격받았으며, 나머지는 물자를 버린 뒤 주변 건물과 골목으로 흩어진 것으로 확인된다.

현재까지 확인된 사망자는 2명이며, 실종 인원의 수는 파악되지 않았다.

방송 발신자의 신원은 확인되지 않았다.
방송 발신자와 현장의 무장 집단 사이에 연관성 또는 사전 공모가 있었는지도 확인하지 못했다.

[추가 조사]

사건 이후에도 민간 구조대를 자칭하는 유사한 방송이 다른 주파수에서 여러 차례 확인됐다.

각 방송은 서로 다른 집결 장소를 안내했으며, 생존자들에게 물자와 이동 수단을 준비하도록 요구했다.

모든 방송이 동일한 집단에 의해 송출된 것인지는 확인되지 않았다.

신원이 확인되지 않은 구조방송에 현재 위치, 생존자 수, 보유 물자 및 차량 정보를 전달하지 말 것.";
        }

        private static string CreateLastWaitRecord()
        {
            return @"군 방송이 다시 들렸다.

이곳에서 움직이지 말라고 했다.
구조 차량이 올 때까지 문을 잠그고 실내에서 기다리면 된다고 했다.

밖으로 나가려던 사람들도 방송을 듣고 다시 집으로 돌아갔다.
군이 온다면 위험하게 길을 돌아다닐 이유가 없다고 생각했다.

[며칠 뒤]

방송에서는 아직 작전이 진행 중이라고 한다.
구조 차량이 늦어지고 있지만 반드시 올 거라고 했다.

창밖을 확인했지만 도로에는 아무것도 보이지 않았다.

물이 많이 남지 않았다.
그래도 지금 밖으로 나가는 것보다는 기다리는 편이 나을 것이다.

[마지막으로 작성된 페이지]

오늘이 방송에서 말한 구조 예정일이다.

아침부터 기다렸지만 아무도 오지 않았다.
해가 졌는데도 차량 소리는 들리지 않는다.

물은 어제 모두 떨어졌다.
남아 있는 음식도 이제 한 번 먹을 양뿐이다.

라디오에서는 여전히 움직이지 말라고 한다.
구조 작전이 끝나지 않았다고 한다.

그래도 밖으로 나가는 것보다는 기다리는 편이 나을 것이다.
군이 정말 오고 있다면, 내가 자리를 비운 사이 지나칠 수도 있으니까.

그다음 페이지에는 날짜만 적혀 있고 내용은 비어 있다.";
        }

        private static string CreateLiveTransmissionRecord()
        {
            return @"[군용 주파수 대역 송출 감시 기록]

06:10 — 생존자 대기 안내 방송 수신
08:40 — 외곽 구조 작전 준비 안내 추가
11:20 — 북부 도로 혼잡 및 우회 권고 추가
14:05 — 구조 대상자 이동 시간 변경
16:30 — 동쪽 터널 폐쇄 경고 추가

[관제 담당자 메모]

같은 내용이 반복되는 자동방송이 아니다.

송출될 때마다 일부 문장과 이동 정보가 달라지고 있다.
시간과 구조 일정도 계속 수정된다.

마지막 방송에는 오늘 새벽 폐쇄된 동쪽 터널에 관한 경고가 추가되어 있었다.
터널 폐쇄 사실은 오전 감시 기록에서도 확인됐다.

오래전에 녹음된 방송이라면 알 수 없는 내용이다.

적어도 누군가는 지금도 주변 상황을 확인하면서 이 주파수의 방송 내용을 수정하고 있다.

다만 발신 위치를 정확히 추적하지는 못했다.
군 장비를 사용하고 있다는 사실만으로 방송을 보내는 사람이 실제 군인이라고 단정할 수는 없다.";
        }

        public static string SignalClueId(int day) => $"Q3_SIGNAL_CLUE_{day:00}";
        public static string SignalSourceId(int day) => $"Q3_SIGNAL_SOURCE_{day:00}";
        public static string JoinClueId(int day, Quarter3JoinSupportTarget target) =>
            $"Q3_JOIN_SUPPORT_{day:00}_{target}";
        public static string JoinSourceId(int day, Quarter3JoinSupportTarget target) =>
            $"Q3_JOIN_SOURCE_{day:00}_{target}";

        private static void RegisterQuarter2(
            Quarter2EventCatalog catalog,
            string id,
            BranchRoute route,
            int order,
            string title,
            string body,
            string location,
            string progressChoiceText,
            string progressResultText,
            string rejectChoiceText,
            string rejectResultText,
            float progressHealthDelta = 0f,
            float progressHungerDelta = 0f,
            float progressThirstDelta = 0f,
            float progressMoraleDelta = 0f,
            bool requiresExploration = false)
        {
            RegisterOrThrow(
                catalog.TryRegister(
                    new Quarter2EventDefinition(
                        id,
                        route,
                        order,
                        title,
                        body,
                        location,
                        progressChoiceText,
                        progressResultText,
                        rejectChoiceText,
                        rejectResultText,
                        progressHealthDelta,
                        progressHungerDelta,
                        progressThirstDelta,
                        progressMoraleDelta,
                        requiresExploration),
                    out string reason),
                reason);
        }

        private static void RegisterJoinClue(
            Quarter3ClueCatalog catalog,
            int day,
            Quarter3JoinSupportTarget target,
            string title = "",
            string content = "")
        {
            RegisterClue(catalog, JoinClueId(day, target), BranchRoute.Join, JoinSourceId(day, target), title, content);
        }

        private static void RegisterClue(
            Quarter3ClueCatalog catalog,
            string id,
            BranchRoute route,
            string source,
            string title = "",
            string content = "")
        {
            RegisterOrThrow(
                catalog.TryRegister(
                    new Quarter3ClueDefinition(
                        id,
                        route,
                        source,
                        title,
                        content),
                    out string reason),
                reason);
        }

        public static bool TryGetSignalExplorationSource(
            string locationName,
            out string sourceId)
        {
            sourceId = locationName switch
            {
                "편의점" => SignalConvenienceStoreSource,
                "병원" => SignalHospitalSource,
                "경찰서" => SignalPoliceStationSource,
                "주택가" => SignalResidentialAreaSource,
                "통신소" => SignalCommunicationsSource,
                "통신소·방송국" => SignalCommunicationsSource,
                _ => string.Empty
            };
            return !string.IsNullOrEmpty(sourceId);
        }

        private static void RegisterSupport(
            Quarter3JoinSupportCatalog catalog,
            int day,
            Quarter3JoinSupportTarget target,
            string displayName)
        {
            Quarter3JoinSupportDefinition definition = new(
                day,
                target,
                JoinSourceId(day, target),
                JoinClueId(day, target),
                displayName);
            RegisterOrThrow(catalog.TryRegister(definition, out string reason), reason);
        }

        private static void RegisterFinalChoice(
            Quarter3FinalChoiceCatalog catalog,
            string id,
            BranchRoute route)
        {
            RegisterOrThrow(
                catalog.TryRegister(new Quarter3FinalChoiceDefinition(id, route), out string reason),
                reason);
        }

        private static void RegisterOrThrow(bool success, string reason)
        {
            if (!success)
            {
                throw new InvalidOperationException(reason);
            }
        }
    }
}
