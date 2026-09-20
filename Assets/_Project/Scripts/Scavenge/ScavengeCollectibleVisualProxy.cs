using UnityEngine;
using YesterdayMap.Interaction;

namespace YesterdayMap.Scavenge
{
    // Runtime delegate that lets duplicated visual models under one pickup root
    // behave as independent world items without changing the saved scene hierarchy.
    [DisallowMultipleComponent]
    public sealed class ScavengeCollectibleVisualProxy : InteractableObject
    {
        private ScavengeCollectible owner;
        private Transform visualRoot;

        public override string InteractionPrompt =>
            owner != null ? owner.InteractionPrompt : string.Empty;
        public override bool CanInteract =>
            owner != null &&
            visualRoot != null &&
            visualRoot.gameObject.activeInHierarchy &&
            owner.CanInteract;
        public override bool RequiresCenterAim => true;

        public void Configure(ScavengeCollectible collectible, Transform root)
        {
            owner = collectible;
            visualRoot = root;
        }

        public override void Interact()
        {
            if (owner != null && visualRoot != null)
            {
                owner.TryCollectVisual(visualRoot);
            }
        }
    }
}
