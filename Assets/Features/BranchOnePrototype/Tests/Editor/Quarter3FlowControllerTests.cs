using System;
using System.Collections.Generic;
using NUnit.Framework;
using YesterdayMap.BranchOne.Quarter3;

namespace YesterdayMap.BranchOne.Tests
{
    public sealed class Quarter3FlowControllerTests
    {
        [Test]
        public void StartQuarter3_StartsSignalRoute()
        {
            Quarter3FlowController controller =
                Quarter3TestFactory.CreateController();

            Assert.That(
                controller.StartQuarter3(BranchRoute.Signal),
                Is.True);
            Assert.That(
                controller.Phase,
                Is.EqualTo(Quarter3FlowPhase.CollectingClues));
            Assert.That(
                controller.CurrentRoute,
                Is.EqualTo(BranchRoute.Signal));
            Assert.That(controller.IsActive, Is.True);
        }

        [Test]
        public void StartQuarter3_StartsJoinRoute()
        {
            Quarter3FlowController controller =
                Quarter3TestFactory.CreateController();

            Assert.That(
                controller.StartQuarter3(BranchRoute.Join),
                Is.True);
            Assert.That(
                controller.CurrentRoute,
                Is.EqualTo(BranchRoute.Join));
        }

        [Test]
        public void StartQuarter3_DuplicateStart_IsBlockedWithoutStateChange()
        {
            Quarter3FlowController controller =
                CreateStartedController(BranchRoute.Signal);

            Assert.That(
                controller.StartQuarter3(BranchRoute.Join),
                Is.False);
            Assert.That(controller.LastFailureReason, Is.Not.Empty);
            Assert.That(
                controller.CurrentRoute,
                Is.EqualTo(BranchRoute.Signal));
            Assert.That(
                controller.Phase,
                Is.EqualTo(Quarter3FlowPhase.CollectingClues));
        }

        [Test]
        public void StartQuarter3_InvalidRoute_IsBlocked()
        {
            Quarter3FlowController controller =
                Quarter3TestFactory.CreateController();

            Assert.That(
                controller.StartQuarter3(BranchRoute.None),
                Is.False);
            Assert.That(
                controller.Phase,
                Is.EqualTo(Quarter3FlowPhase.NotStarted));
        }

        [Test]
        public void StartQuarter3_IncompleteFinalChoiceCatalog_IsBlocked()
        {
            Quarter3FinalChoiceCatalog choices = new();
            Assert.That(
                choices.TryRegister(
                    Quarter3TestFactory.CreateFinalChoice(
                        Quarter3TestFactory.SignalOptionA,
                        BranchRoute.Signal),
                    out _),
                Is.True);
            Quarter3FlowController controller = new(
                Quarter3TestFactory.CreateClueCatalog(),
                choices);

            Assert.That(
                controller.StartQuarter3(BranchRoute.Signal),
                Is.False);
            Assert.That(controller.LastFailureReason, Is.Not.Empty);
        }

        [Test]
        public void Reset_AfterStart_AllowsRestart()
        {
            Quarter3FlowController controller =
                CreateStartedController(BranchRoute.Signal);

            controller.Reset();

            Assert.That(
                controller.StartQuarter3(BranchRoute.Join),
                Is.True);
            Assert.That(
                controller.CurrentRoute,
                Is.EqualTo(BranchRoute.Join));
        }

        [Test]
        public void GetAvailableClues_BeforeStart_ReturnsEmpty()
        {
            Quarter3FlowController controller =
                Quarter3TestFactory.CreateController();

            Assert.That(
                controller.GetAvailableClues(
                    Quarter3TestFactory.SourceA),
                Is.Empty);
        }

        [Test]
        public void GetAvailableClues_ReturnsCurrentRouteAndSourceMatches()
        {
            Quarter3FlowController controller = CreateStartedController();

            var clues = controller.GetAvailableClues(
                Quarter3TestFactory.SourceA);

            Assert.That(clues.Count, Is.EqualTo(2));
            Assert.That(
                clues[0].ClueId,
                Is.EqualTo(Quarter3TestFactory.SignalClue01));
            Assert.That(
                clues[1].ClueId,
                Is.EqualTo(Quarter3TestFactory.SignalClue02));
        }

