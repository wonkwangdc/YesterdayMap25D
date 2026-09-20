using NUnit.Framework;
using YesterdayMap.BranchOne.Campaign;
using YesterdayMap.BranchOne.Decision;
using YesterdayMap.BranchOne.Flow;
using YesterdayMap.BranchOne.Quarter2;
using YesterdayMap.BranchOne.Quarter3;

namespace YesterdayMap.BranchOne.Tests
{
    public sealed class EndingRouteCampaignQuarter3ApiTests
    {
        [Test]
        public void AdvanceToQuarter3_FromSignalPass_StartsSignalRoute()
        {
            EndingRouteCampaignController campaign =
                CreateQuarter2PassedCampaign(BranchRoute.Signal);

            EndingRouteCampaignTransitionResult result =
                campaign.AdvanceToQuarter3();

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(
                campaign.CampaignState.Phase,
                Is.EqualTo(EndingRouteCampaignPhase.Quarter3Running));
            Assert.That(
                campaign.Quarter3CurrentRoute,
                Is.EqualTo(BranchRoute.Signal));
            Assert.That(
                campaign.Quarter3Phase,
                Is.EqualTo(Quarter3FlowPhase.CollectingClues));
        }

        [Test]
        public void AdvanceToQuarter3_FromJoinPass_StartsJoinRoute()
        {
            EndingRouteCampaignController campaign =
                CreateQuarter2PassedCampaign(BranchRoute.Join);

            Assert.That(campaign.AdvanceToQuarter3().IsSuccess, Is.True);
            Assert.That(
                campaign.Quarter3CurrentRoute,
                Is.EqualTo(BranchRoute.Join));
        }

        [Test]
        public void AdvanceToQuarter3_WhileQuarter2Running_IsBlocked()
        {
            EndingRouteCampaignController campaign =
                CreateQuarter2Campaign();
            EndingRouteCampaignPhase phaseBefore =
                campaign.CampaignState.Phase;
            BranchRoute routeBefore = campaign.Quarter2CurrentRoute;

            EndingRouteCampaignTransitionResult result =
                campaign.AdvanceToQuarter3();

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(campaign.CampaignState.Phase, Is.EqualTo(phaseBefore));
            Assert.That(campaign.Quarter2CurrentRoute, Is.EqualTo(routeBefore));
            Assert.That(
                campaign.Quarter3Phase,
                Is.EqualTo(Quarter3FlowPhase.NotStarted));
        }

        [Test]
        public void AdvanceToQuarter3_WhileQuarter1Running_IsBlocked()
        {
            EndingRouteCampaignController campaign =
                EndingRouteCampaignTestFactory.CreateCampaign();
            Assert.That(campaign.StartQuarter1().IsSuccess, Is.True);

            EndingRouteCampaignTransitionResult result =
                campaign.AdvanceToQuarter3();

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(
                campaign.CampaignState.Phase,
                Is.EqualTo(EndingRouteCampaignPhase.Quarter1Running));
        }

        [Test]
        public void AdvanceToQuarter3_AfterAllBranchesFail_IsBlocked()
        {
            EndingRouteCampaignController campaign =
                CreateAllBranchesFailedCampaign();

            EndingRouteCampaignTransitionResult result =
                campaign.AdvanceToQuarter3();

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(
                campaign.CampaignState.Phase,
                Is.EqualTo(EndingRouteCampaignPhase.AllBranchesFailed));
            Assert.That(
                campaign.Quarter3Phase,
                Is.EqualTo(Quarter3FlowPhase.NotStarted));
        }

        [Test]
        public void AdvanceToQuarter3_DuplicateEntry_IsBlocked()
        {
            EndingRouteCampaignController campaign =
                CreateQuarter2PassedCampaign();
            Assert.That(campaign.AdvanceToQuarter3().IsSuccess, Is.True);

            EndingRouteCampaignTransitionResult duplicate =
                campaign.AdvanceToQuarter3();

            Assert.That(duplicate.IsSuccess, Is.False);
            Assert.That(
                campaign.CampaignState.Phase,
                Is.EqualTo(EndingRouteCampaignPhase.Quarter3Running));
            Assert.That(
                campaign.Quarter3Phase,
                Is.EqualTo(Quarter3FlowPhase.CollectingClues));
        }

