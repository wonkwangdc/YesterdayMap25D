using System;
using System.Collections.Generic;
using UnityEngine;
using YesterdayMap.Core;

namespace YesterdayMap.Shelter
{
    [DisallowMultipleComponent]
    public sealed class GeneratorPowerManager : MonoBehaviour
    {
        public enum PowerState
        {
            Off,
            Critical,
            Stable
        }

        private enum GeneratorAudioPhase
        {
            Off,
            Startup,
            Running,
            Shutdown
        }

        private enum PowerTransitionPhase
        {
            None,
            Starting,
            Stopping
        }

        private const float StartupPowerTransitionDuration = 1.6f;
        private const float ShutdownPowerTransitionDuration = 1.1f;
        private static readonly Vector2 PowerTransitionFlickerInterval =
            new(0.045f, 0.14f);

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [Header("Generator Power")]
        [SerializeField, Range(0f, 100f)] private float powerPercent = 75f;
        [SerializeField, Range(1f, 30f)] private float flickerThreshold = 30f;
        [SerializeField] private bool isRunning = true;
        [SerializeField, Range(0f, 100f)] private float dailyFuelConsumption = 10f;

        [Header("Scene References")]
        [SerializeField] private GeneratorObject generator;
        [SerializeField] private Transform wallLightsRoot;
        [SerializeField] private GameObject originalWallLamp;
        [SerializeField] private GameObject tableLantern;

        [Header("Generator Audio")]
        [SerializeField] private AudioClip startupClip;
        [SerializeField] private AudioClip runningLoop;
        [SerializeField] private AudioClip shutdownClip;
        [SerializeField, Range(0f, 1f)] private float runningLoopVolume = 1f;
        [SerializeField, Min(0.1f)] private float audioMinDistance = 3f;
        [SerializeField, Min(1f)] private float audioMaxDistance = 14f;

        [Header("Generator Room Audio Isolation")]
        [SerializeField] private Vector2 generatorRoomMinXZ = new(0.15f, 1.45f);
        [SerializeField] private Vector2 generatorRoomMaxXZ = new(11.1f, 13.65f);
        [SerializeField, Min(0.05f)] private float roomAudioFadeDuration = 0.18f;

        [Header("Critical Power Flicker")]
        [SerializeField] private Vector2 lightOnDuration = new Vector2(0.05f, 0.18f);
        [SerializeField] private Vector2 lightOffDuration = new Vector2(0.025f, 0.11f);
        [SerializeField, Range(1f, 5f)] private float flickerIntervalMultiplier = 5f;
        [SerializeField, Range(0.1f, 1f)] private float minimumFlickerBrightness = 0.3f;

        [Header("Power-Off Blackout")]
        [SerializeField, Range(0f, 0.2f)] private float blackoutAmbientMultiplier = 0.025f;
        [SerializeField, Range(0f, 0.2f)] private float blackoutReflectionMultiplier = 0.05f;

        private readonly List<Light> controlledLights = new List<Light>();
        private readonly List<float> baseIntensities = new List<float>();
        private readonly List<Renderer> emissiveRenderers = new List<Renderer>();
        private readonly List<Color> baseEmissionColors = new List<Color>();

        private MaterialPropertyBlock propertyBlock;
        private bool flickerOn = true;
        private float flickerMultiplier = 1f;
        private float nextFlickerTime;
        private PowerState appliedState = (PowerState)(-1);
        private PowerTransitionPhase powerTransitionPhase;
        private float powerTransitionStartTime;
        private float nextPowerTransitionFlickerTime;
        private float powerTransitionBrightness;
        private float baseAmbientIntensity;
        private float baseReflectionIntensity;
        private bool environmentLightingCaptured;
        private AudioSource generatorAudioSource;
        private AudioClip runtimeRunningLoop;
        private GeneratorAudioPhase generatorAudioPhase;
        private Transform generatorAudioListener;
        private float generatorRoomVolumeMultiplier;
        private DayCycleManager subscribedDayCycle;