        [Test]
        public void GetAvailableClues_ExcludesOtherRoute()
        {
            Quarter3FlowController controller =
                CreateStartedController(BranchRoute.Join);

            var clues = controller.GetAvailableClues(
                Quarter3TestFactory.SourceA);

            Assert.That(clues.Count, Is.EqualTo(1));
            Assert.That(
                clues[0].ClueId,
                Is.EqualTo(Quarter3TestFactory.JoinClue01));
        }

        [Test]
        public void TryAcquireClue_ValidClue_ReturnsSuccessDetails()
        {
            Quarter3FlowController controller = CreateStartedController();

            Quarter3ClueAcquisitionResult result =
                controller.TryAcquireClue(
                    Quarter3TestFactory.SignalClue01);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(
                result.Status,
                Is.EqualTo(Quarter3ClueAcquisitionStatus.Success));
            Assert.That(
                result.RequestedClueId,
                Is.EqualTo(Quarter3TestFactory.SignalClue01));
            Assert.That(
                result.AcquiredClueId,
                Is.EqualTo(Quarter3TestFactory.SignalClue01));
            Assert.That(result.ClueDefinition, Is.Not.Null);
            Assert.That(result.FailureReason, Is.Empty);
        }

        [Test]
        public void GetAvailableClues_AfterAcquisition_ExcludesAcquiredClue()
        {
            Quarter3FlowController controller = CreateStartedController();
            controller.TryAcquireClue(
                Quarter3TestFactory.SignalClue01);

            var clues = controller.GetAvailableClues(
                Quarter3TestFactory.SourceA);

            Assert.That(clues.Count, Is.EqualTo(1));
            Assert.That(
                clues[0].ClueId,
                Is.EqualTo(Quarter3TestFactory.SignalClue02));
        }

        [Test]
        public void HasClueAndGetAcquiredClues_ReflectAcquisition()
        {
            Quarter3FlowController controller = CreateStartedController();
            controller.TryAcquireClue(
                Quarter3TestFactory.SignalClue03);

            Assert.That(
                controller.HasClue(
                    Quarter3TestFactory.SignalClue03),
                Is.True);
            Assert.That(controller.GetAcquiredClues().Count, Is.EqualTo(1));
            Assert.That(
                controller.GetAcquiredClues()[0].ClueId,
                Is.EqualTo(Quarter3TestFactory.SignalClue03));
        }

        [Test]
        public void TryAcquireClue_OtherRoute_IsBlocked()
        {
            Quarter3FlowController controller = CreateStartedController();

            Quarter3ClueAcquisitionResult result =
                controller.TryAcquireClue(
                    Quarter3TestFactory.JoinClue01);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(
                result.Status,
                Is.EqualTo(
                    Quarter3ClueAcquisitionStatus.RouteMismatch));
            Assert.That(controller.AcquiredClueIds, Is.Empty);
        }

        [Test]
        public void TryAcquireClue_UnregisteredId_IsBlocked()
        {
            Quarter3FlowController controller = CreateStartedController();

            Quarter3ClueAcquisitionResult result =
                controller.TryAcquireClue("TEST_UNKNOWN_CLUE");

            Assert.That(
                result.Status,
                Is.EqualTo(
                    Quarter3ClueAcquisitionStatus.ClueNotRegistered));
        }

        [Test]
        public void TryAcquireClue_DuplicateAcquisition_IsBlocked()
        {
            Quarter3FlowController controller = CreateStartedController();
            Quarter3ClueAcquisitionResult first =
                controller.TryAcquireClue(
                    Quarter3TestFactory.SignalClue01);

            Quarter3ClueAcquisitionResult duplicate =
                controller.TryAcquireClue(
                    Quarter3TestFactory.SignalClue01);

            Assert.That(first.IsSuccess, Is.True);
            Assert.That(
                duplicate.Status,
                Is.EqualTo(
                    Quarter3ClueAcquisitionStatus.AlreadyAcquired));
            Assert.That(controller.AcquiredClueIds.Count, Is.EqualTo(1));
        }

