using System;
using UnityEngine;
using YesterdayMap.Core;
using YesterdayMap.Scavenge;

namespace YesterdayMap.Shelter
{
    // Shows the physical shelter prop after its matching scavenged item was deposited.
    public sealed class ShelterCollectedItemDisplay : MonoBehaviour
    {
        [Serializable]
        private struct DisplayEntry
        {
            [SerializeField] private ScavengeItemKind itemKind;
            [SerializeField] private GameObject visual;

            public ScavengeItemKind ItemKind => itemKind;
            public GameObject Visual => visual;
        }

        [SerializeField] private DisplayEntry[] displays = Array.Empty<DisplayEntry>();
        [SerializeField] private GameObject fuelPlacementPreview;

        public int VisibleItemCount { get; private set; }

        private void Awake()
        {
            if (Application.isPlaying && fuelPlacementPreview != null)
            {
                fuelPlacementPreview.SetActive(false);
            }
        }

        private void Start()
        {
            Refresh();
        }

        public void Refresh()
        {
            GameSession session = GameSession.Instance;
            VisibleItemCount = 0;

            foreach (DisplayEntry entry in displays)
            {
                if (entry.Visual == null) continue;

                bool shouldShow = session != null &&
                    entry.ItemKind != ScavengeItemKind.Resource &&
                    session.GetCollected(entry.ItemKind) > 0;
                entry.Visual.SetActive(shouldShow);
                if (!shouldShow) continue;

                if (entry.ItemKind == ScavengeItemKind.SoccerBall)
                {
                    SoccerBallKickObject.Ensure(entry.Visual);
                }

                VisibleItemCount++;
            }
        }
    }
}