        [Test]
        public void AdvanceToQuarter3_AfterResolution_IsBlocked()
        {
            EndingRouteCampaignController campaign =
                CreateResolvedQuarter3Campaign();

            EndingRouteCampaignTransitionResult result =
                campaign.AdvanceToQuarter3();

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(
                campaign.CampaignState.Phase,
                Is.EqualTo(EndingRouteCampaignPhase.Quarter3Resolved));
            Assert.That(
                campaign.Quarter3SelectedFinalChoiceId,
                Is.EqualTo(Quarter3TestFactory.SignalOptionA));
        }

        [Test]
        public void AcquireQuarter3Clue_BeforeEntry_IsBlocked()
        {
            EndingRouteCampaignController campaign =
                CreateQuarter2PassedCampaign();

            Quarter3ClueAcquisitionResult result =
                campaign.AcquireQuarter3Clue(
                    Quarter3TestFactory.SignalClue01);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(
                result.Status,
                Is.EqualTo(Quarter3ClueAcquisitionStatus.NotStarted));
            Assert.That(campaign.Quarter3AcquiredClueIds, Is.Empty);
        }

        [Test]
        public void GetQuarter3AvailableClues_ReturnsCurrentRouteSource()
        {
            EndingRouteCampaignController campaign =
                CreateQuarter3Campaign(BranchRoute.Signal);

            var clues = campaign.GetQuarter3AvailableClues(
                Quarter3TestFactory.SourceA);

            Assert.That(clues.Count, Is.EqualTo(2));
            Assert.That(
                clues,
                Has.All.Property("Route").EqualTo(BranchRoute.Signal));
        }

        [Test]
        public void GetQuarter3AvailableClues_DoesNotReturnOtherRoute()
        {
            EndingRouteCampaignController campaign =
                CreateQuarter3Campaign(BranchRoute.Join);

            var clues = campaign.GetQuarter3AvailableClues(
                Quarter3TestFactory.SourceA);

            Assert.That(clues.Count, Is.EqualTo(1));
            Assert.That(
                clues[0].ClueId,
                Is.EqualTo(Quarter3TestFactory.JoinClue01));
        }

        [Test]
        public void AcquireQuarter3Clue_ReturnsCoreSuccessResult()
        {
            EndingRouteCampaignController campaign =
                CreateQuarter3Campaign();

            Quarter3ClueAcquisitionResult result =
                campaign.AcquireQuarter3Clue(
                    Quarter3TestFactory.SignalClue01);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(
                result.AcquiredClueId,
                Is.EqualTo(Quarter3TestFactory.SignalClue01));
            Assert.That(
                campaign.HasQuarter3Clue(
                    Quarter3TestFactory.SignalClue01),
                Is.True);
            Assert.That(
                campaign.GetQuarter3AcquiredClues().Count,
                Is.EqualTo(1));
        }

        [Test]
        public void AcquireQuarter3Clue_OtherRouteFailure_IsForwarded()
        {
            EndingRouteCampaignController campaign =
                CreateQuarter3Campaign();

            Quarter3ClueAcquisitionResult result =
                campaign.AcquireQuarter3Clue(
                    Quarter3TestFactory.JoinClue01);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(
                result.Status,
                Is.EqualTo(
                    Quarter3ClueAcquisitionStatus.RouteMismatch));
        }

        [Test]
        public void AcquireQuarter3Clue_DuplicateFailure_IsForwarded()
        {
            EndingRouteCampaignController campaign =
                CreateQuarter3Campaign();
            Assert.That(
                campaign.AcquireQuarter3Clue(
                    Quarter3TestFactory.SignalClue01).IsSuccess,
                Is.True);

            Quarter3ClueAcquisitionResult duplicate =
                campaign.AcquireQuarter3Clue(
                    Quarter3TestFactory.SignalClue01);

            Assert.That(
                duplicate.Status,
                Is.EqualTo(
                    Quarter3ClueAcquisitionStatus.AlreadyAcquired));
            Assert.That(campaign.Quarter3AcquiredClueIds.Count, Is.EqualTo(1));
        }

