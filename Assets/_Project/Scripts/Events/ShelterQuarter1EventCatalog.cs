using System;
using System.Collections.Generic;
using YesterdayMap.BranchOne.Events;

namespace YesterdayMap.Events
{
    /// <summary>
    /// Production quarter-one event data and the randomized 2-6 day schedule.
    /// </summary>
    public static class ShelterQuarter1EventCatalog
    {
        public const int FirstEventDay = 2;
        public const int LastEventDay = 6;
        private const int ScheduledBaseEventCount = 1;
        private const int ScheduledSignalEventCount = 2;
        private const int ScheduledRescueEventCount = 2;

        public static BranchEventCatalog CreateScheduledCatalog()
        {
            return CreateScheduledCatalog(UnityEngine.Random.Range(0, int.MaxValue));
        }

        public static BranchEventCatalog CreateScheduledCatalog(int randomSeed)
        {
            List<BranchEventDefinition> all = CreateAllDefinitions();
            List<BranchEventDefinition> lastBunker = all.FindAll(IsLastBunkerEvent);
            List<BranchEventDefinition> signal = all.FindAll(IsSignalEvent);
            List<BranchEventDefinition> join = all.FindAll(IsJoinEvent);
            Random random = new(randomSeed);

            List<BranchEventDefinition> schedule = new();
            AddRandomDistinct(schedule, lastBunker, ScheduledBaseEventCount, random);
            AddRandomDistinct(schedule, signal, ScheduledSignalEventCount, random);
            AddRandomDistinct(schedule, join, ScheduledRescueEventCount, random);
            Shuffle(schedule, random);

            int expectedEventCount =
                ScheduledBaseEventCount +
                ScheduledSignalEventCount +
                ScheduledRescueEventCount;
            if (schedule.Count != expectedEventCount)
            {
                throw new InvalidOperationException("Quarter-one schedule must contain 5 events.");
            }

            BranchEventCatalog catalog = new();
            for (int index = 0; index < schedule.Count; index++)
            {
                BranchEventDefinition source = schedule[index];
                int day = FirstEventDay + index;
                BranchEventDefinition scheduled = new(
                    source.EventId,
                    source.EventNumber,
                    source.Title,
                    source.Body,
                    day,
                    day,
                    false,
                    source.Choices);
                RegisterOrThrow(catalog, scheduled);
            }

            return catalog;
        }

        public static IReadOnlyList<BranchEventDefinition> GetAllDefinitions()
        {
            return CreateAllDefinitions().AsReadOnly();
        }

        public static IReadOnlyList<BranchEventDefinition> GetLastBunkerDefinitions()
        {
            return CreateAllDefinitions().FindAll(IsLastBunkerEvent).AsReadOnly();
        }

        public static bool IsSignalEvent(BranchEventDefinition definition)
        {
            return definition != null && definition.EventNumber is >= 13 and <= 19;
        }

        public static bool IsJoinEvent(BranchEventDefinition definition)
        {
            return definition != null && definition.EventNumber is >= 20 and <= 26;
        }

        public static bool IsLastBunkerEvent(BranchEventDefinition definition)
        {
            return definition != null && definition.EventNumber is >= 1 and <= 12;
        }

