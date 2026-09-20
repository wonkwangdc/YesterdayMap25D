using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using YesterdayMap.Character;
using YesterdayMap.Exploration;
using YesterdayMap.Resources;
using YesterdayMap.Shelter;

namespace YesterdayMap.Core
{
    public sealed class V02RuntimeFlowValidator : MonoBehaviour
    {
        private IEnumerator Start()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-prototypeSmokeTest") < 0) yield break;
            yield return null;
            string scene = SceneManager.GetActiveScene().name;
            UnityEngine.Camera camera = UnityEngine.Camera.main;
            bool cameraValid = camera != null && !camera.orthographic;
            bool requiresPerspective = scene == "Scavenge" || scene == "Shelter";
            Debug.Log($"V02_FLOW_SCENE {scene} perspective={cameraValid}");
            if (requiresPerspective && !cameraValid)
            {
                Debug.LogError($"V02_FLOW_FAIL perspective camera missing in {scene}");
                Application.Quit(1);
                yield break;
            }

            if (scene == "MainMenu") SceneManager.LoadScene("Prologue");
            else if (scene == "Prologue") SceneManager.LoadScene("Scavenge");
            else if (scene == "Scavenge") SceneManager.LoadScene("Shelter");
            else
            {
                int facilities = FindObjectsByType<ShelterObject>(FindObjectsSortMode.None).Length;
                int players = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None).Length;
                bool valid = facilities >= 8 && players == 1;
                PlayerMovement movement = FindFirstObjectByType<PlayerMovement>();
                Vector3 start = movement.transform.position;
                movement.MoveTo(start + Vector3.right * 1.2f, null, 0.1f);
                yield return new WaitForSeconds(0.7f);
                bool moved = movement.transform.position.x > start.x + 0.2f;

                DayCycleManager cycle = FindFirstObjectByType<DayCycleManager>();
                ResourceManager resources = FindFirstObjectByType<ResourceManager>();
                GeneratorObject generator = FindFirstObjectByType<GeneratorObject>();
                RadioObject radio = FindFirstObjectByType<RadioObject>();
                BedObject bed = FindFirstObjectByType<BedObject>();
                WaterPurifierObject purifier = FindFirstObjectByType<WaterPurifierObject>();
                int waterBefore = resources.GetAmount(ResourceType.Water);
                generator.Interact(); radio.Interact(); bed.Interact(); purifier.Interact();
                yield return new WaitForSeconds(2.3f);
                bool facilitiesWorked = resources.GetAmount(ResourceType.Water) > waterBefore;
                cycle.EndDay();
                bool dayAdvanced = cycle.CurrentDay == 2;
                ExplorationManager exploration = FindFirstObjectByType<ExplorationManager>();
                exploration.StartExploration(exploration.GetLocation(0), true);
                bool explored = exploration.ExplorationHistory.Count > 0;
                FindFirstObjectByType<GameManager>().CompleteSurvival(false);
                bool endingShown = Time.timeScale == 0f;
                valid &= moved && facilitiesWorked && dayAdvanced && explored && endingShown;
                Debug.Log(valid
                    ? $"V02_FLOW_TEST_PASS facilities={facilities} players={players} movement={moved} facilityUse={facilitiesWorked} day={cycle.CurrentDay} exploration={explored} ending={endingShown}"
                    : $"V02_FLOW_TEST_FAIL facilities={facilities} players={players} movement={moved} facilityUse={facilitiesWorked} dayAdvanced={dayAdvanced} exploration={explored} ending={endingShown}");
                Application.Quit(valid ? 0 : 1);
            }
        }
    }
}
