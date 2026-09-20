using System;
using YesterdayMap.Character;
using YesterdayMap.Audio;
using YesterdayMap.Core;
using YesterdayMap.Events;
using YesterdayMap.UI;

namespace YesterdayMap.Shelter
{
    public sealed class BedObject : ShelterObject
    {
        [UnityEngine.SerializeField] private CharacterStats stats;
        [UnityEngine.SerializeField] private DayCycleManager dayCycle;
        [UnityEngine.SerializeField] private SceneFader sceneFader;
        [UnityEngine.SerializeField] private SleepConfirmationUI sleepConfirmation;
        [UnityEngine.SerializeField] private SleepTransitionView sleepTransition;
        [UnityEngine.SerializeField] private ShelterEventDialogueController eventDialogue;
        [UnityEngine.SerializeField] private SleepCinematicController sleepCinematic;
        [UnityEngine.SerializeField] private LastBunkerEndingSequence lastBunkerEndingSequence;
        private bool sleeping;
        private bool playLastBunkerEndingAfterSleep;

        public override string InteractionPrompt => "[E] 잠자기";

        public void Configure(CharacterStats characterStats, DayCycleManager cycle)
        {
            stats = characterStats;
            dayCycle = cycle;
        }

        public override void Interact()
        {
            if (RejectIfBroken()) return;
            if (sleeping) return;

            ResolveReferences();
            DayOneTutorialController tutorial = DayOneTutorialController.Instance;
            if (tutorial != null && tutorial.TryBlockSleep())
            {
                return;
            }

            if (eventDialogue != null &&
                eventDialogue.TryShowQuarter3FinalSleepBlockDialogue())
            {
                return;
            }

            if (eventDialogue != null &&
                eventDialogue.TryGetLastBunkerSleepDelayReason(
                    out string lastBunkerDelayReason))
            {
                if (sleepConfirmation != null)
                {
                    sleepConfirmation.Open(
                        ContinueSleepAfterProgressWarning,
                        $"{lastBunkerDelayReason}\n\n" +
                        "지금 잠들면 마지막 벙커 이야기는 현재 단계에 머뭅니다.",
                        "잠자기",
                        "취소");
                }
                else
                {
                    Ui?.ShowMessage(lastBunkerDelayReason);
                    BeginSleep();
                }

                return;
            }

            if (sleepConfirmation != null)
            {
                if (eventDialogue != null &&
                    eventDialogue.IsQuarter3ClueActionPendingBeforeSleep)
                {
                    sleepConfirmation.Open(
                        ContinueSleepAfterClueWarning,
                        "잠을 자면 단서를 얻으실 수 없습니다. 주무시겠습니까?",
                        "수락",
                        "거절");
                    return;
                }

                if (stats != null && stats.RequiresFatalSleepConfirmation)
                {
                    sleepConfirmation.Open(
                        BeginFatalSleep,
                        "현재 체력이 0인 빈사 상태입니다. 회복하지 않고 잠들면 사망합니다. 그래도 잠을 자시겠습니까?");
                }
                else
                {
                    if (tutorial != null && tutorial.ConsumeFirstSleepGuide())
                    {
                        sleepConfirmation.Open(
                            BeginSleep,
                            "잠들면 오늘이 종료되고 다음 날 아침으로 넘어갑니다.",
                            "취침",
                            "취소");
                    }
                    else
                    {
                        sleepConfirmation.Open(BeginSleep);
                    }
                }
                return;
            }

            if (eventDialogue != null &&
                eventDialogue.IsQuarter3ClueActionPendingBeforeSleep)
            {
                Ui?.ShowMessage(
                    "잠을 자면 단서를 얻으실 수 없습니다. 수면 확인 UI를 찾을 수 없어 잠을 진행할 수 없습니다.");
                return;
            }

            Ui.ShowMessage("잠을 자시겠습니까? 확인 UI를 찾을 수 없어 바로 수면을 진행합니다.");
            BeginSleep();
        }

        private void ContinueSleepAfterClueWarning()
        {
            ContinueSleepAfterProgressWarning();
        }