        public event Action<float> PowerChanged;

        public float PowerPercent => powerPercent;
        public float FlickerThreshold => flickerThreshold;
        public bool IsRunning => isRunning && powerPercent > 0f;
        public PowerState CurrentState =>
            !IsRunning
                ? PowerState.Off
                : powerPercent <= flickerThreshold
                    ? PowerState.Critical
                    : PowerState.Stable;

        public bool HasPower => IsRunning;
        public GeneratorObject Generator => generator;

        private void Awake()
        {
            if (powerPercent <= 0f)
            {
                isRunning = false;
            }
            CaptureEnvironmentLighting();
            RefreshControlledLights();
            UpdateGeneratorRoomAudibility(true);
            EnsureGeneratorAudio();
            ResetFlicker();
            ApplyCurrentState(true);
        }

        private void OnEnable()
        {
            GameSettings.ReduceFlashingChanged -= HandleReduceFlashingChanged;
            GameSettings.ReduceFlashingChanged += HandleReduceFlashingChanged;
            SubscribeDayCycle();
            powerTransitionPhase = PowerTransitionPhase.None;
            CaptureEnvironmentLighting();
            UpdateGeneratorRoomAudibility(true);
            EnsureGeneratorAudio();
            if (Application.isPlaying && controlledLights.Count == 0)
            {
                RefreshControlledLights();
            }

            ResetFlicker();
            ApplyCurrentState(true);
        }

        private void Update()
        {
            UpdateGeneratorRoomAudibility(false);
            UpdateGeneratorAudioTimeline();
            if (UpdatePowerTransition())
            {
                return;
            }

            if (CurrentState != PowerState.Critical)
            {
                if (appliedState != CurrentState)
                {
                    ApplyCurrentState(true);
                }

                return;
            }

            if (GameSettings.ReduceFlashing)
            {
                if (appliedState != PowerState.Critical)
                    ApplyCurrentState(true);
                return;
            }

            float now = Time.unscaledTime;
            if (appliedState != PowerState.Critical || now >= nextFlickerTime)
            {
                flickerOn = !flickerOn;

                if (flickerOn)
                {
                    float powerRatio = Mathf.Clamp01(powerPercent / flickerThreshold);
                    float maximumBrightness = Mathf.Lerp(0.55f, 0.9f, powerRatio);
                    flickerMultiplier = UnityEngine.Random.Range(
                        minimumFlickerBrightness,
                        maximumBrightness);
                    nextFlickerTime = now +
                        RandomDuration(lightOnDuration) * flickerIntervalMultiplier;
                }
                else
                {
                    flickerMultiplier = 0f;
                    nextFlickerTime = now +
                        RandomDuration(lightOffDuration) * flickerIntervalMultiplier;
                }

                ApplyCurrentState(true);
            }
        }

        private void OnDisable()
        {
            GameSettings.ReduceFlashingChanged -= HandleReduceFlashingChanged;
            UnsubscribeDayCycle();
            powerTransitionPhase = PowerTransitionPhase.None;

            if (!Application.isPlaying)
            {
                return;
            }

            ApplyLightMultiplier(1f);
            RestoreEnvironmentLighting();
            StopGeneratorAudio();
        }

        private void OnValidate()
        {
            powerPercent = Mathf.Clamp(powerPercent, 0f, 100f);
            dailyFuelConsumption = Mathf.Clamp(dailyFuelConsumption, 0f, 100f);
            flickerThreshold = Mathf.Clamp(flickerThreshold, 1f, 30f);
            lightOnDuration = ValidateDuration(lightOnDuration);
            lightOffDuration = ValidateDuration(lightOffDuration);
            flickerIntervalMultiplier = Mathf.Clamp(flickerIntervalMultiplier, 1f, 5f);
            blackoutAmbientMultiplier = Mathf.Clamp(blackoutAmbientMultiplier, 0f, 0.2f);
            blackoutReflectionMultiplier = Mathf.Clamp(blackoutReflectionMultiplier, 0f, 0.2f);
            runningLoopVolume = Mathf.Clamp01(runningLoopVolume);
            audioMinDistance = Mathf.Max(0.1f, audioMinDistance);
            audioMaxDistance = Mathf.Max(audioMinDistance, audioMaxDistance);
            generatorRoomMaxXZ = new Vector2(
                Mathf.Max(generatorRoomMinXZ.x, generatorRoomMaxXZ.x),
                Mathf.Max(generatorRoomMinXZ.y, generatorRoomMaxXZ.y));
            roomAudioFadeDuration = Mathf.Max(0.05f, roomAudioFadeDuration);
        }