        [Test]
        public void GetQuarter3SourceProgress_ForwardsCurrentProgress()
        {
            EndingRouteCampaignController campaign =
                CreateQuarter3Campaign();
            campaign.AcquireQuarter3Clue(
                Quarter3TestFactory.SignalClue01);

            Quarter3SourceProgress progress =
                campaign.GetQuarter3SourceProgress(
                    Quarter3TestFactory.SourceA);

            Assert.That(progress.TotalCount, Is.EqualTo(2));
            Assert.That(progress.AcquiredCount, Is.EqualTo(1));
            Assert.That(progress.RemainingCount, Is.EqualTo(1));
        }

        [Test]
        public void AcquireQuarter3Clue_WhileFinalChoiceOpen_IsAllowed()
        {
            EndingRouteCampaignController campaign =
                CreateQuarter3Campaign();
            Assert.That(
                campaign.OpenQuarter3FinalChoice().IsSuccess,
                Is.True);

            Quarter3ClueAcquisitionResult result =
                campaign.AcquireQuarter3Clue(
                    Quarter3TestFactory.SignalClue01);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(campaign.Quarter3IsFinalChoiceOpen, Is.True);
        }

        [Test]
        public void AcquireQuarter3Clue_AfterResolution_IsBlocked()
        {
            EndingRouteCampaignController campaign =
                CreateResolvedQuarter3Campaign();

            Quarter3ClueAcquisitionResult result =
                campaign.AcquireQuarter3Clue(
                    Quarter3TestFactory.SignalClue01);

            Assert.That(
                result.Status,
                Is.EqualTo(Quarter3ClueAcquisitionStatus.Resolved));
            Assert.That(
                campaign.CampaignState.Phase,
                Is.EqualTo(EndingRouteCampaignPhase.Quarter3Resolved));
        }

        [Test]
        public void OpenQuarter3FinalChoice_BeforeEntry_IsBlocked()
        {
            EndingRouteCampaignController campaign =
                CreateQuarter2PassedCampaign();

            EndingRouteCampaignTransitionResult result =
                campaign.OpenQuarter3FinalChoice();

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(campaign.Quarter3IsFinalChoiceOpen, Is.False);
        }

        [Test]
        public void OpenQuarter3FinalChoice_WhileRunning_OpensChoice()
        {
            EndingRouteCampaignController campaign =
                CreateQuarter3Campaign();

            EndingRouteCampaignTransitionResult result =
                campaign.OpenQuarter3FinalChoice();

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(campaign.Quarter3IsFinalChoiceOpen, Is.True);
            Assert.That(
                campaign.Quarter3Phase,
                Is.EqualTo(Quarter3FlowPhase.FinalChoiceOpen));
        }

        [Test]
        public void OpenQuarter3FinalChoice_WithZeroClues_IsAllowed()
        {
            EndingRouteCampaignController campaign =
                CreateQuarter3Campaign();

            Assert.That(campaign.Quarter3AcquiredClueIds, Is.Empty);
            Assert.That(
                campaign.OpenQuarter3FinalChoice().IsSuccess,
                Is.True);
        }

        [Test]
        public void GetQuarter3FinalChoiceOptions_ReturnsCurrentRouteTwo()
        {
            EndingRouteCampaignController campaign =
                CreateQuarter3Campaign(BranchRoute.Join);

            var choices = campaign.GetQuarter3FinalChoiceOptions();

            Assert.That(choices.Count, Is.EqualTo(2));
            Assert.That(
                choices,
                Has.All.Property("Route").EqualTo(BranchRoute.Join));
        }

