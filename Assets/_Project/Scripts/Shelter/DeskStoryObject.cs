using UnityEngine;
using YesterdayMap.Events;

namespace YesterdayMap.Shelter
{
    // Signal-route radio broadcasts are listened to at the shelter desk.
    public sealed class DeskStoryObject : ShelterObject
    {
        [SerializeField] private ShelterEventDialogueController eventDialogue;

        public override string InteractionPrompt => string.Empty;
        public override bool CanInteract => base.CanInteract && CanAlternateInteract;
        public override bool RequiresCenterAim => true;

        public override string AlternateInteractionPrompt
        {
            get
            {
                ResolveEventDialogue();
                if (eventDialogue == null ||
                    (!eventDialogue.IsQuarter2SignalDeskEventActive &&
                     !eventDialogue.IsQuarter3SignalStoryActive) ||
                    eventDialogue.IsQuarter3FinalChoiceReady)
                {
                    return string.Empty;
                }

                return "[T] 오늘의 스토리 확인";
            }
        }

        public override bool CanAlternateInteract
        {
            get
            {
                ResolveEventDialogue();
                return eventDialogue != null &&
                       (eventDialogue.IsQuarter2SignalDeskEventActive ||
                        eventDialogue.IsQuarter3SignalStoryActive) &&
                       !eventDialogue.IsQuarter3FinalChoiceReady;
            }
        }

        private void Awake()
        {
            ResolveEventDialogue();
        }

        public override void Interact()
        {
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
                eventDialogue = Object.FindFirstObjectByType<ShelterEventDialogueController>(
                    FindObjectsInactive.Include);
            }
        }
    }
}