        private static List<BranchEventDefinition> CreateAllDefinitions()
        {
            List<BranchEventDefinition> events = new();

            Add(events, 1, "멈추지 않는 물방울 소리",
                "천장에서 일정한 간격으로 물방울이 떨어진다. 양은 적지만 밤새 소리가 이어질 것 같다.",
                "새는 곳을 찾아 막는다",
                "천장에서 물이 새는 곳을 찾아 천과 판자로 틈을 막았다.\n손은 조금 갔지만, 밤새 귀를 괴롭히던 물방울 소리가 멈추니 벙커가 한결 편안해졌다.",
                "소리를 무시하고 쉰다",
                "오늘은 물이 새는 곳을 고칠 힘이 없어 그대로 누웠다.\n몸은 쉬었지만, 일정하게 떨어지는 물방울 소리가 밤새 머릿속을 떠나지 않았다.");
            Add(events, 2, "오래된 음성 녹음",
                "보관함을 정리하다 과거에 녹음한 가족의 목소리를 발견한다.",
                "끝까지 듣는다",
                "보관함에서 찾은 오래된 녹음을 끝까지 들었다.\n잊고 있던 가족의 목소리가 벙커 안에 퍼지자, 잠시나마 다시 함께 있는 듯한 기분이 들었다.",
                "바로 꺼버린다",
                "녹음이 시작되자마자 전원을 껐다.\n지금 그 목소리를 끝까지 들었다가는 마음이 더 흔들릴 것 같았다.");
            Add(events, 3, "오늘이 며칠인지",
                "일기장을 펼쳤지만 오늘 날짜가 바로 떠오르지 않는다.",
                "지금까지의 기록을 다시 읽는다",
                "오늘 날짜가 떠오르지 않아 지금까지 쓴 일기장을 처음부터 다시 읽었다.\n흐릿했던 날짜와 지난날의 기억이 조금씩 이어지면서, 내가 아직 시간을 놓치지 않았다는 생각이 들었다.",
                "대충 날짜를 적는다",
                "정확한 날짜를 확인하지 못한 채 짐작한 날짜를 적었다.\n하루쯤 틀려도 괜찮다고 생각했지만, 내가 시간을 잊어가고 있다는 사실은 마음에 남았다.");
            Add(events, 4, "아무도 없는 식탁",
                "혼자 식사를 준비하다 맞은편의 빈 의자가 유난히 눈에 들어온다.",
                "평소처럼 식사를 차려놓는다",
                "오늘도 예전처럼 식탁을 차리고 맞은편 자리에도 음식을 놓았다.\n혼자 먹는 식사였지만, 잠깐 동안은 누군가와 함께 앉아 있는 것처럼 덜 외로웠다.",
                "의자를 치워버린다",
                "계속 눈에 밟히던 빈 의자를 벽 쪽으로 치워버렸다.\n식탁은 넓어졌지만, 이제 정말 내 자리 하나만 남았다는 사실이 더 선명해졌다.");
            Add(events, 5, "흔들리는 선반",
                "물자를 보관한 선반이 한쪽으로 기울어져 있다.",
                "지금 바로 다시 고정한다",
                "한쪽으로 기울어진 선반을 벽에 다시 단단히 고정했다.\n팔에 힘이 꽤 들었지만, 위태롭게 흔들리던 물자들을 보니 미리 손보길 잘했다는 생각이 들었다.",
                "나중으로 미룬다",
                "선반을 고치는 일은 나중으로 미뤘다.\n결국 큰 소리와 함께 선반이 무너졌고, 떨어진 물자 하나는 다시 쓸 수 없게 되었다.");
            Add(events, 6, "환기구의 먼지",
                "환기구에 먼지와 검은 이물질이 쌓여 공기가 답답해졌다.",
                "직접 청소한다",
                "환기구에 쌓인 먼지와 검은 이물질을 직접 걷어냈다.\n숨이 찰 만큼 힘들었지만, 청소를 마치고 나니 벙커 안의 답답한 공기가 조금은 가벼워진 것 같다.",
                "환기구를 임시로 막는다",
                "이상한 냄새가 들어오는 것이 싫어 환기구를 천과 판자로 막았다.\n냄새는 줄었지만 공기가 더 무거워졌고, 벙커 안이 전보다 훨씬 답답하게 느껴졌다.");
            Add(events, 7, "출입문 틈새",
                "벙커 출입문 아래로 희미한 빛과 찬바람이 들어온다.",
                "만능 수리키트로 완전히 보수한다",
                "만능 수리키트를 사용해 출입문 아래의 틈을 완전히 보수했다.\n찬바람과 희미한 빛이 사라지자, 적어도 이 문만큼은 나를 지켜줄 것이라는 안도감이 들었다.",
                "가구로 임시로 막는다",
                "무거운 가구를 끌어 출입문 앞의 틈을 임시로 막았다.\n몸은 지쳤고 완벽하게 막힌 것도 아니지만, 당분간은 이 상태로 버텨야 할 것 같다.");
            Add(events, 8, "비상등 고장",
                "벙커의 비상등이 계속 깜박이며 불안감을 키운다.",
                "분해해서 고친다",
                "[수리에 성공한 경우]\n깜박이던 비상등을 직접 분해해 고쳤다.\n안정된 불빛이 다시 벙커 안을 채우자, 사소한 고장 하나를 해결했을 뿐인데도 마음이 한결 놓였다.\n[수리에 실패한 경우]\n비상등을 뜯어보았지만 끝내 고장 원인을 찾지 못했다.\n시간과 힘만 쓰고 불빛은 더 불안하게 흔들려, 괜히 건드린 것은 아닌지 후회가 남았다.",
                "완전히 꺼버린다",
                "계속 깜박이는 불빛을 견딜 수 없어 비상등의 전원을 완전히 내렸다.\n거슬리던 빛은 사라졌지만, 어두워진 벙커 안에서 작은 소리까지 더 크게 들리는 것 같다.");
            Add(events, 9, "물탱크의 이상한 냄새",
                "저장된 물 근처에서 약한 금속 냄새가 난다. 실제로 오염됐는지는 알 수 없다.",
                "물을 일부 버리고 탱크를 확인한다",
                "금속 냄새가 나는 물을 일부 버리고 물탱크 안을 확인해 깨끗이 닦았다.\n물을 잃은 것은 아깝지만, 적어도 당분간은 안심하고 마실 수 있을 것 같다.",
                "냄새가 약하니 그대로 둔다",
                "냄새가 약하다는 이유로 물탱크를 그대로 두었다.\n물을 마신 뒤 속이 불편해지기 시작했고, 작은 이상이라도 무시해서는 안 된다는 생각이 들었다.");
            Add(events, 10, "부풀어 오른 통조림",
                "보관 중인 통조림 하나가 이상하게 부풀어 있다.",
                "바로 버린다",
                "이상하게 부풀어 오른 통조림은 열어보지 않고 바로 버렸다.\n식량 하나가 아깝기는 하지만, 상한 음식을 먹고 쓰러지는 것보다는 나은 선택이었을 것이다.",
                "냄새를 확인한 뒤 먹는다",
                "[이상이 없는 경우]\n통조림의 냄새를 확인한 뒤 조심스럽게 먹었다.\n맛도 냄새도 크게 이상하지 않았고, 배를 채울 수 있었으니 이번에는 운이 좋았던 것 같다.\n[체력이 감소한 경우]\n통조림을 먹은 뒤 얼마 지나지 않아 심한 복통이 시작되었다.\n아깝다는 생각에 위험한 음식을 억지로 먹은 대가를 제대로 치른 셈이다.");
            Add(events, 11, "금이 간 물병",
                "보관 중인 물병 하나에 작은 금이 생겼다.",
                "지금 마신다",
                "금이 더 벌어지기 전에 물병을 바로 비워 마셨다.\n보관해두지는 못했지만, 물을 바닥에 흘리지 않고 갈증을 해결한 것으로 만족해야겠다.",
                "다른 용기에 옮긴다",
                "[옮기는 데 성공한 경우]\n금이 간 물병의 물을 다른 용기로 천천히 옮겼다.\n한 방울도 흘리지 않고 보관하는 데 성공해, 귀한 물을 지켜냈다는 안도감이 들었다.\n[옮기는 데 실패한 경우]\n물을 옮기던 도중 병의 금이 갑자기 크게 벌어졌다.\n손쓸 틈도 없이 물이 바닥으로 쏟아졌고, 눈앞에서 사라지는 물을 바라볼 수밖에 없었다.");
            Add(events, 12, "녹슨 못",
                "벙커 안을 정리하다 녹슨 못에 손을 긁혔다.",
                "구급상자를 사용한다",
                "녹슨 못에 긁힌 상처를 깨끗이 닦고 구급상자로 제대로 치료했다.\n출혈과 통증이 가라앉는 것을 보니, 작은 상처라도 방치하지 않길 잘했다.",
                "천으로 대충 감싼다",
                "구급상자를 아끼기 위해 상처를 천으로 대충 감쌌다.\n피는 멎었지만 욱신거림이 계속되는 것을 보니, 제대로 된 치료라고 하기는 어려울 것 같다.");
            Add(events, 13, "멀리서 보이는 손전등",
                "밤에 지상 출입구 틈으로 일정한 간격의 불빛이 보인다.",
                "같은 간격으로 불빛을 보낸다",
                "멀리서 보인 불빛과 같은 간격으로 손전등을 깜박여 답을 보냈다.\n잠시 뒤 상대의 불빛이 한 번 더 돌아왔고, 곧 사라졌지만 누군가가 그곳에 있었다는 생각은 남았다.",
                "출입구를 가리고 숨는다",
                "출입구에서 새어 나가는 빛을 가리고 벙커 안으로 몸을 숨겼다.\n바깥의 불빛은 얼마 지나지 않아 사라졌고, 그 정체를 확인하지 않은 것이 옳았는지는 알 수 없다.", 1, 0);
            Add(events, 14, "버려진 가방",
                "출입구 가까운 곳에 작은 가방 하나가 놓여 있다. 주변에는 아무도 없다.",
                "가방을 가져온다",
                "[물자를 획득한 경우]\n주변을 충분히 살핀 뒤 출입구 앞에 놓인 가방을 벙커 안으로 가져왔다.\n가방 안에는 아직 사용할 수 있는 물자가 남아 있었고, 누가 두고 갔는지는 끝내 알 수 없었다.\n[가방이 비어 있던 경우]\n위험을 감수하고 가방을 가져왔지만 안에는 쓸 만한 것이 하나도 없었다.\n괜한 기대를 했다는 허탈함과, 누군가 일부러 빈 가방을 둔 것은 아닐지 모른다는 불안만 남았다.",
                "손대지 않는다",
                "정체를 알 수 없는 가방에는 손대지 않고 그대로 출입구를 닫았다.\n물자가 들어 있었을지도 모르지만, 지금은 모르는 물건에 목숨을 걸 수 없다.", 1, 0);
            Add(events, 15, "부서진 소형 수신기",
                "출입구 주변에서 망가진 소형 수신기를 발견한다. 전원을 켜자 희미한 잡음과 끊어진 음성이 반복해서 들려온다.",
                "부품과 신호를 조사한다",
                "망가진 소형 수신기를 분해해 남아 있는 부품과 잡히는 주파수를 하나씩 확인했다.\n끊어진 음성밖에는 들리지 않았지만, 누군가 신호를 보냈던 흔적만큼은 분명해 보였다.",
                "쓸모없는 물건으로 판단하고 버린다",
                "고장 난 수신기는 더 조사하지 않고 출입구 밖으로 밀어냈다.\n잡음에 시간을 쓰기보다 지금 가진 물자와 살아남는 일에 집중하는 편이 낫다고 판단했다.", 1, 0);
            Add(events, 16, "지붕 위의 반사광",
                "출입구 밖을 확인하던 중 멀리 있는 건물 옥상에서 햇빛을 반사하는 듯한 빛이 반복해서 보인다.",
                "반사되는 방향과 간격을 확인한다",
                "몸을 낮춘 채 멀리 보이는 반사광의 방향과 반복되는 간격을 확인했다.\n빛은 일정한 순서를 몇 차례 되풀이한 뒤 사라졌고, 단순한 햇빛이 아니었을지도 모른다는 생각이 들었다.",
                "위험할 수 있으니 안으로 들어간다",
                "반사광이 함정일 가능성을 생각해 더 확인하지 않고 벙커 안으로 돌아왔다.\n빛은 잠시 후 시야에서 사라졌지만, 무엇이 있었는지는 끝내 알 수 없게 되었다.", 1, 0);
            Add(events, 17, "하늘의 붉은 섬광",
                "밤하늘 멀리에서 붉은 섬광이 한 번 솟아오른다. 불길인지 신호탄인지 구분하기 어렵다.",
                "섬광이 나타난 방향을 기록한다",
                "밤하늘에 솟아오른 붉은 섬광의 방향과 시간을 일기장에 정확히 남겼다.\n화재였을 수도 있지만, 누군가 구조를 바라며 쏘아 올린 신호탄일 가능성도 쉽게 지울 수 없었다.",
                "화재일 뿐이라고 생각하고 무시한다",
                "붉은 섬광은 멀리서 난 화재일 뿐이라고 생각하고 출입구를 닫았다.\n빛은 곧 사라졌지만, 혹시 누군가의 신호를 외면한 것은 아닌지 마음 한쪽이 불편했다.", 1, 0);
            Add(events, 18, "상공을 스치는 드론",
                "출입구 밖에서 작은 기계음이 들린다. 하늘을 확인하자 정체를 알 수 없는 드론 한 대가 주변을 천천히 지나가고 있다.",
                "반사되는 물건을 흔들어 위치를 알린다",
                "빛이 잘 반사되는 물건을 들고 상공의 드론을 향해 여러 번 흔들었다.\n드론은 잠시 주변을 맴돌다가 아무 반응 없이 멀어졌지만, 내 위치를 보았을 가능성은 있을 것 같다.",
                "들키지 않도록 벙커 안으로 숨는다",
                "드론이 누구의 것인지 알 수 없어 출입구에서 물러나 벙커 안으로 숨었다.\n기계음이 완전히 사라질 때까지 움직이지 않았고, 들키지 않았다는 안도감과 기회를 놓쳤다는 생각이 함께 남았다.", 1, 0);
            Add(events, 19, "멀리서 울리는 경보 사이렌",
                "멀리 떨어진 곳에서 경보 사이렌이 세 차례 울린다. 자동으로 작동한 것인지 누군가 일부러 울린 것인지는 알 수 없다.",
                "사이렌이 들린 방향과 시간을 확인한다",
                "출입구 가까이 올라가 멀리서 울린 사이렌의 방향과 시간을 확인했다.\n세 번째 경보가 끝난 뒤에는 아무 소리도 이어지지 않았지만, 누군가 의도적으로 울린 신호일 가능성은 기록해두기로 했다.",
                "고장 난 시설에서 난 소리라고 생각한다",
                "오래된 경보 시설이 고장 나 저절로 울린 것이라고 생각하기로 했다.\n확인하러 나가지는 않았지만, 사이렌이 들린 방향이 계속 신경 쓰여 쉽게 마음을 놓을 수 없었다.", 1, 0);
            Add(events, 20, "벽 너머의 발소리",
                "밤이 되자 벙커 벽 바깥에서 느린 발소리가 들린다. 사람인지 좀비인지 알 수 없다.",
                "벽에 귀를 대고 확인한다",
                "벽에 귀를 대고 바깥의 느린 발소리에 집중했다.\n사람인지 좀비인지는 끝내 알 수 없었지만, 발소리가 멀어질 때까지 누군가 벙커 곁을 지나고 있다는 느낌을 지울 수 없었다.",
                "불을 끄고 조용히 기다린다",
                "모든 불을 끄고 숨을 죽인 채 발소리가 지나가기를 기다렸다.\n한참 뒤 바깥은 다시 조용해졌지만, 확인하지 못한 정체 때문에 긴장이 쉽게 풀리지 않았다.", 0, 1);
            Add(events, 21, "문 앞의 분필 표시",
                "출입문 근처 벽에 전에는 없던 분필 표시가 남아 있다.",
                "비슷한 표시를 남겨 응답한다",
                "벽에 남겨진 분필 표시 옆에 비슷한 기호를 그려 응답했다.\n누가 남긴 표시인지는 모르지만, 이 근처에 살아 있는 사람이 있다면 내 흔적을 알아볼지도 모른다.",
                "표시를 지운다",
                "출입문 근처의 분필 자국을 흔적이 남지 않도록 깨끗이 지웠다.\n누군가의 메시지였을 가능성도 있지만, 벙커의 위치를 알리는 위험은 감수하고 싶지 않았다.", 0, 1);
            Add(events, 22, "짧은 도움 요청",
                "밖에서 누군가 한 번만 도움을 외친 뒤 조용해진다.",
                "문을 열지 않고 물자를 밖에 둔다",
                "문은 열지 않은 채 통조림이나 물을 출입문 밖에 조심스럽게 내려놓았다.\n잠시 후 물자가 사라진 것을 확인했고, 적어도 오늘 누군가에게 작은 도움이 되었기를 바랐다.",
                "아무 반응도 하지 않는다",
                "밖에서 들린 도움 요청에 아무 반응도 하지 않고 숨을 죽였다.\n목소리는 다시 들리지 않았지만, 그 사람이 어떻게 되었을지 생각하면 마음이 무거워진다.", 0, 1);
            Add(events, 23, "어둠 속의 휘파람",
                "출입구 바깥에서 짧은 휘파람 소리가 일정한 간격으로 반복된다. 사람의 신호처럼 들리지만 확신할 수 없다.",
                "같은 방식으로 휘파람을 돌려준다",
                "바깥에서 들린 휘파람과 같은 간격으로 나도 짧게 휘파람을 불어 답했다.\n잠시 뒤 멀리서 한 번의 응답이 돌아왔고, 주변은 다시 조용해졌지만 누군가가 있다는 사실만큼은 분명해 보였다.",
                "소리를 내지 않고 기다린다",
                "어떤 소리도 내지 않은 채 휘파람이 멈추기를 기다렸다.\n몇 차례 더 이어지던 소리는 점점 멀어졌고, 상대가 누구였는지는 끝내 확인하지 못했다.", 0, 1);
            Add(events, 24, "출입문 앞의 빈 물병",
                "출입문 앞에 빈 물병 하나가 놓여 있다. 병에는 작은 글씨로 “조금만 부탁합니다”라고 적혀 있다.",
                "물을 채워 다시 밖에 둔다",
                "출입문 앞의 빈 병에 귀한 물을 채워 다시 원래 자리에 두었다.\n잠시 후 병이 사라진 것을 보니 누군가 가져간 듯했고, 그 물이 필요한 사람에게 닿았기를 바랐다.",
                "빈 병을 치워버린다",
                "빈 물병을 출입구에서 멀리 치워버렸다.\n다시 도움을 청하러 오는 사람이 없기를 바랐지만, 병에 적힌 짧은 부탁이 계속 마음에 남았다.", 0, 1);
            Add(events, 25, "여러 사람의 발자국",
                "출입구 주변에 크기가 다른 여러 개의 발자국이 남아 있다. 한 사람이 아니라 여러 명이 함께 이동한 흔적처럼 보인다.",
                "이동 방향과 흔적을 확인한다",
                "출입구 주변의 발자국을 따라가며 이동 방향과 대략적인 인원수를 확인했다.\n크기가 다른 흔적이 여러 개 이어져 있었고, 근처를 지나간 것이 한 사람이 아니라는 사실은 분명했다.",
                "발자국을 지워버린다",
                "벙커로 이어질 수 있는 발자국을 주변 흙과 먼지로 모두 지웠다.\n몸은 힘들었지만, 그들이 누구였든 이곳으로 되돌아오는 길을 쉽게 찾지는 못할 것이다.", 0, 1);
            Add(events, 26, "벽에 적힌 이름들",
                "근처 벽면에 여러 사람의 이름과 날짜가 적혀 있다. 가장 최근 날짜는 며칠 전이다.",
                "자신의 이름과 오늘 날짜를 남긴다",
                "벽에 적힌 여러 이름 아래에 내 이름과 오늘 날짜도 함께 남겼다.\n누군가 이곳을 다시 지나간다면, 적어도 오늘까지 내가 살아 있었다는 사실은 알 수 있을 것이다.",
                "이름들을 모두 지운다",
                "벽에 적힌 이름과 날짜를 알아볼 수 없도록 모두 지웠다.\n낯선 사람들의 흔적은 사라졌지만, 그중 몇 명이나 아직 살아 있을지는 계속 생각하게 된다.", 0, 1);

            return events;
        }

