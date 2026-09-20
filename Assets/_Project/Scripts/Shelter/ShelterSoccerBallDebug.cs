using UnityEngine;
using YesterdayMap.Character;

namespace YesterdayMap.Shelter
{
    /// <summary>
    /// Spawns or recalls the shelter soccer ball for administrator-mode tests.
    /// </summary>
    public static class ShelterSoccerBallDebug
    {
        private const string ShelterBallName = "Collected_SoccerBall";
        private const float SpawnDistance = 1.45f;
        private const float SpawnHeight = 0.3f;

        public static bool SpawnOrReset()
        {
            GameObject ball = FindShelterBall();
            if (ball == null)
            {
                return false;
            }

            PlayerInteraction player = Object.FindFirstObjectByType<PlayerInteraction>(
                FindObjectsInactive.Exclude);
            Vector3 forward = player != null
                ? player.transform.forward
                : Vector3.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f)
            {
                forward = Vector3.forward;
            }
            forward.Normalize();

            Vector3 visualTarget = player != null
                ? player.transform.position + forward * SpawnDistance
                : new Vector3(0f, SpawnHeight, -2f);
            visualTarget.y = SpawnHeight;

            ball.SetActive(true);
            ball.transform.position = visualTarget + new Vector3(0f, 0f, -0.225f);
            ball.transform.rotation = Quaternion.identity;

            SoccerBallKickObject.Ensure(ball);
            Rigidbody body = ball.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.position = ball.transform.position;
                body.rotation = ball.transform.rotation;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.WakeUp();
            }

            return true;
        }

        private static GameObject FindShelterBall()
        {
            foreach (GameObject candidate in
                     UnityEngine.Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (candidate.name == ShelterBallName &&
                    candidate.scene.IsValid())
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
