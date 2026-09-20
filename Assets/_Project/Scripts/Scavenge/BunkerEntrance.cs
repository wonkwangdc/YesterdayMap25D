using YesterdayMap.Interaction;

namespace YesterdayMap.Scavenge
{
    public sealed class BunkerEntrance : InteractableObject
    {
        private ScavengeManager manager;

        public override string InteractionPrompt
        {
            get
            {
                ResolveManager();
                return manager != null ? manager.BunkerInteractionPrompt : "[E] 벙커 입구 확인";
            }
        }

        public override bool CanInteract
        {
            get
            {
                ResolveManager();
                return base.CanInteract && manager != null;
            }
        }

        public override string AlternateInteractionPrompt => "[F] 벙커로 들어가기";
        public override bool CanAlternateInteract => CanInteract;

        public void Configure(ScavengeManager scavengeManager)
        {
            manager = scavengeManager;
        }

        private void Awake()
        {
            ScavengeSceneRuntimeFixes.Apply();
            ResolveManager();
        }

        public override void Interact()
        {
            ResolveManager();
            manager?.InteractWithBunker();
        }

        public override void AlternateInteract()
        {
            ResolveManager();
            manager?.EnterShelter();
        }

        private void ResolveManager()
        {
            if (manager == null)
            {
                manager = FindFirstObjectByType<ScavengeManager>();
            }
        }
    }
}