        [Test]
        public void SelectQuarter3FinalChoice_OtherRoute_IsBlocked()
        {
            EndingRouteCampaignController campaign =
                CreateQuarter3Campaign();
            campaign.OpenQuarter3FinalChoice();

            Quarter3FinalSelectionResult result =
                campaign.SelectQuarter3FinalChoice(
                    Quarter3TestFactory.JoinOptionA);

            Assert.That(
                result.Status,
                Is.EqualTo(Quarter3FinalSelectionStatus.RouteMismatch));
            Assert.That(
                campaign.CampaignState.Phase,
                Is.EqualTo(EndingRouteCampaignPhase.Quarter3Running));
        }

        [Test]
        public void SelectQuarter3FinalChoice_SuccessSynchronizesCampaign()
        {
            EndingRouteCampaignController campaign =
                CreateQuarter3Campaign();
            campaign.OpenQuarter3FinalChoice();

            Quarter3FinalSelectionResult result =
                campaign.SelectQuarter3FinalChoice(
                    Quarter3TestFactory.SignalOptionA);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(campaign.Quarter3IsResolved, Is.True);
            Assert.That(
                campaign.CampaignState.Phase,
                Is.EqualTo(EndingRouteCampaignPhase.Quarter3Resolved));
            Assert.That(
                campaign.Quarter3SelectedFinalChoiceId,
                Is.EqualTo(Quarter3TestFactory.SignalOptionA));
            Assert.That(
                campaign.CampaignState.Quarter3FinalChoiceId,
                Is.EqualTo(Quarter3TestFactory.SignalOptionA));
        }

        [Test]
        public void SelectQuarter3FinalChoice_DuplicateSelection_IsBlocked()
        {
            EndingRouteCampaignController campaign =
                CreateResolvedQuarter3Campaign();

            Quarter3FinalSelectionResult result =
                campaign.SelectQuarter3FinalChoice(
                    Quarter3TestFactory.SignalOptionB);

            Assert.That(
                result.Status,
                Is.EqualTo(
                    Quarter3FinalSelectionStatus.AlreadyResolved));
            Assert.That(
                campaign.Quarter3SelectedFinalChoiceId,
                Is.EqualTo(Quarter3TestFactory.SignalOptionA));
        }

        [Test]
        public void ResolvedQuarter3_AllowsReadOnlyResultQueries()
        {
            EndingRouteCampaignController campaign =
                CreateQuarter3Campaign();
            campaign.AcquireQuarter3Clue(
                Quarter3TestFactory.SignalClue01);
            campaign.OpenQuarter3FinalChoice();
            campaign.SelectQuarter3FinalChoice(
                Quarter3TestFactory.SignalOptionA);

            Assert.That(
                campaign.HasQuarter3Clue(
                    Quarter3TestFactory.SignalClue01),
                Is.True);
            Assert.That(
                campaign.GetQuarter3AcquiredClues().Count,
                Is.EqualTo(1));
            Assert.That(
                campaign.GetQuarter3FinalChoiceOptions().Count,
                Is.EqualTo(2));
        }

        [Test]
        public void AllBranchesFailed_PreservesFailureState()
        {
            EndingRouteCampaignController campaign =
                CreateAllBranchesFailedCampaign();

            Assert.That(campaign.AreAllBranchesFailed, Is.True);
            Assert.That(
                campaign.CampaignState.Quarter2PassedRoute,
                Is.EqualTo(BranchRoute.None));
            Assert.That(campaign.SignalFailed, Is.True);
            Assert.That(campaign.JoinFailed, Is.True);
            Assert.That(
                campaign.Quarter3Phase,
                Is.EqualTo(Quarter3FlowPhase.NotStarted));
        }

