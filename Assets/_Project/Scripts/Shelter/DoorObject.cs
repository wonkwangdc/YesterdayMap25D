using YesterdayMap.Core;
using YesterdayMap.Events;
using YesterdayMap.Exploration;

namespace YesterdayMap.Shelter
{
    public sealed class DoorObject : ShelterObject
    {
        [UnityEngine.SerializeField] private ExplorationManager explorationManager;
        [UnityEngine.SerializeField] private SceneFader sceneFader;
        [UnityEngine.SerializeField] private ShelterEventDialogueController eventDialogue;

        public override string AlternateInteractionPrompt
        {
            get
            {
                if (eventDialogue == null)
                {
                    return string.Empty;
                }

                if (eventDialogue.IsQuarter3FinalChoiceReady)
                {
                    return "[T] 엔딩을 고르세요";
                }

                if (eventDialogue.IsQuarter2SignalDeskEventActive)
                {
                    return string.Empty;
                }

                if (eventDialogue.IsQuarter3SignalStoryActive)
                {
                    return string.Empty;
                }

                if (eventDialogue.IsQuarter3JoinStoryPending)
                {
                    return "[T] 오늘의 스토리 확인";
                }

                return "[T] 문 앞 이벤트 확인";
            }
        }

        public override bool CanAlternateInteract
        {
            get
            {
                if (eventDialogue == null)
                {
                    return false;
                }

                return !eventDialogue.IsQuarter2SignalDeskEventActive &&
                       (!eventDialogue.IsQuarter3SignalStoryActive ||
                        eventDialogue.IsQuarter3FinalChoiceReady);
            }
        }

        public void Configure(ExplorationManager manager, SceneFader fader)
        {
            explorationManager = manager;
            sceneFader = fader;
        }

        private void Awake()
        {
            ResolveEventDialogue();
        }

        public override void Interact()
        {
            if (sceneFader == null || explorationManager == null) return;
            if (!explorationManager.CanExploreToday)
            {
                ResolveEventDialogue();
                if (explorationManager.CurrentDay < 2)
                {
                    eventDialogue?.ShowProtagonistMonologue(
                        explorationManager.ExplorationBlockMessage);
                    return;
                }

                Ui?.ShowMessage(explorationManager.ExplorationBlockMessage);
                return;
            }

            sceneFader.LoadExplorationScene(explorationManager);
        }

        public override void AlternateInteract()
        {
            ResolveEventDialogue();
            eventDialogue?.TryOpenTodaysEvent();
        }

        private void ResolveEventDialogue()
        {
            if (eventDialogue == null)
            {
                eventDialogue = UnityEngine.Object.FindFirstObjectByType<ShelterEventDialogueController>(
                    UnityEngine.FindObjectsInactive.Include);
            }
        }
    }
}
