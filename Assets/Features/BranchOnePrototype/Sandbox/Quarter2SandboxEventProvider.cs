using System;
using YesterdayMap.BranchOne.Quarter2;

namespace YesterdayMap.BranchOne.Sandbox
{
    public sealed class Quarter2SandboxEventProvider
    {
        public Quarter2EventCatalog CreateCatalog()
        {
            Quarter2EventCatalog catalog = new();

            Register(
                catalog,
                "Q2_SIGNAL_01",
                BranchRoute.Signal,
                1,
                "구조신호 2분기 이벤트 1");
            Register(
                catalog,
                "Q2_SIGNAL_02",
                BranchRoute.Signal,
                2,
                "구조신호 2분기 이벤트 2");
            Register(
                catalog,
                "Q2_SIGNAL_03",
                BranchRoute.Signal,
                3,
                "구조신호 2분기 이벤트 3");
            Register(
                catalog,
                "Q2_JOIN_01",
                BranchRoute.Join,
                1,
                "합류 2분기 이벤트 1");
            Register(
                catalog,
                "Q2_JOIN_02",
                BranchRoute.Join,
                2,
                "합류 2분기 이벤트 2");
            Register(
                catalog,
                "Q2_JOIN_03",
                BranchRoute.Join,
                3,
                "합류 2분기 이벤트 3");

            return catalog;
        }

        private static void Register(
            Quarter2EventCatalog catalog,
            string eventId,
            BranchRoute route,
            int order,
            string title)
        {
            string routeName =
                route == BranchRoute.Signal ? "구조신호" : "합류";
            Quarter2EventDefinition definition = new(
                eventId,
                route,
                order,
                title,
                $"{routeName} 계열 진행을 확인하기 위한 임시 본문입니다.");

            if (!catalog.TryRegister(definition, out string failureReason))
            {
                throw new InvalidOperationException(failureReason);
            }
        }
    }
}