        [Test]
        public void AllBranchesFailed_Quarter3MutationsDoNotDamageState()
        {
            EndingRouteCampaignController campaign =
                CreateAllBranchesFailedCampaign();

            Quarter3ClueAcquisitionResult acquisition =
                campaign.AcquireQuarter3Clue(
                    Quarter3TestFactory.SignalClue01);
            EndingRouteCampaignTransitionResult open =
                campaign.OpenQuarter3FinalChoice();
            Quarter3FinalSelectionResult selection =
                campaign.SelectQuarter3FinalChoice(
                    Quarter3TestFactory.SignalOptionA);

            Assert.That(acquisition.IsSuccess, Is.False);
            Assert.That(open.IsSuccess, Is.False);
            Assert.That(selection.IsSuccess, Is.False);
            Assert.That(
                campaign.CampaignState.Phase,
                Is.EqualTo(EndingRouteCampaignPhase.AllBranchesFailed));
            Assert.That(
                campaign.Quarter3Phase,
                Is.EqualTo(Quarter3FlowPhase.NotStarted));
        }

        [Test]
        public void ResetCampaign_FromQuarter3Running_ResetsAllFlows()
        {
            EndingRouteCampaignController campaign =
                CreateQuarter3Campaign();
            campaign.AcquireQuarter3Clue(
                Quarter3TestFactory.SignalClue01);

            campaign.ResetCampaign();

            AssertCampaignReset(campaign);
        }

        [Test]
        public void ResetCampaign_FromQuarter3Resolved_ClearsFinalResult()
        {
            EndingRouteCampaignController campaign =
                CreateResolvedQuarter3Campaign();

            campaign.ResetCampaign();

            AssertCampaignReset(campaign);
            Assert.That(
                campaign.CampaignState.Quarter3FinalChoiceId,
                Is.Empty);
        }

        [Test]
        public void ResetCampaign_AllowsCompleteCampaignToRunAgain()
        {
            EndingRouteCampaignController campaign =
                CreateResolvedQuarter3Campaign();
            campaign.ResetCampaign();

            ResolveQuarter1AndStartQuarter2(
                campaign,
                BranchRoute.Join);
            PassCurrentQuarter2Route(campaign);

            Assert.That(campaign.AdvanceToQuarter3().IsSuccess, Is.True);
            Assert.That(
                campaign.Quarter3CurrentRoute,
                Is.EqualTo(BranchRoute.Join));
        }

        [Test]
        public void SynchronizeState_UpdatesDiagnosticDirectResolution()
        {
            EndingRouteCampaignController campaign =
                CreateQuarter3Campaign();
            campaign.Quarter3Controller.OpenFinalChoice();
            campaign.Quarter3Controller.SelectFinalChoice(
                Quarter3TestFactory.SignalOptionA);

            campaign.SynchronizeState();

            Assert.That(
                campaign.CampaignState.Phase,
                Is.EqualTo(EndingRouteCampaignPhase.Quarter3Resolved));
            Assert.That(
                campaign.CampaignState.Quarter3FinalChoiceId,
                Is.EqualTo(Quarter3TestFactory.SignalOptionA));
        }

        private static EndingRouteCampaignController CreateQuarter2Campaign(
            BranchRoute route = BranchRoute.Signal)
        {
            EndingRouteCampaignController campaign =
                EndingRouteCampaignTestFactory.CreateCampaign();
            ResolveQuarter1AndStartQuarter2(campaign, route);
            return campaign;
        }

        private static EndingRouteCampaignController
            CreateQuarter2PassedCampaign(
                BranchRoute route = BranchRoute.Signal)
        {
            EndingRouteCampaignController campaign =
                CreateQuarter2Campaign(route);
            PassCurrentQuarter2Route(campaign);
            return campaign;
        }

        private static EndingRouteCampaignController CreateQuarter3Campaign(
            BranchRoute route = BranchRoute.Signal)
        {
            EndingRouteCampaignController campaign =
                CreateQuarter2PassedCampaign(route);
            Assert.That(campaign.AdvanceToQuarter3().IsSuccess, Is.True);
            return campaign;
        }

        private static EndingRouteCampaignController
            CreateResolvedQuarter3Campaign()
        {
            EndingRouteCampaignController campaign =
                CreateQuarter3Campaign();
            Assert.That(
                campaign.OpenQuarter3FinalChoice().IsSuccess,
                Is.True);
            Assert.That(
                campaign.SelectQuarter3FinalChoice(
                    Quarter3TestFactory.SignalOptionA).IsSuccess,
                Is.True);
            return campaign;
        }

