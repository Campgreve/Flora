using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource chillSource;
    [SerializeField] private AudioSource combatSource;
    [SerializeField] private AudioSource bossSource;

    [Header("Music Clips")]
    [SerializeField] private AudioClip chillMusic;
    [SerializeField] private AudioClip combatMusic;
    [SerializeField] private AudioClip bossMusic;

    [Header("Settings")]
    [SerializeField] private float fadeTime = 1f;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private float deathStopDuration = 1.4f;
    [SerializeField] private float deathStopMinPitch = 0.15f;

    private AudioSource currentSource;
    private MusicState currentState = MusicState.None; // ← CAMBIATO!
    public MusicState CurrentState => currentState;
    public bool IsCombatLocked => combatLockCount > 0;
    private Coroutine deathStopCoroutine;
    private int combatLockCount;

    public enum MusicState
    {
        None,   // ← AGGIUNTO!
        Chill,
        Combat,
        Boss
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioSources();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (musicSlider != null)
        {
            musicSlider.onValueChanged.AddListener(OnVolumeChanged);
            OnVolumeChanged(musicSlider.value);
        }
        else
        {
            // Se non c'è slider, imposta volume di default
            SetDefaultVolume(0.5f);
        }

        // NON chiamare PlayChill qui - lascia che sia il PlayerMovement a farlo
        // PlayChill();
    }

    private void InitializeAudioSources()
    {
        // Crea gli AudioSource se non sono assegnati
        if (chillSource == null)
            chillSource = gameObject.AddComponent<AudioSource>();
        if (combatSource == null)
            combatSource = gameObject.AddComponent<AudioSource>();
        if (bossSource == null)
            bossSource = gameObject.AddComponent<AudioSource>();

        // Configura le sorgenti
        AudioSource[] sources = { chillSource, combatSource, bossSource };
        foreach (var source in sources)
        {
            source.loop = true;
            source.playOnAwake = false;
            source.volume = 0f;
        }

        // Assegna i clip
        chillSource.clip = chillMusic;
        combatSource.clip = combatMusic;
        bossSource.clip = bossMusic;
    }

    private void SetDefaultVolume(float volume)
    {
        chillSource.volume = volume;
        combatSource.volume = volume;
        bossSource.volume = volume;
    }

    public void PlayChill()
    {
        if (IsCombatLocked && currentState != MusicState.Boss)
        {
            TransitionTo(combatSource, MusicState.Combat);
            return;
        }

        TransitionTo(chillSource, MusicState.Chill);
    }

    public void PlayCombat()
    {
        TransitionTo(combatSource, MusicState.Combat);
    }

    public void PlayBoss()
    {
        TransitionTo(bossSource, MusicState.Boss);
    }

    public void PushCombatLock()
    {
        combatLockCount++;
        if (currentState != MusicState.Boss)
        {
            TransitionTo(combatSource, MusicState.Combat);
        }
    }

    public void PopCombatLock()
    {
        combatLockCount = Mathf.Max(0, combatLockCount - 1);
    }

    private void TransitionTo(AudioSource targetSource, MusicState newState)
    {
        if (deathStopCoroutine != null)
        {
            return;
        }

        // Controlla se il clip è assegnato
        if (targetSource.clip == null)
        {
            Debug.LogWarning($"⚠️ Nessun clip assegnato per {newState}!");
            return;
        }

        // Se siamo già in questo stato E la musica sta suonando, ignora
        if (currentState == newState && targetSource.isPlaying)
        {
            Debug.Log($"⏭️ Già in {newState} e la musica sta suonando");
            return;
        }

        Debug.Log($"🎵 Transizione: {currentState} → {newState}");
        currentState = newState;
        StartCoroutine(CrossfadeCoroutine(targetSource));
    }

    private IEnumerator CrossfadeCoroutine(AudioSource targetSource)
    {
        float maxVolume = musicSlider != null ? musicSlider.value : 0.5f;

        // Avvia la nuova traccia se non è in riproduzione
        if (!targetSource.isPlaying)
        {
            targetSource.volume = 0f;
            targetSource.Play();
            Debug.Log($"▶️ Avviata: {targetSource.clip.name}");
        }

        float elapsed = 0f;

        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeTime;

            // Fade out della traccia corrente
            if (currentSource != null && currentSource != targetSource)
            {
                currentSource.volume = Mathf.Lerp(maxVolume, 0f, t);
            }

            // Fade in della nuova traccia
            targetSource.volume = Mathf.Lerp(0f, maxVolume, t);

            yield return null;
        }

        // Ferma la vecchia traccia
        if (currentSource != null && currentSource != targetSource)
        {
            currentSource.Stop();
            currentSource.volume = 0f;
        }

        targetSource.volume = maxVolume;
        currentSource = targetSource;
        
        Debug.Log($"✅ Ora suona: {targetSource.clip.name} a volume {maxVolume}");
    }

    private void OnVolumeChanged(float value)
    {
        if (currentSource != null)
        {
            currentSource.volume = value;
        }

        // Aggiorna anche le altre sorgenti non attive
        if (chillSource != null)
            chillSource.volume = currentSource == chillSource ? value : 0f;
        if (combatSource != null)
            combatSource.volume = currentSource == combatSource ? value : 0f;
        if (bossSource != null)
            bossSource.volume = currentSource == bossSource ? value : 0f;
    }

    public void StopMusic()
    {
        if (deathStopCoroutine != null)
        {
            StopCoroutine(deathStopCoroutine);
            deathStopCoroutine = null;
        }

        StopAllCoroutines();
        if (chillSource != null) chillSource.Stop();
        if (combatSource != null) combatSource.Stop();
        if (bossSource != null) bossSource.Stop();
        ResetSourceState(chillSource);
        ResetSourceState(combatSource);
        ResetSourceState(bossSource);
        currentSource = null;
        currentState = MusicState.None;
        combatLockCount = 0;
    }

    public void StopMusicWithDjSlowdown()
    {
        if (deathStopCoroutine != null)
        {
            return;
        }

        StopAllCoroutines();
        deathStopCoroutine = StartCoroutine(StopMusicWithDjSlowdownCoroutine());
    }

    private IEnumerator StopMusicWithDjSlowdownCoroutine()
    {
        AudioSource[] sources = { chillSource, combatSource, bossSource };
        float[] startVolumes = new float[sources.Length];
        float[] startPitches = new float[sources.Length];

        bool anyPlaying = false;
        for (int i = 0; i < sources.Length; i++)
        {
            AudioSource source = sources[i];
            if (source == null)
            {
                continue;
            }

            startVolumes[i] = source.volume;
            startPitches[i] = source.pitch;

            if (source.isPlaying)
            {
                anyPlaying = true;
            }
        }

        if (anyPlaying)
        {
            float elapsed = 0f;
            float duration = Mathf.Max(0.05f, deathStopDuration);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);

                for (int i = 0; i < sources.Length; i++)
                {
                    AudioSource source = sources[i];
                    if (source == null || !source.isPlaying)
                    {
                        continue;
                    }

                    source.volume = Mathf.Lerp(startVolumes[i], 0f, eased);
                    source.pitch = Mathf.Lerp(startPitches[i], deathStopMinPitch, eased);
                }

                yield return null;
            }
        }

        for (int i = 0; i < sources.Length; i++)
        {
            if (sources[i] == null)
            {
                continue;
            }

            sources[i].Stop();
            ResetSourceState(sources[i]);
        }

        currentSource = null;
        currentState = MusicState.None;
        combatLockCount = 0;
        deathStopCoroutine = null;
    }

    private void ResetSourceState(AudioSource source)
    {
        if (source == null)
        {
            return;
        }

        source.volume = 0f;
        source.pitch = 1f;
    }
}
