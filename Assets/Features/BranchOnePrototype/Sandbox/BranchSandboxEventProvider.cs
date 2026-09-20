using System;
using YesterdayMap.BranchOne.Events;

namespace YesterdayMap.BranchOne.Sandbox
{
    public sealed class BranchSandboxEventProvider
    {
        public BranchEventCatalog CreateCatalog()
        {
            BranchEventCatalog catalog = new();

            for (int eventNumber = 1; eventNumber <= 10; eventNumber++)
            {
                bool isSignalEvent = eventNumber <= 5;
                BranchEventChoice actChoice = new(
                    CreateActChoiceId(eventNumber),
                    "행동을 한다",
                    "행동을 진행했습니다.",
                    isSignalEvent ? 1 : 0,
                    isSignalEvent ? 0 : 1,
                    $"이벤트 {eventNumber}에 해당하는 행동을 진행했다.");
                BranchEventChoice declineChoice = new(
                    CreateDeclineChoiceId(eventNumber),
                    "행동하지 않는다",
                    "행동하지 않기로 했다.",
                    0,
                    0,
                    $"이벤트 {eventNumber}에 해당하는 행동을 하지 않았다.");

                BranchEventDefinition definition = new(
                    $"sandbox-event-{eventNumber}",
                    eventNumber,
                    $"테스트 이벤트 {eventNumber}",
                    isSignalEvent
                        ? "구조 신호 계열 점수가 증가하는 임시 이벤트입니다."
                        : "합류 계열 점수가 증가하는 임시 이벤트입니다.",
                    BranchPrototypeSettings.DefaultStartDay,
                    BranchPrototypeSettings.DefaultLastEventDay,
                    true,
                    new[] { actChoice, declineChoice });

                if (!catalog.TryRegister(definition, out string failureReason))
                {
                    throw new InvalidOperationException(
                        $"Sandbox event registration failed: {failureReason}");
                }
            }

            return catalog;
        }

        public static string CreateActChoiceId(int eventNumber)
        {
            return $"EVENT_{eventNumber}_ACT";
        }

        public static string CreateDeclineChoiceId(int eventNumber)
        {
            return $"EVENT_{eventNumber}_DECLINE";
        }
    }
}