        private static void Add(
            ICollection<BranchEventDefinition> events,
            int number,
            string title,
            string body,
            string firstLabel,
            string firstResult,
            string secondLabel,
            string secondResult,
            int signalDelta = 0,
            int joinDelta = 0)
        {
            string eventId = $"Q1_GENERAL_EVT_{number:00}";
            BranchEventChoice first = new(
                $"{eventId}_CHOICE_1", firstLabel, firstResult,
                signalDelta, joinDelta, firstResult);
            BranchEventChoice second = new(
                $"{eventId}_CHOICE_2", secondLabel, secondResult,
                0, 0, secondResult);
            events.Add(new BranchEventDefinition(
                eventId, number, title, body, 1, int.MaxValue, true,
                new[] { first, second }));
        }

        private static void AddRandomDistinct(
            ICollection<BranchEventDefinition> destination,
            List<BranchEventDefinition> source,
            int count,
            Random random)
        {
            Shuffle(source, random);
            for (int index = 0; index < count; index++)
            {
                destination.Add(source[index]);
            }
        }

        private static void Shuffle<T>(IList<T> items, Random random)
        {
            for (int index = items.Count - 1; index > 0; index--)
            {
                int swapIndex = random.Next(index + 1);
                (items[index], items[swapIndex]) = (items[swapIndex], items[index]);
            }
        }

        private static void RegisterOrThrow(
            BranchEventCatalog catalog,
            BranchEventDefinition definition)
        {
            if (!catalog.TryRegister(definition, out string reason))
            {
                throw new InvalidOperationException(reason);
            }
        }
    }
}
