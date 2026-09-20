using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace YesterdayMap.Audio
{
    public enum GameSfxCue
    {
        UiClick,
        Pickup,
        UseItem,
        DrinkWater,
        Sleep,
        Ending,
        DiaryOpen,
        DiaryPageTurn
    }

    [RequireComponent(typeof(AudioSource), typeof(AudioListener))]
    public sealed class GameSfxPlayer : MonoBehaviour
    {
        [SerializeField] private AudioClip uiClick;
        [SerializeField] private AudioClip pickup;
        [SerializeField] private AudioClip useItem;
        [SerializeField] private AudioClip drinkWater;
        [SerializeField] private AudioClip sleep;
        [SerializeField] private AudioClip ending;
        [SerializeField] private AudioClip diaryOpen;
        [SerializeField] private AudioClip diaryPageTurn;

        private static GameSfxPlayer instance;
        private AudioSource source;
        private AudioListener fallbackListener;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateRuntimeInstance()
        {
            if (instance != null) return;
            GameObject owner = new("GameSfxPlayer");
            instance = owner.AddComponent<GameSfxPlayer>();
            DontDestroyOnLoad(owner);
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            source = GetComponent<AudioSource>();
            fallbackListener = GetComponent<AudioListener>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = 1f;
            source.ignoreListenerPause = true;
            LoadFallbackClips();
            RefreshFallbackListener();
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void Start()
        {
            BindButtonSounds();
        }

        private void LateUpdate()
        {
            FollowActiveCamera();
        }

        private void OnDestroy()
        {
            if (instance != this) return;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            instance = null;
        }

        public static void Play(GameSfxCue cue)
        {
            if (instance == null) CreateRuntimeInstance();
            instance.PlayCue(cue);
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RefreshFallbackListener();
            BindButtonSounds();
        }

        // MainMenu currently has no camera AudioListener. Use this listener only
        // when the loaded scene does not already provide an enabled one.
        private void RefreshFallbackListener()
        {
            if (fallbackListener == null)
                fallbackListener = GetComponent<AudioListener>();

            bool hasOtherListener = false;
            AudioListener[] listeners = FindObjectsByType<AudioListener>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (AudioListener listener in listeners)
            {
                if (listener != null && listener != fallbackListener &&
                    listener.enabled && listener.gameObject.activeInHierarchy)
                {
                    hasOtherListener = true;
                    break;
                }
            }

            fallbackListener.enabled = !hasOtherListener;
            FollowActiveCamera();
        }

        private void FollowActiveCamera()
        {
            if (fallbackListener == null || !fallbackListener.enabled)
            {
                return;
            }

            Camera activeCamera = Camera.main;
            if (activeCamera == null || !activeCamera.isActiveAndEnabled)
            {
                return;
            }

            transform.SetPositionAndRotation(
                activeCamera.transform.position,
                activeCamera.transform.rotation);
        }

        private void BindButtonSounds()
        {
            Button[] buttons = FindObjectsByType<Button>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (Button button in buttons)
            {
                if (button == null) continue;
                button.onClick.RemoveListener(PlayUiClick);
                button.onClick.AddListener(PlayUiClick);
            }
        }

        private static void PlayUiClick()
        {
            Play(GameSfxCue.UiClick);
        }

        private void PlayCue(GameSfxCue cue)
        {
            if (source == null) source = GetComponent<AudioSource>();
            AudioClip clip = cue switch
            {
                GameSfxCue.UiClick => uiClick,
                GameSfxCue.Pickup => pickup,
                GameSfxCue.UseItem => useItem,
                GameSfxCue.DrinkWater => drinkWater,
                GameSfxCue.Sleep => sleep,
                GameSfxCue.Ending => ending,
                GameSfxCue.DiaryOpen => diaryOpen,
                GameSfxCue.DiaryPageTurn => diaryPageTurn,
                _ => null
            };

            if (clip != null) source.PlayOneShot(clip);
        }

        private void LoadFallbackClips()
        {
            uiClick ??= UnityEngine.Resources.Load<AudioClip>("Audio/SFX/UI_Click_290204");
            pickup ??= UnityEngine.Resources.Load<AudioClip>(
                "Audio/SFX/Item_Pickup_37089");
            useItem ??= UnityEngine.Resources.Load<AudioClip>("Audio/SFX/Item_Use");
            drinkWater ??= UnityEngine.Resources.Load<AudioClip>("Audio/SFX/Water_Drink");
            sleep ??= UnityEngine.Resources.Load<AudioClip>("Audio/SFX/Sleep_Transition");
            ending ??= UnityEngine.Resources.Load<AudioClip>("Audio/SFX/Ending_Sting");
            diaryOpen ??= UnityEngine.Resources.Load<AudioClip>("Audio/SFX/Diary_Open");
            diaryPageTurn ??= UnityEngine.Resources.Load<AudioClip>("Audio/SFX/Diary_PageTurn");
        }
    }
}