        private void ContinueSleepAfterProgressWarning()
        {
            if (stats != null &&
                stats.RequiresFatalSleepConfirmation &&
                sleepConfirmation != null)
            {
                sleepConfirmation.Open(
                    BeginFatalSleep,
                    "현재 체력이 0인 빈사 상태입니다. 회복하지 않고 잠들면 사망합니다. 그래도 잠을 자시겠습니까?");
                return;
            }

            BeginSleep();
        }

        private void BeginFatalSleep()
        {
            if (sleeping) return;

            ResolveReferences();
            sleeping = true;
            GameSfxPlayer.Play(GameSfxCue.Sleep);
            PlaySleepSequence(ResolveFatalSleep);
        }

        private void ResolveFatalSleep()
        {
            stats?.ResolveFatalSleep();
            sleeping = false;
        }

        private void BeginSleep()
        {
            if (sleeping) return;

            ResolveReferences();
            sleeping = true;
            GameSfxPlayer.Play(GameSfxCue.Sleep);
            playLastBunkerEndingAfterSleep =
                eventDialogue != null &&
                eventDialogue.IsReadyForLastBunkerEndingSleep;
            eventDialogue?.PrepareLastBunkerSleep();

            PlaySleepSequence(AdvanceDayAfterRest);
        }

        private void PlaySleepSequence(Action atComplete)
        {
            Action beginLoading = () => PlaySleepTransition(() =>
            {
                sleepCinematic?.RestoreAfterTransition();
                atComplete?.Invoke();
            });

            if (sleepCinematic != null && sleepCinematic.Play(beginLoading))
                return;

            beginLoading();
        }

        private void PlaySleepTransition(Action atComplete)
        {
            if (sleepTransition == null)
            {
                sleepTransition = UnityEngine.Object.FindFirstObjectByType<SleepTransitionView>(
                    UnityEngine.FindObjectsInactive.Include);
            }

            if (sleepTransition == null)
            {
                SleepTransitionView prefab =
                    UnityEngine.Resources.Load<SleepTransitionView>(
                        "UI/Sleep/SleepTransition");
                if (prefab != null)
                    sleepTransition = UnityEngine.Object.Instantiate(prefab);
            }

            if (sleepTransition != null && sleepTransition.Play(atComplete))
                return;

            if (sceneFader != null)
            {
                sceneFader.FadeOutAndBack(atComplete);
                return;
            }

            atComplete?.Invoke();
        }

        private void AdvanceDayAfterRest()
        {
            stats?.ApplySleepNeeds();
            dayCycle?.EndDay();
            stats?.ModifyMorale(5f);

            if (playLastBunkerEndingAfterSleep &&
                stats != null && stats.Health > 0f &&
                lastBunkerEndingSequence != null)
            {
                lastBunkerEndingSequence.Play();
            }

            playLastBunkerEndingAfterSleep = false;
            sleeping = false;
        }

        private void ResolveReferences()
        {
            if (stats == null) stats = UnityEngine.Object.FindFirstObjectByType<CharacterStats>();
            if (dayCycle == null) dayCycle = UnityEngine.Object.FindFirstObjectByType<DayCycleManager>();
            if (sceneFader == null)
            {
                sceneFader = UnityEngine.Object.FindFirstObjectByType<SceneFader>(
                    UnityEngine.FindObjectsInactive.Include);
            }

            if (sleepConfirmation == null)
            {
                sleepConfirmation = UnityEngine.Object.FindFirstObjectByType<SleepConfirmationUI>(
                    UnityEngine.FindObjectsInactive.Include);
            }

            if (eventDialogue == null)
            {
                eventDialogue =
                    UnityEngine.Object.FindFirstObjectByType<ShelterEventDialogueController>(
                        UnityEngine.FindObjectsInactive.Include);
            }

            if (sleepCinematic == null)
            {
                sleepCinematic = GetComponent<SleepCinematicController>();
            }

            if (lastBunkerEndingSequence == null)
            {
                lastBunkerEndingSequence =
                    UnityEngine.Object.FindFirstObjectByType<LastBunkerEndingSequence>(
                        UnityEngine.FindObjectsInactive.Include);
            }
        }
    }
}