        public void SetPowerPercent(float value)
        {
            float clamped = Mathf.Clamp(value, 0f, 100f);
            if (Mathf.Approximately(powerPercent, clamped))
            {
                ApplyCurrentState(true);
                return;
            }

            bool wasRunning = isRunning && powerPercent > 0f;
            powerPercent = clamped;
            bool transitionStarted = false;
            if (powerPercent <= 0f)
            {
                isRunning = false;
                if (Application.isPlaying &&
                    (wasRunning ||
                     powerTransitionPhase == PowerTransitionPhase.Starting))
                {
                    BeginPowerTransition(false);
                    transitionStarted = true;
                }
            }

            if (!transitionStarted)
            {
                ResetFlicker();
                ApplyCurrentState(true);
            }

            PowerChanged?.Invoke(powerPercent);
        }

        public bool SetRunning(bool value)
        {
            if (value && powerPercent <= 0f)
            {
                return false;
            }

            if (isRunning == value)
            {
                ApplyCurrentState(true);
                return true;
            }

            isRunning = value;
            if (Application.isPlaying)
            {
                BeginPowerTransition(value);
            }
            else
            {
                ResetFlicker();
                ApplyCurrentState(true);
            }

            PowerChanged?.Invoke(powerPercent);
            return true;
        }

        public void AddPower(float amount)
        {
            SetPowerPercent(powerPercent + amount);
        }

        public bool TryConsumePower(float amount)
        {
            amount = Mathf.Max(0f, amount);
            if (powerPercent < amount)
            {
                return false;
            }

            SetPowerPercent(powerPercent - amount);
            return true;
        }

        public GeneratorPowerSaveData CaptureState()
        {
            return new GeneratorPowerSaveData
            {
                powerPercent = powerPercent,
                isRunning = isRunning
            };
        }

        public void RestoreState(GeneratorPowerSaveData data)
        {
            if (data == null)
            {
                return;
            }

            powerTransitionPhase = PowerTransitionPhase.None;
            powerPercent = Mathf.Clamp(data.powerPercent, 0f, 100f);
            isRunning = data.isRunning && powerPercent > 0f;
            ResetFlicker();
            StopGeneratorAudio();
            ApplyCurrentState(true);
            PowerChanged?.Invoke(powerPercent);
        }

        private void SubscribeDayCycle()
        {
            DayCycleManager next = FindFirstObjectByType<DayCycleManager>();
            if (subscribedDayCycle == next)
            {
                return;
            }

            UnsubscribeDayCycle();
            subscribedDayCycle = next;
            if (subscribedDayCycle != null)
            {
                subscribedDayCycle.DayAdvanced += HandleDayAdvanced;
            }
        }

        private void UnsubscribeDayCycle()
        {
            if (subscribedDayCycle != null)
            {
                subscribedDayCycle.DayAdvanced -= HandleDayAdvanced;
            }

            subscribedDayCycle = null;
        }

        private void HandleDayAdvanced()
        {
            if (!IsRunning || dailyFuelConsumption <= 0f)
            {
                return;
            }

            SetPowerPercent(powerPercent - dailyFuelConsumption);
        }

