using System;
using YesterdayMap.BranchOne.Quarter3;

namespace YesterdayMap.BranchOne.Tests
{
    internal static class Quarter3TestFactory
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

        public static Quarter3ClueCatalog CreateClueCatalog()
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

        public static Quarter3FinalChoiceCatalog CreateFinalChoiceCatalog()
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

        public static Quarter3FlowController CreateController()
        {
            return new Quarter3FlowController(
                CreateClueCatalog(),
                CreateFinalChoiceCatalog());
        }

        public static Quarter3ClueDefinition CreateClue(
            string clueId,
            BranchRoute route,
            string sourceId)
        {
            return new Quarter3ClueDefinition(clueId, route, sourceId);
        }

        public static Quarter3FinalChoiceDefinition CreateFinalChoice(
            string finalChoiceId,
            BranchRoute route)
        {
            return new Quarter3FinalChoiceDefinition(
                finalChoiceId,
                route);
        }

        private static void RegisterClue(
            Quarter3ClueCatalog catalog,
            string clueId,
            BranchRoute route,
            string sourceId)
        {
            if (!catalog.TryRegister(
                    CreateClue(clueId, route, sourceId),
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
            if (!catalog.TryRegister(
                    CreateFinalChoice(finalChoiceId, route),
                    out string failureReason))
            {
                throw new InvalidOperationException(failureReason);
            }
        }
    }
}