        [Test]
        public void TryAcquireClue_BeforeStart_IsBlocked()
        {
            Quarter3FlowController controller =
                Quarter3TestFactory.CreateController();

            Quarter3ClueAcquisitionResult result =
                controller.TryAcquireClue(
                    Quarter3TestFactory.SignalClue01);

            Assert.That(
                result.Status,
                Is.EqualTo(Quarter3ClueAcquisitionStatus.NotStarted));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void TryAcquireClue_InvalidId_IsBlocked(string clueId)
        {
            Quarter3FlowController controller = CreateStartedController();

            Quarter3ClueAcquisitionResult result =
                controller.TryAcquireClue(clueId);

            Assert.That(
                result.Status,
                Is.EqualTo(
                    Quarter3ClueAcquisitionStatus.InvalidClueId));
        }

        [Test]
        public void TryAcquireClue_AfterResolved_IsBlocked()
        {
            Quarter3FlowController controller = CreateStartedController();
            Assert.That(controller.OpenFinalChoice(), Is.True);
            Assert.That(
                controller.SelectFinalChoice(
                    Quarter3TestFactory.SignalOptionA).IsSuccess,
                Is.True);

            Quarter3ClueAcquisitionResult result =
                controller.TryAcquireClue(
                    Quarter3TestFactory.SignalClue01);

            Assert.That(
                result.Status,
                Is.EqualTo(Quarter3ClueAcquisitionStatus.Resolved));
        }

        [Test]
        public void TryAcquireClue_WhileFinalChoiceOpen_IsAllowed()
        {
            Quarter3FlowController controller = CreateStartedController();
            Assert.That(controller.OpenFinalChoice(), Is.True);

            Quarter3ClueAcquisitionResult result =
                controller.TryAcquireClue(
                    Quarter3TestFactory.SignalClue01);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(
                controller.Phase,
                Is.EqualTo(Quarter3FlowPhase.FinalChoiceOpen));
        }

        [Test]
        public void GetSourceProgress_ReturnsInitialTotals()
        {
            Quarter3FlowController controller = CreateStartedController();

            Quarter3SourceProgress progress =
                controller.GetSourceProgress(
                    Quarter3TestFactory.SourceA);

            Assert.That(progress.SourceId, Is.EqualTo(
                Quarter3TestFactory.SourceA));
            Assert.That(progress.TotalCount, Is.EqualTo(2));
            Assert.That(progress.AcquiredCount, Is.Zero);
            Assert.That(progress.RemainingCount, Is.EqualTo(2));
        }

        [Test]
        public void GetSourceProgress_AfterAcquisition_UpdatesCounts()
        {
            Quarter3FlowController controller = CreateStartedController();
            controller.TryAcquireClue(
                Quarter3TestFactory.SignalClue01);

            Quarter3SourceProgress progress =
                controller.GetSourceProgress(
                    Quarter3TestFactory.SourceA);

            Assert.That(progress.TotalCount, Is.EqualTo(2));
            Assert.That(progress.AcquiredCount, Is.EqualTo(1));
            Assert.That(progress.RemainingCount, Is.EqualTo(1));
        }

        [Test]
        public void GetSourceProgress_CountsOnlyCurrentRoute()
        {
            Quarter3FlowController controller =
                CreateStartedController(BranchRoute.Join);

            Quarter3SourceProgress progress =
                controller.GetSourceProgress(
                    Quarter3TestFactory.SourceA);

            Assert.That(progress.TotalCount, Is.EqualTo(1));
        }

        [Test]
        public void GetSourceProgress_UnknownSource_ReturnsZeros()
        {
            Quarter3FlowController controller = CreateStartedController();

            Quarter3SourceProgress progress =
                controller.GetSourceProgress("TEST_UNKNOWN_SOURCE");

            Assert.That(progress.TotalCount, Is.Zero);
            Assert.That(progress.AcquiredCount, Is.Zero);
            Assert.That(progress.RemainingCount, Is.Zero);
        }

        [Test]
        public void OpenFinalChoice_BeforeStart_IsBlocked()
        {
            Quarter3FlowController controller =
                Quarter3TestFactory.CreateController();

            Assert.That(controller.OpenFinalChoice(), Is.False);
            Assert.That(
                controller.Phase,
                Is.EqualTo(Quarter3FlowPhase.NotStarted));
        }

        [Test]
        public void OpenFinalChoice_FromCollectingClues_OpensChoice()
        {
            Quarter3FlowController controller = CreateStartedController();

            Assert.That(controller.OpenFinalChoice(), Is.True);
            Assert.That(
                controller.Phase,
                Is.EqualTo(Quarter3FlowPhase.FinalChoiceOpen));
            Assert.That(controller.IsFinalChoiceOpen, Is.True);
        }

        [Test]
        public void OpenFinalChoice_DuplicateCall_IsSafeFailure()
        {
            Quarter3FlowController controller = CreateStartedController();
            Assert.That(controller.OpenFinalChoice(), Is.True);

            Assert.That(controller.OpenFinalChoice(), Is.False);
            Assert.That(controller.LastFailureReason, Is.Not.Empty);
            Assert.That(
                controller.Phase,
                Is.EqualTo(Quarter3FlowPhase.FinalChoiceOpen));
        }

        [Test]
        public void OpenFinalChoice_WithZeroAcquiredClues_IsAllowed()
        {
            Quarter3FlowController controller = CreateStartedController();

            Assert.That(controller.AcquiredClueIds, Is.Empty);
            Assert.That(controller.OpenFinalChoice(), Is.True);
        }

        [Test]
        public void GetFinalChoiceOptions_ReturnsCurrentRouteChoices()
        {
            Quarter3FlowController controller =
                CreateStartedController(BranchRoute.Join);

            var choices = controller.GetFinalChoiceOptions();

            Assert.That(choices.Count, Is.EqualTo(2));
            Assert.That(
                choices[0].FinalChoiceId,
                Is.EqualTo(Quarter3TestFactory.JoinOptionA));
            Assert.That(
                choices[1].FinalChoiceId,
                Is.EqualTo(Quarter3TestFactory.JoinOptionB));
        }

        [Test]
        public void SelectFinalChoice_BeforeOpen_IsBlocked()
        {
            Quarter3FlowController controller = CreateStartedController();

            Quarter3FinalSelectionResult result =
                controller.SelectFinalChoice(
                    Quarter3TestFactory.SignalOptionA);

            Assert.That(
                result.Status,
                Is.EqualTo(
                    Quarter3FinalSelectionStatus.FinalChoiceNotOpen));
        }

        [Test]
        public void SelectFinalChoice_BeforeStart_IsBlocked()
        {
            Quarter3FlowController controller =
                Quarter3TestFactory.CreateController();

            Quarter3FinalSelectionResult result =
                controller.SelectFinalChoice(
                    Quarter3TestFactory.SignalOptionA);

            Assert.That(
                result.Status,
                Is.EqualTo(Quarter3FinalSelectionStatus.NotStarted));
        }

        [Test]
        public void SelectFinalChoice_CurrentRouteChoice_ResolvesQuarter3()
        {
            Quarter3FlowController controller = CreateStartedController();
            controller.OpenFinalChoice();

            Quarter3FinalSelectionResult result =
                controller.SelectFinalChoice(
                    Quarter3TestFactory.SignalOptionA);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(
                result.Status,
                Is.EqualTo(Quarter3FinalSelectionStatus.Success));
            Assert.That(
                result.RequestedFinalChoiceId,
                Is.EqualTo(Quarter3TestFactory.SignalOptionA));
            Assert.That(
                result.SelectedFinalChoiceId,
                Is.EqualTo(Quarter3TestFactory.SignalOptionA));
            Assert.That(
                controller.SelectedFinalChoiceId,
                Is.EqualTo(Quarter3TestFactory.SignalOptionA));
            Assert.That(
                controller.Phase,
                Is.EqualTo(Quarter3FlowPhase.Resolved));
            Assert.That(controller.IsResolved, Is.True);
            Assert.That(controller.IsActive, Is.False);
        }

        [Test]
        public void SelectFinalChoice_OtherRouteChoice_IsBlocked()
        {
            Quarter3FlowController controller = CreateStartedController();
            controller.OpenFinalChoice();

            Quarter3FinalSelectionResult result =
                controller.SelectFinalChoice(
                    Quarter3TestFactory.JoinOptionA);

            Assert.That(
                result.Status,
                Is.EqualTo(Quarter3FinalSelectionStatus.RouteMismatch));
            Assert.That(
                controller.Phase,
                Is.EqualTo(Quarter3FlowPhase.FinalChoiceOpen));
        }

        [Test]
        public void SelectFinalChoice_UnregisteredId_IsBlocked()
        {
            Quarter3FlowController controller = CreateStartedController();
            controller.OpenFinalChoice();

            Quarter3FinalSelectionResult result =
                controller.SelectFinalChoice("TEST_UNKNOWN_OPTION");

            Assert.That(
                result.Status,
                Is.EqualTo(
                    Quarter3FinalSelectionStatus
                        .FinalChoiceNotRegistered));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void SelectFinalChoice_InvalidId_IsBlocked(
            string finalChoiceId)
        {
            Quarter3FlowController controller = CreateStartedController();
            controller.OpenFinalChoice();

            Quarter3FinalSelectionResult result =
                controller.SelectFinalChoice(finalChoiceId);

            Assert.That(
                result.Status,
                Is.EqualTo(
                    Quarter3FinalSelectionStatus.InvalidFinalChoiceId));
        }

        [Test]
        public void SelectFinalChoice_DuplicateSelection_IsBlocked()
        {
            Quarter3FlowController controller = CreateStartedController();
            controller.OpenFinalChoice();
            Quarter3FinalSelectionResult first =
                controller.SelectFinalChoice(
                    Quarter3TestFactory.SignalOptionA);

            Quarter3FinalSelectionResult duplicate =
                controller.SelectFinalChoice(
                    Quarter3TestFactory.SignalOptionB);

            Assert.That(first.IsSuccess, Is.True);
            Assert.That(
                duplicate.Status,
                Is.EqualTo(
                    Quarter3FinalSelectionStatus.AlreadyResolved));
            Assert.That(
                controller.SelectedFinalChoiceId,
                Is.EqualTo(Quarter3TestFactory.SignalOptionA));
        }

        [Test]
        public void SelectFinalChoice_WithZeroAcquiredClues_IsAllowed()
        {
            Quarter3FlowController controller = CreateStartedController();
            controller.OpenFinalChoice();

            Quarter3FinalSelectionResult result =
                controller.SelectFinalChoice(
                    Quarter3TestFactory.SignalOptionB);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(controller.AcquiredClueIds, Is.Empty);
        }

        [Test]
        public void OpenFinalChoice_AfterResolved_IsBlocked()
        {
            Quarter3FlowController controller = CreateStartedController();
            controller.OpenFinalChoice();
            controller.SelectFinalChoice(
                Quarter3TestFactory.SignalOptionA);

            Assert.That(controller.OpenFinalChoice(), Is.False);
            Assert.That(
                controller.Phase,
                Is.EqualTo(Quarter3FlowPhase.Resolved));
        }

        [Test]
        public void Reset_RestoresCompleteInitialState()
        {
            Quarter3FlowController controller = CreateStartedController();
            controller.TryAcquireClue(
                Quarter3TestFactory.SignalClue01);
            controller.OpenFinalChoice();
            controller.SelectFinalChoice(
                Quarter3TestFactory.SignalOptionA);

            controller.Reset();

            Assert.That(
                controller.Phase,
                Is.EqualTo(Quarter3FlowPhase.NotStarted));
            Assert.That(
                controller.CurrentRoute,
                Is.EqualTo(BranchRoute.None));
            Assert.That(controller.IsActive, Is.False);
            Assert.That(controller.IsFinalChoiceOpen, Is.False);
            Assert.That(controller.IsResolved, Is.False);
            Assert.That(controller.AcquiredClueIds, Is.Empty);
            Assert.That(controller.SelectedFinalChoiceId, Is.Empty);
            Assert.That(controller.LastFailureReason, Is.Empty);
        }

        [Test]
        public void AcquiredClueIds_CannotBeModifiedExternally()
        {
            Quarter3FlowController controller = CreateStartedController();
            controller.TryAcquireClue(
                Quarter3TestFactory.SignalClue01);
            IList<string> acquiredIds =
                (IList<string>)controller.AcquiredClueIds;

            Assert.Throws<NotSupportedException>(
                () => acquiredIds.Add("TEST_EXTERNAL_MUTATION"));
            Assert.That(controller.AcquiredClueIds.Count, Is.EqualTo(1));
        }

        [Test]
        public void Constructor_NullCatalogs_AreRejected()
        {
            Assert.Throws<ArgumentNullException>(
                () => new Quarter3FlowController(
                    null,
                    Quarter3TestFactory.CreateFinalChoiceCatalog()));
            Assert.Throws<ArgumentNullException>(
                () => new Quarter3FlowController(
                    Quarter3TestFactory.CreateClueCatalog(),
                    null));
        }

        private static Quarter3FlowController CreateStartedController(
            BranchRoute route = BranchRoute.Signal)
        {
            Quarter3FlowController controller =
                Quarter3TestFactory.CreateController();
            Assert.That(controller.StartQuarter3(route), Is.True);
            return controller;
        }
    }
}