        public void RefreshControlledLights()
        {
            ResolveSceneReferences();

            controlledLights.Clear();
            baseIntensities.Clear();
            emissiveRenderers.Clear();
            baseEmissionColors.Clear();
            propertyBlock ??= new MaterialPropertyBlock();

            CollectLamp(originalWallLamp);
            CollectLamp(tableLantern);
            if (wallLightsRoot != null)
            {
                CollectLamp(wallLightsRoot.gameObject);
            }
        }

        [ContextMenu("TEST/Generator Power 0%")]
        public void TestPowerOff()
        {
            SetPowerPercent(0f);
        }

        [ContextMenu("TEST/Generator Power 20%")]
        public void TestPowerCritical()
        {
            SetPowerPercent(20f);
            SetRunning(true);
        }

        [ContextMenu("TEST/Generator Power 100%")]
        public void TestPowerFull()
        {
            SetPowerPercent(100f);
            SetRunning(true);
        }

        private void ResolveSceneReferences()
        {
            if (generator == null)
            {
                generator = FindFirstObjectByType<GeneratorObject>();
            }

            if (wallLightsRoot == null)
            {
                GameObject root = GameObject.Find("BunkerWallLights");
                wallLightsRoot = root != null ? root.transform : null;
            }

            if (originalWallLamp == null)
            {
                originalWallLamp = GameObject.Find(
                    "BunkerFurniture/BunkerFurniture_AmberLantern");
            }

            if (tableLantern == null)
            {
                tableLantern = GameObject.Find(
                    "BunkerFurniture/BunkerFurniture_TableLantern");
            }
        }

        private float CurrentGeneratorAudioVolume =>
            Mathf.Clamp01(runningLoopVolume) * generatorRoomVolumeMultiplier;

        private void UpdateGeneratorRoomAudibility(bool immediate)
        {
            ResolveGeneratorAudioListener();

            float targetMultiplier = 0f;
            if (generatorAudioListener != null)
            {
                Vector3 listenerPosition = generatorAudioListener.position;
                bool insideMachineRoom =
                    listenerPosition.x >= generatorRoomMinXZ.x &&
                    listenerPosition.x <= generatorRoomMaxXZ.x &&
                    listenerPosition.z >= generatorRoomMinXZ.y &&
                    listenerPosition.z <= generatorRoomMaxXZ.y;
                targetMultiplier = insideMachineRoom ? 1f : 0f;
            }

            generatorRoomVolumeMultiplier = immediate
                ? targetMultiplier
                : Mathf.MoveTowards(
                    generatorRoomVolumeMultiplier,
                    targetMultiplier,
                    Time.unscaledDeltaTime / roomAudioFadeDuration);

            if (generatorAudioSource != null)
            {
                generatorAudioSource.volume = CurrentGeneratorAudioVolume;
            }
        }

        private void ResolveGeneratorAudioListener()
        {
            if (generatorAudioListener != null)
            {
                return;
            }

            GameObject player = GameObject.Find("HanDoyoon");
            if (player != null)
            {
                generatorAudioListener = player.transform;
                return;
            }

            Camera mainCamera = Camera.main;
            generatorAudioListener = mainCamera != null
                ? mainCamera.transform
                : null;
        }

        private void CollectLamp(GameObject lampRoot)
        {
            if (lampRoot == null)
            {
                return;
            }

            foreach (Light sceneLight in lampRoot.GetComponentsInChildren<Light>(true))
            {
                if (sceneLight == null || controlledLights.Contains(sceneLight))
                {
                    continue;
                }

                controlledLights.Add(sceneLight);
                baseIntensities.Add(Mathf.Max(0f, sceneLight.intensity));
            }

            foreach (Renderer sceneRenderer in lampRoot.GetComponentsInChildren<Renderer>(true))
            {
                if (sceneRenderer == null || emissiveRenderers.Contains(sceneRenderer))
                {
                    continue;
                }

                Material material = sceneRenderer.sharedMaterial;
                if (material == null || !material.HasProperty(EmissionColorId))
                {
                    continue;
                }

                emissiveRenderers.Add(sceneRenderer);
                baseEmissionColors.Add(material.GetColor(EmissionColorId));
            }
        }

