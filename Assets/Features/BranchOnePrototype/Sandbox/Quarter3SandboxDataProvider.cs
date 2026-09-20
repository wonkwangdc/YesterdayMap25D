using System;
using YesterdayMap.BranchOne.Quarter3;
using YesterdayMap.BranchOne.Quarter3.JoinSupport;

namespace YesterdayMap.BranchOne.Sandbox
{
    public sealed class Quarter3SandboxDataProvider
    {
        public const string SourceA = "TEST_SOURCE_A";
        public const string SourceB = "TEST_SOURCE_B";
        public const string SignalClue01 = "TEST_SIGNAL_CLUE_01";
        public const string SignalClue02 = "TEST_SIGNAL_CLUE_02";
        public const string SignalClue03 = "TEST_SIGNAL_CLUE_03";
        public const string JoinClue01 = "TEST_JOIN_CLUE_01";
        public const string JoinClue02 = "TEST_JOIN_CLUE_02";
        public const string SignalOptionA = "TEST_SIGNAL_OPTION_A";
        public const string SignalOptionB = "TEST_SIGNAL_OPTION_B";
        public const string JoinOptionA = "TEST_JOIN_OPTION_A";
        public const string JoinOptionB = "TEST_JOIN_OPTION_B";
        private static readonly string[] SupportClueIds =
        {
            "TEST_JOIN_SUPPORT_DAY_1_SURVIVOR", "TEST_JOIN_SUPPORT_DAY_1_RED_ARMBAND",
            "TEST_JOIN_SUPPORT_DAY_2_SURVIVOR", "TEST_JOIN_SUPPORT_DAY_2_RED_ARMBAND",
            "TEST_JOIN_SUPPORT_DAY_3_SURVIVOR", "TEST_JOIN_SUPPORT_DAY_3_RED_ARMBAND",
            "TEST_JOIN_SUPPORT_DAY_4_SURVIVOR", "TEST_JOIN_SUPPORT_DAY_4_RED_ARMBAND"
        };
        private static readonly string[] SupportTitles =
        {
            "공동 배급 기록", "경비대 활동 기록",
            "부상자 치료 기록", "구조 대상 선별 기준",
            "지원 경로 조사 결과", "차량 및 탑승자 계획",
            "협상 계획과 위험", "물류창고 철거 계획"
        };

        public Quarter3ClueCatalog CreateClueCatalog()
        {
            Quarter3ClueCatalog catalog = new();
            RegisterClue(
                catalog,
                SignalClue01,
                BranchRoute.Signal,
                SourceA);
            RegisterClue(
                catalog,
                SignalClue02,
                BranchRoute.Signal,
                SourceA);
            RegisterClue(
                catalog,
                SignalClue03,
                BranchRoute.Signal,
                SourceB);
            RegisterClue(
                catalog,
                JoinClue01,
                BranchRoute.Join,
                SourceA);
            RegisterClue(
                catalog,
                JoinClue02,
                BranchRoute.Join,
                SourceB);
            return catalog;
        }

        public Quarter3ClueCatalog CreateCampaignClueCatalog()
        {
            Quarter3ClueCatalog catalog = CreateClueCatalog();
            for (int i = 0; i < SupportClueIds.Length; i++)
                RegisterClue(catalog, SupportClueIds[i], BranchRoute.Join,
                    SupportClueIds[i] + "_SOURCE");
            return catalog;
        }

        public Quarter3JoinSupportCatalog CreateJoinSupportCatalog()
        {
            Quarter3JoinSupportCatalog catalog = new();
            for (int day = 1; day <= 4; day++)
            for (int targetIndex = 0; targetIndex < 2; targetIndex++)
            {
                int index = (day - 1) * 2 + targetIndex;
                Quarter3JoinSupportDefinition definition = new(
                    day, (Quarter3JoinSupportTarget)targetIndex,
                    SupportClueIds[index] + "_SOURCE", SupportClueIds[index],
                    SupportTitles[index]);
                if (!catalog.TryRegister(definition, out string reason))
                    throw new InvalidOperationException(reason);
            }
            return catalog;
        }

        public Quarter3FinalChoiceCatalog CreateFinalChoiceCatalog()
        {
            Quarter3FinalChoiceCatalog catalog = new();
            RegisterFinalChoice(
                catalog,
                SignalOptionA,
                BranchRoute.Signal);
            RegisterFinalChoice(
                catalog,
                SignalOptionB,
                BranchRoute.Signal);
            RegisterFinalChoice(
                catalog,
                JoinOptionA,
                BranchRoute.Join);
            RegisterFinalChoice(
                catalog,
                JoinOptionB,
                BranchRoute.Join);
            return catalog;
        }

        private static void RegisterClue(
            Quarter3ClueCatalog catalog,
            string clueId,
            BranchRoute route,
            string sourceId)
        {
            Quarter3ClueDefinition definition =
                new(clueId, route, sourceId);
            if (!catalog.TryRegister(
                    definition,
                    out string failureReason))
            {
                throw new InvalidOperationException(failureReason);
            }
        }

        private static void RegisterFinalChoice(
            Quarter3FinalChoiceCatalog catalog,
            string finalChoiceId,
            BranchRoute route)
        {
            Quarter3FinalChoiceDefinition definition =
                new(finalChoiceId, route);
            if (!catalog.TryRegister(
                    definition,
                    out string failureReason))
            {
                throw new InvalidOperationException(failureReason);
            }
        }
    }
}