        private static EndingRouteCampaignController
            CreateAllBranchesFailedCampaign()
        {
            EndingRouteCampaignController campaign =
                CreateQuarter2Campaign();
            FailCurrentQuarter2Route(campaign);
            Assert.That(
                campaign.AdvanceQuarter2Step().IsSuccess,
                Is.True);
            FailCurrentQuarter2Route(campaign);
            return campaign;
        }

        private static void ResolveQuarter1AndStartQuarter2(
            EndingRouteCampaignController campaign,
            BranchRoute route)
        {
            Assert.That(campaign.StartQuarter1().IsSuccess, Is.True);
            int eventNumber = route == BranchRoute.Signal ? 1 : 6;
            for (
                int day = BranchPrototypeSettings.DefaultStartDay;
                day <= BranchPrototypeSettings.DefaultLastEventDay;
                day++)
            {
                Assert.That(
                    campaign.Quarter1Controller.SelectEvent(
                        eventNumber,
                        BranchTestEventFactory.ChoiceId).IsSuccess,
                    Is.True);
                Assert.That(
                    campaign.Quarter1Controller.AdvanceDay(),
                    Is.True);
            }

            BranchDecisionResult decision =
                campaign.Quarter1Controller.ResolveDecision(0.5d);
            Assert.That(decision.IsSuccess, Is.True);
            Assert.That(decision.DecidedRoute, Is.EqualTo(route));
            Assert.That(campaign.AdvanceToQuarter2().IsSuccess, Is.True);
        }

        private static void PassCurrentQuarter2Route(
            EndingRouteCampaignController campaign)
        {
            SelectAndAdvanceQuarter2(
                campaign,
                Quarter2Decision.Progress);
            SelectAndAdvanceQuarter2(
                campaign,
                Quarter2Decision.Progress);
            Assert.That(
                campaign.SelectQuarter2Decision(
                    Quarter2Decision.Progress).IsSuccess,
                Is.True);
            Assert.That(
                campaign.CampaignState.Phase,
                Is.EqualTo(EndingRouteCampaignPhase.Quarter2Passed));
        }

        private static void FailCurrentQuarter2Route(
            EndingRouteCampaignController campaign)
        {
            SelectAndAdvanceQuarter2(
                campaign,
                Quarter2Decision.Reject);
            Assert.That(
                campaign.SelectQuarter2Decision(
                    Quarter2Decision.Reject).IsSuccess,
                Is.True);
        }

        private static void SelectAndAdvanceQuarter2(
            EndingRouteCampaignController campaign,
            Quarter2Decision decision)
        {
            Assert.That(
                campaign.SelectQuarter2Decision(decision).IsSuccess,
                Is.True);
            Assert.That(
                campaign.AdvanceQuarter2Step().IsSuccess,
                Is.True);
        }

        private static void AssertCampaignReset(
            EndingRouteCampaignController campaign)
        {
            Assert.That(
                campaign.CampaignState.Phase,
                Is.EqualTo(EndingRouteCampaignPhase.NotStarted));
            Assert.That(
                campaign.Quarter1Controller.RuntimeState.Phase,
                Is.EqualTo(BranchFlowPhase.NotStarted));
            Assert.That(
                campaign.Quarter2Phase,
                Is.EqualTo(Quarter2FlowPhase.NotStarted));
            Assert.That(
                campaign.Quarter3Phase,
                Is.EqualTo(Quarter3FlowPhase.NotStarted));
            Assert.That(
                campaign.CampaignState.Quarter1Route,
                Is.EqualTo(BranchRoute.None));
            Assert.That(
                campaign.CampaignState.Quarter2PassedRoute,
                Is.EqualTo(BranchRoute.None));
            Assert.That(campaign.Quarter3AcquiredClueIds, Is.Empty);
            Assert.That(
                campaign.Quarter3SelectedFinalChoiceId,
                Is.Empty);
        }
    }
}