        private void ResetFlicker()
        {
            flickerOn = false;
            flickerMultiplier = 0f;
            nextFlickerTime = Time.unscaledTime;
            appliedState = (PowerState)(-1);
        }

        private void ApplyCurrentState(bool force)
        {
            if (powerTransitionPhase != PowerTransitionPhase.None)
            {
                SyncGeneratorAudio();
                return;
            }

            PowerState state = CurrentState;
            if (!force && appliedState == state)
            {
                return;
            }

            switch (state)
            {
                case PowerState.Off:
                    ApplyEnvironmentBlackout();
                    ApplyLightMultiplier(0f);
                    break;

                case PowerState.Critical:
                    float criticalMultiplier = GameSettings.ReduceFlashing
                        ? Mathf.Lerp(
                            0.55f,
                            0.8f,
                            Mathf.Clamp01(powerPercent / flickerThreshold))
                        : flickerOn ? flickerMultiplier : 0f;
                    ApplyEnvironmentFlicker(criticalMultiplier);
                    ApplyLightMultiplier(criticalMultiplier);
                    break;

                default:
                    RestoreEnvironmentLighting();
                    float stableRatio = Mathf.InverseLerp(
                        flickerThreshold,
                        100f,
                        powerPercent);
                    ApplyLightMultiplier(Mathf.Lerp(0.82f, 1f, stableRatio));
                    break;
            }

            appliedState = state;
            SyncGeneratorAudio();
        }

        private void EnsureGeneratorAudio()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            ResolveSceneReferences();
            if (generator == null)
            {
                generatorAudioSource = null;
                return;
            }

            Transform audioRoot = generator.transform.Find(
                "__GeneratorRunningAudio");
            if (audioRoot == null)
            {
                GameObject owner = new("__GeneratorRunningAudio");
                owner.transform.SetParent(generator.transform, false);
                audioRoot = owner.transform;
            }

            // Always reacquire the component from the live GameObject. This
            // also repairs helpers left behind after a play-mode/domain reload.
            if (!audioRoot.TryGetComponent(out AudioSource liveSource))
            {
                liveSource = audioRoot.gameObject.AddComponent<AudioSource>();
            }

            generatorAudioSource = liveSource;
            if (generatorAudioSource == null)
            {
                return;
            }

            if (startupClip == null)
            {
                startupClip = UnityEngine.Resources.Load<AudioClip>(
                    "Audio/SFX/Generator_Startup");
            }

            if (runningLoop == null)
            {
                runningLoop = UnityEngine.Resources.Load<AudioClip>(
                    "Audio/SFX/Generator_RunningLoop");
            }

            if (shutdownClip == null)
            {
                shutdownClip = UnityEngine.Resources.Load<AudioClip>(
                    "Audio/SFX/Generator_Shutdown");
            }
            if (runningLoop == null)
            {
                if (runtimeRunningLoop == null)
                {
                    runtimeRunningLoop = CreateGeneratorLoopClip();
                }

                runningLoop = runtimeRunningLoop;
            }

            generatorAudioSource.playOnAwake = false;
            generatorAudioSource.spatialBlend = 1f;
            generatorAudioSource.rolloffMode = AudioRolloffMode.Linear;
            generatorAudioSource.minDistance = audioMinDistance;
            generatorAudioSource.maxDistance = Mathf.Max(
                audioMinDistance,
                audioMaxDistance);
            generatorAudioSource.dopplerLevel = 0f;
        }

        private void SyncGeneratorAudio()
        {
            EnsureGeneratorAudio();
            AudioSource liveSource = generatorAudioSource;
            if (liveSource == null)
            {
                return;
            }

            liveSource.volume = CurrentGeneratorAudioVolume;
            if (!HasPower)
            {
                if (generatorAudioPhase == GeneratorAudioPhase.Startup ||
                    generatorAudioPhase == GeneratorAudioPhase.Running)
                {
                    PlayGeneratorShutdown(liveSource);
                }

                return;
            }

            switch (generatorAudioPhase)
            {
                case GeneratorAudioPhase.Startup:
                    if (!liveSource.isPlaying)
                    {
                        PlayGeneratorRunningLoop(liveSource);
                    }
                    break;

                case GeneratorAudioPhase.Running:
                    if (liveSource.clip != runningLoop ||
                        !liveSource.isPlaying)
                    {
                        PlayGeneratorRunningLoop(liveSource);
                    }
                    break;

                default:
                    PlayGeneratorStartup(liveSource);
                    break;
            }
        }

        private void StopGeneratorAudio()
        {
            generatorAudioPhase = GeneratorAudioPhase.Off;
            if (generator == null)
            {
                generatorAudioSource = null;
                return;
            }

            Transform audioRoot = generator.transform.Find(
                "__GeneratorRunningAudio");
            if (audioRoot == null ||
                !audioRoot.TryGetComponent(out AudioSource liveSource))
            {
                generatorAudioSource = null;
                return;
            }

            generatorAudioSource = liveSource;
            liveSource.Stop();
            liveSource.loop = false;
            liveSource.clip = null;
        }

        private static AudioClip CreateGeneratorLoopClip()
        {
            const int sampleRate = 22050;
            const int durationSeconds = 2;
            int sampleCount = sampleRate * durationSeconds;
            float[] samples = new float[sampleCount];
            for (int index = 0; index < sampleCount; index++)
            {
                float time = index / (float)sampleRate;
                float mechanicalPulse = 0.82f +
                                        0.18f * Mathf.Sin(
                                            2f * Mathf.PI * 8f * time);
                float sample =
                    0.38f * Mathf.Sin(2f * Mathf.PI * 48f * time) +
                    0.18f * Mathf.Sin(2f * Mathf.PI * 96f * time) +
                    0.08f * Mathf.Sin(2f * Mathf.PI * 144f * time) +
                    0.035f * Mathf.Sin(2f * Mathf.PI * 240f * time);
                samples[index] = Mathf.Clamp(sample * mechanicalPulse, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create(
                "Generator_RunningLoop_Runtime",
                sampleCount,
                1,
                sampleRate,
                false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void OnDestroy()
        {
            if (runtimeRunningLoop != null)
            {
                Destroy(runtimeRunningLoop);
                runtimeRunningLoop = null;
            }
        }

        private void HandleReduceFlashingChanged(bool _)
        {
            ResetFlicker();
            ApplyCurrentState(true);
        }

        private void CaptureEnvironmentLighting()
        {
            if (environmentLightingCaptured)
            {
                return;
            }

            baseAmbientIntensity = RenderSettings.ambientIntensity;
            baseReflectionIntensity = RenderSettings.reflectionIntensity;
            environmentLightingCaptured = true;
        }

        private void ApplyEnvironmentBlackout()
        {
            CaptureEnvironmentLighting();
            RenderSettings.ambientIntensity =
                baseAmbientIntensity * blackoutAmbientMultiplier;
            RenderSettings.reflectionIntensity =
                baseReflectionIntensity * blackoutReflectionMultiplier;
        }

        private void ApplyEnvironmentFlicker(float multiplier)
        {
            CaptureEnvironmentLighting();
            multiplier = Mathf.Clamp01(multiplier);

            RenderSettings.ambientIntensity = baseAmbientIntensity * Mathf.Lerp(
                blackoutAmbientMultiplier,
                1f,
                multiplier);
            RenderSettings.reflectionIntensity = baseReflectionIntensity * Mathf.Lerp(
                blackoutReflectionMultiplier,
                1f,
                multiplier);
        }

        private void RestoreEnvironmentLighting()
        {
            if (!environmentLightingCaptured)
            {
                return;
            }

            RenderSettings.ambientIntensity = baseAmbientIntensity;
            RenderSettings.reflectionIntensity = baseReflectionIntensity;
        }

        private void ApplyLightMultiplier(float multiplier)
        {
            multiplier = Mathf.Clamp01(multiplier);

            for (int i = 0; i < controlledLights.Count; i++)
            {
                Light sceneLight = controlledLights[i];
                if (sceneLight == null)
                {
                    continue;
                }

                sceneLight.enabled = multiplier > 0.001f;
                sceneLight.intensity = baseIntensities[i] * multiplier;
            }

            propertyBlock ??= new MaterialPropertyBlock();
            for (int i = 0; i < emissiveRenderers.Count; i++)
            {
                Renderer sceneRenderer = emissiveRenderers[i];
                if (sceneRenderer == null)
                {
                    continue;
                }

                sceneRenderer.GetPropertyBlock(propertyBlock);
                Color emission = baseEmissionColors[i] * multiplier;
                emission.a = baseEmissionColors[i].a;
                propertyBlock.SetColor(EmissionColorId, emission);
                sceneRenderer.SetPropertyBlock(propertyBlock);
            }
        }

        private static float RandomDuration(Vector2 duration)
        {
            return UnityEngine.Random.Range(duration.x, duration.y);
        }

        private static Vector2 ValidateDuration(Vector2 duration)
        {
            duration.x = Mathf.Max(0.01f, duration.x);
            duration.y = Mathf.Max(duration.x, duration.y);
            return duration;
        }


        private void PlayGeneratorShutdown(AudioSource liveSource)
        {
            liveSource.Stop();
            liveSource.loop = false;
            liveSource.pitch = 1f;
            liveSource.volume = CurrentGeneratorAudioVolume;
            if (shutdownClip == null)
            {
                liveSource.clip = null;
                generatorAudioPhase = GeneratorAudioPhase.Off;
                return;
            }

            liveSource.clip = shutdownClip;
            liveSource.time = 0f;
            liveSource.Play();
            generatorAudioPhase = GeneratorAudioPhase.Shutdown;
        }


        private void PlayGeneratorRunningLoop(AudioSource liveSource)
        {
            if (runningLoop == null)
            {
                generatorAudioPhase = GeneratorAudioPhase.Off;
                return;
            }

            liveSource.Stop();
            liveSource.clip = runningLoop;
            liveSource.loop = true;
            liveSource.volume = CurrentGeneratorAudioVolume;
            liveSource.pitch = Mathf.Lerp(
                0.84f,
                1f,
                Mathf.Clamp01(powerPercent / 100f));
            liveSource.time = 0f;
            liveSource.Play();
            generatorAudioPhase = GeneratorAudioPhase.Running;
        }


        private void PlayGeneratorStartup(AudioSource liveSource)
        {
            liveSource.Stop();
            liveSource.loop = false;
            liveSource.pitch = 1f;
            liveSource.volume = CurrentGeneratorAudioVolume;
            if (startupClip == null)
            {
                PlayGeneratorRunningLoop(liveSource);
                return;
            }

            liveSource.clip = startupClip;
            liveSource.time = 0f;
            liveSource.Play();
            generatorAudioPhase = GeneratorAudioPhase.Startup;
        }


        private void UpdateGeneratorAudioTimeline()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (generatorAudioSource == null)
            {
                if (HasPower)
                {
                    SyncGeneratorAudio();
                }

                return;
            }

            switch (generatorAudioPhase)
            {
                case GeneratorAudioPhase.Startup:
                    if (!generatorAudioSource.isPlaying)
                    {
                        if (HasPower)
                        {
                            PlayGeneratorRunningLoop(generatorAudioSource);
                        }
                        else
                        {
                            PlayGeneratorShutdown(generatorAudioSource);
                        }
                    }
                    break;

                case GeneratorAudioPhase.Running:
                    if (!HasPower)
                    {
                        PlayGeneratorShutdown(generatorAudioSource);
                    }
                    else if (!generatorAudioSource.isPlaying)
                    {
                        PlayGeneratorRunningLoop(generatorAudioSource);
                    }
                    break;

                case GeneratorAudioPhase.Shutdown:
                    if (HasPower)
                    {
                        PlayGeneratorStartup(generatorAudioSource);
                    }
                    else if (!generatorAudioSource.isPlaying)
                    {
                        generatorAudioSource.clip = null;
                        generatorAudioPhase = GeneratorAudioPhase.Off;
                    }
                    break;
            }
        }


        private bool UpdatePowerTransition()
        {
            if (powerTransitionPhase == PowerTransitionPhase.None)
            {
                return false;
            }

            bool starting =
                powerTransitionPhase == PowerTransitionPhase.Starting;
            float duration = starting
                ? StartupPowerTransitionDuration
                : ShutdownPowerTransitionDuration;
            float now = Time.unscaledTime;
            float progress = Mathf.Clamp01(
                (now - powerTransitionStartTime) / duration);
            if (progress >= 1f)
            {
                powerTransitionPhase = PowerTransitionPhase.None;
                ResetFlicker();
                if (starting &&
                    CurrentState == PowerState.Critical &&
                    !GameSettings.ReduceFlashing)
                {
                    flickerOn = true;
                    flickerMultiplier = Mathf.Lerp(
                        0.55f,
                        0.9f,
                        Mathf.Clamp01(powerPercent / flickerThreshold));
                    nextFlickerTime = now +
                        RandomDuration(lightOnDuration) *
                        flickerIntervalMultiplier;
                }

                ApplyCurrentState(true);
                return true;
            }

            if (GameSettings.ReduceFlashing)
            {
                float eased = Mathf.SmoothStep(0f, 1f, progress);
                powerTransitionBrightness = starting
                    ? eased
                    : 1f - eased;
                ApplyEnvironmentFlicker(powerTransitionBrightness);
                ApplyLightMultiplier(powerTransitionBrightness);
                return true;
            }

            if (now < nextPowerTransitionFlickerTime)
            {
                return true;
            }

            float lightOnChance = starting
                ? Mathf.Lerp(0.18f, 0.92f, progress)
                : Mathf.Lerp(0.88f, 0.12f, progress);
            bool lightOn = UnityEngine.Random.value <= lightOnChance;
            if (lightOn)
            {
                float envelope = starting
                    ? Mathf.Lerp(0.22f, 1f, progress)
                    : Mathf.Lerp(1f, 0.16f, progress);
                powerTransitionBrightness =
                    envelope * UnityEngine.Random.Range(0.62f, 1f);
            }
            else
            {
                powerTransitionBrightness = 0f;
            }

            nextPowerTransitionFlickerTime = now +
                UnityEngine.Random.Range(
                    PowerTransitionFlickerInterval.x,
                    PowerTransitionFlickerInterval.y);
            ApplyEnvironmentFlicker(powerTransitionBrightness);
            ApplyLightMultiplier(powerTransitionBrightness);
            return true;
        }


        private void BeginPowerTransition(bool starting)
        {
            if (!Application.isPlaying)
            {
                powerTransitionPhase = PowerTransitionPhase.None;
                ResetFlicker();
                ApplyCurrentState(true);
                return;
            }

            float initialBrightness = 0f;
            if (!starting)
            {
                initialBrightness = appliedState == PowerState.Critical
                    ? Mathf.Clamp01(
                        flickerOn
                            ? Mathf.Max(0.25f, flickerMultiplier)
                            : 0.35f)
                    : 1f;
            }

            ResetFlicker();
            powerTransitionPhase = starting
                ? PowerTransitionPhase.Starting
                : PowerTransitionPhase.Stopping;
            powerTransitionStartTime = Time.unscaledTime;
            nextPowerTransitionFlickerTime =
                powerTransitionStartTime + 0.08f;
            powerTransitionBrightness = initialBrightness;
            ApplyEnvironmentFlicker(powerTransitionBrightness);
            ApplyLightMultiplier(powerTransitionBrightness);
            SyncGeneratorAudio();
        }
}
}
